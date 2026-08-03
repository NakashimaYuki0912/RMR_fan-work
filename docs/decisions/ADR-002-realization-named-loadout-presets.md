# ADR-002: Named Realization Team Loadout Presets

## Status

Superseded by [ADR-003](ADR-003-remove-realization-loadout-presets.md) on 2026-08-03.

Originally accepted when the user selected Scheme C for full teams through a local decision prototype.

The later single-librarian Scheme A experiment was withdrawn by the user on 2026-08-03 and removed before gameplay verification.

## Date

2026-08-03

## Context

Realization preparation uses a temporary team projected from the permanent Compendium. Rebuilding that team for every challenge is repetitive, but an automatically shared configuration is unsafe because Malkuth, Yesod, Hod, and Netzach are capped at Urban Star while later floors allow Impurity content.

Three local HTML variants were reviewed:

- A: automatically save one independent configuration per floor;
- B: automatically reuse the most recent configuration on every floor;
- C: manually save and import multiple named presets.

## Decision

Implement Scheme C as a dedicated realization-team-preset module with a small public interface: list, save, apply, and delete.

- A preset stores five temporary librarians' selected core page, inherited passives, and combat deck by stable content ID.
- The player explicitly names and saves a preset; entering or starting a battle never overwrites it automatically.
- The player explicitly imports a preset after entering a realization prepare screen.
- Import revalidates every core page, passive donor, and combat page against the permanent Compendium and the selected floor's chapter cap.
- Missing or incompatible content falls back safely and is reported to the player.
- Presets remain separate from both route saves and permanent Compendium unlock data.
- Internal librarian shells (`-854` through `-858`) and transient `BookModel.instanceId` values are never persisted.
- The current realization floor's Compendium and chapter restrictions remain authoritative for team imports.
- Before mutation, the current team is snapshotted. Unexpected apply failures trigger a best-effort rollback and an explicit error instead of being reported as success.

## Consequences

- Players may keep separate named teams for the four Urban Star-capped floors and later Impurity-cap floors.
- Applying a later-floor preset to an early floor is allowed, but incompatible content is replaced or omitted under the early floor's rules.
- A prepare-screen panel is required for naming, listing, importing, and deleting presets.
- The prepare screen exposes one full-team preset launcher. No single-librarian copy/paste entry or runtime API remains.
- Any experimental `RMR_RealizationUnitPresets` user save is left untouched but becomes dormant; the mod no longer reads or writes it.
- The rejected automatic save calls and the throwaway HTML prototype must be removed after implementation.
