import SwiftUI
import Observation
import UniformTypeIdentifiers

@main
struct BudgetApp: App {
    @State private var store = BudgetStore()
    @State private var filePanels = FilePanels()

    @AppStorage("ThemeMode") private var themeModeRaw = ThemeMode.system.rawValue

    private var themeMode: ThemeMode {
        ThemeMode(rawValue: themeModeRaw) ?? .system
    }

    private var colorSchemeOverride: ColorScheme? {
        switch themeMode {
        case .system: nil
        case .light: .light
        case .dark: .dark
        }
    }

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environment(store)
                .environment(filePanels)
                .frame(minWidth: 960, minHeight: 680)
                .preferredColorScheme(colorSchemeOverride)
        }
        .defaultSize(width: 1180, height: 800)
        .commands {
            CommandGroup(replacing: .importExport) {
                Button("Import Budget…") {
                    filePanels.isImporterPresented = true
                }
                .keyboardShortcut("i", modifiers: [.command, .shift])

                Button("Export Budget as JSON…") {
                    filePanels.beginExport(.json, from: store)
                }
                .keyboardShortcut("e", modifiers: .command)

                Button("Export Budget as CSV…") {
                    filePanels.beginExport(.csv, from: store)
                }
                .keyboardShortcut("e", modifiers: [.command, .shift])
            }

            CommandMenu("Budget") {
                Button("Previous Month") {
                    store.goToPreviousMonth()
                }
                .keyboardShortcut("[", modifiers: .command)

                Button("Next Month") {
                    store.goToNextMonth()
                }
                .keyboardShortcut("]", modifiers: .command)

                Divider()

                Button("Copy Last Month's Plan") {
                    store.copyPreviousMonth()
                }
                .disabled(!store.canCopyPreviousMonth)

                Divider()

                Button("Undo Remove") {
                    store.undoRemove()
                }
                .keyboardShortcut("z", modifiers: [.command, .option])
                .disabled(!store.isUndoAvailable)
            }
        }

        Settings {
            SettingsView()
                .preferredColorScheme(colorSchemeOverride)
        }
    }
}

/// The appearance pane behind ⌘, — the Mac-native home for the theme picker
/// the Windows build keeps in its header.
struct SettingsView: View {
    @AppStorage("ThemeMode") private var themeModeRaw = ThemeMode.system.rawValue

    var body: some View {
        Form {
            Picker("Appearance", selection: $themeModeRaw) {
                ForEach(ThemeMode.allCases) { mode in
                    Text(mode.label).tag(mode.rawValue)
                }
            }
            .pickerStyle(.segmented)

            Text("Budget data is saved automatically to Application Support and matches the JSON format of the Windows app, so exports move cleanly between the two.")
                .font(.callout)
                .foregroundStyle(.secondary)
        }
        .formStyle(.grouped)
        .frame(width: 420, height: 190)
    }
}

/// Coordinates the open/save panels so both the menu bar and the toolbar can
/// drive the same fileImporter/fileExporter attached in ContentView.
@Observable
final class FilePanels {
    var isImporterPresented = false
    var exportDocument: TextExportDocument?
    var exportContentType: UTType = .json
    var importErrorMessage: String?

    func beginExport(_ format: BudgetFileExchange.Format, from store: BudgetStore) {
        do {
            let text = try store.exportText(as: format)
            exportContentType = format == .csv ? .commaSeparatedText : .json
            exportDocument = TextExportDocument(text: text)
        } catch {
            store.setStatusMessage("Could not export the budget: \(error.localizedDescription)")
        }
    }
}

/// Plain-text payload handed to the save panel (already-serialized JSON or CSV).
nonisolated struct TextExportDocument: FileDocument {
    static let readableContentTypes: [UTType] = [.json, .commaSeparatedText]
    static let writableContentTypes: [UTType] = [.json, .commaSeparatedText]

    var text: String

    init(text: String) {
        self.text = text
    }

    init(configuration: ReadConfiguration) throws {
        guard let data = configuration.file.regularFileContents else {
            throw CocoaError(.fileReadCorruptFile)
        }
        text = String(decoding: data, as: UTF8.self)
    }

    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper {
        FileWrapper(regularFileWithContents: Data(text.utf8))
    }
}
