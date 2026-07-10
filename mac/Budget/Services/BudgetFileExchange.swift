import Foundation

/// Import/export in the same JSON and CSV shapes the Windows app reads and
/// writes, so budgets round-trip across platforms.
nonisolated enum BudgetFileExchange {
    enum Format {
        case json
        case csv

        static func forFileExtension(_ ext: String) -> Format {
            ext.lowercased() == "csv" ? .csv : .json
        }
    }

    static func export(_ state: BudgetStateFile, as format: Format) throws -> String {
        switch format {
        case .csv:
            return serializeCSV(state)
        case .json:
            let data = try BudgetStateStore.encode(state)
            return String(decoding: data, as: UTF8.self)
        }
    }

    static func importState(from text: String, format: Format) throws -> BudgetStateFile {
        switch format {
        case .csv:
            return deserializeCSV(text)
        case .json:
            return try BudgetStateStore.decode(Data(text.utf8))
        }
    }

    // MARK: - CSV

    private static let header = "Type,Name,Category,MonthKey,TargetAmount,SavedAmount,Amount,ActualAmount"

    private static func serializeCSV(_ state: BudgetStateFile) -> String {
        var lines: [String] = [header]

        if !state.months.isEmpty {
            for month in state.months {
                lines.append(row("Income", "Monthly Take-Home Pay", "", month.monthKey, "", "", month.takeHomePayText, ""))
                for item in month.lineItems {
                    lines.append(row("Expense", item.name, item.category, month.monthKey, "", "",
                                     MoneyText.invariantString(item.amount), MoneyText.invariantString(item.actualAmount)))
                }
            }
        } else {
            lines.append(row("Income", "Monthly Take-Home Pay", "", "", "", "", state.monthlyTakeHomePayText, ""))
            for item in state.lineItems {
                lines.append(row("Expense", item.name, item.category, "", "", "",
                                 MoneyText.invariantString(item.amount), MoneyText.invariantString(item.actualAmount)))
            }
        }

        for goal in state.savingsGoals {
            lines.append(row("Goal", goal.name, "", "", MoneyText.invariantString(goal.targetAmount),
                             MoneyText.invariantString(goal.savedAmount), "", ""))
        }

        for entry in state.incomeEntries {
            lines.append(row("IncomeEntry", "", "", entry.monthKey, "", "", MoneyText.invariantString(entry.amount), ""))
        }

        return lines.joined(separator: "\r\n") + "\r\n"
    }

    private static func deserializeCSV(_ csv: String) -> BudgetStateFile {
        var state = BudgetStateFile()
        var monthsByKey: [String: Int] = [:]

        func month(for key: String) -> Int {
            if let index = monthsByKey[key.lowercased()] {
                return index
            }
            state.months.append(BudgetMonthFile(monthKey: key))
            let index = state.months.count - 1
            monthsByKey[key.lowercased()] = index
            return index
        }

        var headerConsumed = false
        for fields in parseCSV(csv) {
            if fields.isEmpty || fields.allSatisfy({ $0.isBlank }) {
                continue
            }

            if !headerConsumed {
                headerConsumed = true
                continue
            }

            let type = field(fields, 0)
            let name = field(fields, 1)
            let category = field(fields, 2)
            let monthKey = field(fields, 3)
            let targetText = field(fields, 4)
            let savedText = field(fields, 5)
            let amountText = field(fields, 6)
            let actualText = field(fields, 7)

            switch type.lowercased() {
            case "income":
                let payText = amountText.isBlank ? "0" : amountText
                if monthKey.isBlank {
                    state.monthlyTakeHomePayText = payText
                } else {
                    let index = month(for: monthKey)
                    state.months[index].takeHomePayText = payText
                }

            case "expense":
                guard !name.isBlank else { continue }
                let item = BudgetLineItemFile(
                    name: name,
                    category: category,
                    amount: MoneyText.parseInvariant(amountText) ?? 0,
                    actualAmount: MoneyText.parseInvariant(actualText) ?? 0)
                if monthKey.isBlank {
                    state.lineItems.append(item)
                } else {
                    let index = month(for: monthKey)
                    state.months[index].lineItems.append(item)
                }

            case "goal":
                guard !name.isBlank else { continue }
                state.savingsGoals.append(SavingsGoalFile(
                    name: name,
                    targetAmount: MoneyText.parseInvariant(targetText) ?? 0,
                    savedAmount: MoneyText.parseInvariant(savedText) ?? 0))

            case "incomeentry":
                guard !monthKey.isBlank else { continue }
                state.incomeEntries.append(IncomeEntryFile(
                    monthKey: monthKey,
                    amount: MoneyText.parseInvariant(amountText) ?? 0))

            default:
                continue
            }
        }

        return state
    }

    private static func field(_ fields: [String], _ index: Int) -> String {
        index < fields.count ? fields[index].trimmed : ""
    }

    private static func row(_ fields: String...) -> String {
        fields.map(escape).joined(separator: ",")
    }

    private static func escape(_ value: String) -> String {
        if value.contains("\"") || value.contains(",") || value.contains("\n") || value.contains("\r") {
            return "\"" + value.replacingOccurrences(of: "\"", with: "\"\"") + "\""
        }
        return value
    }

    /// Minimal RFC 4180 reader: quoted fields, doubled quotes, CRLF or LF rows.
    private static func parseCSV(_ text: String) -> [[String]] {
        var rows: [[String]] = []
        var fields: [String] = []
        var current = ""
        var inQuotes = false
        var index = text.startIndex

        func endField() {
            fields.append(current)
            current = ""
        }

        func endRow() {
            endField()
            rows.append(fields)
            fields = []
        }

        while index < text.endIndex {
            let character = text[index]

            if inQuotes {
                if character == "\"" {
                    let next = text.index(after: index)
                    if next < text.endIndex, text[next] == "\"" {
                        current.append("\"")
                        index = next
                    } else {
                        inQuotes = false
                    }
                } else {
                    current.append(character)
                }
            } else {
                switch character {
                case "\"":
                    inQuotes = true
                case ",":
                    endField()
                case "\r":
                    let next = text.index(after: index)
                    if next < text.endIndex, text[next] == "\n" {
                        index = next
                    }
                    endRow()
                case "\n":
                    endRow()
                default:
                    current.append(character)
                }
            }

            index = text.index(after: index)
        }

        if !current.isEmpty || !fields.isEmpty {
            endRow()
        }

        return rows
    }
}
