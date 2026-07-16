---
title: Multi-language Example (English)
---

# Multi-language Includes

QmDoc supports multi-language documents. Name a document `<filename>.<language-code>.md` (eg. `multi-language-example.de.md`), and any `include` reference in it will automatically prefer a same-language version of the included file, if one exists - falling back to the plain, non-translated file otherwise.

This document is the plain (English) version, so it pulls in the default partial:

{{ include ./includes/multi-language-partial.md }}

See `multi-language-example.de.md` for the German version of this same document. It uses the exact same include statement, but automatically pulls in `multi-language-partial.de.md` instead, because the document itself is named `multi-language-example.de.md`.
