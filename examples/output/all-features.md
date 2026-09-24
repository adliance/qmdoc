This example document is a showcase for all QmDoc features, using the latest 2026 theme. 
It is built by calling `qmdoc pdf-and-html-and-markdown --source "./examples/input" --target "./examples/output"`.


# 1. General QmDoc features
- The 2026 theme uses automatic hyphenation via CSS (`hyphens: auto;`).
- Headers are automatically numbered.
- PDF contain the outline metadata. Internally, this is also used to render the <span class="link-to-chapter"><i></i>[Table of Content](#table-of-content)</span>.
- Footer is added automatically for PDF, with document title, git version/date (if any) and page numbering.
- QmDoc supports separate `pdf`, `html` and `pdf-and-html` commands, so you can generate either format on its own or both at once.


# 2. Basic formatting

- Bold **asdf**
- Italic *asdf*
- Strikethrough ~~asdf~~
- Superscript ^asdf^
- Subscript ~asdf~
- Highlight ==asdf==
- Insert ++asdf++

Superscript^asdf^ and Subscript~asdf~ are not breaking up line height, because it looks shitty when lines 
suddenly have different visual ~heights~ on longer paragraphs like this one. 
Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. 
At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd ^gubergren^, no sea takimata sanctus est Lorem ipsum dolor sit amet. 
Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. 
At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.


## 2.1. Citations
""There's a proper way to format citations as well.""

# 3. Footnotes
Footnotes[^1] are supported[^note]. They are rendered at the end of the document and can be clicked.

[^1]: This is the first footnote content.
[^note]: Footnotes can have any label, not just numbers.


# 4. Chapter Linking
Links to chapters (to the anchor of the heading) are supported: <span class="link-to-chapter"><i></i>[General QmDoc features](#general-qmdoc-features)</span>. They will automatically include the heading numbering as well.

QmDoc writes a warning if a link to a chapter is detected, but no matching heading.


# 5. Callouts (Alert Blocks)
## 5.1. Custom QmDoc Syntax
{{!}} This will be rendered as a warning/info block.

{{!!}} This will be rendered as a danger block.

{{?}} This will be rendered as a question block.

- {{!}} There's also
- {{?}} support for smaller callout icons
- {{!!}} inside a list, to put an emphasis on specific list items.

## 5.2. Standard Markdown Syntax
QmDoc also supports the GitHub style callouts. There's different flavors of this, but QmDoc supports the Markdig way and the theme just adds proper styling.

> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.


# 6. Custom QmDoc Placeholders
- Current Date: 24. September 2026
- Document Title: QmDoc feature overview
- A `---` in it's own line renders as a page break in PDF.

## 6.1. Frontmatter values
Any key/value pair defined in the frontmatter can also be used as a placeholder, by its key. This works for arbitrary, custom keys, but also for the built-in ones like `title` or `author`.

- Client: Adliance GmbH
- Project Code: QMD-2026
- Author (from frontmatter): Hannes Sachsenhofer

## 6.2. Includes (Partials)
Other Markdown files can be pulled into the current document with an include placeholder: write `include`, followed by a relative path to another Markdown file, wrapped in the same double curly braces used for `DATE` or `TOC` above. The path is resolved relative to the current file, or, if not found there, relative to the currently used theme's folder (useful for partials shared by a theme, like a common legal notice).

Includes are resolved before any other processing happens, so headings inside an included file are numbered correctly, show up in the <span class="link-to-chapter"><i></i>[Table of Content](#table-of-content)</span>, and placeholders inside the included content (like the current date) are resolved as well. Includes can be nested, and circular includes are detected and reported as an error instead of looping forever.

The following heading and paragraph are pulled in from `partial-example.md`:

## 6.3. Included Partial Heading
This heading and paragraph live in a separate file, `partial-example.md`, next to the main document. It gets merged into `all-features.md` at the position of the `{{ include }}` placeholder, before headers are numbered or the table of content is built - that's why this heading is numbered correctly and appears in the <span class="link-to-chapter"><i></i>[Table of Content](#table-of-content)</span> below. Placeholders also still work here, for example the current date: 24. September 2026.


### 6.3.1. Multi-language includes
Documents can be named `<filename>.<language-code>.md` (eg. `report.de.md`). When such a document includes another file, QmDoc first looks for a same-language version of the included file, and only falls back to the plain, non-translated file if no translated version exists. This works for nested includes too - the language of the top-level document is used throughout its entire include tree.

See `multi-language-example.md` and its German counterpart `multi-language-example.de.md` for a working, side-by-side example: both include the exact same `multi-language-partial.md`, but the German document automatically pulls in `multi-language-partial.de.md` instead - without any change to the include statement itself.

<div style="page-break-after: always;"></div>

## 6.4. Table of Content
The table of contents also links to the chapters. Page numbers are only filled in PDF output, not in HTML output.

<div class="toc">

| | |
|-|-:|
| [1. General QmDoc features](#general-qmdoc-features) |  |
| [2. Basic formatting](#basic-formatting) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[2.1. Citations](#citations) |  |
| [3. Footnotes](#footnotes) |  |
| [4. Chapter Linking](#chapter-linking) |  |
| [5. Callouts (Alert Blocks)](#callouts-alert-blocks) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[5.1. Custom QmDoc Syntax](#custom-qmdoc-syntax) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[5.2. Standard Markdown Syntax](#standard-markdown-syntax) |  |
| [6. Custom QmDoc Placeholders](#custom-qmdoc-placeholders) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.1. Frontmatter values](#frontmatter-values) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.2. Includes (Partials)](#includes-partials) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.3. Included Partial Heading](#included-partial-heading) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.3.1. Multi-language includes](#multi-language-includes) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.4. Table of Content](#table-of-content) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[6.5. Git](#git) |  |
| [7. Images](#images) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[7.1. Image captions](#image-captions) |  |
| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[7.2. Inline images](#inline-images) |  |
| [8. Diagrams (Mermaid)](#diagrams-mermaid) |  |

</div>


## 6.5. Git
- Version: 
- Date: 
- Date and Version: 

And a full Git changelog of the current document is also available, formatted as a table:

{{!}} Dieses Dokument befindet sich nicht in Versionskontrolle.

# 7. Images
Images are automatically centered. Wide images are resized to fit the space, 
while small images keep their original size,

![Small Image](test-small.jpg)

![Large Image](test-large.jpg)

## 7.1. Image captions
^^^
![Small Image](test-small.jpg)
^^^ This is an image caption

## 7.2. Inline images
![Large Image](test-large.jpg){.left} There's a way to render images in-line on either the left or the right side. 
This even works with captions. 

^^^
![Small Image](test-small.jpg)
^^^ This is an image caption {.right}
Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua.
At vero eos et accusam et justo duo dolores et ea rebum.

# 8. Diagrams (Mermaid)
```mermaid
graph LR
    A[Parse] --> B[AST]
    B --> C[Render]
    C --> D[HTML]
```
