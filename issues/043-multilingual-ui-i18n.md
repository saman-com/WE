## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

Supporting references: `docs/engineering/EP-001-engineering-package.md` (§18.12)

## What to build

Implement multilingual UI (i18n) end-to-end. All user-facing text in web portals supports at least two languages (English + one additional, e.g. Māori or Arabic for RTL testing). Language selection per user preference. Translation files externalized; no hardcoded UI strings.

Includes: i18n framework setup in Next.js apps, translation files (`locales/en.json`, `locales/mi.json` or similar), language switcher UI, RTL layout support, and tests.

## Acceptance criteria

- [x] All portal UI strings externalized to translation files
- [x] User can switch language via UI preference (persisted)
- [x] At least 2 languages fully translated for core flows (login, dashboard, assessment)
- [x] RTL layout works correctly for RTL languages
- [x] API error messages support i18n (or return error codes for client translation)
- [x] Tests verify language switching and RTL layout

## Blocked by

- `issues/041-phase7-validation-gate.md`

## User stories addressed

- EP-001 §18.12 (multilingual capability)
- SP-001 Ch.22 (Platform Integration — international extensibility)
- SP-001 §1.3 (Scope — national/international scale)
