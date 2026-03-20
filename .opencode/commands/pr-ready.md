---
description: Run pre-PR checks and generate concise readiness report.
agent: pr-readiness-auditor
subtask: true
---
Assess whether this branch is PR-ready for this backend repository.

Must verify:
1. Build in Release succeeds.
2. CI-like non-integration tests succeed.
3. Migration posture is clear.
4. API/DTO contract risk is called out.
5. Commit messages align with Conventional Commits.

Return:
- Ready / Not Ready
- Blocking items
- Exact next commands
