## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

Supporting references: `ABC_EP001.md` (§18.12)

## What to build

Implement multilingual UI (i18n) end-to-end. All user-facing text in web portals supports at least two languages (English + one additional, e.g. Māori or Arabic for RTL testing). Language selection per user preference. Translation files externalized; no hardcoded UI strings.

Includes: i18n framework setup in Next.js apps, translation files (`locales/en.json`, `locales/mi.json` or similar), language switcher UI, RTL layout support, and tests.

## Acceptance criteria

- [ ] All portal UI strings externalized to translation files
- [ ] User can switch language via UI preference (persisted)
- [ ] At least 2 languages fully translated for core flows (login, dashboard, assessment)
- [ ] RTL layout works correctly for RTL languages
- [ ] API error messages support i18n (or return error codes for client translation)
- [ ] Tests verify language switching and RTL layout

## Blocked by

- `issues/041-phase7-validation-gate.md`

## User stories addressed

- EP-001 §18.12 (multilingual capability)
- SP-001 Ch.22 (Platform Integration — international extensibility)
- SP-001 §1.3 (Scope — national/international scale)
