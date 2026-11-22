---
title: QmDoc Changelog
---

All notable changes to QmDoc. New versions are built and published automatically from the `master` branch (version `3.1.<build>`), so changes are grouped by date. Breaking changes are marked as such.

# 2026-11-22

## Added
- Calculations with `{{ = ... }}`, eg. `{{ = 145h * Stundensatz }}`. Supports `+ - * / ( )`, percentages and frontmatter values by name.
- Units in calculations: hours, minutes (converted to hours), currencies (`€`, `$`, `£`, `EUR`, ...), compound units like `€/h` and any other unit like `Stk`. Results keep the formatting of the input, eg. `90 €`, `€ 90` or `135,-`.
- Calculations in tables: references to other cells (`[Kosten]`, `[Kosten:3]`, `[Kosten:Summe netto]`, `[4:3]`) and `{{ = SUM }}` to add up all values above.
- `culture` frontmatter setting for parsing and formatting numbers in calculations (default `de-DE`).
- Chapter links also find chapters by a part of their title, eg. `[#Projektmeldung]` links to "Projektmeldung (Forschungsregister)", as long as only one chapter matches.
- The README documents all QmDoc features and is now included in the NuGet package.
- Examples for calculations in the feature showcase (`examples/input/all-features.md`).
- This changelog.

## Changed
- **Breaking:** QmDoc now requires .NET 11.

## Fixed
- A frontmatter placeholder inside another placeholder (eg. `{{ = 2 * {{ Stundensatz }} }}`) no longer breaks the outer placeholder.

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
- `pdf-and-html-and-markdown` command, which also stores the processed Markdown (after all includes and placeholders have been applied).

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
