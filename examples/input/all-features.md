---
title: QmDoc feature overview
author: Hannes Sachsenhofer
clientName: Adliance GmbH
projectCode: QMD-2026
hourlyRate: 135 €/h
---

This example document is a showcase for all QmDoc features, using the latest 2026 theme. 
It is built by calling `qmdoc pdf-and-html-and-markdown --source "./examples/input" --target "./examples/output"`.


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
Links to chapters (to the anchor of the heading) are supported: [#General QmDoc features]. They will automatically include the heading numbering as well. If multiple chapters share the same title, add (some of) their parent chapters separated by `>` to pick the right one, e.g. a link to "#Risk Analysis > Open Questions" in square brackets. Ambiguous chapter links are reported as an error.

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

{{ include ./includes/partial-example.md }}

### Multi-language includes
Documents can be named `<filename>.<language-code>.md` (eg. `report.de.md`). When such a document includes another file, QmDoc first looks for a same-language version of the included file, and only falls back to the plain, non-translated file if no translated version exists. This works for nested includes too - the language of the top-level document is used throughout its entire include tree.

See `multi-language-example.md` and its German counterpart `multi-language-example.de.md` for a working, side-by-side example: both include the exact same `multi-language-partial.md`, but the German document automatically pulls in `multi-language-partial.de.md` instead - without any change to the include statement itself.

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

# Calculations
QmDoc calculates formulas written as `{{ = ... }}` (every calculation starts with `=`, just like in Excel). Units like hours and euros are kept throughout, minutes are converted to hours, and numbers are parsed and formatted according to the `culture` frontmatter setting (default `de-DE`). Results are written the same way the numbers in the formula are written, eg. `90€`, `90 €`, `€ 90` or `135,-`.

## Calculations in text
- `{{ = 145h * 2€ }}` = {{ = 145h * 2€ }} (multiplying with an amount of money is read as "at that price")
- `{{ = 90min + 1h }}` = {{ = 90min + 1h }} (minutes are converted to hours)
- `{{ = 290€ / 145h }}` = {{ = 290€ / 145h }} (compound units)
- `{{ = 12h * 90 €/h }}` = {{ = 12h * 90 €/h }} (the hours cancel each other out)
- `{{ = 1.200€ * 20% }}` = {{ = 1.200€ * 20% }}
- `{{ = € 135,- * 8 }}` = {{ = € 135,- * 8 }}

Frontmatter values are used by their name, without curly braces: at an hourly rate of {{ hourlyRate }}, the implementation takes {{ = 40h + 90min }} and costs {{ = (40h + 90min) * hourlyRate }}.

## Calculations in tables
Inside a table, square brackets refer to other cells: `[Costs]` is the "Costs" cell in the same row, `[Costs:Total net]` the one in the row whose first cell is "Total net", and `[Costs:3]` or `[4:3]` use the row and column numbers. `{{ = SUM }}` adds up all values above it, except other `{{ = SUM }}` cells, so subtotals are not counted twice.

```markdown
| Position          | Hours           | Rate    | Costs                    |
|-------------------|----------------:|--------:|-------------------------:|
| Concept           | 12h             | 135 €/h | {{ = [Hours] * [Rate] }} |
| Implementation    | 40h             | 120 €/h | {{ = [Hours] * [Rate] }} |
| Meetings          | 90min           | 135 €/h | {{ = [Hours] * [Rate] }} |
| **Total net**     | **{{ = SUM }}** |         | **{{ = SUM }}**          |
| VAT 20%           |                 |         | {{ = SUM * 20% }}        |
| **Total gross**   |                 |         | **{{ = SUM }}**          |
```

| Position          | Hours           | Rate    | Costs                    |
|-------------------|----------------:|--------:|-------------------------:|
| Concept           | 12h             | 135 €/h | {{ = [Hours] * [Rate] }} |
| Implementation    | 40h             | 120 €/h | {{ = [Hours] * [Rate] }} |
| Meetings          | 90min           | 135 €/h | {{ = [Hours] * [Rate] }} |
| **Total net**     | **{{ = SUM }}** |         | **{{ = SUM }}**          |
| VAT 20%           |                 |         | {{ = SUM * 20% }}        |
| **Total gross**   |                 |         | **{{ = SUM }}**          |

Rows and columns don't need their complete text, as long as only one of them matches (`[Hours:Impl]` finds "Implementation"):

```markdown
| Phase              | Hours                                                          |
|--------------------|---------------------------------------------------------------:|
| Concept            | 12h                                                            |
| Implementation     | 40h                                                            |
| Testing            | {{ = [Hours:Impl] * 25% }}                                     |
| Project management | {{ = ([Hours:Concept] + [Hours:Impl] + [Hours:Test]) * 10% }} |
| **Total**          | **{{ = SUM }}**                                                |
```

| Phase              | Hours                                                          |
|--------------------|---------------------------------------------------------------:|
| Concept            | 12h                                                            |
| Implementation     | 40h                                                            |
| Testing            | {{ = [Hours:Impl] * 25% }}                                     |
| Project management | {{ = ([Hours:Concept] + [Hours:Impl] + [Hours:Test]) * 10% }} |
| **Total**          | **{{ = SUM }}**                                                |

If a formula can't be calculated (eg. `{{ = 12h + 5€ }}`, because hours and euros can't be added), QmDoc writes a warning and leaves the formula in the document, so a missing value is never overlooked.


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
