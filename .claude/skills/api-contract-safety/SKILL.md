---
name: api-contract-safety
description: Protect DTO/API response contracts and status-code semantics.
allowed-tools: Read, Grep, Glob, Bash
---

Use before merge when Controllers/Services/DTOs change.

Checklist:
- Keep DTO field names stable unless explicitly approved.
- Keep status code boundaries correct (4xx vs 5xx).
- Preserve frontend-safe JSON shape behavior.
- Avoid exposing secrets in errors/logs.

Validate with:
1. `dotnet build FluentisCore.sln --no-restore --configuration Release`
2. `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`
