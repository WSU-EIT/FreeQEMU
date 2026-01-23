# 001 — Roleplay: Discussions and Planning

> **Document ID:** 001  
> **Category:** Guide  
> **Purpose:** Two modes for working with AI: (1) Team discussions for design, (2) Planning checklists for execution.  
> **Audience:** Devs, AI agents, contributors.  
> **Outcome:** 📖 Productive AI-assisted design and planning.

---

## When to Use What

Not everything needs a roleplay. Match the change size to the approach:

| Change Size | Examples | Approach |
|-------------|----------|----------|
| **Tiny** | Fix typo, update comment | Just do it |
| **Small** | Add a field, simple bug fix | Quick AI question |
| **Medium** | New endpoint, UI component | **Planning checklist** |
| **Large/Unclear** | New feature, architecture change | **Full discussion** |

**Rule of thumb:** If you're unsure how to approach it, use Discussion mode first. If you know what to do but want to not miss things, use Planning mode.

---

## Quick Start

| Want to... | Say... | Mode |
|------------|--------|------|
| Explore a design | `"roleplay design [feature]"` | Discussion |
| Review code | `"roleplay review [file/area]"` | Discussion |
| Debug an issue | `"roleplay debug [problem]"` | Discussion |
| Decide between options | `"roleplay decide [topic]"` | Discussion |
| Prepare a PR | `"plan [feature/bug]"` | Planning |
| Spec out work | `"plan [task]"` | Planning |

---

# MODE 1: DISCUSSION

Use Discussion mode to **explore** — when you need multiple perspectives on a problem.

---

## The Team

| Role | Focus | Key Questions |
|------|-------|---------------|
| **[Architect]** | System design, patterns, boundaries | "How does this fit? What's the blast radius?" |
| **[Backend]** | Data, APIs, services, storage | "What's the schema? What endpoints?" |
| **[Frontend]** | UI, components, UX, state | "What's the user flow? Loading states?" |
| **[Quality]** | Tests, security, docs | "How do we test this? What could break?" |
| **[Sanity]** | Reality checks, complexity | "Are we overcomplicating this?" |
| **[JrDev]** | Clarifying questions | "Wait, why are we doing X?" |
| **[CTO]** | **You, the human** | Final decisions |

### Adapt Roles to Your Project

| Project Type | Use These Roles |
|--------------|-----------------|
| Web app | [Frontend], [Backend], [Database], [API] |
| API only | [API], [Database], [Consumer], [Ops] |
| Mobile | [iOS], [Android], [Core], [UX] |
| CLI tool | [Core], [UX], [Ops], [Docs] |
| Library | [PublicAPI], [Internals], [Perf], [Docs] |

---

## Discussion Flow

1. **[Architect]** frames the problem
2. **Specialists** give their perspectives
3. **[Sanity]** mid-check: "Are we overcomplicating?"
4. **Discussion** continues
5. **[Sanity]** final check: "Did we miss anything?"
6. **Summary** with decisions and next steps

### Pause for CTO

Stop and ask the human when:
- Requirements are ambiguous
- Multiple valid approaches exist
- High-impact decision
- Team is split

```markdown
---
⏸️ **CTO Input Needed**

**Question:** {specific question}

**Options:**
1. Option A — {tradeoff}
2. Option B — {tradeoff}

@CTO — Which way?
---
```

---

## Discussion Output Format

See **doc 003 (Templates)** for the full meeting template. Basic structure:

```markdown
# {NUM} — Meeting: {Topic}

> **Document ID:** {NUM}  
> **Category:** Meeting  
> **Purpose:** {what we're deciding}  
> **Predicted Outcome:** {expected result}  
> **Actual Outcome:** {what happened — fill in after}  
> **Resolution:** {action taken — PR, decision, etc.}

---

## Discussion
[transcript]

## Decisions
- Decision 1
- Decision 2

## Next Steps
| Action | Owner | Priority |
|--------|-------|----------|
| Task | [Role] | P1 |
```

---

# MODE 2: PLANNING

Use Planning mode to **execute** — when you know what to do and want to not miss things.

---

## Planning Roles

| Role | Goal | Key Questions |
|------|------|---------------|
| **Requestor** | Define why & what | What problem? What's done look like? Non-goals? |
| **Implementer** | Turn into tasks | What modules touched? Smallest slice? Risks? |
| **Skeptic** | Break it early | Edge cases? Failure modes? Regressions? |
| **Operator** | Deployability | Config needed? Logs/metrics? Rollback? |
| **Doc Keeper** | Keep docs aligned | What docs update? ADR needed? (Y/N) |

---

## Planning Checklist

Copy this into your PR or issue:

```markdown
## Summary
- **Problem:** 
- **Goal:** 
- **Non-goals:** 

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

## Approach
- **Changes:** {files/modules touched}
- **Data impact:** {schema, migrations}
- **Compat notes:** {breaking changes}

## Test Plan
- **Happy path:** 
- **Edge cases:** 
- **Regression:** 

## Ops & Rollout
- **Config/secrets:** 
- **Monitoring:** 
- **Rollback:** 

## Docs
- [ ] Quickstart still works
- [ ] New config documented
- [ ] ADR needed? (Y/N)
```

---

## ADR Mini-Template

For decisions worth recording, add to your PR or create a decision doc:

```markdown
### ADR: {Title}

**Context:** {why we needed to decide}
**Decision:** {what we chose}
**Rationale:** {why this option}
**Consequences:** {what this means}
**Alternatives:** {what we didn't choose}
```

---

# COMBINING MODES

For complex work, use both:

```
1. DISCUSS first  →  Explore the problem, surface unknowns
2. PLAN second    →  Turn decisions into actionable checklist
3. IMPLEMENT      →  Use checklist as your guide
4. REVIEW         →  Roleplay review if needed
```

---

## File Size Limits

These apply to docs AND code:

| Threshold | Lines | Action |
|-----------|-------|--------|
| Target | ≤300 | Ideal |
| Soft max | 500 | Consider splitting |
| Hard max | 600 | Must split or justify |

---

## File Naming Rules

**MANDATORY for all new files:** `{ProjectName}.App.{Feature}.{Extension}`

| Creating... | Name it... |
|-------------|-----------|
| New page | `FreeManager.App.EntityWizard.razor` |
| Code partial | `FreeManager.App.EntityWizard.State.cs` |
| New entity | `FreeManager.App.FMProject.cs` |
| New DTOs | `FreeManager.App.DataObjects.Projects.cs` |
| Base class extension | `DataController.App.FreeManager.cs` |

**Blazor component references:**
```razor
@* File: FreeManager.App.EntityWizard.razor → Class: FreeManager_App_EntityWizard *@
<FreeManager_App_EntityWizard />
```

**Full rules:** See `docs/004_styleguide.md` → "File Organization"

---

## Templates

All templates are in **doc 003 (Templates)** — including:
- Meeting / Discussion
- Feature Design
- Code Review
- Bug Investigation
- Decision Record (ADR)
- Quick Validation
- Planning Checklist

---

*Created: `<DATE>`*  
*Maintained by: [Quality]*
