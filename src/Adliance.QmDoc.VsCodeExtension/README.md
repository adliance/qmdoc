# QmDoc Preview

Live preview of Markdown files as rendered by [QmDoc](https://github.com/adliance/qmdoc), with the same theme, numbering, placeholders, includes, calculations and diagrams as the PDF.

## Requirements
QmDoc must be installed as a .NET global tool (version with `--stdin`/`--stdout` support):

    dotnet tool install -g Adliance.QmDoc

or, to update it:

    dotnet tool update -g Adliance.QmDoc

The extension calls `qmdoc` from the `PATH`, and falls back to the default location of .NET global tools (`~/.dotnet/tools`).

## Usage
Open a Markdown file and click the PDF icon in the editor title bar, or run one of these commands:

- **QmDoc: Open QmDoc Preview to the Side**
- **QmDoc: Open QmDoc Preview** (also in the context menu of Markdown files in the explorer)
- **QmDoc: Refresh QmDoc Preview**

The preview shows the Markdown file of the active editor and is updated

- when the file is saved,
- shortly after you start a new line or paragraph (or delete one),
- after a pause in typing (see `qmdoc.preview.delay`).

Unsaved changes are previewed as well, QmDoc reads them from standard input. Problems reported by QmDoc (eg. missing images) are shown at the bottom of the preview and in the "QmDoc" output channel. If a conversion fails, the last successful preview stays visible with an error on top.

Links to chapters scroll the preview, links to other documents open their Markdown file, and links to web pages open in the browser.

## Settings

| Setting | Default | Description |
|---|---|---|
| `qmdoc.preview.delay` | `1500` | Milliseconds without typing after which the preview is updated. `0` only updates on new lines and on save. |
| `qmdoc.preview.theme` | | Theme for the preview (`--theme`). Empty uses the theme set with `qmdoc set-theme`. The `theme` frontmatter value of a document always wins. |
| `qmdoc.preview.additionalArguments` | `[]` | Additional arguments for `qmdoc html`, eg. `["--placeholders", "customer.json"]`. |

## Development
Open this folder in VS Code, run `npm install` and press F5. That starts a VS Code window with the extension, which opens the `examples/input` folder of this repository. To test changes of QmDoc itself, make sure the `qmdoc` in the `PATH` is your local build.

`npm run package` creates the `.vsix` file. The pipeline `azure-pipelines-vscode-extension.yml` builds it on every change of this folder on `master`, with the version `1.0.<build>`.
