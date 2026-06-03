# Tournament — Design

Mechanism detail for the requirements in [`../requirements/tournament.md`](../requirements/tournament.md). Covers tournament type, list/copy-source filtering, and deep-link access.

## Tournament type

Governs [FR-TRN-030], [FR-TRN-031].

- Enum `TournamentType { Public, Private, Template }`. `Public` is first → enum value `0`, so Cosmos documents persisted before this feature (no `type` field) deserialize to `Public` — **no data migration**. This persistence fallback is distinct from the create contract below.
- The type is **required on create** (the create request must carry a valid type — the validator rejects a missing/invalid value) and is **editable** afterward by the owner/admin.
- Copy-structure is an ordinary create: the new tournament's type comes from the create request like any other new tournament. The source tournament's type is irrelevant — copying a Template does not produce a Template unless the user picks Template.

## Visibility matrix

Governs [FR-TRN-006], [FR-TRN-033], [FR-TRN-034].

| Type | In tournament list | In copy-structure sources | Direct-link details reachable |
|---|---|---|---|
| Public | Everyone (anonymous, participant, authenticated, admin) | Everyone (authenticated) | Yes |
| Private | Owner & admin only | Owner & admin only | Yes (unlisted) |
| Template | Owner & admin only | **All authenticated users (any owner)** | Yes |

### List filter (per viewer)

Applied in the list endpoint over the full fetch, before enrichment:

- admin → all tournaments
- authenticated non-admin → `Type == Public` OR owned-by-viewer (so an owner sees their own Private/Template)
- anonymous / participant → `Type == Public` only

### Copy-source filter (authenticated only)

- admin → all tournaments
- otherwise → `Type == Public` OR `Type == Template` OR owned-by-viewer

The only difference from the list filter is that **other** owners' Templates are included. This is why copy sources are a distinct set from the plain list, not a client-side filter of it.

## Deep-link access

Governs [FR-TRN-033], [FR-TRN-034].

The details-by-id endpoint stays **public for all types** — privacy is *unlisted* semantics (omission from the list/sources), not access control on the detail view. This is what lets a participant who holds the direct link open a Private/Template tournament and record results. The tournament code remains hidden from non-owners per [FR-TRN-008]; no type-based gate is added to the details endpoint.

## Display

Governs [FR-TRN-032]. The type renders as a badge on the list cards and the details page (Public / Private / Template), distinguished by colour and icon.
