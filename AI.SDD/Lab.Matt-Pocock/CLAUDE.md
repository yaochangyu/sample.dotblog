# Project Guidelines

## Agent skills

### Issue Tracker
- Config: `docs/agents/issue-tracker.md`
- Tracker mode: Local markdown (`.scratch/<feature>/issues/`)
- Spec location: `.scratch/<feature>/spec.md`

### Domain Docs
- Config: `docs/agents/domain.md`
- Glossary: `GLOSSARY.md`
- ADRs: `docs/adr/`

### Architectural Guidelines
- Follow `codebase-design` principles: Deep modules, single entry point / seam as testing surface (`calculateDiscount`).
- Follow TDD red-green cycle for feature implementation.
- Dual-axis code review on Standards and Spec.
