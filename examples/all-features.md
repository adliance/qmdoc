---
title: QMDoc feature overview
theme: 2026
author: Hannes Sachsenhofer
---

This example document is a showcase for all QMDoc features, using the latest 2026 theme. 
It is built by calling `qmdoc pdf --include-html --source "./examples/all-features.md"`.

# General QMDoc features
- The 2026 theme uses automatic hyphenation via CSS (`hyphens: auto;`).
- Headers are automatically numbered.
- PDF contain the outline metadata. Internally, this is also used to render the [#Table of Content].

# Standard Markdown features

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


# Callouts (Alert Blocks)

## Custom QMDoc Syntax
{{!}} This will be rendered as a warning/info block.

{{!!}} This will be rendered as a danger block.

{{?}} This will be rendered as a question block.

- {{!}} There's also
- {{?}} support for smaller callout icons
- {{!!}} inside a list, to put an emphasis on specific list items.

## Standard Markdown Syntax
QMDoc also supports the GitHub style callouts. There's different flavors of this, but QMDoc supports the Markdig way and the theme just adds proper styling.

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

# Custom QMDoc Placeholders
- Current Date: {{ DATE }}

## Table of Content
The table of contents also links to the chapters. Page numbers are only filled in PDF output, not in HTML output.

{{ TOC }}

## Git
- Version: {{ GIT_VERSION }}
- Date: {{ GIT_DATE }}
- Date and Version: {{ GIT_DATE_VERSION }}

And a full Git changelog

{{ GIT_VERSIONS }}
