---
name: dotnet-ci-parity
description: Run CI-equivalent validation for this backend repo.
allowed-tools: Bash, Read, Grep, Glob
---

Use this skill for branch health checks and pre-PR verification.

Run in this order:
1. `dotnet tool restore`
2. `dotnet restore FluentisCore.sln`
3. `dotnet build FluentisCore.sln --no-restore --configuration Release`
4. `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --no-build --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`
5. `dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`

Return pass/fail per step and next action for failures.
