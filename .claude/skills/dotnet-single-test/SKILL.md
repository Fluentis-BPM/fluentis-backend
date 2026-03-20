---
name: dotnet-single-test
description: Run a single test, class, or method fragment with deterministic xUnit filters.
allowed-tools: Bash, Read, Grep, Glob
---

Use for tight test-debug loops.

Modes:
- Exact test: `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName=$ARGUMENTS"`
- Class scope: `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName~$ARGUMENTS"`
- Name fragment: `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "Name~$ARGUMENTS"`

Try exact mode first, fallback to fragment when no tests are discovered.
