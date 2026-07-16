---
title: Mehrsprachiges Beispiel (Deutsch)
---

# Mehrsprachige Includes

QmDoc unterstützt mehrsprachige Dokumente. Ein Dokument wird `<dateiname>.<sprachcode>.md` genannt (zb. `multi-language-example.de.md`), und jede `include`-Referenz darin bevorzugt automatisch eine gleichsprachige Version der eingebundenen Datei, falls vorhanden - andernfalls wird auf die normale, nicht übersetzte Datei zurückgegriffen.

Dieses Dokument ist die deutsche Version, daher wird automatisch die übersetzte Partial-Datei eingebunden:

{{ include ./includes/multi-language-partial.md }}

Die englische Version dieses Dokuments, `multi-language-example.md`, verwendet dieselbe include-Anweisung, bindet aber automatisch `multi-language-partial.md` ein, weil das Dokument selbst nicht sprachspezifisch benannt ist.
