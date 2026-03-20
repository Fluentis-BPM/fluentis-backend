---
name: azure-auth-safe-changes
description: Implement authentication-related changes safely for Azure AD and Graph-integrated flows.
---

Use this skill when changing login, token claims handling, authorization policies, or Graph interactions.

Required checks:
- Preserve claim fallbacks and role/issuer/audience validation intent in `Program.cs`.
- Keep specific exception handling before generic exceptions.
- Do not expose tokens, client secrets, or raw connection details in logs or payloads.
- Keep unauthorized/forbidden responses explicit and consistent.

Validation commands:
1. `dotnet build FluentisCore.sln --no-restore --configuration Release`
2. `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`

If integration tests are requested:
- Call out required test secrets and environment setup first.
