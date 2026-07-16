---
title: QmDoc feature overview
author: Hannes Sachsenhofer
clientName: Adliance GmbH
projectCode: QMD-2026
---

This example document is a showcase for all QmDoc features, using the latest 2026 theme. 
It is built by calling `qmdoc pdf-and-html --source "./examples/all-features.md"`.


# General QmDoc features
- The 2026 theme uses automatic hyphenation via CSS (`hyphens: auto;`).
- Headers are automatically numbered.
- PDF contain the outline metadata. Internally, this is also used to render the [#Table of Content].
- Footer is added automatically for PDF, with document title, git version/date (if any) and page numbering.
- QmDoc supports separate `pdf`, `html` and `pdf-and-html` commands, so you can generate either format on its own or both at once.


# Basic formatting

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


## Citations
""There's a proper way to format citations as well.""

# Footnotes
Footnotes[^1] are supported[^note]. They are rendered at the end of the document and can be clicked.

[^1]: This is the first footnote content.
[^note]: Footnotes can have any label, not just numbers.


# Chapter Linking
Links to chapters (to the anchor of the heading) are supported: [#General QmDoc features]. They will automatically include the heading numbering as well.

QmDoc writes a warning if a link to a chapter is detected, but no matching heading.


# Callouts (Alert Blocks)
## Custom QmDoc Syntax
{{!}} This will be rendered as a warning/info block.

{{!!}} This will be rendered as a danger block.

{{?}} This will be rendered as a question block.

- {{!}} There's also
- {{?}} support for smaller callout icons
- {{!!}} inside a list, to put an emphasis on specific list items.

## Standard Markdown Syntax
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


# Custom QmDoc Placeholders
- Current Date: {{ DATE }}
- Document Title: {{ TITLE }}
- A `---` in it's own line renders as a page break in PDF.

## Frontmatter values
Any key/value pair defined in the frontmatter can also be used as a placeholder, by its key. This works for arbitrary, custom keys, but also for the built-in ones like `title` or `author`.

- Client: {{ clientName }}
- Project Code: {{ PROJECTCODE }}
- Author (from frontmatter): {{ author }}

## Includes (Partials)
Other Markdown files can be pulled into the current document with an include placeholder: write `include`, followed by a relative path to another Markdown file, wrapped in the same double curly braces used for `DATE` or `TOC` above. The path is resolved relative to the current file, or, if not found there, relative to the currently used theme's folder (useful for partials shared by a theme, like a common legal notice).

Includes are resolved before any other processing happens, so headings inside an included file are numbered correctly, show up in the [#Table of Content], and placeholders inside the included content (like the current date) are resolved as well. Includes can be nested, and circular includes are detected and reported as an error instead of looping forever.

The following heading and paragraph are pulled in from `partial-example.md`:

{{ include partial-example.md }}

---

## Table of Content
The table of contents also links to the chapters. Page numbers are only filled in PDF output, not in HTML output.

{{ TOC }}

## Git
- Version: {{ GIT_VERSION }}
- Date: {{ GIT_DATE }}
- Date and Version: {{ GIT_DATE_VERSION }}

And a full Git changelog of the current document is also available, formatted as a table:

{{ GIT_VERSIONS }}

# Images
Images are automatically centered. Wide images are resized to fit the space, 
while small images keep their original size,

![Small Image](test-small.jpg)

![Large Image](test-large.jpg)

## Image captions
^^^
![Small Image](test-small.jpg)
^^^ This is an image caption

## Inline images
![Large Image](test-large.jpg){.left} There's a way to render images in-line on either the left or the right side. 
This even works with captions. 

^^^
![Small Image](test-small.jpg)
^^^ This is an image caption {.right}
Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua.
At vero eos et accusam et justo duo dolores et ea rebum.

# Diagrams (Mermaid)
```mermaid
graph LR
    A[Parse] --> B[AST]
    B --> C[Render]
    C --> D[HTML]
```
