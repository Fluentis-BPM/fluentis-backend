---
description: Run one backend test with filter fallback.
---

Run a single test target: $ARGUMENTS

Try exact first:
!`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName=$ARGUMENTS"`

If no tests are found, try name fragment:
!`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "Name~$ARGUMENTS"`
