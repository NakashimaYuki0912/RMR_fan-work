# ADR-003: Remove Realization Loadout Presets

## Status

Accepted

## Date

2026-08-03

## Context

ADR-002 introduced manually named five-librarian presets for realization preparation. After testing the in-game launcher and modal, the user decided the whole preset workflow was unnecessary. Keeping the launcher also made the normal preparation screen look as if preset management were required to configure librarians.

This withdrawal applies to the complete feature, not only the later single-librarian copy experiment.

## Decision

Remove realization loadout presets from the product:

- remove the prepare-screen launcher and modal;
- remove the list, save, import, and delete public interfaces;
- stop reading or writing `RMR_RealizationPresets`;
- remove preset-only localization and build inputs;
- keep existing on-disk experimental preset saves untouched and dormant.

Realization preparation continues to use its temporary Compendium-projected team. The existing route snapshot and restoration rules remain unchanged.

## Alternatives Considered

### Keep named full-team presets

Rejected because the user no longer considers the extra workflow valuable enough to justify its UI and maintenance surface.

### Keep only single-librarian copy and paste

Rejected earlier because it was harder to discover and did not solve the floor-specific team configuration problem cleanly.

### Automatically restore one team per floor

Rejected because automatic mutation is difficult to understand and can silently conflict with floor chapter limits.

## Consequences

- The realization preparation screen has no preset button or modal.
- Players configure the temporary realization team directly using the standard key-page, passive, and deck editors.
- Old `RMR_RealizationPresets` save data is not deleted, migrated, loaded, or updated.
- Future agents must not restore a realization preset feature without a new explicit user decision.
