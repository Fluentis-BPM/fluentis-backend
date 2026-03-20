---
description: Triage and fix a failing test.
agent: dotnet-test-triage
subtask: true
---
Triage and fix this failing test target: $ARGUMENTS

Workflow:
1. Run targeted test first.
2. Identify root cause in implementation or test setup.
3. Apply minimal fix.
4. Rerun targeted test.
5. Run non-integration CI-like test filter.

Prefer preserving API/DTO contracts unless explicitly approved.
