# 003 — Templates: Ready-to-Use Document Templates

> **Document ID:** 003  
> **Category:** Templates  
> **Purpose:** Copy-paste templates for all common doc types.  
> **Audience:** Anyone creating docs, AI agents.  
> **Outcome:** 📖 Grab the right template, fill it in, done.

---

## Quick Template Selector

| I need to... | Use this template |
|--------------|-------------------|
| Start a design discussion | [Meeting: Design Discussion](#meeting-design-discussion) |
| Review code with the team | [Meeting: Code Review](#meeting-code-review) |
| Debug a problem together | [Meeting: Bug Investigation](#meeting-bug-investigation) |
| Record an architecture decision | [Decision Record (ADR)](#decision-record-adr) |
| Get quick feedback | [Quick Validation](#quick-validation) |
| Spec out a feature | [Feature Design](#feature-design) |
| Prepare a PR | [Planning Checklist](#planning-checklist) |
| Document a procedure | [Runbook](#runbook) |
| Analyze an incident | [Postmortem](#postmortem) |

---

## Header Formats

### For Reference / Guide Docs

```markdown
# {NUM} — {Title}

> **Document ID:** {NUM}  
> **Category:** {Category}  
> **Purpose:** {One line}  
> **Audience:** {Who reads this}  
> **Outcome:** {emoji} {Brief description}

---
```

### For Meeting / Planning Docs

```markdown
# {NUM} — {Title}

> **Document ID:** {NUM}  
> **Category:** {Category}  
> **Purpose:** {One line}  
> **Audience:** {Who reads this}  
> **Predicted Outcome:** {What we expect}  
> **Actual Outcome:** {What happened — update when done}  
> **Resolution:** {Action taken — PR link, decision, next doc}

---
```

---

## Meeting: Design Discussion

Use when: Exploring how to build something new.

```markdown
# {NUM} — Meeting: {Feature/Topic} Design

> **Document ID:** {NUM}  
> **Category:** Meeting  
> **Purpose:** Design discussion for {feature}  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity]  
> **Date:** {YYYY-MM-DD}  
> **Predicted Outcome:** {What we expect to decide}  
> **Actual Outcome:** {Update after meeting}  
> **Resolution:** {Link to PR, follow-up doc, or "No action needed"}

---

## Context

{What problem are we solving? Why now?}

---

## Discussion

**[Architect]:** {Frames the problem, constraints, options}

**[Backend]:** {Data and API perspective}

**[Frontend]:** {UI and UX perspective}

**[Quality]:** {Testing and security concerns}

**[Sanity]:** Mid-check — {Are we overcomplicating this?}

{Continue discussion...}

**[Sanity]:** Final check — {Did we miss anything obvious?}

---

## Decisions

- {Decision 1}
- {Decision 2}

## Open Questions

- {Question for later}

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| {Task} | [Role] | P1 |

---

*Created: {YYYY-MM-DD}*  
*Maintained by: [Quality]*
```

---

## Meeting: Code Review

Use when: Reviewing existing code or a PR.

```markdown
# {NUM} — Review: {File/Component/PR}

> **Document ID:** {NUM}  
> **Category:** Meeting  
> **Purpose:** Code review of {what}  
> **Attendees:** [Author], [Reviewer], [Quality], [Sanity]  
> **Date:** {YYYY-MM-DD}  
> **Predicted Outcome:** {Approval, changes needed, etc.}  
> **Actual Outcome:** {Update after review}  
> **Resolution:** {PR approved, changes requested, etc.}

---

## What We're Reviewing

- **File(s):** {path/to/files}
- **PR:** {link if applicable}
- **Context:** {Why this code exists}

---

## Discussion

**[Author]:** {Explains the approach}

**[Reviewer]:** {Technical feedback}

**[Quality]:** {Test coverage, security concerns}

**[Sanity]:** {Complexity check}

---

## Feedback

### Must Fix
- {Critical issue}

### Should Fix
- {Important but not blocking}

### Consider
- {Nice to have}

### Looks Good
- {What works well}

---

## Verdict

{Approved / Approved with changes / Needs revision}

---

*Created: {YYYY-MM-DD}*  
*Maintained by: [Quality]*
```

---

## Meeting: Bug Investigation

Use when: Debugging a problem together.

```markdown
# {NUM} — Debug: {Bug/Issue Description}

> **Document ID:** {NUM}  
> **Category:** Meeting  
> **Purpose:** Investigate {bug}  
> **Attendees:** [Reporter], [Investigator], [Quality], [Sanity]  
> **Date:** {YYYY-MM-DD}  
> **Predicted Outcome:** Root cause identified  
> **Actual Outcome:** {Update when resolved}  
> **Resolution:** {Fixed in PR #X, workaround documented, etc.}

---

## Symptoms

- **What happens:** {Description}
- **Expected:** {What should happen}
- **Frequency:** {Always, sometimes, rare}
- **Environment:** {Where it occurs}

## Repro Steps

1. {Step 1}
2. {Step 2}
3. {Observe issue}

---

## Investigation

**[Reporter]:** {Initial observations}

**[Investigator]:** {Technical analysis}

**[Quality]:** {What tests exist? What's missing?}

**[Sanity]:** {Any obvious causes we're missing?}

---

## Findings

- **Root cause:** {What's actually wrong}
- **Contributing factors:** {What made it worse}
- **Why not caught:** {Gap in testing/monitoring}

## Fix

- **Approach:** {How we'll fix it}
- **Risk:** {Could this break anything?}
- **Test:** {How we'll verify the fix}

---

*Created: {YYYY-MM-DD}*  
*Maintained by: [Quality]*
```

---

## Decision Record (ADR)

Use when: Making a significant technical decision.

```markdown
# {NUM} — Decision: {Title}

> **Document ID:** {NUM}  
> **Category:** Decision  
> **Purpose:** Record decision on {topic}  
> **Participants:** [Architect], [Backend], [Frontend]  
> **Date:** {YYYY-MM-DD}  
> **Predicted Outcome:** Decision made and documented  
> **Actual Outcome:** ✅ {Option chosen}  
> **Resolution:** {Implemented in PR #X / Superseded by doc Y / Active}

---

## Context

{Why did we need to make this decision? What problem are we solving?}

---

## Options Considered

### Option 1: {Name}

{Description}

**Pros:**
- {Pro 1}
- {Pro 2}

**Cons:**
- {Con 1}
- {Con 2}

### Option 2: {Name}

{Description}

**Pros:**
- {Pro 1}

**Cons:**
- {Con 1}

### Option 3: {Name} (if applicable)

{Description}

---

## Decision

We chose **{Option X}** because {rationale}.

---

## Consequences

**Positive:**
- {Benefit 1}

**Negative:**
- {Trade-off 1}

**Neutral:**
- {Change in how we work}

---

*Decided: {YYYY-MM-DD}*  
*Status: Active / Superseded by {NUM}*
```

---

## ADR Mini-Template (Inline)

Use when: Recording a decision in a PR or meeting doc.

```markdown
### ADR: {Title}

**Context:** {Why we needed to decide}  
**Decision:** {What we chose}  
**Rationale:** {Why this option}  
**Consequences:** {What this means}  
**Alternatives:** {What we didn't choose}
```

---

## Quick Validation

Use when: Getting fast feedback without a full meeting.

```markdown
# {NUM} — Quick Validation: {Topic}

> **Document ID:** {NUM}  
> **Category:** Validation  
> **Purpose:** Quick feedback on {what}  
> **Facilitator:** [Quality]  
> **Date:** {YYYY-MM-DD}  
> **Predicted Outcome:** Feedback collected  
> **Actual Outcome:** {Update when done}  
> **Resolution:** {Changes made / No changes needed / Deferred}

---

## What We're Validating

{2-3 sentences describing what we want feedback on}

---

## Feedback Round

| Persona | Perspective | Feedback |
|---------|-------------|----------|
| **Senior Dev** | Technical depth | {Feedback} |
| **New Dev** | Newcomer experience | {Feedback} |
| **End User** | User perspective | {Feedback} |
| **Skeptic** | Complexity concern | {Feedback} |

---

## Summary

### Works Well ✅
- {Item}

### Needs Improvement ⚠️
- {Item}

### Quick Wins (< 30 min)
| Improvement | Effort |
|-------------|--------|
| {Fix} | {X} min |

### Deferred
- {For later}

---

*Created: {YYYY-MM-DD}*  
*Maintained by: [Quality]*
```

---

## Feature Design

Use when: Speccing out a new feature before building.

```markdown
# {NUM} — Feature: {Feature Name}

> **Document ID:** {NUM}  
> **Category:** Feature  
> **Purpose:** Design spec for {feature}  
> **Audience:** Dev team  
> **Predicted Outcome:** Design approved  
> **Actual Outcome:** {Update when reviewed}  
> **Resolution:** {Approved / Changes needed / Rejected}

---

## Problem

{What problem are we solving? For whom?}

## Solution

{High-level approach}

---

## Changes

### Files to Create/Modify

**New files** (must use `{ProjectName}.App.{Feature}` pattern):
- `FreeManager.App.{Feature}.razor` — {description}
- `FreeManager.App.{Feature}.cs` — {description}
- `FreeManager.App.DataObjects.{Feature}.cs` — {DTOs}

**Modified files**:
- `{existing file}` — {what changes}

### Data/Schema Changes
- {Change 1}

### API Changes
- {Endpoint 1}

### UI Changes
- {Screen/component 1}

---

## Security

- {Consideration 1}
- {Consideration 2}

## Testing

- {Test approach 1}
- {Test approach 2}

## Rollout

- {How we'll deploy this}
- {Feature flags?}

---

## Open Questions

- {Question 1}

---

*Created: {YYYY-MM-DD}*  
*Maintained by: [Role]*
```

---

## Planning Checklist

Use when: Preparing to implement a change.

```markdown
## Summary

- **Problem:** {What's wrong or missing}
- **Goal:** {What done looks like}
- **Non-goals:** {What we're NOT doing}

## Acceptance Criteria

- [ ] {Criterion 1}
- [ ] {Criterion 2}
- [ ] {Criterion 3}

## Approach

- **New files:** (use `{ProjectName}.App.{Feature}` pattern)
  - {file 1}
  - {file 2}
- **Modified files:** {existing files to change}
- **Data impact:** {Schema changes, migrations}
- **Compat notes:** {Breaking changes, versioning}

## Test Plan

- **Happy path:** {Main flow}
- **Edge cases:** {What could break}
- **Regression:** {What else might break}

## Ops & Rollout

- **Config/secrets:** {New keys needed}
- **Monitoring:** {Logs, metrics, alerts}
- **Rollback:** {How to undo}

## Docs

- [ ] Quickstart still works
- [ ] New config documented  
- [ ] ADR needed? (Y/N)
- [ ] Other docs to update: {list}

## File Naming Checklist

- [ ] All new files use `{ProjectName}.App.{Feature}.{Extension}` pattern
- [ ] Blazor components reference class names correctly (dots → underscores)
- [ ] No `FM` prefix on new files (use full project name)
```

---

## Runbook

Use when: Documenting an operational procedure.

```markdown
# {NUM} — Runbook: {Procedure Name}

> **Document ID:** {NUM}  
> **Category:** Runbook  
> **Purpose:** How to {do thing}  
> **Audience:** Ops, on-call  
> **Outcome:** 📖 Step-by-step procedure

---

## When to Use

{Conditions that trigger this runbook}

## Prerequisites

- {What you need before starting}
- {Access, tools, etc.}

---

## Steps

### 1. {First Step}

```bash
{command if applicable}
```

{Explanation}

### 2. {Second Step}

{Details}

### 3. {Third Step}

{Details}

---

## Verification

- [ ] {How to confirm it worked}
- [ ] {What to check}

## Rollback

If something goes wrong:

1. {Rollback step 1}
2. {Rollback step 2}

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| {Problem} | {Why} | {Solution} |

---

*Created: {YYYY-MM-DD}*  
*Maintained by: [Ops]*
```

---

## Postmortem

Use when: Analyzing an incident after it's resolved.

```markdown
# {NUM} — Postmortem: {Incident Title}

> **Document ID:** {NUM}  
> **Category:** Postmortem  
> **Purpose:** Analysis of {incident}  
> **Audience:** Team  
> **Predicted Outcome:** Root cause identified, fixes planned  
> **Actual Outcome:** {Update when complete}  
> **Resolution:** {Fixes implemented in PR #X, process changes, etc.}

---

## Summary

- **What happened:** {Brief description}
- **Duration:** {Start} to {End}
- **Impact:** {Who/what was affected}
- **Severity:** {High/Medium/Low}

---

## Timeline

| Time | Event |
|------|-------|
| {HH:MM} | {Event 1} |
| {HH:MM} | {Event 2} |
| {HH:MM} | {Resolution} |

---

## Root Cause

{What actually caused the issue}

## Contributing Factors

- {Factor 1}
- {Factor 2}

---

## What Went Well

- {Thing 1}
- {Thing 2}

## What Went Wrong

- {Thing 1}
- {Thing 2}

---

## Action Items

| Action | Owner | Status | Due |
|--------|-------|--------|-----|
| {Fix 1} | [Role] | {Status} | {Date} |
| {Fix 2} | [Role] | {Status} | {Date} |

---

## Lessons Learned

- {Key takeaway 1}
- {Key takeaway 2}

---

*Created: {YYYY-MM-DD}*  
*Maintained by: [Quality]*
```

---

## Tips for Using Templates

1. **Copy the whole template** — Don't skip sections
2. **Fill in placeholders** — Look for `{curly braces}`
3. **Update Actual Outcome** — When work completes
4. **Add Resolution** — Link to PRs, follow-up docs
5. **Delete unused sections** — After filling in, remove what's N/A

---

*Created: `<DATE>`*  
*Maintained by: [Quality]*
