---
name: dotnet-clean-architecture
description: Apply when designing or modifying any .NET code in api/ — deciding where logic goes between Domain, Application, Infrastructure, and Api layers, and how aggregates relate.
metadata:
  stack: [dotnet]
---

# Clean Architecture (Continia.Card .NET API)

## Layers

```
Domain (no dependencies)
  ← Application (depends on Domain)
    ← Infrastructure (depends on Application)
       Api (depends on Infrastructure + Application; composition root)
```

Dependencies point inward. Domain has no external dependencies. Application defines interfaces (ports) that Infrastructure implements. Api wires everything together.

## Where logic lives

| Logic type | Location |
|---|---|
| Single-entity validation / rules | Entity class (Domain) |
| Single-entity creation / mutation | Entity factory or mutation methods on the entity |
| Cross-entity calculations | Domain services — static classes in `Domain/Services/` |
| Multi-repository orchestration | Application services |
| Request validation | FluentValidation validators (Application) |
| HTTP / auth concerns | API helpers or endpoint handlers |
| Simple CRUD wiring | Endpoint handlers directly |

**Rule:** If logic only needs the entity's own state → entity. If it needs multiple repositories → Application service. API layer = HTTP concerns + delegation only.

## Domain and Application services

- **Domain services** — static classes in `Domain/Services/`. Pure functions over entities. No external dependencies. Use when cross-entity logic must live in the Domain layer for testability and reuse.
- **Application services** — extract when an endpoint handler grows beyond simple CRUD. Triggers: multi-repository coordination, conditional logic spanning entities, reuse across multiple endpoints.

## Service extraction criteria

Extract a service when **any** of these are true:

- The handler coordinates more than one repository.
- The handler contains conditional logic that spans multiple entity types.
- The same code would be needed by another endpoint.

Keep services focused — one cohesive responsibility per service. Don't bundle unrelated operations just because they share a repository.

## Cross-aggregate references

Reference sibling aggregates by scalar `Guid?` FK on the aggregate root, **not** via EF navigation properties. One transaction modifies one aggregate; the application service loads the linked aggregate through its own repository.

This rule:

- Keeps transaction scope narrow.
- Avoids cascading EF loads that pull half the database.
- Makes concurrency boundaries explicit (each aggregate has its own RowVersion — see `dotnet-data-access` skill).

## Anti-patterns

- Putting cross-entity business logic in endpoint handlers. Endpoints are HTTP I/O only.
- EF navigation properties that cross aggregate boundaries.
- "God" application services that mix unrelated workflows.
- Domain services with `DbContext` dependencies — those belong in Application.
