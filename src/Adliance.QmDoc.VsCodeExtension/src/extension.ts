import * as vscode from 'vscode';
import { ToolInstaller } from './installer';
import { PreviewManager, PreviewState, previewViewType } from './preview';

export function activate(context: vscode.ExtensionContext): void {
    const output = vscode.window.createOutputChannel('QmDoc');
    const installer = new ToolInstaller(context.globalState, output);
    const preview = new PreviewManager(context.extensionUri, output, installer);
    void installer.start(false);

    const showPreview = (toSide: boolean) => (uri?: vscode.Uri) => {
        const target = uri instanceof vscode.Uri ? uri : vscode.window.activeTextEditor?.document.uri;
        if (!target) {
            void vscode.window.showInformationMessage('Open a Markdown file to show the QmDoc preview.');
            return;
        }
        preview.show(target, toSide);
    };

    context.subscriptions.push(
        output,
        preview,
        vscode.commands.registerCommand('qmdoc.showPreviewToSide', showPreview(true)),
        vscode.commands.registerCommand('qmdoc.showPreview', showPreview(false)),
        vscode.commands.registerCommand('qmdoc.refreshPreview', () => preview.refresh()),
        vscode.commands.registerCommand('qmdoc.installOrUpdate', async () => {
            await installer.start(true, true);
            preview.refresh();
        }),
        vscode.window.registerWebviewPanelSerializer(previewViewType, {
            deserializeWebviewPanel: async (panel, state) => preview.restore(panel, state as PreviewState | undefined)
        })
    );
}

export function deactivate(): void {
    // everything is disposed with the subscriptions
}
