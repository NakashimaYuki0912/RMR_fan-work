# RMR Localization docs

This folder is the **source of truth for translators and developers** working on RMR text.

| Doc | Audience | Purpose |
|-----|----------|---------|
| [TRANSLATOR_GUIDE_EN.md](TRANSLATOR_GUIDE_EN.md) | English (and other) translators | How to edit strings, file map, workflow, QA |
| [TRANSLATION_WORKFLOW.md](TRANSLATION_WORKFLOW.md) | All translators | Task routing for events, pages, passives and descriptions; safe edit and QA workflow |
| [GLOSSARY.md](GLOSSARY.md) | All languages | Canonical terms (CN / EN / KR) — **use these names** |
| [FILE_MAP.md](FILE_MAP.md) | Translators + coders | Which folder/file holds which content |
| [CODE_TERMINOLOGY.md](CODE_TERMINOLOGY.md) | Developers | C# name map (Compendium vs disk keys vs Lastest) |
| [../agent-handbook/01-localization-fonts.md](../agent-handbook/01-localization-fonts.md) | Developers | Fonts, tofu, encoding accidents |

Game data path (repo):

```text
Localize/
  cn/   Chinese (Simplified) — primary author language for many hub strings
  en/   English
  kr/   Korean  (folder name is "kr", NOT Japanese)
```

Package / Workshop id for this fan build: `abcdcodecalmmagma.LogueLikeReborn`  
Workshop content item (author): `3743867841`

---

## Quick start (translator)

1. Read **GLOSSARY.md** (especially *Compendium*, *Realization*, *Abnormality page*).
2. Edit only the language folder you own, e.g. `Localize/en/…`.
3. **Never rename `id="..."` keys** — only change text between tags.
4. Save as **UTF-8** (no GBK/ANSI).
5. Keep the same relative path/filename as `cn/` and `en/` when possible.
6. Run `tools/localization/validate_localization.ps1`, then ask a developer to deploy `Localize/` and restart the game.

---

## Key principles

- **Same key id, three languages** — `ui_RMR_Hub_Atlas` is one key; CN shows 图鉴, EN shows *Compendium*, KR shows 도감.
- **Internal code may still say “Atlas”** (`LogAtlasPanel`, key suffix `_Atlas`). That is **not** player-facing English.
- **Do not invent new key ids** without a developer adding a code lookup.
- **Vanilla LoR terms** (Canard, Urban Myth, Star of the City, etc.) should match Project Moon English where possible.
