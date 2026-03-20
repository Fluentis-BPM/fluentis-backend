---
name: api-contract-safety
description: Protect API contracts, status codes, and DTO stability during backend changes.
---

Use this skill before merging controller/service changes that impact API behavior.

Checklist:
- Keep controller responses aligned with expected `ActionResult`/`IActionResult` semantics.
- Preserve DTO field names unless explicitly approved.
- Ensure JSON shape remains frontend-safe (camelCase behavior configured in `Program.cs`).
- Validate 4xx vs 5xx error boundaries.
- Ensure external errors do not leak secrets, tokens, or connection strings.

Verification commands:
1. `dotnet build FluentisCore.sln --no-restore --configuration Release`
2. `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`

Output contract:
- List potential contract risks as critical, warning, or safe.
- Include exact file paths for each risk.
