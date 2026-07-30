# archgen

Opinionated multi-architecture scaffolding CLI for .NET. Generates complete, build-ready
project structures across four architecture patterns, five persistence/ORM combinations, and
five UI types — so starting a new .NET solution doesn't mean building the same layered
skeleton by hand every time.

## Why

`dotnet new` is generic and unopinionated about architecture. archgen is opinionated and
architecturally correct from the first command: pick a pattern, a persistence backend, and a
UI type, and get a solution with proper project references, dependency direction, and a
working example wired end-to-end — ready to build and run immediately.

## Features

- **4 architecture patterns**: N-Tier, Clean Architecture, CQRS, Minimal API
- **5 persistence backends**: JSON, SQLite (Dapper or EF Core), PostgreSQL (Dapper or EF Core)
- **5 UI types**: Console, Minimal API (ASP.NET Core), WinForms, WPF, Blazor Server
- **Interactive TUI**: run `archgen` with no arguments for a lazygit-style menu
  (built with Spectre.Console); `archgen new ...` with flags works identically for
  scripting/CI
- **Git/GitHub integration**: `--git` initializes a repository and publishes it via the
  `gh` CLI
- **Dependency injection**: Clean Architecture, CQRS, and Minimal API wire
  `IPersistenceProvider` through `Microsoft.Extensions.DependencyInjection` — no manual
  `new SomeProvider()` calls in generated code
- **Hand-rolled CQRS mediator**: no MediatR dependency. As of v13 (July 2025) MediatR
  requires a paid commercial license for larger teams — a poor fit for an open-source
  scaffolding tool. archgen ships its own ~20-line mediator instead.

## Installation

```bash
dotnet tool install --global archgen-cli
```

The tool installs as `archgen`:

```bash
archgen new MyProject --pattern ntier
```

Alternatively, clone and run from source:

```bash
git clone https://github.com/juanchiappa/archgen.git
cd archgen
dotnet build
dotnet run --project src/ArchGen.Cli -- new MyProject --pattern ntier
```

## Usage

### Interactive mode

```bash
archgen
```

Walks you through project name, pattern, persistence, ORM, and UI type with arrow-key
prompts.

### Flag mode

```bash
archgen new <ProjectName> [options]
```

| Flag | Values | Default |
|---|---|---|
| `--pattern`, `-p` | `ntier`, `clean-architecture`, `cqrs`, `minimal-api` | `ntier` |
| `--persistence` | `json`, `sqlite`, `postgres` | `json` |
| `--orm` | `efcore`, `dapper` (ignored for `json`) | `efcore` |
| `--ui` | `console`, `api`, `winforms`, `wpf`, `blazor` | `console` |
| `--output`, `-o` | target directory | current directory |
| `--git` | initialize git + publish to GitHub | off |

### Examples

```bash
# N-Tier, JSON persistence, console UI (closest to a minimal setup)
archgen new MyApp --pattern ntier

# Clean Architecture, SQLite + Dapper, ASP.NET Core Minimal API host
archgen new MyApi --pattern clean-architecture --persistence sqlite --orm dapper --ui api

# CQRS, PostgreSQL + EF Core, and publish straight to GitHub
archgen new MyService --pattern cqrs --persistence postgres --orm efcore --git

# Minimal API pattern: single project, no layers, fastest path to a running API
archgen new MyPrototype --pattern minimal-api --persistence sqlite --orm dapper
```

## Architecture patterns

### N-Tier

Classic layering: `Entities → DataAccess → BusinessLogic → UI`. Each layer references only
the one below it.

### Clean Architecture

Dependency inversion at the center: `Domain` has no dependencies and defines
`IPersistenceProvider`. `Application` and `Infrastructure` both depend on `Domain` only —
`Infrastructure` implements the interface `Domain` defines. `UI` is the composition root: it
wires `Application` and `Infrastructure` together via `AddApplication()` / `AddInfrastructure()`
extension methods, registered through `Microsoft.Extensions.DependencyInjection`.

### CQRS

Commands (writes) and Queries (reads) are dispatched through a mediator to separate handler
types, physically separated into `Application/Commands/` and `Application/Queries/`. A
working example (`ExampleItem` + `CreateExampleItemCommand` + `GetAllExampleItemsQuery`) is
generated in every CQRS project so the wiring can be seen running immediately, not just
inspected as empty folders.

### Minimal API

No layers — entities, persistence, and HTTP endpoints all live in a single project. Trades
structure for speed: the fastest path to a running CRUD API, at the cost of the separation of
concerns the other three patterns provide. A good fit for small services or prototypes.

## Persistence

All five backends implement the same `IPersistenceProvider` interface
(`GetAll<T>`, `GetById<T>`, `Save<T>`, `Delete<T>`), discovered via reflection over the
entity's public properties — no manual mapping code required, and no changes needed above the
persistence layer when switching backends.

| Backend | Notes |
|---|---|
| JSON | Default. One `.json` file per entity type, no external dependencies. |
| SQLite + Dapper | Raw SQL generated from entity properties via reflection; tables created on first use. |
| SQLite + EF Core | `DbContext` discovers entity types via reflection over the entities assembly — no manual `DbSet<T>` registration. |
| PostgreSQL + Dapper | Same approach as SQLite + Dapper, targeting PostgreSQL syntax (`RETURNING`, lowercase identifiers). |
| PostgreSQL + EF Core | Same reflection-based `DbContext` as SQLite + EF Core, using `UseNpgsql`. |

> Never point PostgreSQL persistence at a cloud database for personal projects — use a
> local/self-hosted instance (e.g. via Docker).

## Roadmap status

- [x] Phase 1 — CLI base + N-Tier + JSON persistence
- [x] Phase 2 — SQLite/PostgreSQL × EF Core/Dapper
- [x] Phase 3 — Clean Architecture + full dependency injection
- [x] Phase 4 — 5 UI types (Console, Api, WinForms, Wpf, Blazor Server)
- [x] Phase 5 — CQRS + Minimal API patterns
- [x] Phase 6 — NuGet publication as a global tool

## Tech stack

- `System.CommandLine` — flag parsing
- `Spectre.Console` — interactive TUI
- `dotnet` CLI — shelled out for solution/project scaffolding (not hand-rolled XML)
- `gh` CLI — shelled out for GitHub integration
- .NET 10 target framework

## License

MIT — see [LICENSE](LICENSE).
