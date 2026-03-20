---
name: azure-auth-safe-changes
description: Safely implement Azure AD and Graph-related auth changes.
allowed-tools: Read, Grep, Glob, Bash
---

Use when auth, claims mapping, policy checks, or Graph logic changes.

Guardrails:
- Keep claims fallback and authorization logic intact unless intentional.
- Keep specific exception handling before generic catch blocks.
- Never leak tokens/client secrets/connection strings.

Validation:
1. `dotnet build FluentisCore.sln --no-restore --configuration Release`
2. `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`
