// Runs inside the QmDoc preview webview. The extension injects it into the HTML created by QmDoc,
// together with "window.qmdocPreview = { uri, problems }".
(function () {
    // must be the same URL as in QmDoc's MermaidJsSnippet, so that the module loaded by the page is reused
    const mermaidUrl = 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

    const vscode = acquireVsCodeApi();
    const settings = window.qmdocPreview || {};

    // the state is also used by VS Code to restore the preview after a restart
    const previous = vscode.getState() || {};
    const sameDocument = previous.uri === settings.uri;
    vscode.setState({ uri: settings.uri, scrollY: sameDocument ? previous.scrollY || 0 : 0 });

    // the line of the editor the preview is synchronized with (see scrollToLine), undefined after scrolling the preview manually
    let syncedLine = typeof settings.line === 'number' ? settings.line : undefined;
    let ignoreScrollEventsUntil = 0;

    if (syncedLine !== undefined) {
        scrollToLine(syncedLine);
    } else if (sameDocument && previous.scrollY) {
        scrollTo(previous.scrollY);
    }

    // fonts, images and Mermaid diagrams change the height of the page after it's loaded
    const restore = () => {
        if (syncedLine !== undefined) scrollToLine(syncedLine);
        else if (sameDocument && previous.scrollY && window.scrollY === 0) scrollTo(previous.scrollY);
    };
    window.addEventListener('load', restore);
    setTimeout(restore, 300);
    if (typeof ResizeObserver !== 'undefined') {
        new ResizeObserver(() => {
            if (syncedLine !== undefined) scrollToLine(syncedLine);
        }).observe(document.documentElement);
    }

    let scrollTimer;
    window.addEventListener('scroll', () => {
        if (Date.now() > ignoreScrollEventsUntil) syncedLine = undefined;
        clearTimeout(scrollTimer);
        scrollTimer = setTimeout(() => vscode.setState({ uri: settings.uri, scrollY: window.scrollY }), 100);
    });

    window.addEventListener('message', event => {
        const message = event.data;
        if (message.type === 'update') {
            const scrollY = window.scrollY;
            document.body.innerHTML = message.body;
            if (typeof message.line === 'number') scrollToLine(message.line);
            else scrollTo(scrollY);
            showProblems(message.problems || []);
            renderMermaid();
        } else if (message.type === 'scrollToLine') {
            scrollToLine(message.line);
        } else if (message.type === 'error') {
            showBanner('qmdoc-preview-error', [message.message], true);
        }
    });

    document.addEventListener('click', event => {
        const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
        if (!link) return;
        const href = link.getAttribute('href') || '';
        event.preventDefault();

        if (href.startsWith('#')) {
            const target = document.getElementById(decodeURIComponent(href.substring(1)));
            if (target) target.scrollIntoView();
            return;
        }
        vscode.postMessage({ type: 'openLink', href });
    });

    function scrollTo(y) {
        ignoreScrollEventsUntil = Date.now() + 200;
        window.scrollTo(0, y);
    }

    /**
     * QmDoc adds the (zero-based) source line to all blocks with --source-lines ("data-line").
     * The position of a line is interpolated between the block that starts at or before it and the next block after it.
     * Lines in an include all map to the include, so scrolling over the include in the editor scrolls through all of its content.
     */
    function scrollToLine(line) {
        syncedLine = line;
        let before, beforeLine, after, afterLine;
        for (const element of document.querySelectorAll('[data-line]')) {
            if (element.getClientRects().length === 0) continue; // hidden
            const elementLine = Number(element.getAttribute('data-line'));
            if (elementLine <= line) {
                if (before === undefined || elementLine > beforeLine) {
                    before = element;
                    beforeLine = elementLine;
                }
            } else if (after === undefined || elementLine < afterLine) {
                after = element;
                afterLine = elementLine;
            }
        }

        if (!before) {
            scrollTo(0);
            return;
        }

        const top = element => element.getBoundingClientRect().top + window.scrollY;
        let y = top(before);
        if (after && top(after) > y) {
            y += (top(after) - y) * (line - beforeLine) / (afterLine - beforeLine);
        }
        scrollTo(Math.max(0, y));
    }

    function renderMermaid() {
        if (!document.querySelector('.mermaid:not([data-processed])')) return;
        import(mermaidUrl)
            .then(module => module.default.run({ querySelector: '.mermaid:not([data-processed])' }))
            .catch(error => console.error('Unable to render Mermaid diagrams', error));
    }

    function showProblems(problems) {
        showBanner('qmdoc-preview-problems', problems, false);
    }

    /**
     * Banners are added to the body, so they disappear when the body is replaced with the next update.
     */
    function showBanner(id, lines, isError) {
        const existing = document.getElementById(id);
        if (existing) existing.remove();
        if (!lines || lines.length === 0) return;

        const banner = document.createElement('div');
        banner.id = id;
        banner.title = 'Click to hide';
        banner.style.cssText = [
            'position: fixed', 'z-index: 10000', 'left: 12px', 'right: 12px', isError ? 'top: 12px' : 'bottom: 12px',
            'max-height: 30vh', 'overflow: auto', 'padding: 8px 12px', 'border-radius: 4px', 'cursor: pointer',
            'font: 12px/1.4 var(--vscode-font-family, sans-serif)', 'white-space: pre-wrap',
            'box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25)',
            isError
                ? 'background: var(--vscode-inputValidation-errorBackground, #5a1d1d); color: var(--vscode-foreground, #fff); border: 1px solid var(--vscode-inputValidation-errorBorder, #be1100)'
                : 'background: var(--vscode-inputValidation-warningBackground, #352a05); color: var(--vscode-foreground, #fff); border: 1px solid var(--vscode-inputValidation-warningBorder, #b89500)'
        ].join(';');
        banner.textContent = isError
            ? 'QmDoc failed, the preview is outdated:\n' + lines.join('\n')
            : 'QmDoc reported ' + (lines.length === 1 ? '1 problem' : lines.length + ' problems') + ':\n' + lines.join('\n');
        banner.addEventListener('click', event => {
            event.stopPropagation();
            banner.remove();
        });
        document.body.appendChild(banner);
    }

    showProblems(settings.problems || []);
})();
