---
description: Run CI-equivalent backend validation.
agent: build
---
Run a strict CI-style validation for this repository and report pass/fail by step.

Execute exactly:
!`dotnet tool restore`
!`dotnet restore FluentisCore.sln`
!`dotnet build FluentisCore.sln --no-restore --configuration Release`
!`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --no-build --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`
!`dotnet ef migrations list --project FluentisCore/FluentisCore.csproj --startup-project FluentisCore/FluentisCore.csproj`

If any step fails, stop and provide the most likely root cause and the minimal next fix.
