import SwiftUI

/// The "Income" tab: per-month income history, the trend chart, and the
/// editable list of recorded months.
struct IncomeSectionView: View {
    @Environment(BudgetStore.self) private var store

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                addIncomeCard
                chartCard
                recordsCard
            }
            .padding(24)
            .padding(.bottom, 44)
        }
        .scrollEdgeEffectStyle(.soft, for: .top)
    }

    private var addIncomeCard: some View {
        @Bindable var store = store

        return CardView {
            HStack(alignment: .top, spacing: 26) {
                VStack(alignment: .leading, spacing: 6) {
                    Text("Monthly income tracker")
                        .font(.title2.weight(.semibold))

                    Text("Track your monthly income over time and compare it with your budget.")
                        .font(.callout)
                        .foregroundStyle(.secondary)
                        .padding(.bottom, 12)

                    HStack(alignment: .bottom, spacing: 12) {
                        FieldColumn("Month") {
                            DatePicker(
                                "Month",
                                selection: $store.newIncomeMonth,
                                displayedComponents: .date)
                                .labelsHidden()
                                .datePickerStyle(.field)
                        }

                        FieldColumn("Income") {
                            TextField("0.00", text: $store.newIncomeAmountText)
                                .textFieldStyle(.roundedBorder)
                                .multilineTextAlignment(.trailing)
                                .onSubmit { addIncomeIfPossible() }
                        }
                        .frame(maxWidth: 140)

                        Button("Add month") {
                            store.addIncomeEntry()
                        }
                        .buttonStyle(.glassProminent)
                        .tint(.accentColor)
                        .disabled(!store.canAddIncomeEntry)
                    }
                }
                .frame(maxWidth: .infinity, alignment: .leading)

                StatTileGrid {
                    StatTile("Records", value: "\(store.incomeEntries.count)")
                } tile2: {
                    StatTile("Latest income", value: store.latestIncomeDisplay)
                } tile3: {
                    StatTile("Total tracked", value: MoneyText.format(store.incomeHistoryTotalValue))
                } tile4: {
                    StatTile("Average", value: MoneyText.format(store.incomeHistoryAverageValue))
                }
                .frame(maxWidth: 400)
            }
        }
    }

    private func addIncomeIfPossible() {
        if store.canAddIncomeEntry {
            store.addIncomeEntry()
        }
    }

    private var chartCard: some View {
        CardView {
            HStack(alignment: .firstTextBaseline) {
                Text("Income over time")
                    .font(.title2.weight(.semibold))

                Spacer()

                if store.incomeChangeDirection != .none {
                    Text(store.incomeChangeDisplay)
                        .font(.callout.weight(.semibold))
                        .monospacedDigit()
                        .foregroundStyle(changeColor)
                        .help("Latest month compared with the one before it")
                }
            }

            Text("Each month's income plotted like a stock ticker — hover the chart to read a month's exact amount.")
                .font(.callout)
                .foregroundStyle(.secondary)

            if store.incomeTrendPoints.isEmpty {
                Text("No income recorded yet — add a month above to see the trend.")
                    .font(.callout)
                    .foregroundStyle(.secondary)
                    .padding(.vertical, 8)
            } else {
                IncomeChartView(points: store.incomeTrendPoints)
                    .frame(height: 260)
                    .padding(.top, 6)
            }
        }
    }

    private var changeColor: Color {
        switch store.incomeChangeDirection {
        case .up: .green
        case .down: .red
        case .flat, .none: .secondary
        }
    }

    private var recordsCard: some View {
        CardView(
            title: "Income records",
            subtitle: "Use this list to manage the months you have entered."
        ) {
            if store.incomeEntries.isEmpty {
                Text("No months tracked yet.")
                    .font(.callout)
                    .foregroundStyle(.secondary)
            } else {
                VStack(spacing: 12) {
                    // Insertion order, like the Windows list — undo restores
                    // a removed month back into its original slot.
                    ForEach(store.incomeEntries) { entry in
                        IncomeRow(entry: entry)
                    }
                }
            }
        }
    }
}

/// One recorded month with an editable amount.
private struct IncomeRow: View {
    @Environment(BudgetStore.self) private var store
    @Bindable var entry: IncomeEntry

    var body: some View {
        HStack(alignment: .center, spacing: 16) {
            VStack(alignment: .leading, spacing: 3) {
                Text(entry.monthLabel)
                    .font(.system(size: 15, weight: .semibold))

                Text("Monthly income entry")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            .frame(maxWidth: .infinity, alignment: .leading)

            MoneyField(value: Binding(
                get: { entry.amount },
                set: { if $0 > 0 { entry.amount = $0 } }))
                .frame(width: 130)
                .help("Income for this month — click to edit")

            Button("Remove", role: .destructive) {
                store.removeIncomeEntry(entry)
            }
            .buttonStyle(.bordered)
        }
        .rowCard()
    }
}
