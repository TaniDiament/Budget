import Foundation
import Observation

/// One budgeted expense row. Mirrors the Windows BudgetLineItem: a planned
/// amount, the running actual spend, and a non-persisted "add spending" scratch box.
@Observable
final class BudgetLineItem: Identifiable {
    let id = UUID()

    var name: String
    var category: String
    var plannedAmount: Decimal
    var actualAmount: Decimal

    /// Scratch input for the per-item "add spending" box; intentionally not persisted.
    var spendingText: String = ""

    init(name: String, category: String, plannedAmount: Decimal, actualAmount: Decimal = 0) {
        self.name = name
        self.category = category.isBlank ? "General" : category.trimmed
        self.plannedAmount = plannedAmount
        self.actualAmount = actualAmount
    }

    var displayCategory: String {
        category.isBlank ? "General" : category
    }
}

/// A savings goal with a target and running balance.
@Observable
final class SavingsGoal: Identifiable {
    let id = UUID()

    var name: String
    var targetAmount: Decimal
    var savedAmount: Decimal

    /// Scratch input for the per-goal contribution box; intentionally not persisted.
    var contributionText: String = ""

    init(name: String, targetAmount: Decimal, savedAmount: Decimal) {
        self.name = name
        self.targetAmount = targetAmount
        self.savedAmount = savedAmount
    }

    var progressFraction: Double {
        guard targetAmount > 0 else { return 0 }
        let percent = (savedAmount / targetAmount).doubleValue
        return min(1, max(0, percent))
    }

    var progressDisplay: String {
        (progressFraction).formatted(.percent.precision(.fractionLength(0)))
    }

    var remainingDisplay: String {
        MoneyText.format(max(0, targetAmount - savedAmount))
    }
}

/// One month of take-home income history.
@Observable
final class IncomeEntry: Identifiable {
    let id = UUID()

    var month: Date {
        didSet {
            let normalized = MonthKey.firstOfMonth(month)
            if normalized != month {
                month = normalized
            }
        }
    }
    var amount: Decimal

    init(month: Date, amount: Decimal) {
        self.month = MonthKey.firstOfMonth(month)
        self.amount = amount
    }

    var monthLabel: String {
        month.formatted(.dateTime.month(.abbreviated).year())
    }

    var sortKey: String {
        MonthKey.string(from: month)
    }
}

/// One month's plan: pay plus its line items. Months are created on demand
/// as the user navigates, exactly like the Windows app.
@Observable
final class BudgetMonth: Identifiable {
    let month: Date
    var takeHomePayText: String = "0"
    var lineItems: [BudgetLineItem] = []

    init(month: Date) {
        self.month = MonthKey.firstOfMonth(month)
    }

    var id: String { monthKey }

    var monthKey: String {
        MonthKey.string(from: month)
    }

    var label: String {
        month.formatted(.dateTime.month(.wide).year())
    }
}

enum Severity {
    case normal
    case high
    case over
}

/// Derived per-category totals for the "Spending by category" meters.
struct CategorySummary: Identifiable {
    let category: String
    let plannedAmount: Decimal
    let actualAmount: Decimal
    /// How much of the planned amount has been spent, 0...1, clamped for the meter.
    let consumptionFraction: Double
    /// Normal, high (≥85% spent), or over (spent past the plan).
    let severity: Severity
    let detailText: String

    var id: String { category }
}

/// One plotted point for the income chart.
struct IncomeTrendPoint: Identifiable {
    let month: Date
    let monthLabel: String
    let amount: Decimal

    var id: Date { month }

    var amountValue: Double { amount.doubleValue }
}

/// Appearance override, persisted in UserDefaults (the native equivalent of
/// the Windows app's theme-settings.json).
enum ThemeMode: Int, CaseIterable, Identifiable {
    case system = 0
    case light = 1
    case dark = 2

    var id: Int { rawValue }

    var label: String {
        switch self {
        case .system: "System"
        case .light: "Light"
        case .dark: "Dark"
        }
    }
}

// nonisolated: these helpers are called from nonisolated code (the file
// exchange service), so they must not pick up the module's MainActor default.
nonisolated extension Decimal {
    var doubleValue: Double {
        (self as NSDecimalNumber).doubleValue
    }
}

nonisolated extension String {
    var trimmed: String {
        trimmingCharacters(in: .whitespacesAndNewlines)
    }

    var isBlank: Bool {
        trimmed.isEmpty
    }
}
