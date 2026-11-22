import { spawn } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';

export interface ConversionResult {
    /** The resulting HTML, or undefined if the conversion failed. */
    html?: string;
    /** Problems QmDoc reported for the document (missing images, unknown chapters, ...). */
    problems: string[];
    /** Everything QmDoc wrote to stderr (its log). */
    log: string;
    /** Why the conversion failed. */
    error?: string;
    /** Set when QmDoc is missing or too old, which an installation or update fixes. */
    needsInstall?: boolean;
}

const timeoutMs = 120_000;

/**
 * QmDoc is installed as a .NET global tool. VS Code doesn't always have the global tools directory in its PATH
 * (eg. when started from the Dock on macOS), so the default location of global tools is tried as well.
 */
function candidates(): string[] {
    const executable = process.platform === 'win32' ? 'qmdoc.exe' : 'qmdoc';
    return ['qmdoc', path.join(os.homedir(), '.dotnet', 'tools', executable)];
}

let resolvedExecutable: string | undefined;

/**
 * Looks for QmDoc again on the next conversion, eg. after it was installed.
 */
export function resetExecutable(): void {
    resolvedExecutable = undefined;
}

/**
 * Runs "qmdoc html --stdin --stdout" for the given Markdown.
 * @param sourcePath Path of the Markdown file the text belongs to. QmDoc uses it to resolve includes, images and links, it does not need to exist.
 */
export async function convertToHtml(markdown: string, sourcePath: string, theme: string, additionalArguments: string[]): Promise<ConversionResult> {
    const args = ['html', '--stdin', '--stdout', '--source-lines', '--source', sourcePath];
    if (theme.trim()) args.push('--theme', theme.trim());
    args.push(...additionalArguments);

    const executables = resolvedExecutable ? [resolvedExecutable] : candidates();
    for (const executable of executables) {
        const result = await run(executable, args, markdown, path.dirname(sourcePath));
        if (result === 'not-found') continue;
        resolvedExecutable = executable;
        return result;
    }

    resolvedExecutable = undefined;
    return {
        problems: [],
        log: '',
        needsInstall: true,
        error: 'QmDoc was not found. Please install it with "dotnet tool install -g Adliance.QmDoc" (requires .NET 11) and restart VS Code.'
    };
}

function run(executable: string, args: string[], stdin: string, cwd: string): Promise<ConversionResult | 'not-found'> {
    return new Promise(resolve => {
        const child = spawn(executable, args, { cwd: existingDirectory(cwd), windowsHide: true });
        const stdout: Buffer[] = [];
        const stderr: Buffer[] = [];
        let settled = false;
        const finish = (result: ConversionResult | 'not-found') => {
            if (settled) return;
            settled = true;
            clearTimeout(timer);
            resolve(result);
        };

        const timer = setTimeout(() => {
            child.kill();
            finish({ problems: [], log: Buffer.concat(stderr).toString('utf8'), error: `QmDoc did not finish within ${timeoutMs / 1000} seconds.` });
        }, timeoutMs);

        child.stdout.on('data', (chunk: Buffer) => stdout.push(chunk));
        child.stderr.on('data', (chunk: Buffer) => stderr.push(chunk));
        child.on('error', (error: NodeJS.ErrnoException) => {
            if (error.code === 'ENOENT') finish('not-found');
            else finish({ problems: [], log: '', error: `Unable to start QmDoc: ${error.message}` });
        });
        child.on('close', code => {
            const log = Buffer.concat(stderr).toString('utf8');
            const html = Buffer.concat(stdout).toString('utf8');
            if (/Option '(stdin|stdout|source-lines)' is unknown/i.test(log)) {
                finish({ problems: [], log, needsInstall: true, error: 'The installed QmDoc is too old for the preview. Please update it with "dotnet tool update -g Adliance.QmDoc".' });
                return;
            }
            if (code !== 0 || !html) {
                finish({ problems: [], log, error: lastLine(log) || `QmDoc exited with code ${code}.` });
                return;
            }
            finish({ html, problems: parseProblems(log), log });
        });

        // QmDoc may exit before reading all of stdin (eg. on invalid arguments), which must not crash the extension
        child.stdin.on('error', () => undefined);
        child.stdin.end(stdin, 'utf8');
    });
}

/**
 * QmDoc writes problems of a document as lines indented with two tabs.
 */
function parseProblems(log: string): string[] {
    return log.split(/\r?\n/)
        .filter(line => line.startsWith('\t\t'))
        .map(line => line.trim())
        .filter(line => line.length > 0);
}

function lastLine(log: string): string {
    const lines = log.split(/\r?\n/).map(l => l.trim()).filter(l => l.length > 0);
    return lines[lines.length - 1] ?? '';
}

function existingDirectory(directory: string): string | undefined {
    try {
        return fs.statSync(directory).isDirectory() ? directory : undefined;
    } catch {
        return undefined;
    }
}
