---
description: Run CI-equivalent backend checks.
---

Run backend preflight checks and summarize pass/fail per step:

!`dotnet tool restore`
!`dotnet restore FluentisCore.sln`
!`dotnet build FluentisCore.sln --no-restore --configuration Release`
!`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --no-build --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`
!`dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`
