# 000 — Quickstart: Get Running Locally

> **Document ID:** 000  
> **Category:** Quickstart  
> **Purpose:** Get a new dev from zero → running locally, plus AI assistant commands.  
> **Audience:** Devs, contributors, AI agents.  
> **Outcome:** ✅ Working local run + AI ready to assist.

---

# 🤖 AI AGENT COMMANDS

**Match the user's request:**

| User Says | Action |
|-----------|--------|
| `"sitrep"` / `"status"` | → Run SITREP (see below) |
| `"explore"` / `"deep dive"` | → Run EXPLORE (see below) |
| `"roleplay [topic]"` | → Discussion mode (see doc 001) |
| `"plan [feature/bug]"` | → Planning mode (see doc 001) |
| `"build"` / `"test"` | → Run command, report results |
| `"menu"` / `"help"` | → Show command table |
| *(anything else)* | → Run STARTUP first, then address |

---

## AI Startup

**Do this at the start of every conversation:**

1. **READ IN FULL:** `docs/000_quickstart.md` (this file)
2. **READ IN FULL:** `docs/001_roleplay.md` (discussion + planning)
3. **READ IN FULL:** `docs/002_docsguide.md` (standards)
4. **SKIM:** `docs/003_templates.md` (grab templates as needed)
5. **SCAN:** Any other docs — read headers to understand purpose

**Confirm:**
```
✓ Startup complete. 
  Read: 000, 001, 002
  Skimmed: 003 (templates)
  Scanned: [X] other docs
  Ready to: [user's request]
```

### Reading Modes

| Instruction | Meaning |
|-------------|---------|
| **READ IN FULL** | Every line, don't skip |
| **SKIM** | Get the gist: topic, decisions, timeline |
| **SCAN** | Headers only, note what exists |

---

## Sitrep Format

When user says "sitrep" / "status":

```
## Sitrep: <PROJECT_NAME>

**As of:** [date]
**Purpose:** [one-liner from project]

**Current:** [from tracker doc or "no active sprint"]
- Task 1: status
- Task 2: status

**Recent:** [last completed work]
**Blocked:** [anything stuck]

Commands: `build` · `test` · `explore` · `plan [thing]`
```

---

## Explore Sequence

When user says "explore" / "deep dive":

1. **READ IN FULL:** All docs in `docs/` folder
2. **SCAN:** Project files (`.csproj`, `package.json`, etc.)
3. **READ:** Main entry point (`Program.cs`, `main.py`, etc.)
4. **SAMPLE:** One model, one endpoint, one UI component
5. **OUTPUT:** Summary of architecture, tech, and current state

---

# 👤 HUMAN: START HERE

---

## What is This Project?

**Name:** FreeManager (FreeCRM Example Extension)
**One-liner:** Example project extending FreeCRM + comprehensive FreeCRM documentation
**Stack:** Blazor + C# + .NET 10
**Fork-friendly:** Keep doc structure, swap toolchain commands.

---

## FreeCRM Ecosystem

This project is part of the FreeCRM ecosystem:

| Project | Status | Description |
|---------|--------|-------------|
| **FreeCRM-main** | Public | Base template — authoritative source for all patterns |
| **FreeCICD** | Public | Example extension — community-contributed CI/CD tooling |
| **FreeManager** | Public | Example extension — demonstrates extending FreeCRM + houses docs |
| nForm, Helpdesk4, etc. | Private | Production implementations — referenced but not accessible |

**Namespace Note:** FreeManager uses the original FreeCRM namespace. This is intentional — demonstrates you can fork and extend without renaming. Only rename if you need multiple FreeCRM projects in one solution.

---

## ⚠️ MANDATORY: File Naming Convention

**All new/custom files MUST use this pattern:**

```
{ProjectName}.App.{Feature}.{OptionalSubFeature}.{Extension}
```

| Type | Example |
|------|---------|
| New page | `FreeManager.App.EntityWizard.razor` |
| Partial file | `FreeManager.App.EntityWizard.State.cs` |
| New entity | `FreeManager.App.FMProject.cs` |
| New DTOs | `FreeManager.App.DataObjects.Projects.cs` |
| Base extension | `DataController.App.FreeManager.cs` |

**Why this matters:**
- Find all your code instantly: `find . -name "FreeManager.App.*"`
- Safe during FreeCRM framework updates
- Clear separation of base vs custom

**Blazor components:** Dots become underscores in class names:
- File: `FreeManager.App.EntityWizard.razor`
- Class: `FreeManager_App_EntityWizard`
- Usage: `<FreeManager_App_EntityWizard />`

**Full details:** See `docs/004_styleguide.md` → "File Organization"

---

## Prerequisites

| Required | Notes |
|----------|-------|
| Git | Latest |
| SDK | Version in `global.json` or repo config |
| IDE | VS / Rider / VS Code |

| Optional | When Needed |
|----------|-------------|
| Docker | Running dependencies locally |
| Node.js | Building web assets |
| `<OTHER>` | `<REASON>` |

---

## Setup

```bash
git clone <REPO_URL>
cd <REPO_FOLDER>
dotnet restore   # or: npm install, pip install -r requirements.txt
dotnet build     # or: npm run build, etc.
```

### Run Tests First

```bash
dotnet test      # or: npm test, pytest, etc.
```

---

## Running Locally

### Single App

```bash
dotnet run --project src/<AppProject>
```

### Web + API (Two Terminals)

```bash
# Terminal 1 - API
dotnet run --project src/<ApiProject>

# Terminal 2 - Web
dotnet run --project src/<WebProject>
```

### Smoke Check

- [ ] App loads in browser
- [ ] Health endpoint responds
- [ ] Basic flow works

---

## Configuration

### Local Dev (User Secrets)

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<VALUE>"
dotnet user-secrets set "Auth:Key" "<VALUE>"
dotnet user-secrets set "<KEY>" "<VALUE>"
```

### Production (Environment Variables)

```
ConnectionStrings__Default=...
Auth__Key=...
```

---

## Common Commands

| Task | Command |
|------|---------|
| Build | `dotnet build` |
| Test | `dotnet test` |
| Format | `dotnet format` |
| Add Migration | `dotnet ef migrations add <Name> --project src/<DataProject>` |
| Update DB | `dotnet ef database update --project src/<DataProject>` |

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Won't build | Check SDK version matches `global.json`; run `dotnet clean && dotnet build` |
| Config missing | Re-check user-secrets naming; verify `appsettings.Development.json` |
| Port in use | Change in `launchSettings.json` or kill process |

---

## Next Steps

1. **Read** docs 001-002 for team patterns and standards
2. **Run** `sitrep` to see current state
3. **Use** `plan [feature]` before starting work

---

*Created: `<DATE>`*  
*Maintained by: [Quality]*
