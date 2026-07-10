import Foundation

/// Wire format of budget-state.json. Field names are PascalCase to stay
/// byte-compatible with the Windows (System.Text.Json) and Avalonia builds,
/// so a budget exported on one platform imports cleanly on the other.
nonisolated struct BudgetStateFile: Codable {
    /// Legacy mirror of the selected month's pay, kept so older builds and files stay compatible.
    var monthlyTakeHomePayText: String = "0"
    /// Legacy mirror of the selected month's line items, kept for the same reason.
    var lineItems: [BudgetLineItemFile] = []
    var selectedMonthKey: String?
    var months: [BudgetMonthFile] = []
    var savingsGoals: [SavingsGoalFile] = []
    var incomeEntries: [IncomeEntryFile] = []

    enum CodingKeys: String, CodingKey {
        case monthlyTakeHomePayText = "MonthlyTakeHomePayText"
        case lineItems = "LineItems"
        case selectedMonthKey = "SelectedMonthKey"
        case months = "Months"
        case savingsGoals = "SavingsGoals"
        case incomeEntries = "IncomeEntries"
    }

    init() {}

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        monthlyTakeHomePayText = (try? container.decodeIfPresent(String.self, forKey: .monthlyTakeHomePayText)) ?? "0"
        if monthlyTakeHomePayText.trimmingCharacters(in: .whitespaces).isEmpty {
            monthlyTakeHomePayText = "0"
        }
        lineItems = (try? container.decodeIfPresent([BudgetLineItemFile].self, forKey: .lineItems)) ?? []
        selectedMonthKey = try? container.decodeIfPresent(String.self, forKey: .selectedMonthKey)
        months = (try? container.decodeIfPresent([BudgetMonthFile].self, forKey: .months)) ?? []
        savingsGoals = (try? container.decodeIfPresent([SavingsGoalFile].self, forKey: .savingsGoals)) ?? []
        incomeEntries = (try? container.decodeIfPresent([IncomeEntryFile].self, forKey: .incomeEntries)) ?? []
    }
}

nonisolated struct BudgetMonthFile: Codable {
    var monthKey: String = ""
    var takeHomePayText: String = "0"
    var lineItems: [BudgetLineItemFile] = []

    enum CodingKeys: String, CodingKey {
        case monthKey = "MonthKey"
        case takeHomePayText = "TakeHomePayText"
        case lineItems = "LineItems"
    }

    init(monthKey: String = "", takeHomePayText: String = "0", lineItems: [BudgetLineItemFile] = []) {
        self.monthKey = monthKey
        self.takeHomePayText = takeHomePayText
        self.lineItems = lineItems
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        monthKey = (try? container.decodeIfPresent(String.self, forKey: .monthKey)) ?? ""
        takeHomePayText = (try? container.decodeIfPresent(String.self, forKey: .takeHomePayText)) ?? "0"
        if takeHomePayText.trimmingCharacters(in: .whitespaces).isEmpty {
            takeHomePayText = "0"
        }
        lineItems = (try? container.decodeIfPresent([BudgetLineItemFile].self, forKey: .lineItems)) ?? []
    }
}

nonisolated struct BudgetLineItemFile: Codable {
    var name: String = ""
    var category: String = ""
    var amount: Decimal = 0
    var actualAmount: Decimal = 0

    enum CodingKeys: String, CodingKey {
        case name = "Name"
        case category = "Category"
        case amount = "Amount"
        case actualAmount = "ActualAmount"
    }

    init(name: String = "", category: String = "", amount: Decimal = 0, actualAmount: Decimal = 0) {
        self.name = name
        self.category = category
        self.amount = amount
        self.actualAmount = actualAmount
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        name = (try? container.decodeIfPresent(String.self, forKey: .name)) ?? ""
        category = (try? container.decodeIfPresent(String.self, forKey: .category)) ?? ""
        amount = (try? container.decodeIfPresent(Decimal.self, forKey: .amount)) ?? 0
        actualAmount = (try? container.decodeIfPresent(Decimal.self, forKey: .actualAmount)) ?? 0
    }
}

nonisolated struct SavingsGoalFile: Codable {
    var name: String = ""
    var targetAmount: Decimal = 0
    var savedAmount: Decimal = 0

    enum CodingKeys: String, CodingKey {
        case name = "Name"
        case targetAmount = "TargetAmount"
        case savedAmount = "SavedAmount"
    }

    init(name: String = "", targetAmount: Decimal = 0, savedAmount: Decimal = 0) {
        self.name = name
        self.targetAmount = targetAmount
        self.savedAmount = savedAmount
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        name = (try? container.decodeIfPresent(String.self, forKey: .name)) ?? ""
        targetAmount = (try? container.decodeIfPresent(Decimal.self, forKey: .targetAmount)) ?? 0
        savedAmount = (try? container.decodeIfPresent(Decimal.self, forKey: .savedAmount)) ?? 0
    }
}

nonisolated struct IncomeEntryFile: Codable {
    var monthKey: String = ""
    var amount: Decimal = 0

    enum CodingKeys: String, CodingKey {
        case monthKey = "MonthKey"
        case amount = "Amount"
    }

    init(monthKey: String = "", amount: Decimal = 0) {
        self.monthKey = monthKey
        self.amount = amount
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        monthKey = (try? container.decodeIfPresent(String.self, forKey: .monthKey)) ?? ""
        amount = (try? container.decodeIfPresent(Decimal.self, forKey: .amount)) ?? 0
    }
}
