import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { convertToHtml } from './qmdoc';

export const previewViewType = 'qmdoc.preview';

/** Delay after a new line or paragraph, so that pressing Enter several times only converts once. */
const newLineDelayMs = 250;

export interface PreviewState {
    uri?: string;
    scrollY?: number;
}

/**
 * The single "QmDoc Preview" tab. It shows the Markdown file of the active editor and converts it with
 * "qmdoc html --stdin --stdout" on save, on new lines/paragraphs and after a pause in typing.
 */
export class PreviewManager implements vscode.Disposable {
    private panel?: vscode.WebviewPanel;
    private documentUri?: vscode.Uri;
    /** Increased whenever the previewed document changes, so that results for a previous document are ignored. */
    private generation = 0;
    private running = false;
    private pending = false;
    private timer?: NodeJS.Timeout;
    private timerIsNewLine = false;
    /** The <head> of the currently shown HTML. As long as it doesn't change, only the <body> is replaced, which keeps the scroll position. */
    private shownHead?: string;
    private readonly previewScript: string;
    private readonly disposables: vscode.Disposable[] = [];

    constructor(extensionUri: vscode.Uri, private readonly output: vscode.OutputChannel) {
        this.previewScript = fs.readFileSync(vscode.Uri.joinPath(extensionUri, 'media', 'preview.js').fsPath, 'utf8');

        this.disposables.push(
            vscode.workspace.onDidChangeTextDocument(e => this.onDidChangeTextDocument(e)),
            vscode.workspace.onDidSaveTextDocument(d => {
                if (this.isPreviewed(d)) this.convertNow();
            }),
            vscode.window.onDidChangeActiveTextEditor(editor => {
                if (!this.panel || !editor || editor.document.languageId !== 'markdown' || this.isPreviewed(editor.document)) return;
                // eg. the old version of a file in a diff editor
                if (editor.document.uri.scheme !== 'file' && editor.document.uri.scheme !== 'untitled') return;
                this.setDocument(editor.document.uri);
            }),
            vscode.workspace.onDidChangeConfiguration(e => {
                if (e.affectsConfiguration('qmdoc.preview')) this.convertNow();
            })
        );
    }

    public show(uri: vscode.Uri, toSide: boolean): void {
        if (this.panel) {
            this.panel.reveal(undefined, true);
        } else {
            const column = toSide ? vscode.ViewColumn.Beside : vscode.ViewColumn.Active;
            this.attach(vscode.window.createWebviewPanel(previewViewType, 'QmDoc Preview', { viewColumn: column, preserveFocus: toSide }, this.webviewOptions()));
        }
        if (!this.documentUri || this.documentUri.toString() !== uri.toString()) this.setDocument(uri);
    }

    /**
     * Restores a preview after VS Code was restarted.
     */
    public restore(panel: vscode.WebviewPanel, state: PreviewState | undefined): void {
        if (this.panel || !state?.uri) {
            panel.dispose();
            return;
        }
        panel.webview.options = this.webviewOptions();
        this.attach(panel);
        this.setDocument(vscode.Uri.parse(state.uri));
    }

    public refresh(): void {
        this.shownHead = undefined;
        this.convertNow();
    }

    public dispose(): void {
        this.panel?.dispose();
        this.disposables.forEach(d => d.dispose());
    }

    private webviewOptions(): vscode.WebviewPanelOptions & vscode.WebviewOptions {
        return {
            enableScripts: true,
            // the preview is updated with messages, those would be lost if the webview were recreated when its tab is shown again
            retainContextWhenHidden: true,
            localResourceRoots: []
        };
    }

    private attach(panel: vscode.WebviewPanel): void {
        this.panel = panel;
        void vscode.commands.executeCommand('setContext', 'qmdoc.previewVisible', true);
        panel.onDidDispose(() => {
            this.panel = undefined;
            this.documentUri = undefined;
            this.shownHead = undefined;
            this.generation++;
            this.clearTimer();
            void vscode.commands.executeCommand('setContext', 'qmdoc.previewVisible', false);
        });
        panel.webview.onDidReceiveMessage(message => {
            if (message?.type === 'openLink' && typeof message.href === 'string') void this.openLink(message.href);
        });
    }

    private setDocument(uri: vscode.Uri): void {
        if (!this.panel) return;
        this.documentUri = uri;
        this.generation++;
        this.shownHead = undefined;
        this.panel.title = `QmDoc Preview: ${path.basename(uri.path)}`;
        this.panel.webview.html = this.messagePage('Converting …');
        this.convertNow();
    }

    private isPreviewed(document: vscode.TextDocument): boolean {
        return !!this.documentUri && document.uri.toString() === this.documentUri.toString();
    }

    private onDidChangeTextDocument(e: vscode.TextDocumentChangeEvent): void {
        if (!this.panel || !this.isPreviewed(e.document) || e.contentChanges.length === 0) return;

        const changesLines = e.contentChanges.some(c => c.text.includes('\n') || c.range.start.line !== c.range.end.line);
        if (changesLines) {
            this.schedule(newLineDelayMs, true);
        } else {
            const delay = vscode.workspace.getConfiguration('qmdoc.preview').get<number>('delay', 1500);
            if (delay > 0) this.schedule(delay, false);
        }
    }

    /**
     * A conversion for a new line is not delayed further by typing after it, a conversion after a pause in typing is.
     */
    private schedule(delayMs: number, isNewLine: boolean): void {
        if (this.timer && this.timerIsNewLine) return;
        this.clearTimer();
        this.timerIsNewLine = isNewLine;
        this.timer = setTimeout(() => {
            this.timer = undefined;
            this.convertNow();
        }, delayMs);
    }

    private clearTimer(): void {
        if (this.timer) clearTimeout(this.timer);
        this.timer = undefined;
        this.timerIsNewLine = false;
    }

    /**
     * Only one QmDoc process runs at a time. Changes made while it runs are converted right after it.
     */
    private convertNow(): void {
        this.clearTimer();
        if (!this.panel || !this.documentUri) return;
        if (this.running) {
            this.pending = true;
            return;
        }

        this.running = true;
        void this.convert(this.documentUri, this.generation).finally(() => {
            this.running = false;
            if (this.pending) {
                this.pending = false;
                this.convertNow();
            }
        });
    }

    private async convert(uri: vscode.Uri, generation: number): Promise<void> {
        let document: vscode.TextDocument;
        try {
            document = await vscode.workspace.openTextDocument(uri);
        } catch (error) {
            this.showError(`Unable to open ${uri.fsPath}: ${error}`);
            return;
        }

        const config = vscode.workspace.getConfiguration('qmdoc.preview');
        const sourcePath = this.sourcePath(document);
        const started = Date.now();
        const result = await convertToHtml(document.getText(), sourcePath, config.get<string>('theme', ''), config.get<string[]>('additionalArguments', []));
        if (generation !== this.generation || !this.panel) return;

        if (!result.html) {
            this.output.appendLine(`[${new Date().toLocaleTimeString()}] ${sourcePath} failed: ${result.error}`);
            if (result.log) this.output.appendLine(result.log);
            this.showError(result.error ?? 'Conversion failed.');
            return;
        }

        if (result.problems.length > 0) {
            this.output.appendLine(`[${new Date().toLocaleTimeString()}] ${sourcePath} (${Date.now() - started} ms)`);
            result.problems.forEach(p => this.output.appendLine(`\t${p}`));
        }
        this.render(result.html, result.problems, uri);
    }

    /**
     * Unsaved files don't have a path, they are resolved relative to the workspace (or the home directory).
     */
    private sourcePath(document: vscode.TextDocument): string {
        if (document.uri.scheme === 'file') return document.uri.fsPath;
        const baseDirectory = vscode.workspace.getWorkspaceFolder(document.uri)?.uri.fsPath
            ?? vscode.workspace.workspaceFolders?.[0]?.uri.fsPath
            ?? os.homedir();
        const name = path.basename(document.uri.path) || 'Untitled';
        return path.join(baseDirectory, name.toLowerCase().endsWith('.md') ? name : `${name}.md`);
    }

    private render(html: string, problems: string[], uri: vscode.Uri): void {
        if (!this.panel) return;
        const head = /<head[^>]*>([\s\S]*?)<\/head>/i.exec(html)?.[1] ?? '';
        const body = /<body[^>]*>([\s\S]*)<\/body>/i.exec(html)?.[1];

        if (body !== undefined && this.shownHead === head) {
            void this.panel.webview.postMessage({ type: 'update', body, problems });
            return;
        }

        this.shownHead = head;
        this.panel.webview.html = this.injectPreviewScript(html, problems, uri);
    }

    private injectPreviewScript(html: string, problems: string[], uri: vscode.Uri): string {
        const settings = JSON.stringify({ uri: uri.toString(), problems }).replace(/</g, '\\u003c');
        const script = `<script>window.qmdocPreview = ${settings};\n${this.previewScript}</script>`;
        // a function as replacement, so that "$" in the script or in the problems isn't interpreted as a replacement pattern
        return /<\/body>/i.test(html) ? html.replace(/<\/body>(?![\s\S]*<\/body>)/i, () => `${script}</body>`) : html + script;
    }

    private showError(message: string): void {
        if (!this.panel) return;
        if (this.shownHead === undefined) {
            this.panel.webview.html = this.messagePage(message, true);
        } else {
            void this.panel.webview.postMessage({ type: 'error', message });
        }
    }

    private messagePage(message: string, isError = false): string {
        const uri = JSON.stringify({ uri: this.documentUri?.toString(), problems: [] }).replace(/</g, '\\u003c');
        return `<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
body { font-family: var(--vscode-font-family); color: ${isError ? 'var(--vscode-errorForeground)' : 'var(--vscode-descriptionForeground)'}; padding: 2em; white-space: pre-wrap; }
</style>
</head>
<body>${escapeHtml(message)}<script>window.qmdocPreview = ${uri};\n${this.previewScript}</script></body>
</html>`;
    }

    /**
     * Links to other documents point to their PDF (that's what QmDoc does for the PDF), the preview opens their Markdown file instead.
     */
    private async openLink(href: string): Promise<void> {
        if (/^[a-z][a-z0-9+.-]*:/i.test(href)) {
            if (/^(https?|mailto):/i.test(href)) await vscode.env.openExternal(vscode.Uri.parse(href));
            return;
        }
        if (!this.documentUri || this.documentUri.scheme !== 'file') return;

        const relativePath = decodeURIComponent(href.split('#')[0].split('?')[0]);
        if (!relativePath) return;
        let target = path.resolve(path.dirname(this.documentUri.fsPath), relativePath);
        if (/\.(pdf|html|docx)$/i.test(target)) {
            const markdown = target.replace(/\.(pdf|html|docx)$/i, '.md');
            if (fs.existsSync(markdown)) target = markdown;
        }
        if (!fs.existsSync(target)) {
            void vscode.window.showWarningMessage(`QmDoc Preview: ${relativePath} does not exist.`);
            return;
        }

        const targetUri = vscode.Uri.file(target);
        if (target.toLowerCase().endsWith('.md')) {
            const editorColumn = vscode.window.visibleTextEditors.find(e => this.documentUri && e.document.uri.toString() === this.documentUri.toString())?.viewColumn;
            await vscode.window.showTextDocument(targetUri, { viewColumn: editorColumn ?? vscode.ViewColumn.One });
        } else {
            await vscode.commands.executeCommand('vscode.open', targetUri);
        }
    }
}

function escapeHtml(text: string): string {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}
