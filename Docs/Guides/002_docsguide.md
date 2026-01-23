# 002 — Docs Guide: Writing and Organizing Documentation

> **Document ID:** 002
> **Category:** Guide
> **Purpose:** How we name, number, format, and maintain docs.
> **Audience:** Anyone writing docs.
> **Outcome:** 📖 Consistent, discoverable, maintainable documentation.

**Scope:** This documentation covers FreeCRM patterns and conventions. While housed in FreeManager, these docs serve the entire FreeCRM ecosystem.

---

## Principles

1. **Numbered chronologically** — Each doc gets the next available number
2. **Categorized by name** — Filename tells you the type
3. **Self-contained** — Each doc stands alone
4. **Versioned with code** — Docs live in Git
5. **Docs as part of done** — PRs include doc updates

---

## Folder Structure

```
<project>/
├── docs/
│   ├── 000_quickstart.md       # Getting started + AI commands
│   ├── 001_roleplay.md         # Discussion + Planning modes
│   ├── 002_docsguide.md        # Standards (this file)
│   ├── 003_templates.md        # All templates
│   ├── 004_feature_xyz.md      # Feature design
│   ├── 005_meeting_abc.md      # Meeting notes
│   └── ...
├── docs/archive/               # Old docs (keep numbers)
└── README.md
```

---

## Naming Convention

```
{NUM}_{CATEGORY}_{TOPIC}.md
```

| Part | Rule |
|------|------|
| **NUM** | 3-digit, next available (000, 001, 002...) |
| **CATEGORY** | Doc type (see below) |
| **TOPIC** | Main subject, underscores for spaces |

### Get Next Number

```bash
ls docs/*.md | sort -r | head -1
# Shows 005_... → next is 006
```

---

## Categories

| Category | Use For | Example |
|----------|---------|---------|
| `quickstart` | Getting started | `000_quickstart.md` |
| `guide` | How-to, standards | `001_roleplay.md` |
| `templates` | Reusable templates | `003_templates.md` |
| `feature` | Feature designs | `004_feature_auth.md` |
| `meeting` | Discussion notes | `005_meeting_api.md` |
| `decision` | ADRs | `006_decision_db.md` |
| `runbook` | Ops procedures | `007_runbook_deploy.md` |
| `postmortem` | Incident analysis | `008_postmortem_outage.md` |
| `reference` | Technical details | `009_reference_arch.md` |

---

## Header Format

Every doc starts with a header. Use the right format for the doc type:

### Reference / Guide Docs (Single Outcome)

```markdown
# {NUM} — {Title}

> **Document ID:** {NUM}  
> **Category:** {Category}  
> **Purpose:** {One line}  
> **Audience:** {Who reads this}  
> **Outcome:** {Status + brief description}

---
```

### Meeting / Planning Docs (Predicted + Actual + Resolution)

```markdown
# {NUM} — {Title}

> **Document ID:** {NUM}  
> **Category:** {Category}  
> **Purpose:** {One line}  
> **Audience:** {Who reads this}  
> **Predicted Outcome:** {What we expected}  
> **Actual Outcome:** {What happened — update when done}  
> **Resolution:** {Action taken — PR link, decision, etc.}

---
```

### Outcome Prefixes

| Emoji | Meaning |
|-------|---------|
| ✅ | Complete/success |
| ⚠️ | Partial/needs follow-up |
| ❌ | Failed/blocked |
| 📋 | Informational |
| 🔄 | In progress |
| 📖 | Reference doc |

### Resolution Examples

| Resolution | Meaning |
|------------|---------|
| `Implemented in PR #123` | Code was merged |
| `Decided against — see doc 045` | We chose not to do it |
| `Deferred to backlog` | Postponed |
| `No action needed` | Informational only |
| `Superseded by doc 067` | Replaced by newer doc |

---

## Document Footer

Every doc ends with:

```markdown
---

*Created: YYYY-MM-DD*  
*Maintained by: [Role]*
```

---

## File Size Limits

| Threshold | Lines | Action |
|-----------|-------|--------|
| Target | ≤300 | Ideal |
| Soft max | 500 | Consider splitting |
| Hard max | 600 | Must split or justify |

If a doc grows too long, split it into focused pieces.

---

## Docs as Part of Done

**Every PR that changes behavior should update docs.**

### PR Checklist

- [ ] Quickstart still works (or updated)
- [ ] New config keys documented
- [ ] New behavior has an example
- [ ] ADR added for meaningful decisions
- [ ] Runbook updated if ops changed

---

## Writing Rules

### Keep It Short
- Target "fits on one screen"
- Split when it gets long

### Keep It Structured
- Use consistent headings
- Use tables for comparisons
- Use code blocks for commands

### Keep Placeholders Obvious
- Use `<PLACEHOLDER>` or `{PLACEHOLDER}` style
- Be consistent within a doc

### Keep It Current
- Update "Actual Outcome" when work completes
- Fill in "Resolution" with links to PRs/docs
- Archive obsolete docs (don't delete — move to `archive/`)

---

## Templates

All templates are in **doc 003 (Templates)**.

Available templates:
- Document headers (both formats)
- Meeting / Discussion
- Feature Design
- Decision Record (ADR)
- Quick Validation
- Runbook
- Postmortem
- Planning Checklist

---

## Quick Reference

### Create a Doc

1. Find next number: `ls docs/*.md | sort -r | head -1`
2. Create: `{NUM}_{category}_{topic}.md`
3. Add header + content + footer
4. Commit: `git commit -m "docs: add {NUM} {topic}"`

### Archive a Doc

1. Move to `docs/archive/`
2. Keep the number (no renumbering)
3. Update cross-references

### Remember

- Lower numbers = older/foundational
- Higher numbers = newer/recent
- Gaps are fine (don't renumber)

---

*Created: `<DATE>`*  
*Maintained by: [Quality]*
