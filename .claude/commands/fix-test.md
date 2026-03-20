---
description: Triage and fix failing backend tests.
---

Use `dotnet-test-triage` subagent for target: $ARGUMENTS

Expected flow:
1. Reproduce with targeted test command.
2. Identify root cause.
3. Apply minimal fix.
4. Re-run targeted test.
5. Re-run CI-like non-integration tests.
