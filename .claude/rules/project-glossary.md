# Project glossary

KamScore ubiquitous language. Read this when terminology in code, docs, or chat conflicts with what something is called elsewhere — the terms here are the project's canonical names.

## Domain

| Term | Meaning |
|---|---|
| **Tournament** | The top-level aggregate. Has a code, an owner, courts, teams, a structure, and games. Volleyball or beach volleyball, single- or multi-day. |
| **Tournament code** | Short shared secret that lets a *participant* record results via `X-Tournament-Code` header. Hidden from anonymous viewers. |
| **Structure** | The phase tree of a tournament — an ordered list of phases that teams progress through. |
| **Phase** | A stage of the tournament with a single format (e.g. group stage → quarter-finals → semis → final). Has status `New → Scheduled → InProgress → Completed`. |
| **Phase format** | One of `RoundRobin`, `PlayoffElimination`, `PlayoffWithPlacement`, `DoubleElimination`, `DoubleEliminationVd`. Determines how games are generated and how standings are ranked. |
| **Group** | A subdivision within a phase (e.g. "Pool A" inside a round-robin phase). Teams play other teams in the same group. |
| **Level** | A per-phase division of teams by skill or category (e.g. "Mens A", "Mens B"). Levels run in parallel inside a phase. |
| **Court** | A physical playing surface a game can be scheduled on. |
| **Game** | One match between two teams on a court at a start time. Has a result once played. |
| **Team** | A registered competitor in a tournament. Has contact info hidden from non-owners. |
| **Result** | The recorded outcome of a game — set scores + computed winner. Triggers standings recalculation and (for play-off phases) bracket advancement. |
| **Standings** | Ranked list of teams within a group or phase, computed by the format's `*StandingsRanker`. |
| **Bracket** | The fixture tree of an elimination-style play-off phase. |
| **Placeholder team** | A typed reference like *"winner of game X"* or *"3rd place of group Y"* used in a play-off bracket before the upstream result is known. Resolves to a real team once that upstream game/phase finishes. |
| **Referee assignment** | Auto-allocation of a team to officiate a game based on availability — owned by `RefereeAssigner` for formats that support it. |
| **Volunteer** | A person who staffs a shift (not a player). Tracked per-tournament. |
| **Shift** | A specific volunteer slot at a tournament — has a group, time, and optional assigned volunteer. |
| **Shift group** | A grouping of volunteer shifts (e.g. by role: scorekeepers, court manager, registration). |
| **Owner** | The tournament organiser who created it. Has a JWT and full CRUD on their tournaments. |
| **Participant** | An anonymous user holding a tournament code; can record game results, nothing else. |

## Architecture

| Term | Meaning |
|---|---|
| **Aggregate** | DDD term — an entity cluster modified atomically through a single root. KamScore aggregates: Tournament (with Structure, Courts, Teams), Game, Volunteer. |
| **Domain service** | Static class in `Domain/Services/` for cross-entity logic with no external dependencies (e.g. `GameScheduler`, `PhaseAdvancementCalculator`, `BracketUtilities`). |
| **Application service** | Class in `Application/Services/` that coordinates multiple repositories and orchestrates cross-entity state transitions (e.g. `PhaseCompletionService`, `BracketAdvancementService`). |
| **Strategy / Generator / Ranker triad** | The three-file layout per phase format: `XxxStrategy.cs` (orchestrator implementing `IPhaseFormatStrategy`), `XxxGenerator.cs` (game/bracket construction), `XxxStandingsRanker.cs` (per-group and cross-group ranking). |
| **Endpoint handler** | The lambda registered via `Map*Endpoints()` extension on a Minimal API group. Owns HTTP concerns only — validation, mapping, delegation. |
| **FluentValidation validator** | `XxxDtoValidator` in `Application/Validators/`, auto-discovered. Runs before any handler logic. |
| **Optimistic concurrency** | Cosmos DB ETag check via `If-Match` header on writes — every persisted aggregate carries an ETag. |
| **Partition key** | Cosmos DB physical-partition discriminator. Always included in `QueryRequestOptions` to avoid cross-partition queries. |
| **Three-tier access** | Owner (JWT) / Participant (tournament code) / Anonymous (no auth). Enforced per-endpoint. |
| **Composition root** | `KamSquare.KamScore.Api` — the only project allowed to wire up DI for Infrastructure adapters. |

## Outside scope

Anything not in this glossary that needs naming consensus → add it here in the same PR that introduces it. Anything contradicting this glossary (in code, docs, or chat) is wrong by default.
