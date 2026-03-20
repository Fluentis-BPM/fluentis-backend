# AGENTS.md

Guidance for coding agents working in `fluentis-backend`.

## Scope and Sources

- This repository is a .NET 9 backend solution: `FluentisCore.sln`.
- Main API project: `FluentisCore/FluentisCore.csproj`.
- Test project: `FluentisCore.Tests/FluentisCore.Tests.csproj`.
- CI/CD definitions live in `.github/workflows/`.
- Local hook tooling lives in `.husky/` and `.config/dotnet-tools.json`.

## Rules Files Status (Cursor / Copilot)

- No `.cursorrules` file was found.
- No `.cursor/rules/` directory was found.
- No `.github/copilot-instructions.md` file was found.
- Therefore, this document is the primary agent instruction file for this repo.

## Environment and Setup

- Required SDK: .NET 9 (`net9.0` target frameworks).
- Restore tools and packages:
  - `dotnet tool restore`
  - `dotnet restore FluentisCore.sln`
- Build:
  - `dotnet build FluentisCore.sln --configuration Release`

## Build / Lint / Test Commands

### Core day-to-day commands

- Build (no restore):
  - `dotnet build FluentisCore.sln --no-restore --configuration Release`
- Run API locally:
  - `dotnet run --project FluentisCore/FluentisCore.csproj`
- Run tests:
  - `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --configuration Release`

### Linting / formatting

- Repo uses Husky.Net tasks for pre-commit formatting and commit message linting.
- Pre-commit formatter task name: `dotnet-format` (configured in `.husky/task-runner.json`).
- Direct format command for the solution:
  - `dotnet format FluentisCore.sln`
- Format only staged files is handled by Husky during commits.

### Test selection (single test focus)

- Run a single test by fully qualified name:
  - `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName=FluentisCore.Tests.UsuarioTests.CanAddAndRetrieveUsuario"`
- Run all tests in one class:
  - `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName~FluentisCore.Tests.UsuarioTests"`
- Run tests by method name fragment:
  - `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "Name~F40_ConsumirEndpointDeUsuarios"`
- Exclude integration tests (matches CI):
  - `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName!~IntegrationTests"`

### Useful test diagnostics

- Collect coverage (coverlet collector is referenced):
  - `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --collect:"XPlat Code Coverage"`

## EF Core / Database Commands

- List migrations:
  - `dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
- Apply migrations locally:
  - `dotnet ef database update --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
- Add migration (example):
  - `dotnet ef migrations add <MigrationName> --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
- Generate idempotent SQL migration script:
  - `dotnet ef migrations script --idempotent --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`

## CI/CD Behavior to Mirror Locally

- Main CI workflow (`ci.yml`) does:
  - restore
  - build Release
  - test with `FullyQualifiedName!~IntegrationTests`
  - EF migration listing check
- Preferred local pre-PR validation:
  - `dotnet restore FluentisCore.sln`
  - `dotnet build FluentisCore.sln --no-restore --configuration Release`
  - `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --no-build --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`

## Commit / Hook Conventions

- Commit messages are linted by Husky against Conventional Commits.
- Accepted types include:
  - `build`, `feat`, `ci`, `chore`, `docs`, `fix`, `perf`, `refactor`, `revert`, `style`, `test`
- Valid examples:
  - `feat(auth): add Azure AD fallback for email claim`
  - `fix(workflow): prevent duplicate visualizer relations`
- Keep commit header length in the configured bounds (10-100 chars).

## Code Style Guidelines (C# / ASP.NET Core)

### General conventions

- Use nullable reference types correctly (`<Nullable>enable</Nullable>` is active).
- Prefer explicit, domain-aligned names in Spanish/English mixed style already used in the codebase.
- Follow existing architecture folders (`Controllers`, `Services`, `Models`, `DTO`, `Extensions`, `Converters`, `Modules`).
- Do not rename public API DTO fields casually; frontend contracts rely on current JSON shape.

### Imports and file structure

- Keep `using` directives at top of file.
- Remove unused `using` directives.
- One primary class per file; filename should match main class name.

### Formatting

- Use `dotnet format` style defaults plus existing repo conventions.
- Keep brace style and indentation consistent with surrounding code (4 spaces, braces on new lines).
- Avoid adding trailing whitespace.

### Naming

- Types, methods, properties, enums: `PascalCase`.
- Local variables and parameters: `camelCase`.
- Interfaces: prefix with `I` (e.g., `IWorkflowService`).
- Async methods: suffix with `Async` when introducing new methods.
- DTO classes: suffix with `Dto` / `DTO` to match neighboring files.

### Types and nullability

- Prefer strong types over `object`/`dynamic`.
- Use nullable annotations (`?`) intentionally.
- Validate external inputs early (controller actions, integration boundaries).

### Controllers and API behavior

- Controllers should return appropriate `ActionResult`/`IActionResult` status codes.
- Keep route patterns and casing consistent with existing controllers.
- Prefer DTOs at API boundaries instead of returning tracked entities directly.

### Services and business logic

- Keep controllers thin; place business rules in services.
- Prefer async EF calls (`ToListAsync`, `FirstOrDefaultAsync`, etc.) for I/O.
- Reuse existing mapping extensions in `FluentisCore/Extensions/` where possible.

### Error handling and logging

- Validate preconditions and return meaningful 4xx responses for client errors.
- Use 5xx responses for unexpected server failures.
- Catch specific exceptions first (e.g., Graph/API exceptions) then general exceptions.
- Avoid exposing secrets/tokens/connection strings in logs or error payloads.
- Keep error messages actionable and consistent; include internal details only when safe.

### EF Core and migrations

- Any model change should include a migration unless explicitly not persisted.
- Do not hand-edit migration designer files unless absolutely necessary.
- Run migration list/check commands after schema changes.

### Testing expectations

- Add or update tests for behavior changes.
- Keep unit tests deterministic; avoid shared mutable state between tests.
- Integration tests may depend on Azure AD secrets/tokens; do not run them by default in CI-like local checks.

## Agent Execution Checklist

- Before change: restore/build targeted project if needed.
- During change: follow existing naming, DTO contracts, and route conventions.
- After change (minimum): build + targeted tests.
- Before finalizing: run CI-equivalent test filter excluding integration tests.
