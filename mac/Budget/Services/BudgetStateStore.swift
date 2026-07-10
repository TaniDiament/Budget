import Foundation

/// Loads and saves budget-state.json in Application Support (inside the app's
/// sandbox container), the macOS counterpart of %LOCALAPPDATA%\Budget.
nonisolated struct BudgetStateStore {
    static let folderURL: URL = {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? FileManager.default.homeDirectoryForCurrentUser.appending(path: "Library/Application Support")
        return base.appending(path: "Budget", directoryHint: .isDirectory)
    }()

    static let fileURL = folderURL.appending(path: "budget-state.json")

    func load() -> BudgetStateFile? {
        do {
            let data = try Data(contentsOf: Self.fileURL)
            return try Self.decode(data)
        } catch {
            return nil
        }
    }

    @discardableResult
    func save(_ state: BudgetStateFile) -> Bool {
        do {
            let data = try Self.encode(state)
            try FileManager.default.createDirectory(at: Self.folderURL, withIntermediateDirectories: true)
            try data.write(to: Self.fileURL, options: .atomic)
            return true
        } catch {
            return false
        }
    }

    static func decode(_ data: Data) throws -> BudgetStateFile {
        try JSONDecoder().decode(BudgetStateFile.self, from: data)
    }

    static func encode(_ state: BudgetStateFile) throws -> Data {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        return try encoder.encode(state)
    }
}
