---
inclusion: always
---

# Minimal Comment Policy

## Core Principle

Write self-documenting code and keep comments to a minimum. Comments are not required by default. Prefer clear naming, simple control flow, and well-structured functions over explanatory comments.

Add a comment only when it provides meaningful information that cannot be easily inferred from the code.

## Allowed Comments

Comments should be limited to the following categories:

1. **Non-obvious behavior:** Explain complex algorithms, subtle implementation details, unexpected language behavior, workarounds, or non-trivial decisions.
2. **Critical context:** Document important invariants, external constraints, security considerations, memory ownership, concurrency requirements, or other critical information that is not evident from the implementation.
3. **TODO/FIXME:** Identify incomplete work, known defects, or planned improvements. Be specific and actionable.
4. **Required documentation:** Include API documentation or public interface comments only when required by the language, project conventions, tooling, or the need to communicate a non-obvious contract.

## Prohibited Comment Patterns

Do not add comments that:

- Restate what the code already expresses.
- Describe straightforward operations, conditions, loops, assignments, or function calls.
- Translate code into natural language line by line.
- Explain implementation details that are already obvious from meaningful names and structure.
- Add section headers or decorative separators without a clear organizational need.
- Describe code that can be made clearer through better naming, simpler logic, or a small refactoring.
- Duplicate information already available in documentation, type definitions, tests, or function signatures.

## TODO/FIXME Conventions

- Use `TODO` for pending work and planned improvements.
- Use `FIXME` for known defects or incorrect behavior.
- State the required action or unresolved problem concisely.
- Include an issue identifier when one is available.
- Do not add speculative TODOs for hypothetical future requirements.

Examples:

```c
// TODO: Replace the temporary parser with the production implementation.
// FIXME: Handle integer overflow before emitting the instruction.
```

## Comment Quality

Every comment must justify its existence by adding context that the code alone does not provide.

Prefer explaining **why** an unusual implementation is necessary rather than **what** the implementation does.

Keep comments concise, accurate, and close to the relevant code.

Do not add comments merely to make the code appear more documented.

## Code Generation and Refactoring

When creating or modifying code:

- Do not add comments by default.
- Preserve existing comments that still provide meaningful information.
- Remove redundant, misleading, or outdated comments when their removal is safe.
- Do not remove meaningful TODOs, FIXMEs, critical explanations, or required API documentation.
- Do not introduce comments solely to describe changes made during a refactoring.
- When a comment becomes unnecessary because the code has been improved, prefer removing the comment.

## Final Review

Before completing a coding task, review every newly added or modified comment.

For each comment, ask:

1. Does it explain something non-obvious or critically important?
2. Is this information unavailable from the code, naming, types, tests, or existing documentation?
3. Would removing it make the code harder to understand or maintain?

If the comment does not pass this review, remove it.
