# FreeCRM: Code Style Guide

> Comprehensive coding conventions for C# and Razor in FreeCRM-based projects.

**Authoritative Sources (in order of precedence):**
1. **FreeCRM-main** - Base template all projects derive from
2. **nForm** - Built from scratch 2025, cleanest implementation
3. **TrusselBuilder** - Large project (migrated across versions)
4. **Helpdesk4** - Large project (migrated across versions)

When in doubt, reference FreeCRM-main first, then nForm for modern patterns.

This guide is consolidated from analysis of the authoritative projects above, plus 30+ additional FreeCRM-based projects including SSO, Tasks, DependencyManager, TouchpointsCRM, and WSAF.

---

## Quick Reference

| Topic | Rule |
|-------|------|
| Opening braces (classes/methods) | New line |
| Opening braces (if/for/while) | Same line |
| Variable declarations | Explicit type + `new()` |
| Private fields | `_camelCase` prefix |
| Local variables | `camelCase` (no prefix) |
| String building | Interpolation `$""` |
| Null checks | Explicit `if (x == null)` |
| LINQ | Fluent method syntax |
| Async methods | No `Async` suffix |
| File size | 0-300 ideal, 600 max |
| Partial file naming | `{Class}.App.{App}.{Feature}.cs` |
| Project-specific files | `{ProjectName}.App.{Feature}.cs` |
| Section separators | 76 chars (file), 60 chars (section) |

---

## EditorConfig

All authoritative projects share an identical `.editorconfig`. Copy from nForm, TrusselBuilder, or FreeCRM-main.

### Key Settings

```ini
# Core
indent_size = 4
indent_style = space
end_of_line = crlf
insert_final_newline = false

# Braces - new line for types/methods only
csharp_new_line_before_open_brace = types,methods
csharp_new_line_before_catch = false
csharp_new_line_before_else = false
csharp_new_line_before_finally = false

# var preferences - prefer explicit types
csharp_style_var_elsewhere = false
csharp_style_var_for_built_in_types = false
csharp_style_var_when_type_is_apparent = false

# Expression-bodied members
csharp_style_expression_bodied_properties = true:silent
csharp_style_expression_bodied_accessors = true:silent
csharp_style_expression_bodied_methods = false:silent
csharp_style_expression_bodied_constructors = false:silent

# Naming
dotnet_naming_rule.interface_should_be_begins_with_i.severity = suggestion
dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
```

Full `.editorconfig` files are 267 lines. See the FreeCRM-main project for the canonical `.editorconfig`.

**Source:** FreeCRM base template (public), private repos nForm and TrusselBuilder follow the same config.

---

## General Principles

1. **Prefer simplicity** — Straightforward code over clever abstractions
2. **Null safety** — Always handle nullable values with helper methods
3. **Consistent naming** — Same patterns across all projects
4. **Partial classes** — Split large files by domain (Users, Settings, etc.)
5. **Interface first** — Define interface methods alongside implementations

---

# Part 1: C# Conventions

---

## Namespace Declaration

Use file-scoped namespaces (no braces):

```csharp
// ✅ Correct
namespace HelpDesk;

public partial class DataAccess
{
    // ...
}

// ❌ Avoid
namespace HelpDesk
{
    public partial class DataAccess
    {
        // ...
    }
}
```

---

## Using Statements

- Keep using statements at the top of the file
- Order: System namespaces, then third-party, then project-specific
- Remove unused usings

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.Server.Controllers;
```

---

## Braces

### Opening Brace Placement

**Classes, methods, namespaces:** new line
```csharp
public class MyService
{
    public void DoSomething()
    {
    }
}
```

**Control statements (if, for, while, foreach, switch, try):** same line
```csharp
if (condition) {
    Execute();
}

for (int i = 0; i < 10; i++) {
    Process(i);
}

foreach (var item in items) {
    Process(item);
}
```

### Single-Statement Braces

Always use braces, except for early guard clauses:
```csharp
public void Process(User user)
{
    // Guard clauses: single-line, no braces
    if (user == null) throw new ArgumentNullException(nameof(user));
    if (!user.IsAuthorized) return;

    // Regular control flow: always braces
    if (condition) {
        DoSomething();
    }
}
```

### Return Statements

Prefer single return at end. Early returns only for guard clauses:
```csharp
// GOOD
public string GetDisplayName(User user)
{
    if (user == null) throw new ArgumentNullException(nameof(user));
    
    string result = String.Empty;
    
    if (user.HasNickname) {
        result = user.Nickname;
    } else {
        result = user.FullName;
    }
    
    return result;
}
```

---

## Type Declarations

### var vs Explicit Types

**New code** should use explicit types with target-typed `new()`:
```csharp
// PREFERRED for new code
List<string> names = new();
Dictionary<int, User> userCache = new();
StringBuilder sb = new();
string name = GetName();
int count = items.Count;
```

**Legacy pattern** - acceptable, no need to refactor existing code:
```csharp
// ACCEPTABLE (existing code)
var names = new List<string>();
var user = GetUser();
```

Both patterns are valid. Don't refactor working `var` code, but write new code with explicit types.

`var` always acceptable for iteration:
```csharp
foreach (var item in items) {
    Process(item);
}
```

---

## Naming Conventions

### Complete Reference Table

| Element | Convention | Example |
|---------|------------|---------|
| **Namespaces** | File-scoped, PascalCase | `namespace nForm.Server.Controllers;` |
| **Classes** | PascalCase | `DataController`, `ConfigurationHelper` |
| **Interfaces** | I + PascalCase | `IDataAccess`, `IConfigurationHelper` |
| **Methods** | PascalCase | `DeleteUser()`, `GetUserFromToken()` |
| **Properties** | PascalCase | `TenantId`, `CurrentUser`, `Enabled` |
| **Private Fields** | _camelCase | `_connectionString`, `_fingerprint` |
| **DI Service Fields** | camelCase (no underscore) | `da`, `context`, `configurationHelper` |
| **Protected Fields** | _camelCase | `_loading`, `_loadedData` |
| **Local Variables** | camelCase | `output`, `tenantId`, `optionsBuilder` |
| **Constructor DI Params** | camelCase | `daInjection`, `httpContextAccessor`, `auth` |
| **Method Parameters** | PascalCase | `UserId`, `TenantId`, `CurrentUser` |
| **Hub Classes** | camelCase (exception) | `nFormhub`, `crmHub`, `trusselHub` |

### Private Fields

Two patterns based on origin:

**DI-injected services** - keep constructor param name, no underscore:
```csharp
public partial class DataController : ControllerBase
{
    private HttpContext? context;
    private IDataAccess da;
    private IConfigurationHelper configurationHelper;
}
```

**Special case** - `CurrentUser` uses PascalCase (mirrors ASP.NET Identity conventions):
```csharp
private DataObjects.User CurrentUser;  // Like HttpContext.User
```

**Other private fields** - underscore prefix:
```csharp
public partial class DataController : ControllerBase
{
    private readonly IHubContext<nFormhub>? _signalR;
    private string _fingerprint = "";
    private string _returnCodeAccessDenied = "{{AccessDenied}}";
}
```

### Protected Fields

Underscore prefix:
```csharp
public class BaseComponent
{
    protected bool _loadedData = false;
    protected string _pageName = "home";
}
```

### Local Variables

camelCase, no prefix:
```csharp
public void ProcessOrder(Order order)
{
    OrderValidator validator = new();
    DateTime now = DateTime.UtcNow;
    bool newRecord = false;
}
```

### Parameter Naming (Important Distinction)

**Constructor DI parameters** - camelCase:
```csharp
// ✅ Correct
public DataController(IDataAccess daInjection,
    IHttpContextAccessor httpContextAccessor,
    ICustomAuthentication auth,
    IConfigurationHelper configHelper)
```

**Regular method parameters** - PascalCase (differs from standard .NET):
```csharp
// ✅ Correct - FreeCRM convention
public async Task<User> GetUser(Guid UserId, User? CurrentUser = null)
public async Task<BooleanResponse> DeleteUser(Guid UserId, bool ForceDeleteImmediately = false)
private string HeaderValue(String ValueName)

// ❌ Avoid - standard .NET but not our convention for methods
public async Task<User> GetUser(Guid userId, User? currentUser = null)
```

### Hub Class Naming Exception

SignalR hub classes use camelCase (exception to PascalCase rule):
```csharp
// ✅ Correct
public class nFormhub : Hub { }
public class crmHub : Hub<IsrHub> { }
public class trusselHub : Hub { }

// ❌ Avoid
public class NFormHub : Hub { }
```

---

## Properties

Auto-properties for simple get/set:
```csharp
public string Name { get; set; }
public int Count { get; private set; }
```

Expression-bodied for computed:
```csharp
public string FullName => $"{FirstName} {LastName}";
public bool IsValid => Items.Count > 0;
```

---

## Methods

### Expression-Bodied vs Block Body

Expression-bodied if it fits one line:
```csharp
public string GetName() => _name;
public bool IsValid() => _count > 0;
public void Notify() => _eventBus.Publish(this);
```

Block body if it would wrap:
```csharp
public string GetFullDisplayName()
{
    return $"{FirstName} {MiddleName} {LastName}".Trim();
}
```

---

## Strings

Use interpolation as default:
```csharp
string message = $"Hello, {name}! You have {count} items.";
string path = $"{baseDir}/{folder}/{filename}";
```

Use `String.IsNullOrEmpty` and `String.IsNullOrWhiteSpace`:
```csharp
// ✅ Correct
if (String.IsNullOrWhiteSpace(input)) {
    return String.Empty;
}

// ❌ Avoid
if (input == null || input == "") {
    return "";
}
```

---

## LINQ

Fluent method syntax, one operation per line:
```csharp
List<string> names = users
    .Where(u => u.IsActive)
    .OrderBy(u => u.LastName)
    .Select(u => u.FullName)
    .ToList();
```

Avoid query syntax.

---

## Null Handling

Prefer explicit null checks:
```csharp
// GOOD
if (user != null) {
    string name = user.Name;
    Process(user);
}

if (user == null) throw new ArgumentNullException(nameof(user));
if (user == null) return;
```

Null-coalescing assignment for initialization:
```csharp
_cache ??= new Dictionary<int, User>();
name ??= "Unknown";
```

### Helper Methods

Use helper methods for null-safe conversions:
```csharp
public bool BooleanValue(bool? value)
{
    bool output = value.HasValue ? (bool)value : false;
    return output;
}

public Guid GuidValue(Guid? guid)
{
    Guid output = guid.HasValue ? (Guid)guid : Guid.Empty;
    return output;
}

public string StringValue(string? input)
{
    return input ?? String.Empty;
}

public int IntValue(int? value)
{
    return value.HasValue ? (int)value : 0;
}

public decimal DecimalValue(decimal? value)
{
    return value.HasValue ? (decimal)value : 0m;
}
```

---

## Exception Handling

Catch general Exception, type-check for specific handling:
```csharp
try {
    await SaveAsync();
} catch (Exception ex) {
    if (ex is DbUpdateException) {
        _logger.LogError(ex, "Database error");
        throw;
    }
    if (ex is ValidationException validationEx) {
        return BadRequest(validationEx.Message);
    }
    
    _logger.LogError(ex, "Unexpected error");
    throw;
}
```

Use `RecurseException` to capture full error details:
```csharp
try {
    await data.SaveChangesAsync();
    output.Result = true;
} catch (Exception ex) {
    output.Messages.Add("Error Saving Category:");
    output.Messages.AddRange(RecurseException(ex));
}
```

---

## Async/Await

No `Async` suffix on method names. Always await:
```csharp
// GOOD
public async Task<User> GetUser(int id)
{
    User user = await _repository.FindById(id);
    return user;
}

// AVOID
public Task<User> GetUserAsync(int id)
{
    return _repository.FindById(id);
}
```

---

## Switch

Switch expressions for value mappings:
```csharp
string text = status switch
{
    Status.Active => "Active",
    Status.Pending => "Pending",
    _ => "Unknown"
};
```

Traditional switch for complex logic with side effects.

---

## File Organization

### The `.App.` Naming Convention - MANDATORY

**CRITICAL:** All files that add or modify functionality beyond the base FreeCRM framework **MUST** use the `.App.` naming convention. This is not optional.

#### Why This Matters

1. **Framework Updates:** When FreeCRM releases updates, you can instantly identify which files are yours
2. **Easy Extraction:** Find all project-specific files with `find . -name "{ProjectName}.App.*"`
3. **Clear Ownership:** At a glance, know if a file is base framework or project-specific
4. **Blazor Class Names:** Blazor generates class names from file names - dots become underscores

#### File Categories

| Category | Description | Naming Pattern | Example |
|----------|-------------|----------------|---------|
| **Base Framework** | Stock FreeCRM files | `{ClassName}.cs` | `DataAccess.cs`, `User.cs` |
| **Base Customization** | Extend base FreeCRM classes | `{ClassName}.App.cs` | `DataAccess.App.cs` |
| **Project Extension** | Extend base + add project methods | `{ClassName}.App.{ProjectName}.cs` | `DataAccess.App.FreeManager.cs` |
| **Project-Specific NEW** | Entirely new features | `{ProjectName}.App.{Feature}.cs` | `FreeManager.App.EntityWizard.cs` |

### Project-Specific Files (NEW Features)

**When creating ANY new file that doesn't exist in FreeCRM-base, use this pattern:**

```
{ProjectName}.App.{Feature}.{OptionalSubFeature}.{Extension}
```

**This applies to ALL file types:**

| Extension | Pattern | Example |
|-----------|---------|---------|
| `.cs` | `{ProjectName}.App.{Feature}.cs` | `FreeManager.App.EntityTemplates.cs` |
| `.razor` | `{ProjectName}.App.{Feature}.razor` | `FreeManager.App.EntityWizard.razor` |
| `.js` | `{ProjectName}.App.{Feature}.js` | `FreeManager.App.EntityWizard.js` |
| `.css` | `{ProjectName}.App.{Feature}.css` | `FreeManager.App.EntityWizard.css` |
| `.json` | `{ProjectName}.App.{Feature}.json` | `FreeManager.App.Config.json` |

#### Multi-Level Naming (for large features)

Nest as deep as needed to organize code logically:

```
{ProjectName}.App.{Feature}.{SubFeature}.{SubSubFeature}.{Extension}
```

**Examples:**
```
FreeManager.App.EntityWizard.razor                           # Main page
FreeManager.App.EntityWizard.State.cs                        # State management
FreeManager.App.EntityWizard.Handlers.cs                     # Event handlers
FreeManager.App.EntityWizard.Generation.cs                   # Code generation

FreeManager.App.EntityTemplates.cs                           # Coordinator
FreeManager.App.EntityTemplates.Controller.cs                # Controller templates
FreeManager.App.EntityTemplates.DataAccess.cs                # DataAccess templates
FreeManager.App.EntityTemplates.DataObjects.cs               # DTO templates
FreeManager.App.EntityTemplates.EFModel.cs                   # EF Model templates
FreeManager.App.EntityTemplates.RazorPages.cs                # Razor templates

FreeManager.App.DataObjects.cs                               # DTO coordinator
FreeManager.App.DataObjects.EntityWizard.cs                  # Wizard DTOs
FreeManager.App.DataObjects.Projects.cs                      # Project DTOs
FreeManager.App.DataObjects.Persistence.cs                   # Persistence DTOs

FreeManager.App.DataAccess.cs                                # DataAccess coordinator
FreeManager.App.DataAccess.Projects.cs                       # Project CRUD
FreeManager.App.DataAccess.Files.cs                          # File operations
FreeManager.App.DataAccess.Builds.cs                         # Build operations
FreeManager.App.DataAccess.Templates.cs                      # Template queries
FreeManager.App.DataAccess.EntityWizardPersistence.cs        # Wizard persistence

FreeManager.App.EFDataModel.cs                               # EF DbContext extension
FreeManager.App.FMProject.cs                                 # Entity class
FreeManager.App.FMAppFile.cs                                 # Entity class
FreeManager.App.FMAppFileVersion.cs                          # Entity class
FreeManager.App.FMBuild.cs                                   # Entity class
```

#### Blazor Component Naming (IMPORTANT)

Blazor converts file name dots to underscores for class names:

| File Name | Generated Class Name |
|-----------|---------------------|
| `EntityWizard.razor` | `EntityWizard` |
| `EntityWizard.App.razor` | `EntityWizard_App` |
| `FreeManager.App.EntityWizard.razor` | `FreeManager_App_EntityWizard` |

**When referencing components in markup:**
```razor
@* File: FreeManager.App.EntityWizardStepper.razor *@
@* Reference as: *@
<FreeManager_App_EntityWizardStepper Steps="@_steps" />

@* NOT as (this won't work): *@
<EntityWizardStepper_App />  @* WRONG - old pattern *@
```

**Ensure _Imports.razor includes the namespace:**
```razor
@using FreeManager.Client.Shared.Wizard
```

#### Drop Redundant Prefixes

When using `{ProjectName}.App.*`, remove short prefixes like `FM`:

| Old Name | New Name |
|----------|----------|
| `FMWizardTemplates.cs` | `FreeManager.App.WizardTemplates.cs` |
| `FMEntityWizard.razor` | `FreeManager.App.EntityWizard.razor` |
| `FMProjectEditor.razor` | `FreeManager.App.ProjectEditor.razor` |
| `FMBuild.cs` | `FreeManager.App.FMBuild.cs` *(keep FM in entity names for DB table clarity)* |

### Framework Customization Files

Files that customize or extend **existing** FreeCRM base classes:

```
DataController.App.cs              # Customize base DataController
DataController.App.FreeManager.cs  # Add FreeManager-specific endpoints
DataAccess.App.cs                  # Customize base DataAccess
DataAccess.App.FreeManager.cs      # Add FreeManager-specific methods
DataObjects.App.cs                 # Customize base DataObjects
ConfigurationHelper.App.cs         # Add config properties (in DataObjects only!)
Program.App.cs                     # Customize startup
Index.App.razor                    # Customize home page
Modules.App.razor                  # Add custom JS/CSS to layout
```

**Note:** `ConfigurationHelper.App.cs` should only exist in `DataObjects` project, NOT in the Server project. Having it in both causes partial class conflicts.

### The Coordinator Pattern

When using multi-level partials, create a coordinator file documenting all related files:

```csharp
// FreeManager.App.DataAccess.cs (coordinator)
namespace FreeManager;

#region FreeManager Platform - DataAccess Methods (Coordinator)
// ============================================================================
// FREEMANAGER DATAACCESS EXTENSION
// This partial class adds FreeManager-specific DataAccess methods.
// Methods are split into feature-specific partial files:
//
// - FreeManager.App.DataAccess.Projects.cs       → Project CRUD
// - FreeManager.App.DataAccess.Files.cs          → File operations
// - FreeManager.App.DataAccess.Builds.cs         → Build operations
// - FreeManager.App.DataAccess.Templates.cs      → Template queries
// - FreeManager.App.DataAccess.EntityWizardPersistence.cs → Wizard saves
//
// These are NOT part of the stock FreeCRM framework.
// ============================================================================

public partial interface IDataAccess
{
    // Interface methods declared here
}

public partial class DataAccess
{
    // Implementations in feature files
}

#endregion
```

### Quick Reference: Which Pattern to Use?

| Scenario | Pattern | Example |
|----------|---------|---------|
| Modify base FreeCRM behavior | `{Base}.App.cs` | `DataAccess.App.cs` |
| Add methods to base class | `{Base}.App.{Project}.cs` | `DataAccess.App.FreeManager.cs` |
| Create new feature/page | `{Project}.App.{Feature}.razor` | `FreeManager.App.EntityWizard.razor` |
| Create new entity class | `{Project}.App.{Entity}.cs` | `FreeManager.App.FMProject.cs` |
| Create new DTO collection | `{Project}.App.DataObjects.{Area}.cs` | `FreeManager.App.DataObjects.Projects.cs` |
| Create new helper/utility | `{Project}.App.{Name}.cs` | `FreeManager.App.EntityTemplates.cs` |
| Split large feature | `{Project}.App.{Feature}.{Sub}.cs` | `FreeManager.App.EntityWizard.State.cs` |

### Verification

Before committing, verify naming compliance:

```bash
# List all project-specific files (should all start with ProjectName.App.)
find . -name "FreeManager.App.*" -type f

# Find any mis-named files (should return nothing for custom files)
find . -name "*App.FreeManager*" -type f  # Wrong order
find . -name "FM*.cs" -type f             # Old FM prefix without proper pattern
```

---

## DataObjects Project

### Class Structure

All DTOs live in a single `DataObjects` partial class:

```csharp
namespace HelpDesk;

public partial class DataObjects
{
    // Enums first
    public enum DeletePreference
    {
        Immediate,
        MarkAsDeleted,
    }

    // Base classes
    public class ActionResponseObject
    {
        public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
    }

    // Domain classes in alphabetical order
    public class Category
    {
        public Guid CategoryId { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? LastModifiedBy { get; set; }
    }
}
```

### Property Conventions

| Type | Convention | Example |
|------|------------|---------|
| **Required string** | Initialize empty | `string Name { get; set; } = string.Empty;` |
| **Optional string** | Nullable | `string? Description { get; set; }` |
| **Required Guid** | No initializer | `Guid CategoryId { get; set; }` |
| **Optional Guid** | Nullable | `Guid? ParentId { get; set; }` |
| **Boolean** | Default value | `bool Enabled { get; set; } = true;` |
| **Collections** | Initialize empty | `List<Item> Items { get; set; } = new();` |
| **Complex objects** | Initialize new | `ActionResponse { get; set; } = new();` |

### Standard Soft-Delete Properties

Every deletable entity should have:

```csharp
public bool Deleted { get; set; }
public DateTime? DeletedAt { get; set; }
public string? LastModifiedBy { get; set; }
```

### Response Objects

Inherit from `ActionResponseObject`:

```csharp
public class CategoryResponse : ActionResponseObject
{
    public Category? Category { get; set; }
}

public class CategoryListResponse : ActionResponseObject
{
    public List<Category> Categories { get; set; } = new();
}
```

### BooleanResponse Pattern

Always use `BooleanResponse` for success/failure:

```csharp
public class BooleanResponse
{
    public List<string> Messages { get; set; } = new List<string>();
    public bool Result { get; set; }
}
```

---

## DataAccess Project

### File Naming

Split by domain using partial classes:

```
DataAccess/
├── DataAccess.cs              # Constructor, core setup
├── DataAccess.App.cs          # App-specific methods
├── DataAccess.Categories.cs   # Category CRUD
├── DataAccess.Requests.cs     # Request/ticket methods
├── DataAccess.Settings.cs     # Settings methods
├── DataAccess.Users.cs        # User methods
├── DataAccess.Utilities.cs    # Helper methods
└── GlobalUsings.cs            # Shared using statements
```

### Interface + Implementation Pattern

Define interface methods in the same file as implementation:

```csharp
namespace HelpDesk;

// Interface at the top of the file
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteCategory(Guid CategoryId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false);
    Task<DataObjects.Category> GetCategory(Guid CategoryId);
    Task<List<DataObjects.Category>> GetCategories(Guid TenantId);
    Task<DataObjects.Category> SaveCategory(DataObjects.Category category, DataObjects.User? CurrentUser = null);
}

// Implementation follows
public partial class DataAccess
{
    public async Task<DataObjects.BooleanResponse> DeleteCategory(Guid CategoryId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false)
    {
        // Implementation
    }

    // ...
}
```

### Method Signature Conventions

| Pattern | Convention |
|---------|------------|
| **Delete methods** | `Task<BooleanResponse> Delete{Entity}(Guid Id, User? CurrentUser = null, bool ForceDeleteImmediately = false)` |
| **Get single** | `Task<{Entity}> Get{Entity}(Guid Id, User? CurrentUser = null)` |
| **Get list** | `Task<List<{Entity}>> Get{Entities}(Guid TenantId, User? CurrentUser = null)` |
| **Save** | `Task<{Entity}> Save{Entity}({Entity} item, User? CurrentUser = null)` |
| **Filtered list** | `Task<Filter{Entities}> Get{Entities}Filtered(Filter{Entities} filter, User CurrentUser)` |

### CRUD Method Template

```csharp
public async Task<DataObjects.BooleanResponse> DeleteCategory(Guid CategoryId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false)
{
    DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();

    var rec = await data.Categories.FirstOrDefaultAsync(x => x.CategoryId == CategoryId);
    if (rec == null) {
        output.Messages.Add("Error Deleting Category " + CategoryId.ToString() + " - Record No Longer Exists");
        return output;
    }

    var now = DateTime.UtcNow;
    Guid tenantId = GuidValue(rec.TenantId);
    var tenantSettings = GetTenantSettings(tenantId);

    if (ForceDeleteImmediately || tenantSettings.DeletePreference == DataObjects.DeletePreference.Immediate) {
        // Delete related records first
        try {
            data.SubItems.RemoveRange(data.SubItems.Where(x => x.CategoryId == CategoryId));
        } catch (Exception ex) {
            output.Messages.Add("Error Deleting Related Records:");
            output.Messages.AddRange(RecurseException(ex));
            return output;
        }

        // Delete main record
        data.Categories.Remove(rec);
    } else {
        // Soft delete
        rec.Deleted = true;
        rec.DeletedAt = now;
        rec.LastModified = now;
        if (CurrentUser != null) {
            rec.LastModifiedBy = CurrentUserIdString(CurrentUser);
        }
    }

    try {
        await data.SaveChangesAsync();
        output.Result = true;

        // Signal real-time update
        await SignalRUpdate(new DataObjects.SignalRUpdate {
            TenantId = tenantId,
            ItemId = CategoryId,
            UpdateType = DataObjects.SignalRUpdateType.Category,
            Message = "Deleted"
        });
    } catch (Exception ex) {
        output.Messages.Add("Error Deleting Category " + CategoryId.ToString() + ":");
        output.Messages.AddRange(RecurseException(ex));
    }

    return output;
}
```

### Private Field Naming

Use underscore prefix for private fields:

```csharp
public partial class DataAccess : IDisposable, IDataAccess
{
    private int _accountLockoutMaxAttempts = 5;
    private int _accountLockoutMinutes = 10;
    private string _appName = "HelpDesk";
    private string _connectionString;
    private EFDataModel data;
    private bool _firstInit = true;
    private Guid _guid1 = new Guid("00000000-0000-0000-0000-000000000001");
    private bool _inMemoryDatabase = false;
    private bool _open;
    private DateOnly _released = DateOnly.FromDateTime(Convert.ToDateTime("12/12/2025"));
    private string _uniqueId = Guid.NewGuid().ToString().Replace("-", "").ToLower();
    private string _version = "4.1.6";
}
```

### CurrentUser Handling

```csharp
// Check if user is admin
if (CurrentUser != null && CurrentUser.Admin) {
    // Admin-only logic
}

// Get user ID string for audit
string userId = CurrentUserIdString(CurrentUser);  // Returns null if CurrentUser is null

// Get user ID as Guid
Guid? userId = CurrentUserId(CurrentUser);  // Returns null if CurrentUser is null
```

---

## Controllers (DataController)

### File Naming

Split by domain:

```
Controllers/
├── DataController.cs           # Constructor, core setup
├── DataController.App.cs       # App-specific endpoints
├── DataController.Categories.cs
├── DataController.Users.cs
└── DataController.Utilities.cs
```

### Class Declaration

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CRM.Server.Controllers;

[ApiController]
public partial class DataController : ControllerBase
{
    private IDataAccess da;
    private DataObjects.User CurrentUser;
    private Guid TenantId = Guid.Empty;

    private string _returnCodeAccessDenied = "{{AccessDenied}}";

    // Constructor...
}
```

### Endpoint Patterns

#### GET - Single Item
```csharp
[HttpGet]
[Authorize]
[Route("~/api/Data/GetCategory/{id}")]
public async Task<ActionResult<DataObjects.Category>> GetCategory(Guid id)
{
    var output = await da.GetCategory(id, CurrentUser);
    return Ok(output);
}
```

#### GET - List
```csharp
[HttpGet]
[Authorize]
[Route("~/api/Data/GetCategories")]
public async Task<ActionResult<List<DataObjects.Category>>> GetCategories()
{
    var output = await da.GetCategories(TenantId, CurrentUser);
    return Ok(output);
}
```

#### POST - Save
```csharp
[HttpPost]
[Authorize(Policy = Policies.Admin)]
[Route("~/api/Data/SaveCategory")]
public async Task<ActionResult<DataObjects.Category>> SaveCategory(DataObjects.Category category)
{
    var output = await da.SaveCategory(category, CurrentUser);
    return Ok(output);
}
```

#### DELETE
```csharp
[HttpGet]
[Authorize(Policy = Policies.Admin)]
[Route("~/api/Data/DeleteCategory/{id}")]
public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteCategory(Guid id)
{
    var output = await da.DeleteCategory(id, CurrentUser);
    return Ok(output);
}
```

### Authorization Patterns

| Scenario | Attribute |
|----------|-----------|
| Public endpoint | `[AllowAnonymous]` |
| Any logged-in user | `[Authorize]` |
| Admin only | `[Authorize(Policy = Policies.Admin)]` |
| Custom check | Check in method body |

### Custom Authorization Check

```csharp
[HttpGet]
[Authorize]
[Route("~/api/Data/GetUser/{id}")]
public async Task<ActionResult<DataObjects.User>> GetUser(Guid id)
{
    // User can view themselves or admin can view anyone
    if (CurrentUser.Admin || CurrentUser.UserId == id) {
        var output = await da.GetUser(id, false, CurrentUser);
        return Ok(output);
    } else {
        return Unauthorized(_returnCodeAccessDenied);
    }
}
```

---

## EFModels Project

### File Structure

EF models are typically scaffolded. Only customize in `EFModelOverrides.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace CRM.EFModels;

public partial class EFDataModel : DbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        var providerName = this.Database.ProviderName;
        if (!String.IsNullOrEmpty(providerName)) {
            switch (providerName.ToUpper()) {
                case "MICROSOFT.ENTITYFRAMEWORKCORE.SQLSERVER":
                case "MICROSOFT.ENTITYFRAMEWORKCORE.INMEMORY":
                    break;

                case "MYSQL.ENTITYFRAMEWORKCORE":
                case "NPGSQL.ENTITYFRAMEWORKCORE.POSTGRESQL":
                case "MICROSOFT.ENTITYFRAMEWORKCORE.SQLITE":
                    // Convert GUIDs to strings for these providers
                    configurationBuilder
                        .Properties<Guid>()
                        .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.GuidToStringConverter>();
                    break;
            }
        }
    }
}
```

---

# Part 2: Razor Conventions

---

## Razor File Structure

### Page Organization

```
YourProject.Client/
├── Pages/
│   ├── Auth/
│   │   ├── Login.razor
│   │   ├── Logout.razor
│   │   └── AccessDenied.razor
│   ├── Settings/
│   │   ├── Categories/
│   │   │   ├── Categories.razor
│   │   │   └── EditCategory.razor
│   │   ├── Users/
│   │   │   ├── Users.razor
│   │   │   └── EditUser.razor
│   │   └── AppSettings.razor
│   ├── Index.razor
│   └── Profile.razor
├── Shared/
│   ├── App/                    # App-specific components
│   │   └── App.Dashboard.razor
│   ├── AppComponents/          # .App. extension components
│   │   └── Index.App.razor
│   ├── Icon.razor
│   ├── Language.razor
│   ├── LoadingMessage.razor
│   └── MainLayout.razor
├── DataModel.cs
├── DataModel.App.cs
├── Helpers.cs
└── Helpers.App.cs
```

---

## Razor Component Template Structure

### Standard Page Component

```razor
@page "/Categories"
@page "/{TenantCode}/Categories"
@using Blazored.LocalStorage
@inject IJSRuntime jsRuntime
@inject HttpClient Http
@inject ILocalStorageService LocalStorage
@inject BlazorDataModel Model
@implements IDisposable

@if (Model.Loaded && Model.View == _pageName) {
    @if (_loading) {
        <h1 class="page-title">
            <Language Tag="Categories" IncludeIcon="true" />
        </h1>
        <LoadingMessage />
    } else {
        @* Page content here *@
    }
}

@code {
    [Parameter] public string? TenantCode { get; set; }

    protected bool _loading = true;
    protected bool _loadedData = false;
    protected string _pageName = "categories";

    // Data fields...

    public void Dispose() {
        Model.OnChange -= OnDataModelUpdated;
    }

    protected override void OnInitialized() {
        Model.View = _pageName;
        Model.OnChange += OnDataModelUpdated;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            Model.TenantCodeFromUrl = TenantCode;
        }

        if (Model.Loaded && Model.LoggedIn && !_loadedData) {
            _loadedData = true;
            await LoadData();
        }
    }

    protected void OnDataModelUpdated() {
        if (Model.View == _pageName) {
            StateHasChanged();
        }
    }

    protected async Task LoadData() {
        _loading = true;
        StateHasChanged();

        // Load data...

        _loading = false;
        StateHasChanged();
    }
}
```

---

## Razor Directives Order

Always use this order for directives at the top of the file:

```razor
@* 1. Page directive(s) *@
@page "/EditCategory/{itemid}"
@page "/{TenantCode}/EditCategory/{itemid}"
@page "/NewCategory"
@page "/{TenantCode}/NewCategory"

@* 2. Using statements *@
@using Blazored.LocalStorage

@* 3. Inject statements *@
@inject IJSRuntime jsRuntime
@inject HttpClient Http
@inject ILocalStorageService LocalStorage
@inject BlazorDataModel Model
@inject Radzen.DialogService DialogService

@* 4. Implements *@
@implements IDisposable
```

---

## Page Route Patterns

### List Pages
```razor
@page "/Categories"
@page "/{TenantCode}/Categories"
```

### Edit Pages (dual route for new/edit)
```razor
@page "/EditCategory/{itemid}"
@page "/{TenantCode}/EditCategory/{itemid}"
@page "/NewCategory"
@page "/{TenantCode}/NewCategory"
```

### Detail/View Pages
```razor
@page "/ViewCategory/{itemid}"
@page "/{TenantCode}/ViewCategory/{itemid}"
```

---

## Code Block Conventions

### Field Naming

| Type | Convention | Example |
|------|------------|---------|
| **Loading state** | `_loading` | `protected bool _loading = true;` |
| **Data loaded flag** | `_loadedData` | `protected bool _loadedData = false;` |
| **Page name** | `_pageName` | `protected string _pageName = "editcategory";` |
| **Page title** | `_title` | `protected string _title = "EditCategory";` |
| **New item flag** | `_newItem` | `protected bool _newItem = false;` |
| **Entity data** | `_entityName` | `protected DataObjects.Category _category = new();` |

### Field Declaration Order

```razor
@code {
    // 1. Parameters
    [Parameter] public string? itemid { get; set; }
    [Parameter] public string? TenantCode { get; set; }

    // 2. Private/protected state fields (underscore prefix)
    protected DataObjects.Category _category = new DataObjects.Category();
    protected bool _loading = true;
    protected bool _loadedData = false;
    protected bool _newItem = false;
    protected string _title = "EditCategory";
    protected string _pageName = "editcategory";

    // 3. Lifecycle methods
    public void Dispose() { }
    protected override void OnInitialized() { }
    protected override async Task OnAfterRenderAsync(bool firstRender) { }

    // 4. Event handlers
    protected void OnDataModelUpdated() { }
    protected async void SignalRUpdate(DataObjects.SignalRUpdate update) { }

    // 5. Navigation methods
    protected void Back() { }

    // 6. Data methods
    protected async Task LoadData() { }
    protected async Task Save() { }
    protected async Task Delete() { }

    // 7. Helper methods
    protected string GetDisplayName() { }
}
```

---

## Razor Lifecycle Methods

### OnInitialized Pattern

```razor
protected override void OnInitialized()
{
    // Determine if new or edit mode
    _newItem = String.IsNullOrWhiteSpace(itemid);
    _title = _newItem ? "AddNewCategory" : "EditCategory";

    // Set view and subscribe to changes
    Model.View = _pageName;
    Model.OnChange += OnDataModelUpdated;
}
```

### OnAfterRenderAsync Pattern

```razor
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    // Set tenant code on first render
    if (firstRender) {
        Model.TenantCodeFromUrl = TenantCode;
    }

    // Load data when model is ready
    if (Model.Loaded && Model.LoggedIn) {
        // Optional: Admin check
        if (!Model.User.Admin) {
            Helpers.NavigateToRoot();
            return;
        }

        // Validate URL
        await Helpers.ValidateUrl(TenantCode);

        // Load data if not already loaded or navigation changed
        if (!_loadedData || Helpers.StringValue(Model.NavigationId) != Helpers.StringValue(itemid)) {
            _loadedData = true;
            await LoadData();
        }
    }
}
```

### Dispose Pattern

```razor
public void Dispose()
{
    // Unsubscribe from events
    Model.OnChange -= OnDataModelUpdated;
    Model.Subscribers_OnChange.Remove(_pageName);

    // Unsubscribe from SignalR if used
    Model.OnSignalRUpdate -= SignalRUpdate;
    Model.Subscribers_OnSignalRUpdate.Remove(_pageName);
}
```

---

## SignalR Integration

### Subscribe Pattern

```razor
protected override void OnInitialized()
{
    if (!Model.Subscribers_OnChange.Contains(_pageName)) {
        Model.Subscribers_OnChange.Add(_pageName);
        Model.OnChange += OnDataModelUpdated;
    }

    if (!Model.Subscribers_OnSignalRUpdate.Contains(_pageName)) {
        Model.Subscribers_OnSignalRUpdate.Add(_pageName);
        Model.OnSignalRUpdate += SignalRUpdate;
    }

    Model.View = _pageName;
}
```

### SignalR Update Handler

```razor
protected async void SignalRUpdate(DataObjects.SignalRUpdate update)
{
    await Task.Delay(0);

    // Only process if on this page and update is from another user
    if (Model.View == _pageName && update.UserId != Model.User.UserId) {
        if (update.UpdateType == DataObjects.SignalRUpdateType.Category) {
            switch (update.Message.ToLower()) {
                case "deleted":
                    if (update.ItemId == _category.CategoryId) {
                        Back();
                        Model.Message_RecordDeleted("", update.UserDisplayName);
                    }
                    break;

                case "saved":
                    var savedItem = Helpers.DeserializeObject<DataObjects.Category>(update.ObjectAsString);
                    if (savedItem != null) {
                        _category = savedItem;
                        StateHasChanged();
                        Model.Message_RecordUpdated("", update.UserDisplayName);
                    }
                    break;
            }
        }
    }
}
```

---

## Common UI Patterns

### Page Header with Sticky Menu

```razor
<div class="@Model.StickyMenuClass">
    <h1 class="page-title">
        <Language Tag="Categories" IncludeIcon="true" />
        <StickyMenuIcon />
    </h1>

    <div class="btn-toolbar mb-2">
        <button type="button" class="btn btn-success" @onclick="Add">
            <Language Tag="AddNewCategory" IncludeIcon="true" />
        </button>
    </div>
</div>
```

### Button Groups

```razor
<div class="btn-toolbar mb-2">
    <div class="btn-group" role="group">
        <button type="button" class="btn btn-dark" @onclick="Back">
            <Language Tag="Cancel" IncludeIcon="true" />
        </button>

        @if (!_item.Deleted) {
            <button type="button" class="btn btn-success" @onclick="Save">
                <Language Tag="Save" IncludeIcon="true" />
            </button>

            @if (!_newItem) {
                <DeleteConfirmation
                    OnConfirmed="Delete"
                    CancelText="@Helpers.ConfirmButtonTextCancel"
                    DeleteText="@Helpers.ConfirmButtonTextDelete"
                    ConfirmDeleteText="@Helpers.ConfirmButtonTextConfirmDelete" />
            }
        }
    </div>
</div>
```

### Form Fields

#### Text Input
```razor
<div class="mb-2">
    <label for="edit-category-Description">
        <Language Tag="Description" Required="true" />
    </label>
    <input type="text" id="edit-category-Description"
           class="form-control" @bind="_category.Description" />
</div>
```

#### Dropdown Select
```razor
<div class="mb-2">
    <label for="edit-category-ParentId">
        <Language Tag="ParentCategory" />
    </label>
    <select id="edit-category-ParentId" class="form-select" @bind="_category.ParentCategoryId">
        <option value=""></option>
        @foreach (var item in Model.CategoryList) {
            if (item.CategoryId == _category.CategoryId) {
                <option value="@item.CategoryId" disabled>@item.Description</option>
            } else {
                <option value="@item.CategoryId">@item.Description</option>
            }
        }
    </select>
</div>
```

#### Toggle Switch
```razor
<div class="mb-2 form-check form-switch">
    <input type="checkbox" id="edit-category-Enabled" role="switch"
           class="form-check-input" @bind="_category.Enabled" />
    <label for="edit-category-Enabled" class="form-check-label">
        <Language Tag="Enabled" />
    </label>
</div>
```

#### Filter Toggles
```razor
<div class="mb-2">
    <div class="form-check form-switch">
        <input type="checkbox" id="list-IncludeDeleted" class="form-check-input"
               @bind="Model.User.UserPreferences.IncludeDeletedItems" />
        <label for="list-IncludeDeleted" class="form-check-label">
            <Language Tag="IncludeDeletedRecords" />
        </label>
    </div>
</div>
```

### Table Pattern

```razor
@if (FilteredItems.Any()) {
    <table class="table table-sm mb-2">
        <thead>
            <tr class="table-dark">
                <th style="width:1%;"></th>
                <th style="width:50%;">
                    <Language Tag="Description" ReplaceSpaces="true" />
                </th>
                <th style="width:1%;" class="center">
                    <Language Tag="Enabled" ReplaceSpaces="true" />
                </th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in FilteredItems) {
                string rowClass = item.Deleted ? "item-deleted"
                                : !item.Enabled ? "disabled"
                                : "";
                <tr class="@rowClass">
                    <td>
                        <button type="button" class="btn btn-primary btn-sm nowrap"
                                @onclick="() => Edit(item)">
                            <Language Tag="Edit" IncludeIcon="true" />
                        </button>
                    </td>
                    <td>@item.Description</td>
                    <td class="center">@((MarkupString)Helpers.BooleanToIcon(item.Enabled))</td>
                </tr>
            }
        </tbody>
    </table>
} else {
    <Language Tag="NoItemsToShow" />
}
```

---

## Razor CRUD Operations

### LoadData Pattern

```razor
protected async Task LoadData()
{
    if (!String.IsNullOrWhiteSpace(itemid)) {
        // Edit mode
        Model.NavigationId = itemid;
        Model.ViewIsEditPage = true;

        _loading = true;
        _newItem = false;
        _title = "EditCategory";

        var getItem = await Helpers.GetOrPost<DataObjects.Category>("api/Data/GetCategory/" + itemid);
        if (getItem != null) {
            if (getItem.ActionResponse.Result) {
                _category = getItem;
            } else {
                Model.ErrorMessages(getItem.ActionResponse.Messages);
            }
        } else {
            Model.UnknownError();
        }
    } else {
        // New mode
        _newItem = true;
        _title = "AddNewCategory";

        _category = new DataObjects.Category {
            CategoryId = Guid.Empty,
            TenantId = Model.TenantId,
            Enabled = true,
        };
    }

    _loading = false;
    StateHasChanged();

    await Helpers.DelayedFocus("edit-category-Description");
}
```

### Save Pattern

```razor
protected async Task Save()
{
    Model.ClearMessages();

    // Validation
    List<string> errors = new List<string>();
    string focus = "";

    if (String.IsNullOrWhiteSpace(_category.Description)) {
        errors.Add(Helpers.MissingRequiredField("Description"));
        if (focus == "") { focus = "edit-category-Description"; }
    }

    if (errors.Any()) {
        Model.ErrorMessages(errors);
        await Helpers.DelayedFocus(focus);
        return;
    }

    // Save
    Model.Message_Saving();

    var saved = await Helpers.GetOrPost<DataObjects.Category>("api/Data/SaveCategory", _category);

    Model.ClearMessages();

    if (saved != null) {
        if (saved.ActionResponse.Result) {
            Back();
        } else {
            Model.ErrorMessages(saved.ActionResponse.Messages);
        }
    } else {
        Model.UnknownError();
    }
}
```

### Delete Pattern

```razor
protected async Task Delete()
{
    Model.ClearMessages();
    Model.Message_Deleting();

    var deleted = await Helpers.GetOrPost<DataObjects.BooleanResponse>(
        "api/Data/DeleteCategory/" + _category.CategoryId.ToString());

    Model.ClearMessages();

    if (deleted != null) {
        if (deleted.Result) {
            Back();
        } else {
            Model.ErrorMessages(deleted.Messages);
        }
    } else {
        Model.UnknownError();
    }
}
```

---

## Shared Components

### Language Component
```razor
<Language Tag="Save" />
<Language Tag="Save" IncludeIcon="true" />
<Language Tag="Description" Required="true" />
<Language Tag="NoItemsToShow" ReplaceSpaces="true" />
```

### Icon Component
```razor
<Icon Name="Save" />
<Icon Name="Edit" Title="EditItem" />
```

### LoadingMessage Component
```razor
@if (_loading) {
    <LoadingMessage />
}
```

### LastModifiedMessage Component
```razor
@if (!_newItem) {
    <div class="mb-2">
        <hr />
        <LastModifiedMessage DataObject="_category" />
    </div>
}
```

### UndeleteMessage Component
```razor
@if (_item.Deleted) {
    <UndeleteMessage
        DeletedAt="_item.DeletedAt"
        LastModifiedBy="@_item.LastModifiedBy"
        OnUndelete="Undelete" />
}
```

---

## Helper Method Usage

### Navigation
```razor
Helpers.NavigateTo("Categories");
Helpers.NavigateTo("EditCategory/" + item.CategoryId.ToString());
Helpers.NavigateToRoot();
Helpers.NavigateToLogin();
```

### API Calls
```razor
// GET or POST
var result = await Helpers.GetOrPost<DataObjects.Category>("api/Data/GetCategory/" + id);
var saved = await Helpers.GetOrPost<DataObjects.Category>("api/Data/SaveCategory", _category);
```

### Messages
```razor
Model.ClearMessages();
Model.Message_Saving();
Model.Message_Deleting();
Model.ErrorMessages(response.ActionResponse.Messages);
Model.UnknownError();
```

### Validation
```razor
errors.Add(Helpers.MissingRequiredField("Description"));
await Helpers.DelayedFocus("edit-category-Description");
```

### Text/Localization
```razor
string text = Helpers.Text("Save");
string icon = Helpers.Icon("Save", includeClass: true);
@((MarkupString)Helpers.BooleanToIcon(item.Enabled))
```

---

## CSS Classes

### Standard Classes

| Class | Usage |
|-------|-------|
| `page-title` | Main page heading |
| `mb-2` | Bottom margin (Bootstrap) |
| `btn-toolbar` | Button container |
| `btn-group` | Grouped buttons |
| `form-check form-switch` | Toggle switches |
| `form-control` | Text inputs |
| `form-select` | Dropdowns |
| `table table-sm` | Compact tables |
| `table-dark` | Dark table header |
| `item-deleted` | Deleted item styling |
| `disabled` | Disabled item styling |
| `center` | Center-aligned text |
| `nowrap` | No text wrapping |
| `note` | Small helper text |

---

## ID Naming Convention

Use consistent ID patterns for form elements:

```
{context}-{entity}-{field}
```

Examples:
- `edit-category-Description`
- `edit-category-Enabled`
- `list-IncludeDeleted`
- `filter-status`

---

## Common Patterns Summary

| Pattern | Usage |
|---------|-------|
| `DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();` | Start of delete/action methods |
| `var rec = await data.Entities.FirstOrDefaultAsync(x => x.Id == id);` | Get single record |
| `if (rec == null) { output.Messages.Add(...); return output; }` | Not found handling |
| `var now = DateTime.UtcNow;` | Timestamp for modifications |
| `output.Result = true;` | Set on success |
| `output.Messages.AddRange(RecurseException(ex));` | Add error details |
| `await SignalRUpdate(...)` | Notify clients of changes |
| `ClearTenantCache(tenantId);` | Invalidate cached data |

---

*Created: 2025-01-20*
*Maintained by: Development Team*