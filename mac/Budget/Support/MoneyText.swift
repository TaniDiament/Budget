import Foundation

/// Currency parsing/formatting that mirrors the Windows app's MoneyText:
/// lenient input ("3200", "3,200.50", "$3,200.50") and locale currency output.
nonisolated enum MoneyText {
    static var currencyCode: String {
        Locale.current.currency?.identifier ?? "USD"
    }

    static func format(_ value: Decimal) -> String {
        value.formatted(.currency(code: currencyCode))
    }

    static func parse(_ text: String?) -> Decimal? {
        guard let text else { return nil }
        let trimmed = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmed.isEmpty else { return nil }

        if let value = try? Decimal(trimmed, format: .currency(code: currencyCode)) {
            return value
        }

        if let value = try? Decimal(trimmed, format: .number) {
            return value
        }

        return nil
    }

    /// Invariant-culture parse for CSV interchange (plain "1234.56"), with a
    /// lenient local-currency fallback, matching the C# importer's two attempts.
    static func parseInvariant(_ text: String?) -> Decimal? {
        guard let text else { return nil }
        let trimmed = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmed.isEmpty else { return nil }

        if let value = try? Decimal(trimmed, format: .number.locale(Locale(identifier: "en_US_POSIX"))) {
            return value
        }

        return parse(trimmed)
    }

    /// Invariant serialization for CSV ("1234.56", no grouping or symbol).
    static func invariantString(_ value: Decimal) -> String {
        value.formatted(
            .number
                .locale(Locale(identifier: "en_US_POSIX"))
                .grouping(.never)
                .precision(.fractionLength(0...6)))
    }
}

/// "yyyy-MM" month keys, identical to the Windows app's invariant format.
nonisolated enum MonthKey {
    static func string(from date: Date) -> String {
        let parts = Calendar(identifier: .gregorian).dateComponents([.year, .month], from: date)
        return String(format: "%04d-%02d", parts.year ?? 1, parts.month ?? 1)
    }

    static func date(from key: String?) -> Date? {
        guard let key else { return nil }
        let trimmed = key.trimmingCharacters(in: .whitespacesAndNewlines)

        // Accept "yyyy-MM" and tolerate longer forms like "yyyy-MM-dd".
        let pieces = trimmed.split(separator: "-")
        guard pieces.count >= 2,
              let year = Int(pieces[0]),
              let month = Int(pieces[1]),
              (1...12).contains(month),
              year >= 1
        else { return nil }

        var components = DateComponents()
        components.year = year
        components.month = month
        components.day = 1
        return Calendar(identifier: .gregorian).date(from: components)
    }

    static func firstOfMonth(_ date: Date) -> Date {
        let calendar = Calendar(identifier: .gregorian)
        let parts = calendar.dateComponents([.year, .month], from: date)
        return calendar.date(from: parts) ?? date
    }
}
