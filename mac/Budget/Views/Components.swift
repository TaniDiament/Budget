import SwiftUI

/// Rounded surface card, the SwiftUI counterpart of the WPF CardStyle border.
struct CardView<Content: View>: View {
    var title: String?
    var subtitle: String?
    @ViewBuilder var content: Content

    init(title: String? = nil, subtitle: String? = nil, @ViewBuilder content: () -> Content) {
        self.title = title
        self.subtitle = subtitle
        self.content = content()
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            if title != nil || subtitle != nil {
                VStack(alignment: .leading, spacing: 6) {
                    if let title {
                        Text(title)
                            .font(.title2.weight(.semibold))
                    }
                    if let subtitle {
                        Text(subtitle)
                            .font(.callout)
                            .foregroundStyle(.secondary)
                    }
                }
            }

            content
        }
        .padding(22)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Color(nsColor: .controlBackgroundColor), in: .rect(cornerRadius: 24))
        .overlay {
            RoundedRectangle(cornerRadius: 24)
                .strokeBorder(.separator.opacity(0.6), lineWidth: 1)
        }
    }
}

/// One stat in the 2×2 summary grids. Sits on Liquid Glass inside a
/// GlassEffectContainer so neighboring tiles blend when they get close.
struct StatTile<Footer: View>: View {
    var label: String
    var value: String
    var valueColor: Color?
    @ViewBuilder var footer: Footer

    init(_ label: String, value: String, valueColor: Color? = nil) where Footer == EmptyView {
        self.label = label
        self.value = value
        self.valueColor = valueColor
        self.footer = EmptyView()
    }

    init(_ label: String, value: String, valueColor: Color? = nil, @ViewBuilder footer: () -> Footer) {
        self.label = label
        self.value = value
        self.valueColor = valueColor
        self.footer = footer()
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(label)
                .font(.caption.weight(.semibold))
                .foregroundStyle(.secondary)

            Text(value)
                .font(.system(.title2, design: .rounded, weight: .semibold))
                .monospacedDigit()
                .contentTransition(.numericText())
                .foregroundStyle(valueColor ?? .primary)
                .lineLimit(1)
                .minimumScaleFactor(0.6)

            footer
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(16)
        .glassEffect(.regular, in: .rect(cornerRadius: 18))
    }
}

/// 2×2 tile grid used by all three sections, wrapped in a glass container.
struct StatTileGrid<T1: View, T2: View, T3: View, T4: View>: View {
    @ViewBuilder var tile1: T1
    @ViewBuilder var tile2: T2
    @ViewBuilder var tile3: T3
    @ViewBuilder var tile4: T4

    var body: some View {
        GlassEffectContainer(spacing: 14) {
            VStack(spacing: 14) {
                HStack(alignment: .top, spacing: 14) {
                    tile1
                    tile2
                }
                HStack(alignment: .top, spacing: 14) {
                    tile3
                    tile4
                }
            }
        }
    }
}

/// Small secondary label above a control, matching the WPF form columns.
struct FieldColumn<Content: View>: View {
    var label: String
    var alignment: HorizontalAlignment = .leading
    @ViewBuilder var content: Content

    init(_ label: String, alignment: HorizontalAlignment = .leading, @ViewBuilder content: () -> Content) {
        self.label = label
        self.alignment = alignment
        self.content = content()
    }

    var body: some View {
        VStack(alignment: alignment, spacing: 5) {
            Text(label)
                .font(.caption.weight(.semibold))
                .foregroundStyle(.secondary)
            content
        }
    }
}

/// Right-aligned currency text field bound to a Decimal.
struct MoneyField: View {
    var title: String
    @Binding var value: Decimal

    init(_ title: String = "0.00", value: Binding<Decimal>) {
        self.title = title
        self._value = value
    }

    var body: some View {
        TextField(title, value: $value, format: .currency(code: MoneyText.currencyCode))
            .textFieldStyle(.roundedBorder)
            .multilineTextAlignment(.trailing)
            .monospacedDigit()
    }
}

/// Inset row inside a card, the counterpart of the WPF SurfaceAlt row borders.
struct RowBackground: ViewModifier {
    func body(content: Content) -> some View {
        content
            .padding(16)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(Color(nsColor: .quaternarySystemFill), in: .rect(cornerRadius: 16))
    }
}

extension View {
    func rowCard() -> some View {
        modifier(RowBackground())
    }
}

/// Editable category text with a pull-down of known categories, standing in
/// for the WPF editable combo box.
struct CategoryField: View {
    var prompt: String
    @Binding var text: String
    var options: [String]

    var body: some View {
        HStack(spacing: 6) {
            TextField(prompt, text: $text)
                .textFieldStyle(.roundedBorder)

            Menu {
                ForEach(options, id: \.self) { option in
                    Button(option) {
                        text = option
                    }
                }
            } label: {
                Image(systemName: "chevron.up.chevron.down")
                    .imageScale(.small)
            }
            .menuIndicator(.hidden)
            .fixedSize()
            .accessibilityLabel("Choose a category")
        }
    }
}

extension Severity {
    var tint: Color {
        switch self {
        case .normal: .accentColor
        case .high: .orange
        case .over: .red
        }
    }
}

/// Meter row for the "Spending by category" panel.
struct CategoryMeterRow: View {
    let summary: CategorySummary

    var body: some View {
        VStack(alignment: .leading, spacing: 7) {
            HStack(alignment: .firstTextBaseline) {
                Text(summary.category)
                    .font(.callout.weight(.semibold))
                Spacer(minLength: 12)
                Text(summary.detailText)
                    .font(.callout)
                    .foregroundStyle(.secondary)
                    .monospacedDigit()
            }

            ProgressView(value: summary.consumptionFraction)
                .tint(summary.severity.tint)
        }
    }
}
