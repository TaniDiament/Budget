import SwiftUI
import AppKit
import Combine
import UniformTypeIdentifiers

enum BudgetSection: String, Identifiable, CaseIterable {
    case budget
    case goals
    case income

    var id: Self { self }

    var title: String {
        switch self {
        case .budget: "Budget"
        case .goals: "Goals"
        case .income: "Income"
        }
    }

    var symbol: String {
        switch self {
        case .budget: "list.bullet.rectangle.portrait"
        case .goals: "target"
        case .income: "chart.line.uptrend.xyaxis"
        }
    }
}

struct ContentView: View {
    @Environment(BudgetStore.self) private var store
    @Environment(FilePanels.self) private var filePanels

    @State private var selectedSection: BudgetSection = .budget

    var body: some View {
        @Bindable var filePanels = filePanels

        NavigationSplitView {
            List(BudgetSection.allCases, selection: $selectedSection) { section in
                Label(section.title, systemImage: section.symbol)
                    .tag(section)
            }
            .navigationSplitViewColumnWidth(min: 180, ideal: 210)
        } detail: {
            detailView
                .safeAreaInset(edge: .bottom) {
                    statusBar
                }
        }
        .navigationTitle("Budget")
        .toolbar {
            ToolbarItemGroup {
                Button {
                    store.goToPreviousMonth()
                } label: {
                    Label("Previous month", systemImage: "chevron.left")
                }
                .help("Previous month")

                Text(store.currentMonthLabel)
                    .font(.headline)
                    .monospacedDigit()
                    .frame(minWidth: 132)

                Button {
                    store.goToNextMonth()
                } label: {
                    Label("Next month", systemImage: "chevron.right")
                }
                .help("Next month")
            }

            ToolbarSpacer(.fixed)

            ToolbarItemGroup {
                Button {
                    filePanels.isImporterPresented = true
                } label: {
                    Label("Import", systemImage: "square.and.arrow.down")
                }
                .help("Import a budget from a JSON or CSV file")

                Menu {
                    Button("Export as JSON…") {
                        filePanels.beginExport(.json, from: store)
                    }
                    Button("Export as CSV…") {
                        filePanels.beginExport(.csv, from: store)
                    }
                } label: {
                    Label("Export", systemImage: "square.and.arrow.up")
                }
                .help("Export your budget to a JSON or CSV file")
            }
        }
        .fileImporter(
            isPresented: $filePanels.isImporterPresented,
            allowedContentTypes: [.json, .commaSeparatedText]
        ) { result in
            handleImport(result)
        }
        .fileExporter(
            isPresented: Binding(
                get: { filePanels.exportDocument != nil },
                set: { isPresented in
                    if !isPresented {
                        filePanels.exportDocument = nil
                    }
                }),
            document: filePanels.exportDocument,
            contentType: filePanels.exportContentType,
            defaultFilename: filePanels.exportContentType == .commaSeparatedText ? "budget.csv" : "budget.json"
        ) { result in
            if case .success(let url) = result {
                store.setStatusMessage("Exported budget to \(url.lastPathComponent).")
            }
        }
        .alert(
            "Could not import the selected file.",
            isPresented: Binding(
                get: { filePanels.importErrorMessage != nil },
                set: { isPresented in
                    if !isPresented {
                        filePanels.importErrorMessage = nil
                    }
                })
        ) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(filePanels.importErrorMessage ?? "")
        }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.willTerminateNotification)) { _ in
            store.saveNow()
        }
    }

    @ViewBuilder
    private var detailView: some View {
        switch selectedSection {
        case .budget:
            BudgetSectionView()
        case .goals:
            GoalsSectionView()
        case .income:
            IncomeSectionView()
        }
    }

    /// Floating Liquid Glass status bar: the port of the WPF status strip,
    /// including its single-level Undo affordance.
    private var statusBar: some View {
        GlassEffectContainer {
            HStack(spacing: 12) {
                Image(systemName: "info.circle")
                    .foregroundStyle(.secondary)

                Text(store.statusMessage)
                    .font(.callout)
                    .lineLimit(2)

                Spacer(minLength: 0)

                if store.isUndoAvailable {
                    Button("Undo") {
                        store.undoRemove()
                    }
                    .buttonStyle(.glass)
                    .help("Restore the last removed entry")
                }
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 10)
            .glassEffect(.regular, in: .rect(cornerRadius: 22))
            .padding(.horizontal, 18)
            .padding(.bottom, 12)
        }
    }

    private func handleImport(_ result: Result<URL, Error>) {
        guard case .success(let url) = result else {
            return
        }

        let accessing = url.startAccessingSecurityScopedResource()
        defer {
            if accessing {
                url.stopAccessingSecurityScopedResource()
            }
        }

        do {
            let text = try String(contentsOf: url, encoding: .utf8)
            try store.importState(
                from: text,
                format: .forFileExtension(url.pathExtension),
                fileName: url.lastPathComponent)
        } catch {
            filePanels.importErrorMessage = error.localizedDescription
        }
    }
}

#Preview {
    ContentView()
        .environment(BudgetStore())
        .environment(FilePanels())
}
