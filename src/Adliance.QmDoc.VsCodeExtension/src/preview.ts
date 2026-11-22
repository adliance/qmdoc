import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { ToolInstaller } from './installer';
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
    /** Kills the running QmDoc when another document is previewed or the preview is closed. */
    private abortController?: AbortController;
    private timer?: NodeJS.Timeout;
    private timerIsNewLine = false;
    /** The <head> of the currently shown HTML. As long as it doesn't change, only the <body> is replaced, which keeps the scroll position. */
    private shownHead?: string;
    /** The first visible line of the editor of the previewed document, the preview scrolls to the same position. */
    private editorLine?: number;
    private readonly previewScript: string;
    private readonly disposables: vscode.Disposable[] = [];

    /** QmDoc is installed or updated automatically only once per session when the preview finds it missing or outdated. */
    private installAttempted = false;

    constructor(extensionUri: vscode.Uri, private readonly output: vscode.OutputChannel, private readonly installer: ToolInstaller) {
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
            vscode.window.onDidChangeTextEditorVisibleRanges(e => {
                if (!this.panel || !this.isPreviewed(e.textEditor.document) || e.visibleRanges.length === 0) return;
                this.editorLine = e.visibleRanges[0].start.line;
                if (this.scrollSync()) void this.panel.webview.postMessage({ type: 'scrollToLine', line: this.editorLine });
            }),
            vscode.workspace.onDidChangeConfiguration(e => {
                if (e.affectsConfiguration('qmdoc.preview.theme') || e.affectsConfiguration('qmdoc.preview.additionalArguments')) this.convertNow();
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
        // unsaved documents are gone after a restart, opening their URI would create a new empty document
        if (this.panel || !state?.uri || !state.uri.startsWith('file:')) {
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
            this.abortController?.abort();
            void vscode.commands.executeCommand('setContext', 'qmdoc.previewVisible', false);
        });
        panel.webview.onDidReceiveMessage(message => {
            if (message?.type === 'openLink' && typeof message.href === 'string') void this.openLink(message.href);
        });
    }

    private setDocument(uri: vscode.Uri): void {
        if (!this.panel) return;
        this.documentUri = uri;
        this.editorLine = vscode.window.visibleTextEditors.find(e => e.document.uri.toString() === uri.toString())?.visibleRanges[0]?.start.line;
        this.generation++;
        this.abortController?.abort();
        this.shownHead = undefined;
        this.panel.title = `QmDoc Preview: ${path.basename(uri.path)}`;
        this.panel.webview.html = this.messagePage('Converting …');
        this.convertNow();
    }

    private scrollSync(): boolean {
        return vscode.workspace.getConfiguration('qmdoc.preview').get<boolean>('scrollSync', true);
    }

    /** The line the preview should scroll to, or undefined to keep its scroll position. */
    private syncLine(): number | undefined {
        return this.scrollSync() ? this.editorLine : undefined;
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
        this.abortController = new AbortController();
        void this.convert(this.documentUri, this.generation, this.abortController.signal).finally(() => {
            this.running = false;
            this.abortController = undefined;
            if (this.pending) {
                this.pending = false;
                this.convertNow();
            }
        });
    }

    private async convert(uri: vscode.Uri, generation: number, signal: AbortSignal): Promise<void> {
        let document: vscode.TextDocument | undefined;
        if (uri.scheme === 'file') {
            try {
                document = await vscode.workspace.openTextDocument(uri);
            } catch (error) {
                this.showError(`Unable to open ${uri.fsPath}: ${error}`);
                return;
            }
        } else {
            // opening the URI of a closed unsaved document would create a new empty one
            document = vscode.workspace.textDocuments.find(d => d.uri.toString() === uri.toString());
            if (!document) {
                this.showError(`${path.basename(uri.path)} was closed.`);
                return;
            }
        }
        const previewed = document;

        const config = vscode.workspace.getConfiguration('qmdoc.preview');
        const sourcePath = this.sourcePath(document);
        const started = Date.now();
        const convert = () => convertToHtml(previewed.getText(), sourcePath, config.get<string>('theme', ''), config.get<string[]>('additionalArguments', []), signal);
        await this.installer.whenReady(); // don't run QmDoc while it's being updated
        let result = await convert();
        if (result.needsInstall && !this.installAttempted && vscode.workspace.getConfiguration('qmdoc').get<boolean>('autoUpdate', true)) {
            this.installAttempted = true;
            await this.installer.start(true);
            if (generation !== this.generation || !this.panel) return;
            result = await convert();
        }
        if (generation !== this.generation || !this.panel || result.aborted) return;

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
            void this.panel.webview.postMessage({ type: 'update', body, problems, line: this.syncLine() });
            return;
        }

        this.shownHead = head;
        this.panel.webview.html = this.injectPreviewScript(html, problems, uri);
    }

    /**
     * Only the preview script and QmDoc's Mermaid snippet may run (they get a nonce), scripts and event handlers in the
     * previewed document (eg. a cloned repository) are blocked by the content security policy.
     */
    private injectPreviewScript(html: string, problems: string[], uri: vscode.Uri): string {
        const nonce = crypto.randomBytes(16).toString('base64');
        const settings = JSON.stringify({ uri: uri.toString(), problems, line: this.syncLine() }).replace(/</g, '\\u003c');
        const script = `<script nonce="${nonce}">window.qmdocPreview = ${settings};\n${this.previewScript}</script>`;
        // functions as replacement, so that "$" in the script or in the problems isn't interpreted as a replacement pattern
        let result = html.replace(/<script type="module">(\s*import mermaid from 'https:\/\/cdn\.jsdelivr\.net\/)/g, (_, start: string) => `<script type="module" nonce="${nonce}">${start}`);
        result = /<\/body>/i.test(result) ? result.replace(/<\/body>(?![\s\S]*<\/body>)/i, () => `${script}</body>`) : result + script;
        const csp = `<meta http-equiv="Content-Security-Policy" content="${contentSecurityPolicy(nonce)}">`;
        return /<head[^>]*>/i.test(result) ? result.replace(/<head[^>]*>/i, head => head + csp) : csp + result;
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
        const nonce = crypto.randomBytes(16).toString('base64');
        return `<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<meta http-equiv="Content-Security-Policy" content="${contentSecurityPolicy(nonce)}">
<style>
body { font-family: var(--vscode-font-family); color: ${isError ? 'var(--vscode-errorForeground)' : 'var(--vscode-descriptionForeground)'}; padding: 2em; white-space: pre-wrap; }
</style>
</head>
<body>${escapeHtml(message)}<script nonce="${nonce}">window.qmdocPreview = ${uri};\n${this.previewScript}</script></body>
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

        const relativePath = decodeUri(href.split('#')[0].split('?')[0]);
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

/**
 * Images are embedded by QmDoc (data URIs) or come from the web, fonts from Google Fonts, Mermaid from jsdelivr.
 * Inline styles are needed by the themes and Mermaid.
 */
function contentSecurityPolicy(nonce: string): string {
    return [
        "default-src 'none'",
        'img-src data: https:',
        "style-src 'unsafe-inline' https:",
        'font-src data: https:',
        `script-src 'nonce-${nonce}' https://cdn.jsdelivr.net`
    ].join('; ');
}

/** "50%.md" is not a valid encoded URI, it's used as it is. */
function decodeUri(text: string): string {
    try {
        return decodeURIComponent(text);
    } catch {
        return text;
    }
}

function escapeHtml(text: string): string {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}
