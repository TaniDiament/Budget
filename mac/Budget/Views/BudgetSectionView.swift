import SwiftUI

/// The "Budget" tab: monthly overview, the add-line-item form with live
/// category meters, and the editable list of line items.
struct BudgetSectionView: View {
    @Environment(BudgetStore.self) private var store

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                overviewCard
                addItemCard
                lineItemsCard
            }
            .padding(24)
            .padding(.bottom, 44)
        }
        .scrollEdgeEffectStyle(.soft, for: .top)
    }

    // MARK: - Overview

    private var overviewCard: some View {
        @Bindable var store = store

        return CardView {
            HStack(alignment: .top, spacing: 26) {
                VStack(alignment: .leading, spacing: 12) {
                    Text("Budget planner")
                        .font(.largeTitle.weight(.semibold))

                    Text("Enter your monthly take-home pay, then add every recurring expense and savings goal below.")
                        .font(.callout)
                        .foregroundStyle(.secondary)

                    FieldColumn("Monthly take-home pay") {
                        TextField("e.g. $3,200", text: $store.monthlyTakeHomePayText)
                            .textFieldStyle(.roundedBorder)
                            .font(.title3)
                            .frame(maxWidth: 340)
                    }
                    .padding(.top, 8)

                    Text(store.budgetSummaryMessage)
                        .font(.callout)
                        .fontWeight(store.isOverBudget ? .semibold : .regular)
                        .foregroundStyle(store.isOverBudget ? Color.red : Color.secondary)

                    Text("Tip: use currency values like 3200, 3200.50, or $3,200.50.")
                        .font(.footnote)
                        .foregroundStyle(.tertiary)
                }
                .frame(maxWidth: .infinity, alignment: .leading)

                StatTileGrid {
                    StatTile("Take-home pay", value: MoneyText.format(store.monthlyTakeHomePayValue))
                } tile2: {
                    StatTile("Planned deductions", value: MoneyText.format(store.totalPlannedValue))
                } tile3: {
                    StatTile(
                        "Leftover",
                        value: MoneyText.format(store.leftoverValue),
                        valueColor: store.isOverBudget ? .red : .green)
                } tile4: {
                    StatTile("Usage", value: store.budgetUsageDisplay) {
                        ProgressView(value: store.budgetUsageFraction)
                            .tint(store.usageSeverity.tint)
                            .padding(.top, 4)
                    }
                }
                .frame(maxWidth: 420)
            }
        }
    }

    // MARK: - Add form + category meters

    private var addItemCard: some View {
        @Bindable var store = store

        return CardView {
            HStack(alignment: .top, spacing: 26) {
                VStack(alignment: .leading, spacing: 6) {
                    Text("Add a line item")
                        .font(.title2.weight(.semibold))

                    Text("Group expenses by category and watch the category chart update automatically.")
                        .font(.callout)
                        .foregroundStyle(.secondary)
                        .padding(.bottom, 12)

                    HStack(alignment: .bottom, spacing: 12) {
                        FieldColumn("Line item name") {
                            TextField("e.g. Rent", text: $store.newItemName)
                                .textFieldStyle(.roundedBorder)
                                .onSubmit { addItemIfPossible() }
                        }
                        .frame(maxWidth: .infinity)

                        FieldColumn("Category") {
                            CategoryField(
                                prompt: "General",
                                text: $store.newItemCategory,
                                options: store.categoryOptions)
                        }
                        .frame(maxWidth: 190)

                        FieldColumn("Amount") {
                            TextField("0.00", text: $store.newItemAmountText)
                                .textFieldStyle(.roundedBorder)
                                .multilineTextAlignment(.trailing)
                                .onSubmit { addItemIfPossible() }
                        }
                        .frame(maxWidth: 130)

                        Button("Add item") {
                            store.addLineItem()
                        }
                        .buttonStyle(.glassProminent)
                        .tint(.accentColor)
                        .disabled(!store.canAddLineItem)
                    }
                }
                .frame(maxWidth: .infinity, alignment: .leading)

                categoryPanel
                    .frame(maxWidth: 380)
            }
        }
    }

    private var categoryPanel: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Spending by category")
                .font(.title3.weight(.semibold))

            Text(store.spendingSummaryMessage)
                .font(.footnote)
                .foregroundStyle(.secondary)

            if store.categorySummaries.isEmpty {
                Text("Nothing to chart yet — add a line item to see category totals.")
                    .font(.footnote)
                    .foregroundStyle(.secondary)
                    .padding(.top, 4)
            } else {
                VStack(alignment: .leading, spacing: 12) {
                    ForEach(store.categorySummaries) { summary in
                        CategoryMeterRow(summary: summary)
                    }
                }
                .padding(.top, 4)
            }
        }
        .rowCard()
    }

    private func addItemIfPossible() {
        if store.canAddLineItem {
            store.addLineItem()
        }
    }

    // MARK: - Line items

    private var lineItemsCard: some View {
        CardView(
            title: "Budget line items",
            subtitle: "Each item is assigned to a category so you can see where your money is going."
        ) {
            if store.lineItems.isEmpty {
                VStack(alignment: .leading, spacing: 10) {
                    Text("No line items for \(store.currentMonthLabel) yet — add your first expense above.")
                        .font(.callout)
                        .foregroundStyle(.secondary)

                    if store.canCopyPreviousMonth {
                        Button("Copy last month's plan") {
                            store.copyPreviousMonth()
                        }
                        .buttonStyle(.bordered)
                        .help("Copies the most recent earlier month's pay and line items into this month")
                    }
                }
            } else {
                VStack(spacing: 12) {
                    ForEach(store.lineItems) { item in
                        LineItemRow(item: item)
                    }
                }
            }
        }
    }
}

/// One editable expense row: inline name/category, planned and spent amounts,
/// the "add spending" quick box, and remove.
private struct LineItemRow: View {
    @Environment(BudgetStore.self) private var store
    @Bindable var item: BudgetLineItem

    var body: some View {
        HStack(alignment: .center, spacing: 16) {
            VStack(alignment: .leading, spacing: 4) {
                TextField(
                    "Name",
                    text: Binding(
                        get: { item.name },
                        set: { store.rename(item, to: $0) }))
                    .textFieldStyle(.plain)
                    .font(.system(size: 15, weight: .semibold))
                    .help("Name — click to edit")

                TextField(
                    "Category",
                    text: Binding(
                        get: { item.category },
                        set: { store.setCategory($0, for: item) }))
                    .textFieldStyle(.plain)
                    .font(.caption.weight(.semibold))
                    .foregroundStyle(.secondary)
                    .onSubmit { store.commitCategory(for: item) }
                    .help("Category — click to edit")
            }
            .frame(minWidth: 150, maxWidth: .infinity, alignment: .leading)

            FieldColumn("Planned", alignment: .trailing) {
                MoneyField(value: Binding(
                    get: { item.plannedAmount },
                    set: { if $0 >= 0 { item.plannedAmount = $0 } }))
                    .frame(width: 110)
                    .help("Planned amount — click to edit")
            }

            FieldColumn("Spent", alignment: .trailing) {
                MoneyField(value: Binding(
                    get: { item.actualAmount },
                    set: { if $0 >= 0 { item.actualAmount = $0 } }))
                    .frame(width: 110)
                    .help("Total spent so far this month — click to edit")
            }

            FieldColumn("Add spending") {
                HStack(spacing: 8) {
                    TextField("0.00", text: $item.spendingText)
                        .textFieldStyle(.roundedBorder)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 90)
                        .onSubmit { store.recordSpending(for: item) }
                        .help("Amount to add to this item's Spent total")

                    Button("Add") {
                        store.recordSpending(for: item)
                    }
                    .buttonStyle(.glass)
                    .help("Add this amount to what you've spent on this item")
                }
            }

            Button("Remove", role: .destructive) {
                store.removeLineItem(item)
            }
            .buttonStyle(.bordered)
        }
        .rowCard()
    }
}
