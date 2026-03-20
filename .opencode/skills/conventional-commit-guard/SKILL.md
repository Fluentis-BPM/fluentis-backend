---
name: conventional-commit-guard
description: Enforce commit message format and hook compatibility for this repository.
---

Use this skill before creating commits.

Rules from repo hooks:
- Conventional Commit types allowed: `build`, `feat`, `ci`, `chore`, `docs`, `fix`, `perf`, `refactor`, `revert`, `style`, `test`.
- Header length must be 10-100 characters.

Recommended process:
1. Review scope with `git status` and `git diff`.
2. Draft message in `<type>(<scope>): <subject>` format.
3. Keep message intent-focused and concise.

Examples:
- `fix(auth): fallback to preferred_username when email claim is missing`
- `test(workflow): add regression for visualizer role merge behavior`

Guardrails:
- Do not include secrets in commit content or message.
- Do not use amend unless explicitly requested.
