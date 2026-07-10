import Foundation
import Observation

/// The app's single source of truth — a Swift port of the Windows MainViewModel.
/// Views bind straight to it; any change to persisted data triggers a debounced
/// save through Observation tracking (the WPF build saved on PropertyChanged).
@Observable
final class BudgetStore {
    static let defaultCategories = [
        "General",
        "Housing",
        "Utilities",
        "Transportation",
        "Groceries",
        "Dining",
        "Savings",
        "Entertainment",
        "Healthcare",
        "Other"
    ]

    private let stateStore = BudgetStateStore()
    private var months: [String: BudgetMonth] = [:]
    private(set) var currentMonth: BudgetMonth

    var savingsGoals: [SavingsGoal] = []
    var incomeEntries: [IncomeEntry] = []
    var categoryOptions: [String] = BudgetStore.defaultCategories

    // New line item form.
    var newItemName = ""
    var newItemAmountText = ""
    var newItemCategory = "General"

    // New goal form.
    var newGoalName = ""
    var newGoalTargetAmountText = ""
    var newGoalSavedAmountText = ""

    // New income form.
    var newIncomeMonth = MonthKey.firstOfMonth(.now) {
        didSet {
            let normalized = MonthKey.firstOfMonth(newIncomeMonth)
            if normalized != newIncomeMonth {
                newIncomeMonth = normalized
            }
        }
    }
    var newIncomeAmountText = ""

    private(set) var statusMessage = "Add a line item to get started."

    private enum LastRemoval {
        case lineItem(BudgetLineItem, index: Int, monthKey: String)
        case goal(SavingsGoal, index: Int)
        case income(IncomeEntry, index: Int)
    }

    private var lastRemoval: LastRemoval?
    private var isRestoringState = false

    @ObservationIgnored private var pendingSave: Task<Void, Never>?

    init() {
        currentMonth = BudgetMonth(month: .now)
        months[currentMonth.monthKey] = currentMonth

        if let saved = stateStore.load() {
            apply(saved, statusMessage: nil)
        }

        armAutosave()
    }

    // MARK: - Derived values

    var lineItems: [BudgetLineItem] { currentMonth.lineItems }

    var currentMonthLabel: String { currentMonth.label }

    var monthlyTakeHomePayText: String {
        get { currentMonth.takeHomePayText }
        set { currentMonth.takeHomePayText = newValue }
    }

    var monthlyTakeHomePayValue: Decimal {
        MoneyText.parse(currentMonth.takeHomePayText) ?? 0
    }

    var totalPlannedValue: Decimal {
        lineItems.reduce(0) { $0 + $1.plannedAmount }
    }

    var totalActualValue: Decimal {
        lineItems.reduce(0) { $0 + $1.actualAmount }
    }

    var leftoverValue: Decimal {
        monthlyTakeHomePayValue - totalPlannedValue
    }

    var isOverBudget: Bool {
        monthlyTakeHomePayValue > 0 && leftoverValue < 0
    }

    var budgetUsageFraction: Double {
        guard monthlyTakeHomePayValue > 0 else { return 0 }
        return min(1, (totalPlannedValue / monthlyTakeHomePayValue).doubleValue)
    }

    var budgetUsageDisplay: String {
        guard monthlyTakeHomePayValue > 0 else { return "0% used" }
        let usage = (totalPlannedValue / monthlyTakeHomePayValue).doubleValue
        return usage.formatted(.percent.precision(.fractionLength(0))) + " used"
    }

    var usageSeverity: Severity {
        guard monthlyTakeHomePayValue > 0 else { return .normal }
        let usage = (totalPlannedValue / monthlyTakeHomePayValue).doubleValue * 100
        if usage > 100 { return .over }
        return usage >= 85 ? .high : .normal
    }

    var budgetSummaryMessage: String {
        if monthlyTakeHomePayValue <= 0 {
            return "Enter your take-home pay for \(currentMonthLabel) to see your available budget."
        }
        if leftoverValue >= 0 {
            return "You have \(MoneyText.format(leftoverValue)) left after planned expenses."
        }
        return "You are over budget by \(MoneyText.format(abs(leftoverValue)))."
    }

    var spendingSummaryMessage: String {
        if lineItems.isEmpty {
            return "Your categories are summarized as you add line items."
        }
        if totalActualValue <= 0 {
            return "Nothing spent yet against \(MoneyText.format(totalPlannedValue)) planned. Edit an item's Spent box to track it."
        }
        return "Spent \(MoneyText.format(totalActualValue)) of \(MoneyText.format(totalPlannedValue)) planned so far."
    }

    var categorySummaries: [CategorySummary] {
        let income = monthlyTakeHomePayValue

        var order: [String] = []
        var planned: [String: Decimal] = [:]
        var actual: [String: Decimal] = [:]
        var displayName: [String: String] = [:]

        for item in lineItems {
            let name = item.category.isBlank ? "General" : item.category.trimmed
            let key = name.lowercased()
            if displayName[key] == nil {
                displayName[key] = name
                order.append(key)
            }
            planned[key, default: 0] += item.plannedAmount
            actual[key, default: 0] += item.actualAmount
        }

        return order
            .map { key -> (String, Decimal, Decimal) in
                (displayName[key] ?? key, planned[key] ?? 0, actual[key] ?? 0)
            }
            .sorted { $0.1 > $1.1 }
            .map { name, plannedTotal, actualTotal in
                let consumption: Double
                if plannedTotal > 0 {
                    consumption = ((actualTotal / plannedTotal) * 100).doubleValue
                } else {
                    consumption = actualTotal > 0 ? 100 : 0
                }

                let severity: Severity
                if (plannedTotal <= 0 && actualTotal > 0) || actualTotal > plannedTotal {
                    severity = .over
                } else {
                    severity = consumption >= 85 ? .high : .normal
                }

                var detail = "\(MoneyText.format(actualTotal)) of \(MoneyText.format(plannedTotal))"
                if income > 0 && plannedTotal > 0 {
                    let share = ((plannedTotal / income).doubleValue).formatted(.percent.precision(.fractionLength(0)))
                    detail += " · \(share) of pay"
                }

                return CategorySummary(
                    category: name,
                    plannedAmount: plannedTotal,
                    actualAmount: actualTotal,
                    consumptionFraction: min(1, consumption / 100),
                    severity: severity,
                    detailText: detail)
            }
    }

    // MARK: - Goal + income aggregates

    var totalGoalTargetValue: Decimal { savingsGoals.reduce(0) { $0 + $1.targetAmount } }
    var totalGoalSavedValue: Decimal { savingsGoals.reduce(0) { $0 + $1.savedAmount } }

    var savingsGoalProgressDisplay: String {
        guard totalGoalTargetValue > 0 else { return "0%" }
        return ((totalGoalSavedValue / totalGoalTargetValue).doubleValue)
            .formatted(.percent.precision(.fractionLength(0)))
    }

    var incomeHistoryTotalValue: Decimal { incomeEntries.reduce(0) { $0 + $1.amount } }

    var incomeHistoryAverageValue: Decimal {
        incomeEntries.isEmpty ? 0 : incomeHistoryTotalValue / Decimal(incomeEntries.count)
    }

    var sortedIncomeEntries: [IncomeEntry] {
        incomeEntries.sorted { $0.sortKey < $1.sortKey }
    }

    var incomeTrendPoints: [IncomeTrendPoint] {
        sortedIncomeEntries.map { IncomeTrendPoint(month: $0.month, monthLabel: $0.monthLabel, amount: $0.amount) }
    }

    var latestIncomeDisplay: String {
        MoneyText.format(sortedIncomeEntries.last?.amount ?? 0)
    }

    private var latestIncomePair: (latest: IncomeEntry, previous: IncomeEntry)? {
        let ordered = sortedIncomeEntries
        guard ordered.count >= 2 else { return nil }
        return (ordered[ordered.count - 1], ordered[ordered.count - 2])
    }

    enum IncomeChangeDirection {
        case none, up, down, flat
    }

    var incomeChangeDirection: IncomeChangeDirection {
        guard let pair = latestIncomePair else { return .none }
        let difference = pair.latest.amount - pair.previous.amount
        if difference > 0 { return .up }
        if difference < 0 { return .down }
        return .flat
    }

    var incomeChangeDisplay: String {
        guard let pair = latestIncomePair else { return "" }
        let difference = pair.latest.amount - pair.previous.amount
        if difference == 0 {
            return "No change vs \(pair.previous.monthLabel)"
        }

        var text = "\(difference > 0 ? "▲" : "▼") \(MoneyText.format(abs(difference)))"
        if pair.previous.amount > 0 {
            let percent = (abs(difference / pair.previous.amount).doubleValue)
                .formatted(.percent.precision(.fractionLength(0...1)))
            text += " (\(percent))"
        }
        return "\(text) vs \(pair.previous.monthLabel)"
    }

    // MARK: - Month navigation

    func goToPreviousMonth() { shiftMonth(by: -1) }
    func goToNextMonth() { shiftMonth(by: 1) }

    private func shiftMonth(by offset: Int) {
        let calendar = Calendar(identifier: .gregorian)
        guard let target = calendar.date(byAdding: .month, value: offset, to: currentMonth.month) else { return }
        currentMonth = month(for: target)
    }

    private func month(for date: Date) -> BudgetMonth {
        let key = MonthKey.string(from: MonthKey.firstOfMonth(date))
        if let existing = months[key] {
            return existing
        }
        let created = BudgetMonth(month: date)
        months[key] = created
        return created
    }

    private var nearestEarlierMonthWithItems: BudgetMonth? {
        months.values
            .filter { $0.month < currentMonth.month && !$0.lineItems.isEmpty }
            .max { $0.month < $1.month }
    }

    var canCopyPreviousMonth: Bool {
        lineItems.isEmpty && nearestEarlierMonthWithItems != nil
    }

    func copyPreviousMonth() {
        guard let source = nearestEarlierMonthWithItems, lineItems.isEmpty else { return }

        currentMonth.takeHomePayText = source.takeHomePayText
        for item in source.lineItems {
            currentMonth.lineItems.append(
                BudgetLineItem(name: item.name, category: item.category, plannedAmount: item.plannedAmount))
        }

        statusMessage = "Copied the \(source.label) plan into \(currentMonthLabel). Spent amounts start fresh."
    }

    // MARK: - Line items

    var canAddLineItem: Bool {
        guard !newItemName.isBlank, let amount = MoneyText.parse(newItemAmountText) else { return false }
        return amount > 0
    }

    func addLineItem() {
        guard canAddLineItem else {
            statusMessage = "Enter a valid name and amount before adding a line item."
            return
        }

        let category = normalizeCategory(newItemCategory)
        let item = BudgetLineItem(
            name: newItemName.trimmed,
            category: category,
            plannedAmount: MoneyText.parse(newItemAmountText) ?? 0)
        currentMonth.lineItems.append(item)
        learnCategory(category)
        newItemName = ""
        newItemAmountText = ""
        newItemCategory = category
        statusMessage = "Added \(item.name) to \(currentMonthLabel)."
    }

    func removeLineItem(_ item: BudgetLineItem) {
        guard let index = currentMonth.lineItems.firstIndex(where: { $0.id == item.id }) else { return }
        currentMonth.lineItems.remove(at: index)
        lastRemoval = .lineItem(item, index: index, monthKey: currentMonth.monthKey)
        statusMessage = "Removed \(item.name)."
    }

    func recordSpending(for item: BudgetLineItem) {
        guard let amount = MoneyText.parse(item.spendingText), amount > 0 else {
            statusMessage = "Enter a spent amount above zero first."
            return
        }

        item.actualAmount += amount
        item.spendingText = ""
        statusMessage = "Added \(MoneyText.format(amount)) spent on \(item.name) — \(MoneyText.format(item.actualAmount)) of \(MoneyText.format(item.plannedAmount)) used."
    }

    /// Live keystroke path for inline edits: keep exactly what was typed so the
    /// caret isn't disturbed (trimming mid-edit would eat spaces as they're
    /// typed). Blank names are ignored, which makes the field revert on blur —
    /// the same net behavior as the WPF inline editors.
    func rename(_ item: BudgetLineItem, to name: String) {
        guard !name.isBlank else { return }
        item.name = name
    }

    func setCategory(_ category: String, for item: BudgetLineItem) {
        item.category = category
    }

    /// Commit path (Enter in the category field): normalize and learn the
    /// category for the suggestions menu, like WPF's LostFocus binding did.
    func commitCategory(for item: BudgetLineItem) {
        let normalized = normalizeCategory(item.category)
        if item.category != normalized {
            item.category = normalized
        }
        learnCategory(normalized)
    }

    // MARK: - Savings goals

    var canAddGoal: Bool {
        guard !newGoalName.isBlank,
              let target = MoneyText.parse(newGoalTargetAmountText),
              target > 0,
              let saved = MoneyText.parse(newGoalSavedAmountText),
              saved >= 0
        else { return false }
        return true
    }

    func addGoal() {
        guard canAddGoal else {
            statusMessage = "Enter a valid savings goal name and amounts before adding."
            return
        }

        let goal = SavingsGoal(
            name: newGoalName.trimmed,
            targetAmount: MoneyText.parse(newGoalTargetAmountText) ?? 0,
            savedAmount: MoneyText.parse(newGoalSavedAmountText) ?? 0)
        savingsGoals.append(goal)
        newGoalName = ""
        newGoalTargetAmountText = ""
        newGoalSavedAmountText = ""
        statusMessage = "Added savings goal \(goal.name)."
    }

    func removeGoal(_ goal: SavingsGoal) {
        guard let index = savingsGoals.firstIndex(where: { $0.id == goal.id }) else { return }
        savingsGoals.remove(at: index)
        lastRemoval = .goal(goal, index: index)
        statusMessage = "Removed savings goal \(goal.name)."
    }

    func contribute(to goal: SavingsGoal) {
        guard let amount = MoneyText.parse(goal.contributionText), amount > 0 else {
            statusMessage = "Enter a contribution amount above zero first."
            return
        }

        goal.savedAmount += amount
        goal.contributionText = ""
        statusMessage = "Added \(MoneyText.format(amount)) to \(goal.name)."
    }

    func rename(_ goal: SavingsGoal, to name: String) {
        guard !name.isBlank else { return }
        goal.name = name
    }

    // MARK: - Income entries

    var canAddIncomeEntry: Bool {
        guard let amount = MoneyText.parse(newIncomeAmountText) else { return false }
        return amount > 0
    }

    func addIncomeEntry() {
        guard canAddIncomeEntry else {
            statusMessage = "Enter a valid month and income amount before adding."
            return
        }

        let amount = MoneyText.parse(newIncomeAmountText) ?? 0
        let key = MonthKey.string(from: newIncomeMonth)

        if let existing = incomeEntries.first(where: { $0.sortKey == key }) {
            existing.amount = amount
            statusMessage = "Updated income for \(existing.monthLabel)."
        } else {
            let entry = IncomeEntry(month: newIncomeMonth, amount: amount)
            incomeEntries.append(entry)
            statusMessage = "Added income for \(entry.monthLabel)."
        }

        newIncomeAmountText = ""
    }

    func removeIncomeEntry(_ entry: IncomeEntry) {
        guard let index = incomeEntries.firstIndex(where: { $0.id == entry.id }) else { return }
        incomeEntries.remove(at: index)
        lastRemoval = .income(entry, index: index)
        statusMessage = "Removed income for \(entry.monthLabel)."
    }

    // MARK: - Undo

    var isUndoAvailable: Bool { lastRemoval != nil }

    func undoRemove() {
        guard let removal = lastRemoval else { return }

        switch removal {
        case let .lineItem(item, index, monthKey):
            if let monthDate = MonthKey.date(from: monthKey) {
                currentMonth = month(for: monthDate)
            }
            currentMonth.lineItems.insert(item, at: min(max(index, 0), currentMonth.lineItems.count))
            statusMessage = "Restored \(item.name)."

        case let .goal(goal, index):
            savingsGoals.insert(goal, at: min(max(index, 0), savingsGoals.count))
            statusMessage = "Restored savings goal \(goal.name)."

        case let .income(entry, index):
            incomeEntries.insert(entry, at: min(max(index, 0), incomeEntries.count))
            statusMessage = "Restored income for \(entry.monthLabel)."
        }

        lastRemoval = nil
    }

    // MARK: - Import / export / persistence

    func setStatusMessage(_ message: String) {
        statusMessage = message
    }

    func exportText(as format: BudgetFileExchange.Format) throws -> String {
        try BudgetFileExchange.export(snapshotFile(), as: format)
    }

    func importState(from text: String, format: BudgetFileExchange.Format, fileName: String) throws {
        let state = try BudgetFileExchange.importState(from: text, format: format)
        apply(state, statusMessage: "Imported budget from \(fileName).")
        saveNow()
    }

    func snapshotFile() -> BudgetStateFile {
        var file = BudgetStateFile()
        file.selectedMonthKey = currentMonth.monthKey
        file.monthlyTakeHomePayText = currentMonth.takeHomePayText

        let keptMonths = months.values
            .filter { month in
                month === currentMonth
                    || !month.lineItems.isEmpty
                    || (MoneyText.parse(month.takeHomePayText) ?? 0) > 0
            }
            .sorted { $0.month < $1.month }

        file.months = keptMonths.map { month in
            BudgetMonthFile(
                monthKey: month.monthKey,
                takeHomePayText: month.takeHomePayText,
                lineItems: month.lineItems.map { fileItem($0) })
        }

        file.lineItems = currentMonth.lineItems.map { fileItem($0) }

        file.savingsGoals = savingsGoals.map {
            SavingsGoalFile(name: $0.name.trimmed, targetAmount: $0.targetAmount, savedAmount: $0.savedAmount)
        }

        file.incomeEntries = incomeEntries.map {
            IncomeEntryFile(monthKey: $0.sortKey, amount: $0.amount)
        }

        return file
    }

    /// Persisted rows carry the normalized form (trimmed name, blank category
    /// promoted to "General") even while an inline edit is mid-flight.
    private func fileItem(_ item: BudgetLineItem) -> BudgetLineItemFile {
        BudgetLineItemFile(
            name: item.name.trimmed,
            category: normalizeCategory(item.category),
            amount: item.plannedAmount,
            actualAmount: item.actualAmount)
    }

    private func apply(_ state: BudgetStateFile, statusMessage: String?) {
        isRestoringState = true
        defer { isRestoringState = false }

        months.removeAll()

        if !state.months.isEmpty {
            for monthState in state.months {
                guard let monthDate = MonthKey.date(from: monthState.monthKey) else { continue }
                let budgetMonth = month(for: monthDate)
                budgetMonth.takeHomePayText = monthState.takeHomePayText.isBlank ? "0" : monthState.takeHomePayText
                appendItems(monthState.lineItems, to: budgetMonth)
            }
        } else {
            let monthDate = MonthKey.date(from: state.selectedMonthKey) ?? .now
            let budgetMonth = month(for: monthDate)
            budgetMonth.takeHomePayText = state.monthlyTakeHomePayText.isBlank ? "0" : state.monthlyTakeHomePayText
            appendItems(state.lineItems, to: budgetMonth)
        }

        if let selected = MonthKey.date(from: state.selectedMonthKey),
           let existing = months[MonthKey.string(from: selected)] {
            currentMonth = existing
        } else if let latest = months.values.max(by: { $0.month < $1.month }) {
            currentMonth = latest
        } else {
            currentMonth = month(for: .now)
        }

        savingsGoals = state.savingsGoals.compactMap { goal in
            guard !goal.name.isBlank else { return nil }
            return SavingsGoal(name: goal.name.trimmed, targetAmount: goal.targetAmount, savedAmount: goal.savedAmount)
        }

        incomeEntries = state.incomeEntries.compactMap { entry in
            guard let monthDate = MonthKey.date(from: entry.monthKey) else { return nil }
            return IncomeEntry(month: monthDate, amount: entry.amount)
        }

        lastRemoval = nil

        let itemCount = months.values.reduce(0) { $0 + $1.lineItems.count }
        self.statusMessage = statusMessage
            ?? "Restored \(itemCount) line item\(itemCount == 1 ? "" : "s"), \(savingsGoals.count) goal\(savingsGoals.count == 1 ? "" : "s"), and \(incomeEntries.count) income record\(incomeEntries.count == 1 ? "" : "s") from your last session."
    }

    private func appendItems(_ items: [BudgetLineItemFile], to month: BudgetMonth) {
        for item in items {
            guard !item.name.isBlank else { continue }
            let category = normalizeCategory(item.category)
            learnCategory(category)
            month.lineItems.append(
                BudgetLineItem(name: item.name.trimmed, category: category, plannedAmount: item.amount, actualAmount: item.actualAmount))
        }
    }

    // MARK: - Categories

    func learnCategory(_ category: String) {
        guard !categoryOptions.contains(where: { $0.compare(category, options: .caseInsensitive) == .orderedSame }) else {
            return
        }
        categoryOptions.append(category)
    }

    private func normalizeCategory(_ category: String) -> String {
        category.isBlank ? "General" : category.trimmed
    }

    // MARK: - Autosave

    /// Re-arming observation of everything `snapshotFile()` touches gives the
    /// same net behavior as WPF's save-on-PropertyChanged, without wiring
    /// event handlers to every model object.
    private func armAutosave() {
        withObservationTracking {
            _ = snapshotFile()
        } onChange: { [weak self] in
            Task { @MainActor [weak self] in
                guard let self else { return }
                self.scheduleSave()
                self.armAutosave()
            }
        }
    }

    private func scheduleSave() {
        guard !isRestoringState else { return }
        pendingSave?.cancel()
        pendingSave = Task { @MainActor [weak self] in
            try? await Task.sleep(for: .milliseconds(250))
            guard !Task.isCancelled else { return }
            self?.saveNow()
        }
    }

    @discardableResult
    func saveNow() -> Bool {
        pendingSave?.cancel()
        pendingSave = nil
        return stateStore.save(snapshotFile())
    }
}
