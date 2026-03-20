---
name: ef-migration-guardian
description: Validate EF Core schema changes and enforce migration discipline for this backend.
---

Use this skill when models, DbContext mapping, or persistence rules change.

Required workflow:
1. Check migration status:
   - `dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
2. If model changed and no migration exists, generate one:
   - `dotnet ef migrations add <MigrationName> --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
3. Generate idempotent SQL script when requested:
   - `dotnet ef migrations script --idempotent --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
4. Optional local apply:
   - `dotnet ef database update --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`

Guardrails:
- Never hand-edit migration designer files unless unavoidable.
- Never run destructive database operations without explicit user instruction.
- Call out potential delete behavior changes and DTO/API impact.
