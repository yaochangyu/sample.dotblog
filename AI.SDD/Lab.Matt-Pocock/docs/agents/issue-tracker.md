# Issue Tracker Configuration

## Storage
- Strategy: Local markdown
- Location: `.scratch/<feature>/issues/`
- Spec Location: `.scratch/<feature>/spec.md`

## Format
Each ticket is stored as an individual Markdown file named `<number>-<short-slug>.md`.

Each ticket must contain:
1. Title and Description
2. What to build (Tracer-bullet vertical slice)
3. Blocked by (explicit dependencies or None)
4. Acceptance criteria
