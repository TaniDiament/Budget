import SwiftUI

/// The "Goals" tab: create savings goals, track totals, and manage each
/// goal's progress with quick contributions.
struct GoalsSectionView: View {
    @Environment(BudgetStore.self) private var store

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                addGoalCard
                goalsListCard
            }
            .padding(24)
            .padding(.bottom, 44)
        }
        .scrollEdgeEffectStyle(.soft, for: .top)
    }

    private var addGoalCard: some View {
        @Bindable var store = store

        return CardView {
            HStack(alignment: .top, spacing: 26) {
                VStack(alignment: .leading, spacing: 6) {
                    Text("Savings goals")
                        .font(.title2.weight(.semibold))

                    Text("Create goals, set targets, and track how close you are to reaching them.")
                        .font(.callout)
                        .foregroundStyle(.secondary)

                    Text("Leftover this month: \(MoneyText.format(store.leftoverValue)) — contribute it to a goal below.")
                        .font(.footnote)
                        .foregroundStyle(.secondary)
                        .padding(.bottom, 12)

                    HStack(alignment: .bottom, spacing: 12) {
                        FieldColumn("Goal name") {
                            TextField("e.g. Emergency fund", text: $store.newGoalName)
                                .textFieldStyle(.roundedBorder)
                                .onSubmit { addGoalIfPossible() }
                        }
                        .frame(maxWidth: .infinity)

                        FieldColumn("Target") {
                            TextField("5,000", text: $store.newGoalTargetAmountText)
                                .textFieldStyle(.roundedBorder)
                                .multilineTextAlignment(.trailing)
                                .onSubmit { addGoalIfPossible() }
                        }
                        .frame(maxWidth: 120)

                        FieldColumn("Saved so far") {
                            TextField("0.00", text: $store.newGoalSavedAmountText)
                                .textFieldStyle(.roundedBorder)
                                .multilineTextAlignment(.trailing)
                                .onSubmit { addGoalIfPossible() }
                        }
                        .frame(maxWidth: 120)

                        Button("Add goal") {
                            store.addGoal()
                        }
                        .buttonStyle(.glassProminent)
                        .tint(.accentColor)
                        .disabled(!store.canAddGoal)
                    }
                }
                .frame(maxWidth: .infinity, alignment: .leading)

                StatTileGrid {
                    StatTile("Target total", value: MoneyText.format(store.totalGoalTargetValue))
                } tile2: {
                    StatTile("Saved total", value: MoneyText.format(store.totalGoalSavedValue))
                } tile3: {
                    StatTile("Goal progress", value: store.savingsGoalProgressDisplay)
                } tile4: {
                    StatTile("Goals count", value: "\(store.savingsGoals.count)")
                }
                .frame(maxWidth: 400)
            }
        }
    }

    private func addGoalIfPossible() {
        if store.canAddGoal {
            store.addGoal()
        }
    }

    private var goalsListCard: some View {
        CardView(
            title: "Goal progress chart",
            subtitle: "Each goal shows a live progress bar against its target."
        ) {
            if store.savingsGoals.isEmpty {
                Text("No savings goals yet — create one above to start tracking progress.")
                    .font(.callout)
                    .foregroundStyle(.secondary)
            } else {
                VStack(spacing: 12) {
                    ForEach(store.savingsGoals) { goal in
                        GoalRow(goal: goal)
                    }
                }
            }
        }
    }
}

/// One editable goal row with its progress meter and quick-contribute box.
private struct GoalRow: View {
    @Environment(BudgetStore.self) private var store
    @Bindable var goal: SavingsGoal

    var body: some View {
        HStack(alignment: .center, spacing: 16) {
            VStack(alignment: .leading, spacing: 5) {
                TextField(
                    "Name",
                    text: Binding(
                        get: { goal.name },
                        set: { store.rename(goal, to: $0) }))
                    .textFieldStyle(.plain)
                    .font(.system(size: 15, weight: .semibold))
                    .help("Name — click to edit")

                Text("\(goal.progressDisplay) funded · \(goal.remainingDisplay) to go")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .monospacedDigit()

                ProgressView(value: goal.progressFraction)
                    .tint(goal.progressFraction >= 1 ? .green : .accentColor)
                    .padding(.top, 4)
            }
            .frame(minWidth: 200, maxWidth: .infinity, alignment: .leading)

            FieldColumn("Saved", alignment: .trailing) {
                MoneyField(value: Binding(
                    get: { goal.savedAmount },
                    set: { if $0 >= 0 { goal.savedAmount = $0 } }))
                    .frame(width: 110)
                    .help("Saved so far — click to edit")
            }

            FieldColumn("Target", alignment: .trailing) {
                MoneyField(value: Binding(
                    get: { goal.targetAmount },
                    set: { if $0 > 0 { goal.targetAmount = $0 } }))
                    .frame(width: 110)
                    .help("Target amount — click to edit")
            }

            FieldColumn("Contribute") {
                HStack(spacing: 8) {
                    TextField("0.00", text: $goal.contributionText)
                        .textFieldStyle(.roundedBorder)
                        .multilineTextAlignment(.trailing)
                        .frame(width: 90)
                        .onSubmit { store.contribute(to: goal) }
                        .help("Amount to add to this goal")

                    Button("Add") {
                        store.contribute(to: goal)
                    }
                    .buttonStyle(.glass)
                    .help("Add this amount to the goal's savings")
                }
            }

            Button("Remove", role: .destructive) {
                store.removeGoal(goal)
            }
            .buttonStyle(.bordered)
        }
        .rowCard()
    }
}
