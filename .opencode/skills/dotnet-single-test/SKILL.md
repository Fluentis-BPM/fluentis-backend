---
name: dotnet-single-test
description: Execute a single xUnit test, class, or method fragment with deterministic filters.
---

Use this skill for tight feedback loops while debugging tests.

Supported filter modes:
- Fully qualified test: `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName=$ARGUMENTS"`
- Class scope: `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName~$ARGUMENTS"`
- Method fragment: `dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "Name~$ARGUMENTS"`

Behavior:
- If argument looks like a fully-qualified method path, run exact mode first.
- If exact mode returns no tests, fallback to class scope.
- Print summary with discovered tests and failures.

Safety checks:
- Keep changes minimal and targeted to the failing behavior.
- Avoid broad refactors during single-test triage unless explicitly requested.
