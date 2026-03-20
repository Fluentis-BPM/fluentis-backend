---
description: Execute a backend feature workflow end-to-end.
agent: build
---
Implement this backend feature request using repo conventions: $ARGUMENTS

Required workflow:
1. Analyze current controller/service/model/DTO structure.
2. Implement minimal changes in controller + service + DTO as needed.
3. Add or update tests (targeted first).
4. If persistence changed, run migration discipline checks.
5. Run CI-like verification (exclude integration tests).

Verification commands:
!`dotnet build FluentisCore.sln --no-restore --configuration Release`
!`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --configuration Release --filter "FullyQualifiedName!~IntegrationTests"`

Output:
- Changed files
- Why each change was needed
- Any migration/API contract risk
