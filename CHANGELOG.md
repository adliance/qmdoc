---
title: QmDoc Changelog
---

All notable changes to QmDoc. New versions are built and published automatically from the `master` branch (version `3.2.<build>`), so changes are grouped by date. Breaking changes are marked as such.

# 2026-11-24

## Changed
- Inline code has the same light background as code blocks (in all themes and in DOCX), and is no longer hyphenated. In the VS Code preview it is no longer shown with the dark VS Code code background.

# 2026-11-22

## Added
- Calculations with `{{ = ... }}`, eg. `{{ = 145h * Stundensatz }}`. Supports `+ - * / ( )`, percentages and frontmatter values by name.
- Units in calculations: hours, minutes (converted to hours), currencies (`€`, `$`, `£`, `EUR`, ...), compound units like `€/h` and any other unit like `Stk`. Results keep the formatting of the input, eg. `90 €`, `€ 90` or `135,-`.
- Calculations in tables: references to other cells (`[Kosten]`, `[Kosten:3]`, `[Kosten:Summe netto]`, `[4:3]`) and `{{ = SUM }}` to add up all values above.
- `culture` frontmatter setting for parsing and formatting numbers in calculations (default `de-DE`).
- DOCX: images are embedded (centered, resized to fit, `{.left}`/`{.right}` floating), figures with captions (`^^^`) are shown as a box like in the PDF.
- DOCX: chapter links (`[#Chapter]`) link to the heading and show its title with numbering.
- DOCX: footnotes are rendered as Word endnotes at the end of the document.
- DOCX: GitHub style callouts (`> [!NOTE]`, `> [!WARNING]`, ...) are shown like the `{{!}}` callouts.
- Chapter links also find chapters by a part of their title, eg. `[#Projektmeldung]` links to "Projektmeldung (Forschungsregister)", as long as only one chapter matches.
- The README documents all QmDoc features and is now included in the NuGet package.
- Examples for calculations, links to other documents, `{{ LINKED_DOCUMENTS }}`, `placeholders.json`, task lists and tables in the feature showcase (`examples/input/all-features.md`).
- `--stdin` reads the Markdown from standard input instead of the source file (`--source` is then only used to resolve includes, images and links, and doesn't need to exist). `--stdout` writes the resulting file to standard output, with all log messages on standard error.
- `--source-lines` (for `html`) adds `data-line` attributes with the line in the source file to all blocks (headings, paragraphs, list items, tables, ...), even for content from includes and placeholders.
- Adliance QmDoc Previewer, an extension for VS Code (`src/Adliance.QmDoc.VsCodeExtension`): a live preview of Markdown files rendered by QmDoc, updated on save, on new lines/paragraphs and after a pause in typing, and scrolled along with the editor. It installs and updates the QmDoc global tool automatically. It's built as `.vsix` by its own pipeline (`azure-pipelines-vscode-extension.yml`).
- This changelog.
- Unit tests (`test/Adliance.QmDoc.Tests`) for the conversion (including integration tests of the PDF conversion against the actual PDF service), placeholders, calculations and formulas. They run in the build pipeline, which also publishes the test results and code coverage.

## Changed
- **Breaking:** QmDoc now requires .NET 11.
- DOCX files look much closer to the PDF: colors, fonts and sizes are taken from the theme (`--theme` works for `docx` as well), the page is A4, text is justified and hyphenated, headings stay with the following text, and the title and author are centered like in the PDF.
- DOCX files only use the built-in styles of Word (Normal, Heading 1-6, Title, Subtitle, Quote, Hyperlink, ...) instead of the "MD ..." styles of the template, so headings work with Word's navigation pane and table of contents.
- DOCX: tables look like in the PDF (full width, horizontal lines, alternating row colors, column alignment), and links, code and other formatting in table cells are kept. The table of contents has no lines and less padding.
- If one of several files can't be converted, the other files are still converted (QmDoc exits with an error at the end). Error messages include the details, eg. which frontmatter value is invalid instead of only "Exception during deserialization".
- An unknown theme is reported and the default theme is used instead (before, the document was rendered without the theme's styles). `set-theme` only accepts existing themes, and no longer tries to download themes from GitHub (they haven't been there since 2022). The README describes the built-in themes and how to create a custom theme.
- `{{ GIT_VERSIONS }}` and the other Git placeholders are much faster on large repositories.
- `qmdoc update` exits with an error if the update fails.
- The `author` frontmatter value is shown as it is, without ", Adliance GmbH" added to it. To show a company (and e-mail address), use a `#### Name | Company | Email` heading in the first line instead.
- Placeholders in code (`` `{{ TOC }}` `` or code blocks) are left alone, so their syntax can be shown in a document, like includes and calculations already were.
- The footer of PDF and DOCX files shows the document title across the whole width (next to the page number), with the Git version in a smaller line below it if there is one. Long titles no longer wrap into a narrow column, and the footer starts and ends at the same position as the text.

## Fixed
- A frontmatter placeholder inside another placeholder (eg. `{{ = 2 * {{ Stundensatz }} }}`) no longer breaks the outer placeholder.
- An empty chapter link like `[#]` no longer aborts the conversion.
- Regular links and images whose text looks like a file name (eg. `![chart.png](chart.png)`) are no longer turned into broken document links.
- `$` in the document or in placeholder values (eg. `$0` or `$$` in a code example) is no longer mangled.
- Callouts (`{{!}}`, `{{!!}}`, `{{?}}`) work in DOCX files, with the same colors as in the PDF.
- DOCX: text with two ` | ` (eg. a table in a code block) is no longer turned into an author line, only `#### Name | Company | Email` headings are. The `author` frontmatter value is shown, and `enableDocumentTitle: false` is respected.
- DOCX: inline code is shown in the code font, and the document language is no longer Russian (it's German by default, or the language of the file name like `report.en.md`, or the `culture` frontmatter value).
- The table of contents links to headings that contain frontmatter placeholders or calculations.
- `---` in code blocks, under headings and in tables without leading `|` is no longer turned into a page break.
- Images with spaces or non-ASCII characters in the file name are embedded, images from the web are no longer reported as missing.
- Amounts like `135,- €` can be used in calculations, and units like `1/h` are shown as `5/h` instead of `51/h`.
- The Git history includes the first commit of a repository.
- Commit messages with multiple lines are shown correctly in `{{ GIT_VERSIONS }}`.
- Includes, chapter links and links to other documents inside code are left alone, so their syntax can be shown in code examples.
- Links to documents in other folders use the correct path, and `[my%20document.md]` works.
- An empty frontmatter, and a document starting with text between two `---` page breaks, no longer cause errors.
- PDF: a `theme` in the frontmatter also sets the header and footer height, scale and timeout of that theme. Before, those of the `--theme` parameter (or the default theme) were used, so the header or footer could overlap the text.
- Links to web pages ending in `.html` (eg. `https://example.com/page.html`) are no longer changed to `.pdf`, only links to other documents are.
- PDF: warnings of documents with a `{{ TOC }}` are no longer shown twice.
- Headings underlined with `===` or `---` are numbered correctly. Before, the underline stayed in the document as text, or became a page break.
- GIF and WebP images are embedded with the correct image type.
- The `pdf-and-html-and-markdown` command works as documented (`pdf-html-markdown` still works as well).
- Calculations with very large numbers, more than 28 decimals or non-ASCII digits (eg. `１`) show a warning instead of aborting the conversion.
- DOCX: an empty figure (`^^^` directly followed by `^^^`) no longer aborts the conversion.
- PDF/HTML: when a bullet point wraps over several lines, the following lines start at the same position as the first line. Before, they were indented a few pixels further.
- The Git version in the footer of PDF and DOCX files was empty unless the document itself contained a Git placeholder. Git placeholders in included files are replaced as well, and a `{{ TOC }}` in an included file gets page numbers in the PDF.
- A heading (or other text) directly after a `---` page break is no longer shown as literal text.
- Regular links like `[#123](https://example.com/issues/123)` are no longer turned into chapter links, and links to a chapter with their own text (`[see above](#intro)`) keep their text instead of showing the chapter title.
- Chapter links and the table of contents work for headings with inline code (eg. ``# Using `qmdoc` ``), and a `|` in a heading no longer breaks the table of contents.
- The frontmatter of an included file is no longer inserted into the document as text.
- Commit messages and names with `|`, `<` or `>` no longer break the `{{ GIT_VERSIONS }}` table.
- Callouts (`{{!}}`, `{{!!}}`, `{{?}}`) written over several lines are shown as callout in PDF/HTML as well.
- `[1.2]` or `[see fig. 3.1]` are no longer treated as links to documents, and folders that can't be read no longer abort the search for linked documents.
- `pdf-and-html --stdin` (and `pdf-and-html-and-markdown --stdin`) create the PDF from standard input as well, instead of an empty one.

# 2026-10-14

## Added
- `watermark` frontmatter setting that stamps a text (eg. "Draft") diagonally over every page of the PDF.

## Fixed
- Heading ids keep non-ASCII letters (eg. `#élimination-des-déchets`), like VS Code and GitHub do. Existing links to the old ASCII-only ids keep working.

# 2026-10-05

## Fixed
- Chapter links if multiple chapters have the same title. Such links can now be made unique by adding their parent chapters separated by `>` (eg. "Risk Analysis > Open Questions"), and ambiguous links are reported.

## Changed
- Updated dependencies.

# 2026-09-24

## Added
- `pdf-and-html-and-markdown` command (then called `pdf-html-markdown`), which also stores the processed Markdown (after all includes and placeholders have been applied).

# 2026-09-14

## Fixed
- Mermaid diagrams in PDFs, by using a dedicated script snippet for the PDF rendering.

# 2026-08-31

## Fixed
- A few minor styling issues.

# 2026-08-04

## Added
- Styling for blockquotes.

# 2026-07-28

## Added
- `pdfTimeoutSeconds` frontmatter setting (and `timeout_seconds` theme option) for very large PDFs that take longer to render.

# 2026-07-24

## Fixed
- Logo in the header of the Adliance theme.

## Changed
- Updated dependencies.

# 2026-07-22

## Fixed
- Page numbers in the table of contents for chapters with uppercase titles.

# 2026-07-17

## Fixed
- The page background is always white.

# 2026-07-16

## Added
- Includes: the `include` placeholder inserts another Markdown file, either relative to the document or from the theme's folder.
- Multi-language includes: `report.de.md` automatically includes `partial.de.md` instead of `partial.md`, if it exists.
- Any frontmatter value can be used as a placeholder, eg. `{{ clientName }}`.
- `html` command to convert to HTML only, and `pdf-and-html` to convert to both.
- MADx theme.
- `--include-subdirectories` parameter.

## Changed
- **Breaking:** Subdirectories of the source directory are no longer converted automatically, use `--include-subdirectories`.
- **Breaking:** The `--include-html` parameter was replaced by the `html` and `pdf-and-html` commands.
- **Breaking:** The `--disable-header-numbering` parameter was replaced by the `enableHeaderNumbering: false` frontmatter setting.
- Chapter ids are calculated by Markdig itself, so chapter links always match the actual headings.
- Reworked CSS variables to make theming easier.
- Improved styling.

# 2026-07-08

## Changed
- Improved table formatting.
- Improved handling of chapter ids.

# 2026-07-06

## Fixed
- Minor styling issues.

For older changes, see the Git history.
