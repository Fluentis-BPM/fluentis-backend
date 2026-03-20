---
description: Run tests by class name fragment.
agent: build
---
Run class-scoped tests using:

`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName~$ARGUMENTS"`

Summarize failing tests and propose minimal targeted edits only.
