---
name: code-review
description: Code review and refactoring skill for repository changes, pull requests, diffs, patches, or local code when the user or process asks for a review, code review, PR review, refactor, cleanup, simplification, over-engineering check, design critique, architecture review, or maintainability pass. Use to find correctness risks, missing tests, hidden side effects, KISS/DRY/SOLID issues, composition-vs-inheritance problems, dependency-injection misuse, broad interfaces, misplaced code, arbitrary layer/type folders, speculative abstractions, and opportunities to delete or simplify code.
---

# Code Review

Review or refactor code with a bias toward concrete defects, simpler design, and clear ownership. Treat a user or process request for code review or refactoring as permission to use unbiased Explorer subagents when the tool is available.

## Workflow

1. Establish scope from the user request, changed files, diff, PR, issue/spec, and relevant tests. If scope is ambiguous, infer the smallest useful review target from local context.
2. Decide mode:
   - `review`: inspect and report findings. Do not edit files unless asked.
   - `refactor`: inspect, decide changes, edit files, and verify. Keep edits scoped to the requested behavior and findings.
3. Use Explorer subagents when available:
   - For review requests, spawn one or more `explorer` agents with `fork_context: false` so they receive only raw artifacts and do not inherit the host session's conclusions.
   - Ask each Explorer for a specific, non-overlapping focus. Good splits: correctness/tests, KISS/DRY/over-engineering, SOLID/DI/side effects, composition/locality.
   - For small changes, one Explorer is enough. For medium changes, use two. For large or risky changes, use three or four.
   - Pass only the minimal task context: repo path, diff/branch/files, user-visible requirements, and the focus lens. Do not pass your own diagnosis.
   - Continue useful local inspection while Explorers run. In refactor mode, keep that concurrent host work read-only; do not edit or resolve issues from an agent pass until that agent returns. Integrate findings, but do not blindly repeat duplicates.
   - If Explorer tools are unavailable, do the review locally and state that no independent Explorer pass was run.
4. For refactors with multiple agents, run agents sequentially. After each agent returns, the host agent must either fix, explicitly defer, or reject the flagged issues before spawning the next agent. This prevents later agents from converging on already-known issues.
5. Verify with focused tests, type checks, linters, build commands, or smoke checks proportional to the risk. If verification cannot run, say exactly why.

Windows search guardrail: in PowerShell, do not pass file globs such as `*.cs` or `*.md` as positional `rg` paths. Use `rg -g "*.cs" -g "*.md" "pattern" .` so Windows does not treat the globs as invalid literal paths.

## Review Lenses

Findings must be actionable and grounded in file/line references whenever possible.

- `correctness`: broken behavior, bad edge cases, races, invalid assumptions, data loss, migration risk, error handling gaps.
- `tests`: missing coverage for changed contracts, regressions, edge cases, or refactor safety.
- `security`: injection, authz/authn mistakes, secret handling, unsafe deserialization, path traversal, confused deputy risks.
- `performance`: avoidable algorithmic blowups, repeated I/O, chatty remote calls, expensive work in hot paths.
- `kiss`: ask whether the problem has a simpler direct solution. Prefer the smallest design that solves the current requirement.
- `dry`: find meaningful repeated blocks of code, docs, tests, fixtures, assertions, setup, or workflow with only minimal differences. Do not flag harmless two-line repetition that is clearer inline.
- `delete`: dead code, unused options, speculative features, unused compatibility layers. Replacement: nothing.
- `stdlib`: hand-rolled behavior already provided by the language standard library or common framework API. Name the replacement.
- `native`: dependency or custom code doing what the platform, runtime, database, browser, framework, or build system already does.
- `yagni`: abstraction with one real use, configuration nobody sets, layer with one caller, future-proofing without a current requirement.
- `solid`: check that responsibilities are clear, side effects are visible at the highest useful level, invariants are protected, derived classes honor base contracts, interfaces are narrow, dependencies point inward, and orchestration uses the standard dependency-injection mechanism where available.
- `composition`: prefer composition over inheritance when the relationship is really `has-a`, policy, capability, or delegation rather than a stable `is-a` domain relationship.
- `local`: check whether code is placed by behavior and meaning instead of arbitrary component type. Strongly coupled code should live close together. Small single-use records, enums, return types, or interfaces may live in the same file as their only consumer. Larger or reused concepts should live next to related behavior, not in generic `models`, `controllers`, `services`, or similar type buckets.
- `shrink`: same behavior with fewer moving parts or fewer lines.

## SOLID, DI, and YAGNI Precedence

Use top-down design pressure:

- Prefer YAGNI over inventing an interface solely because SOLID says abstractions are useful.
- Still use the project's normal DI container or dependency orchestration for real dependencies, especially side-effecting services, external clients, clocks, storage, messaging, and expensive resources.
- Add an explicit interface when it has a real job: multiple implementations, a stable boundary, plugin/adapter behavior, cross-layer contract, test double need that cannot be handled more simply, or a side-effect boundary worth naming.
- Do not hide side effects in deep components when callers need to understand the write, network, time, random, cache, process, or UI effects. Push orchestration up until the behavior is visible at the right level.
- Reject broad interfaces that force implementers or callers to know about unrelated behavior.
- Validate invariants in base and derived classes. A derived type must not weaken required preconditions, break postconditions, or silently ignore base-class promises.

## Output

For a review, lead with findings ordered by severity and confidence. Keep summaries secondary.

Use this shape:

```text
Findings
- [P1] file:line - Concrete problem and user-visible or maintenance impact. Suggested fix.
- [P2] file:line - Concrete problem and impact. Suggested fix.

Open Questions
- ...

Verification
- ...
```

Severity:

- `P0`: production outage, data loss, critical security issue.
- `P1`: likely bug, broken contract, serious regression, unsafe design that should block merge.
- `P2`: maintainability, test, design, or moderate risk issue that should be fixed soon.
- `P3`: small cleanup, naming, minor locality, or optional simplification.

When the request is specifically for over-engineering, simplification, or "what can we delete", use a tighter ponytail-style line format:

```text
file:line: tag: what to cut or simplify. What replaces it.
net: -N lines possible.
```

Valid tags for that mode: `delete`, `stdlib`, `native`, `yagni`, `kiss`, `dry`, `solid`, `composition`, `local`, `shrink`.

If no issues are found, say `No issues found.` Include residual risk or test gaps if any. For pure simplification review with nothing to cut, say `Lean already. Ship.`

## Refactor Mode

When the user asks for a refactor:

- First identify the target behavior and safety checks.
- Prefer a sequence of small edits over a broad rewrite.
- Preserve public behavior unless the user explicitly asks for behavior changes.
- Keep strongly related code close together even when that means a small type remains in the same file as its only consumer.
- Run or add focused tests when the refactor changes shared behavior, contracts, or side-effect boundaries.
- Final response should state changed files and verification, plus any issues intentionally deferred.

## Boundaries

- Do not flag a single smoke test, assertion, or focused regression test as bloat.
- Do not recommend a design pattern by name unless it materially simplifies the code or protects a real boundary.
- Do not request an interface only because a class exists. Show the current need.
- Do not move code into type-based folders as a default cleanup.
- Do not bury the main issue in prose. Findings come first.
