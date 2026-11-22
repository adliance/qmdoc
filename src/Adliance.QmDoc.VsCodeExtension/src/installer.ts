import { spawn } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { resetExecutable } from './qmdoc';

const packageId = 'Adliance.QmDoc';
const nugetVersionsUrl = 'https://api.nuget.org/v3-flatcontainer/adliance.qmdoc/index.json';
const checkIntervalMs = 24 * 60 * 60 * 1000;
const lastCheckKey = 'qmdoc.lastToolCheck';
const dotnetTimeoutMs = 5 * 60 * 1000;
/** All VS Code windows start at the same time after a restart, only one of them installs or updates QmDoc. */
const lockFile = path.join(os.tmpdir(), 'qmdoc-vscode-tool-check.lock');
const lockTimeoutMs = 10 * 60 * 1000;

/**
 * Installs QmDoc as .NET global tool if it's missing, and updates it when there's a newer stable version on NuGet.
 * VS Code has no hook for the installation of an extension, so this runs when VS Code starts (at most once a day)
 * and when the preview finds no or an outdated QmDoc. Conversions wait until it's done, so that QmDoc isn't replaced while it runs.
 */
export class ToolInstaller {
    private ready: Promise<void> = Promise.resolve();
    private running = false;
    private dotnetMissingShown = false;

    constructor(private readonly globalState: vscode.Memento, private readonly output: vscode.OutputChannel) {}

    public whenReady(): Promise<void> {
        return this.ready;
    }

    /**
     * @param force check even if the last check was less than a day ago
     * @param interactive show a message if QmDoc is already up to date
     */
    public start(force: boolean, interactive = false): Promise<void> {
        if (this.running) return this.ready;
        this.running = true;
        this.ready = this.check(force, interactive)
            .catch(error => this.output.appendLine(`Unable to install or update QmDoc: ${error}`))
            .finally(() => this.running = false);
        return this.ready;
    }

    private async check(force: boolean, interactive: boolean): Promise<void> {
        if (!interactive && !vscode.workspace.getConfiguration('qmdoc').get<boolean>('autoUpdate', true)) return;
        const lastCheck = this.globalState.get<number>(lastCheckKey, 0);
        if (!force && Date.now() - lastCheck < checkIntervalMs) return;

        if (!acquireLock()) {
            this.output.appendLine('QmDoc is checked or updated by another VS Code window.');
            if (interactive) void vscode.window.showInformationMessage('QmDoc is being checked or updated by another VS Code window.');
            return;
        }
        try {
            await this.checkAndUpdate(interactive);
        } finally {
            fs.rmSync(lockFile, { force: true });
        }
    }

    private async checkAndUpdate(interactive: boolean): Promise<void> {
        const list = await dotnet(['tool', 'list', '-g']);
        if (list === 'not-found') {
            this.showDotnetMissing();
            return;
        }
        const installed = installedVersion(list.stdout);
        const latest = await latestVersion();
        this.output.appendLine(`QmDoc installed: ${installed ?? 'no'}, latest: ${latest ?? 'unknown (offline?)'}`);

        if (installed && (!latest || compareVersions(installed, latest) >= 0)) {
            await this.globalState.update(lastCheckKey, Date.now());
            if (interactive) void vscode.window.showInformationMessage(`QmDoc ${installed} is up to date.`);
            return;
        }

        const verb = installed ? 'update' : 'install';
        const result = await vscode.window.withProgress(
            { location: vscode.ProgressLocation.Notification, title: `${installed ? 'Updating' : 'Installing'} QmDoc${latest ? ` ${latest}` : ''} ...` },
            () => dotnet(['tool', verb, '-g', packageId]));
        if (result === 'not-found') {
            this.showDotnetMissing();
            return;
        }

        this.output.appendLine(`dotnet tool ${verb} -g ${packageId}:\n${result.stdout}${result.stderr}`);
        resetExecutable();
        if (result.code === 0) {
            await this.globalState.update(lastCheckKey, Date.now());
            void vscode.window.showInformationMessage(installed ? `QmDoc was updated from ${installed} to ${latest}.` : `QmDoc ${latest ?? ''} was installed.`);
        } else {
            const action = await vscode.window.showWarningMessage(`Unable to ${verb} QmDoc (dotnet tool ${verb} -g ${packageId}).`, 'Show Log');
            if (action) this.output.show();
        }
    }

    private showDotnetMissing(): void {
        if (this.dotnetMissingShown) return;
        this.dotnetMissingShown = true;
        void vscode.window.showWarningMessage('QmDoc requires the .NET 11 SDK, but "dotnet" was not found.', 'Download .NET').then(action => {
            if (action) void vscode.env.openExternal(vscode.Uri.parse('https://dotnet.microsoft.com/download'));
        });
    }
}

function installedVersion(toolList: string): string | undefined {
    return /^adliance\.qmdoc\s+(\S+)/im.exec(toolList)?.[1];
}

/**
 * Creates the lock file, or returns false if another window holds it (unless it's so old that the window probably crashed).
 */
function acquireLock(): boolean {
    for (let attempt = 0; attempt < 2; attempt++) {
        try {
            fs.writeFileSync(lockFile, String(process.pid), { flag: 'wx' });
            return true;
        } catch {
            try {
                if (Date.now() - fs.statSync(lockFile).mtimeMs < lockTimeoutMs) return false;
                fs.rmSync(lockFile, { force: true });
            } catch {
                // removed in the meantime, try again
            }
        }
    }
    return false;
}

async function latestVersion(): Promise<string | undefined> {
    try {
        const response = await fetch(nugetVersionsUrl, { signal: AbortSignal.timeout(10_000) });
        if (!response.ok) return undefined;
        const json = await response.json() as { versions?: string[] };
        return (json.versions ?? []).filter(v => !v.includes('-')).sort(compareVersions).pop();
    } catch {
        return undefined;
    }
}

/**
 * Compares "3.2.10" and "3.2.3-local" like NuGet does: numbers first, then a pre-release is lower than the release.
 */
export function compareVersions(a: string, b: string): number {
    const [aNumbers, aPre] = a.split('-', 2);
    const [bNumbers, bPre] = b.split('-', 2);
    const aParts = aNumbers.split('.').map(Number);
    const bParts = bNumbers.split('.').map(Number);
    for (let i = 0; i < Math.max(aParts.length, bParts.length); i++) {
        const diff = (aParts[i] ?? 0) - (bParts[i] ?? 0);
        if (diff !== 0) return diff;
    }
    if (aPre && !bPre) return -1;
    if (!aPre && bPre) return 1;
    return (aPre ?? '').localeCompare(bPre ?? '');
}

/**
 * Like QmDoc itself, "dotnet" isn't always in the PATH of VS Code (eg. when started from the Dock on macOS).
 */
function dotnetCandidates(): string[] {
    if (process.platform === 'win32') return ['dotnet', 'C:\\Program Files\\dotnet\\dotnet.exe'];
    return ['dotnet', '/usr/local/share/dotnet/dotnet', '/usr/share/dotnet/dotnet', path.join(os.homedir(), '.dotnet', 'dotnet')];
}

async function dotnet(args: string[]): Promise<{ code: number | null; stdout: string; stderr: string } | 'not-found'> {
    for (const executable of dotnetCandidates()) {
        const result = await new Promise<{ code: number | null; stdout: string; stderr: string } | 'not-found'>(resolve => {
            const child = spawn(executable, args, { windowsHide: true, env: { ...process.env, DOTNET_NOLOGO: '1' } });
            let stdout = '';
            let stderr = '';
            // eg. a credential provider of a private NuGet feed waiting for input, the preview waits for the installation
            const timer = setTimeout(() => {
                child.kill();
                resolve({ code: -1, stdout, stderr: `${stderr}\ndotnet did not finish within ${dotnetTimeoutMs / 60_000} minutes.` });
            }, dotnetTimeoutMs);
            child.stdout.on('data', chunk => stdout += chunk);
            child.stderr.on('data', chunk => stderr += chunk);
            child.on('error', (error: NodeJS.ErrnoException) => {
                clearTimeout(timer);
                resolve(error.code === 'ENOENT' ? 'not-found' : { code: -1, stdout, stderr: String(error) });
            });
            child.on('close', code => {
                clearTimeout(timer);
                resolve({ code, stdout, stderr });
            });
        });
        if (result !== 'not-found') return result;
    }
    return 'not-found';
}
