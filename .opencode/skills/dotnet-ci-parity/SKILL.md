---
name: dotnet-ci-parity
description: Run repository validation with the same build and test behavior used in CI.
---

Use this skill when preparing a branch for review or checking local health.

Execution order:
1. `dotnet tool restore`
2. `dotnet restore FluentisCore.sln`
3. `dotnet build FluentisCore.sln --no-restore --configuration Release`
4. `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --no-build --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`
5. `dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`

Output contract:
- Report pass or fail per step.
- On failure, include the first actionable root-cause line and the next command to run.

Safety checks:
- Never change production configuration values.
- Do not run integration tests by default.
