---
name: conventional-commit-guard
description: Enforce Conventional Commit format and husky-compatible commit headers.
allowed-tools: Read, Grep, Glob, Bash
disable-model-invocation: true
---

Use manually before committing.

Rules:
- Allowed types: build, feat, ci, chore, docs, fix, perf, refactor, revert, style, test
- Header length: 10-100 characters

Suggested format:
- `<type>(<scope>): <subject>`
