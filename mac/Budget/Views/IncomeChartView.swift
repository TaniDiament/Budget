import SwiftUI
import Charts

/// The income ticker, rebuilt on Swift Charts: soft area wash under a 2pt
/// line, dollar-tick gridlines, and a hover crosshair that snaps to the
/// nearest month — the native replacement for the WPF IncomeTickerChart.
struct IncomeChartView: View {
    let points: [IncomeTrendPoint]

    @State private var selectedDate: Date?

    private var selectedPoint: IncomeTrendPoint? {
        guard let selectedDate else { return nil }
        return points.min {
            abs($0.month.timeIntervalSince(selectedDate)) < abs($1.month.timeIntervalSince(selectedDate))
        }
    }

    var body: some View {
        Chart {
            ForEach(points) { point in
                AreaMark(
                    x: .value("Month", point.month, unit: .month),
                    y: .value("Income", point.amountValue))
                    .interpolationMethod(.linear)
                    .foregroundStyle(
                        LinearGradient(
                            colors: [Color.accentColor.opacity(0.18), Color.accentColor.opacity(0.02)],
                            startPoint: .top,
                            endPoint: .bottom))

                LineMark(
                    x: .value("Month", point.month, unit: .month),
                    y: .value("Income", point.amountValue))
                    .interpolationMethod(.linear)
                    .lineStyle(StrokeStyle(lineWidth: 2, lineCap: .round, lineJoin: .round))
                    .foregroundStyle(Color.accentColor)

                if points.count <= 24 {
                    PointMark(
                        x: .value("Month", point.month, unit: .month),
                        y: .value("Income", point.amountValue))
                        .symbolSize(points.count > 1 ? 70 : 110)
                        .foregroundStyle(Color.accentColor)
                }
            }

            // The newest month always keeps its dot and value label, like the
            // WPF ticker's endpoint treatment.
            if let last = points.last {
                PointMark(
                    x: .value("Month", last.month, unit: .month),
                    y: .value("Income", last.amountValue))
                    .symbolSize(100)
                    .foregroundStyle(Color.accentColor)
                    .annotation(
                        position: .topTrailing,
                        overflowResolution: .init(x: .fit(to: .chart), y: .disabled)
                    ) {
                        Text(last.amount.formatted(.currency(code: MoneyText.currencyCode).precision(.fractionLength(0))))
                            .font(.caption.weight(.semibold))
                            .monospacedDigit()
                    }
            }

            if let selectedPoint {
                RuleMark(x: .value("Month", selectedPoint.month, unit: .month))
                    .lineStyle(StrokeStyle(lineWidth: 1))
                    .foregroundStyle(.secondary.opacity(0.5))
                    .annotation(
                        position: .top,
                        overflowResolution: .init(x: .fit(to: .chart), y: .disabled)
                    ) {
                        VStack(alignment: .leading, spacing: 2) {
                            Text(MoneyText.format(selectedPoint.amount))
                                .font(.callout.weight(.semibold))
                                .monospacedDigit()
                            Text(selectedPoint.monthLabel)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                        .padding(.horizontal, 12)
                        .padding(.vertical, 8)
                        .glassEffect(.regular, in: .rect(cornerRadius: 12))
                    }

                PointMark(
                    x: .value("Month", selectedPoint.month, unit: .month),
                    y: .value("Income", selectedPoint.amountValue))
                    .symbolSize(130)
                    .foregroundStyle(Color.accentColor)
            }
        }
        .chartXSelection(value: $selectedDate)
        .chartXAxis {
            AxisMarks(values: .automatic(desiredCount: 8)) {
                AxisGridLine()
                AxisValueLabel(format: .dateTime.month(.abbreviated).year(.twoDigits))
            }
        }
        .chartYAxis {
            AxisMarks(position: .leading, values: .automatic(desiredCount: 5)) { value in
                AxisGridLine()
                AxisValueLabel {
                    if let amount = value.as(Double.self) {
                        Text(amount.formatted(.currency(code: MoneyText.currencyCode).precision(.fractionLength(0))))
                            .monospacedDigit()
                    }
                }
            }
        }
        .chartYScale(domain: yDomain)
    }

    /// Padded "nice" bounds like the original chart: income never dips the
    /// floor below zero, and flat series still get breathing room.
    private var yDomain: ClosedRange<Double> {
        let values = points.map { $0.amountValue }
        guard var minValue = values.min(), var maxValue = values.max() else {
            return 0...1
        }

        if maxValue - minValue < 0.01 {
            let spread = max(abs(maxValue) * 0.1, 1)
            minValue -= spread
            maxValue += spread
        }

        let padding = (maxValue - minValue) * 0.08
        maxValue += padding
        let paddedMin = minValue - padding
        minValue = minValue >= 0 ? max(0, paddedMin) : paddedMin

        return minValue...maxValue
    }
}
