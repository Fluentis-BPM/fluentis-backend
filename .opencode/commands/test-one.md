---
description: Run one test by FullyQualifiedName.
agent: build
---
Run a single backend test using this exact filter:

`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "FullyQualifiedName=$ARGUMENTS"`

If no test is found, fallback once to:

`dotnet test FluentisCore.Tests/FluentisCore.Tests.csproj --filter "Name~$ARGUMENTS"`

Then summarize: discovered tests, pass/fail, and likely fix direction.
