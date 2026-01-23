# .NET vs FreeCRM: What's Custom, What's Standard

> A guide for developers to understand what's stock .NET/Blazor vs FreeCRM-specific patterns.

**Source:** FreeCRM base template (public)

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [The Core Principle](#the-core-principle) | ~40 | Why FreeCRM wraps .NET features |
| [Namespace Conventions](#namespace-conventions) | ~55 | DataObjects and DataAccess patterns |
| [Navigation Patterns](#navigation-patterns) | ~130 | Custom vs standard navigation |
| [Authentication Patterns](#authentication--user-patterns) | ~180 | User objects and auth state |
| [HTTP & API Patterns](#http--api-patterns) | ~250 | GetOrPost vs HttpClient |
| [Localization Patterns](#localization-patterns) | ~300 | Text() and Language component |
| [State Management](#state-management) | ~350 | BlazorDataModel explained |
| [Quick Reference](#quick-reference-custom-vs-standard) | ~430 | Custom vs standard lookup tables |
| [Gotchas](#gotchas-for-net-developers) | ~500 | Common mistakes |
| [Multi-Tenant vs Single-Tenant](#multi-tenant-vs-single-tenant-architecture) | ~570 | When to use each |

**Why this matters:** Newcomers often can't tell what's framework vs custom. They might search Microsoft docs for `Helpers.NavigateTo()` (custom) or assume `DataObjects.User` is a .NET type (it's not). This guide clarifies the boundary.

---

## The Core Principle

FreeCRM wraps many .NET features with custom helpers to:
- **Add tenant awareness** (multi-tenant architecture)
- **Reduce boilerplate** (auth headers, JSON handling)
- **Enforce consistency** (navigation, validation patterns)
- **Enable autocomplete** (namespaced classes like `DataObjects.`)

**Rule of thumb:** If it starts with `Helpers.`, `Model.`, `DataObjects.`, or `DataAccess.`, it's FreeCRM custom code.

---

## Namespace Conventions

### DataObjects Namespace

```csharp
// FreeCRM pattern
public partial class DataObjects
{
    public partial class User { ... }
    public partial class Tag { ... }
    public partial class Tenant { ... }
}

// Usage
DataObjects.User user = new DataObjects.User();
DataObjects.Tag tag = new DataObjects.Tag();
```

**Why this exists:** Visual Studio autocomplete. When you type `DataObjects.`, you get a filtered list of all data transfer objects. Without this, you'd have hundreds of classes mixed together.

**This is NOT:**
- A .NET requirement
- A namespace in the traditional sense
- Entity Framework convention

**Standard .NET alternative:**
```csharp
// Normal .NET would use namespaces
namespace MyApp.Models;
public class User { ... }

// Or just flat classes
public class User { ... }
```

### DataAccess Namespace

```csharp
// FreeCRM pattern
public partial class DataAccess : IDataAccess
{
    public async Task<DataObjects.User?> GetUser(Guid UserId) { ... }
    public async Task<DataObjects.Tag?> SaveTag(DataObjects.Tag Tag) { ... }
}
```

**Why this exists:** Same autocomplete benefit. Type `da.` (the DataAccess instance) and see all data methods grouped together.

**Standard .NET alternative:**
```csharp
// Normal .NET repository pattern
public interface IUserRepository { ... }
public interface ITagRepository { ... }
public class UserRepository : IUserRepository { ... }
```

### The Partial Class Pattern

Both `DataObjects` and `DataAccess` use C#'s `partial class` feature:

```csharp
// DataObjects.cs (base)
public partial class DataObjects { }

// DataObjects.Users.cs
public partial class DataObjects
{
    public partial class User { ... }
}

// DataObjects.Tags.cs
public partial class DataObjects
{
    public partial class Tag { ... }
}
```

**Why:** Allows splitting large classes across multiple files while maintaining the autocomplete benefit.

**This IS standard .NET:** The `partial` keyword is official C#. The nested class pattern is the FreeCRM convention.

---

## Navigation Patterns

### FreeCRM Custom

```csharp
// Custom wrapper - handles tenant URL, external URLs, etc.
Helpers.NavigateTo("Settings/Tags");
Helpers.NavigateTo("Settings/EditTag/" + id);
Helpers.NavigateToRoot();

// Custom URL builder for href attributes
string url = Helpers.BuildUrl("Settings/Tags");
```

### Standard .NET Blazor

```csharp
// .NET's built-in NavigationManager
@inject NavigationManager NavManager

NavManager.NavigateTo("/Settings/Tags");
NavManager.NavigateTo("/Settings/Tags", forceLoad: true);
NavManager.Uri;  // Current URL
NavManager.BaseUri;  // Base URL
```

### Why FreeCRM Wraps This

```csharp
// What Helpers.NavigateTo does internally (simplified):
public static void NavigateTo(string subUrl, bool forceReload = false)
{
    // Handle external URLs directly
    if (subUrl.StartsWith("http")) {
        _navManager.NavigateTo(subUrl, forceReload);
        return;
    }

    // Prepend tenant code for multi-tenant routing
    string fullUrl = "/" + Model.TenantCode + "/" + subUrl;
    _navManager.NavigateTo(fullUrl, forceReload);
}
```

**Key insight:** If you use `NavManager.NavigateTo()` directly, you'll break multi-tenant routing.

---

## Authentication & User Patterns

### FreeCRM Custom

```csharp
// The logged-in user comes from BlazorDataModel
@inject BlazorDataModel Model

if (Model.LoggedIn) {
    var userId = Model.User.UserId;
    var userName = Model.User.DisplayName;
    var isAdmin = Model.User.Admin;
}

// CurrentUser in controllers
protected DataObjects.User CurrentUser;  // Set in constructor
```

### Standard .NET Blazor/ASP.NET

```csharp
// .NET's built-in authentication
@inject AuthenticationStateProvider AuthProvider

var authState = await AuthProvider.GetAuthenticationStateAsync();
var user = authState.User;  // ClaimsPrincipal
var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
var userName = user.Identity?.Name;

// In controllers
User.Identity.IsAuthenticated;
User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

### Why FreeCRM Has Its Own Pattern

| .NET's `ClaimsPrincipal` | FreeCRM's `DataObjects.User` |
|--------------------------|----------------------------------|
| Claims-based, minimal | Full user object with all properties |
| Standard JWT/Cookie auth | Custom token + fingerprint auth |
| No tenant awareness | Built-in `TenantId` |
| Requires claims parsing | Direct property access |

```csharp
// .NET way - extract data from claims
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;

// FreeCRM way - direct access
var userId = Model.User.UserId;
var email = Model.User.Email;
var isAdmin = Model.User.Admin;
var tenantId = Model.User.TenantId;
```

### The CurrentUser Naming Convention

In controllers, you'll see:
```csharp
protected DataObjects.User CurrentUser;
```

This name was chosen to **mirror .NET Identity conventions** where `HttpContext.User` is the current user. It's not a .NET requirement - just a naming convention that feels familiar.

---

## HTTP & API Patterns

### FreeCRM Custom

```csharp
// Single method for all API calls
var user = await Helpers.GetOrPost<DataObjects.User>("api/Data/GetUser/" + id);
var saved = await Helpers.GetOrPost<DataObjects.User>("api/Data/SaveUser", user);
```

### Standard .NET

```csharp
// .NET's HttpClient
@inject HttpClient Http

// GET
var response = await Http.GetAsync("api/Data/GetUser/" + id);
var user = await response.Content.ReadFromJsonAsync<User>();

// POST
var response = await Http.PostAsJsonAsync("api/Data/SaveUser", user);
var saved = await response.Content.ReadFromJsonAsync<User>();
```

### Why FreeCRM Wraps This

```csharp
// What GetOrPost does internally (simplified):
public static async Task<T?> GetOrPost<T>(string url, object? post = null)
{
    // Add authentication headers automatically
    _http.DefaultRequestHeaders.Add("TenantId", Model.TenantId.ToString());
    _http.DefaultRequestHeaders.Add("Token", Model.Token);
    _http.DefaultRequestHeaders.Add("Fingerprint", Model.Fingerprint);

    // Determine GET vs POST based on whether data is provided
    HttpResponseMessage response;
    if (post == null) {
        response = await _http.GetAsync(url);
    } else {
        response = await _http.PostAsJsonAsync(url, post);
    }

    // Handle errors, deserialize, return
    return await response.Content.ReadFromJsonAsync<T>();
}
```

**Key insight:** Using `HttpClient` directly means manually adding auth headers every time.

---

## Localization Patterns

### FreeCRM Custom

```razor
@* Component for markup *@
<Language Tag="Save" />
<Language Tag="Save" IncludeIcon="true" />
<Language Tag="Email" Required="true" />

@* Helper for C# code *@
@code {
    string label = Helpers.Text("Save");
    string error = Helpers.MissingRequiredField("Email");
}
```

### Standard .NET

```csharp
// .NET's IStringLocalizer
@inject IStringLocalizer<SharedResource> Localizer

<span>@Localizer["Save"]</span>

@code {
    string label = Localizer["Save"];
}
```

### Why FreeCRM Has Its Own

| .NET `IStringLocalizer` | FreeCRM `<Language>` / `Helpers.Text()` |
|-------------------------|---------------------------------------------|
| Resource files (.resx) | Database-driven language strings |
| Compile-time | Runtime editable by admin |
| Per-assembly resources | Tenant-specific overrides |
| No icon support | Built-in icon lookup |

FreeCRM's system allows:
- Editing translations in the admin UI without recompiling
- Different tenants having different wording
- Icons automatically associated with language tags

---

## State Management

### FreeCRM Custom

```csharp
// Single injected state object
@inject BlazorDataModel Model

// Access shared state
Model.User;           // Current user
Model.TenantId;       // Current tenant
Model.Tags;           // Cached tag list
Model.LoggedIn;       // Auth state
Model.View;           // Current page name

// Trigger re-render across components
Model.NotifyStateChanged();
```

### Standard .NET Blazor

```csharp
// Cascading parameters
<CascadingValue Value="@currentUser">
    <Router>...</Router>
</CascadingValue>

// State container pattern
public class AppState
{
    public User CurrentUser { get; set; }
    public event Action OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}

// Or: Fluxor, Blazor-State, etc.
```

### BlazorDataModel Explained

`BlazorDataModel` is FreeCRM's **central state container**. It:
- Holds the logged-in user
- Caches frequently-used lists (Tags, Users, Departments)
- Manages SignalR connection state
- Provides UI state (current view, loading flags)
- Fires events for cross-component updates

```csharp
// Standard pattern in every page
@inject BlazorDataModel Model

@code {
    protected override void OnInitialized()
    {
        Model.OnChange += OnDataModelUpdated;
    }

    protected void OnDataModelUpdated()
    {
        if (Model.View == _pageName) {
            StateHasChanged();
        }
    }
}
```

---

## Dependency Injection

### Both Are Standard .NET DI

```csharp
// These are all standard .NET dependency injection:
@inject HttpClient Http
@inject IJSRuntime jsRuntime
@inject NavigationManager NavManager
@inject ILocalStorageService LocalStorage  // Blazored.LocalStorage package

// This is FreeCRM custom, but injected via standard DI:
@inject BlazorDataModel Model
```

### Registration (Program.cs)

```csharp
// Standard .NET registrations
builder.Services.AddScoped<HttpClient>();
builder.Services.AddBlazoredLocalStorage();

// FreeCRM custom registrations
builder.Services.AddScoped<BlazorDataModel>();
builder.Services.AddScoped<IDataAccess, DataAccess>();
```

---

## Quick Reference: Custom vs Standard

### Definitely FreeCRM Custom

| Pattern | What It Does |
|---------|--------------|
| `Helpers.*` | All utility methods |
| `DataObjects.*` | All data transfer objects |
| `DataAccess.*` | All database methods |
| `Model.*` | BlazorDataModel state |
| `<Language>` | Localization component |
| `<Icon>` | Icon rendering component |
| `<DeleteConfirmation>` | Delete button with confirm |
| `<LoadingMessage>` | Loading spinner |
| `<RequiredIndicator>` | "* = required" legend |
| `CurrentUser` | Controller user property |

### Definitely Standard .NET

| Pattern | What It Is |
|---------|-----------|
| `@inject` | Blazor dependency injection |
| `@bind` | Blazor two-way binding |
| `@onclick` | Blazor event handling |
| `HttpClient` | .NET HTTP client |
| `NavigationManager` | Blazor navigation service |
| `IJSRuntime` | Blazor JS interop |
| `Task<T>`, `async/await` | C# async patterns |
| `partial class` | C# language feature |
| `IDisposable` | .NET interface |
| `System.Timers.Timer` | .NET timer class |

### Looks Custom But Is Standard

| Pattern | Actually Is |
|---------|-------------|
| `@implements IDisposable` | Standard C# interface |
| `StateHasChanged()` | Blazor component method |
| `OnInitialized()` | Blazor lifecycle |
| `OnAfterRenderAsync()` | Blazor lifecycle |
| `InvokeAsync()` | Blazor thread marshaling |
| `[Parameter]` | Blazor attribute |
| `EventCallback<T>` | Blazor event pattern |

### Looks Standard But Is Custom

| Pattern | Actually Is |
|---------|-------------|
| `Model.User` | FreeCRM (not .NET's User) |
| `Model.LoggedIn` | FreeCRM (not AuthenticationState) |
| `Helpers.NavigateTo()` | FreeCRM wrapper |
| `Helpers.GetOrPost<T>()` | FreeCRM wrapper |
| `DataObjects.User` | FreeCRM class |
| `CurrentUser` | FreeCRM pattern |

---

## Gotchas for .NET Developers

### Coming from ASP.NET MVC/Web API

| You might expect... | FreeCRM does... |
|---------------------|---------------------|
| `User.Identity.Name` | `CurrentUser.DisplayName` |
| `[Authorize]` attribute | Manual `if (!Model.User.Admin)` checks |
| `ILogger<T>` | `Helpers.ConsoleLog()` (client-side) |
| Entity Framework DbSet | `DataAccess.GetUsers()` methods |
| AutoMapper | `Helpers.DuplicateObject<T>()` |

### Coming from Standard Blazor

| You might expect... | FreeCRM does... |
|---------------------|---------------------|
| `NavigationManager.NavigateTo()` | `Helpers.NavigateTo()` |
| `IStringLocalizer` | `<Language>` / `Helpers.Text()` |
| Cascading auth state | `Model.LoggedIn` / `Model.User` |
| `HttpClient.GetFromJsonAsync()` | `Helpers.GetOrPost<T>()` |
| Component libraries (MudBlazor, etc.) | Custom + Radzen + Bootstrap |

### Common Mistakes

```csharp
// DON'T: Use NavigationManager directly (breaks tenant routing)
NavManager.NavigateTo("/Settings/Tags");

// DO: Use Helpers wrapper
Helpers.NavigateTo("Settings/Tags");
```

```csharp
// DON'T: Use HttpClient directly (missing auth headers)
var user = await Http.GetFromJsonAsync<User>("api/Data/GetUser/" + id);

// DO: Use Helpers wrapper
var user = await Helpers.GetOrPost<DataObjects.User>("api/Data/GetUser/" + id);
```

```csharp
// DON'T: Create flat classes for DTOs
public class User { ... }

// DO: Nest in DataObjects for autocomplete
public partial class DataObjects
{
    public partial class User { ... }
}
```

```csharp
// DON'T: Manually serialize/deserialize
var json = JsonSerializer.Serialize(obj);
var obj = JsonSerializer.Deserialize<T>(json);

// DO: Use Helpers
var json = Helpers.SerializeObject(obj);
var obj = Helpers.DeserializeObject<T>(json);
```

---

## When to Use Standard .NET vs FreeCRM

### Use FreeCRM Custom When:
- Navigating between pages (`Helpers.NavigateTo`)
- Making API calls (`Helpers.GetOrPost`)
- Accessing user info (`Model.User`)
- Localizing text (`<Language>`, `Helpers.Text`)
- Checking auth state (`Model.LoggedIn`)
- Working with data objects (`DataObjects.*`)

### Use Standard .NET When:
- Lower-level operations (string manipulation, LINQ, etc.)
- Lifecycle methods (`OnInitialized`, etc.)
- Component communication (`[Parameter]`, `EventCallback`)
- Async patterns (`async/await`, `Task<T>`)
- Collections (`List<T>`, `Dictionary<K,V>`)
- Timers (`System.Timers.Timer`)

### The Line Is Clear

If you're wondering "is this FreeCRM or .NET?":
1. Check if it starts with `Helpers.`, `Model.`, `DataObjects.`, or `DataAccess.`
2. If yes → FreeCRM custom
3. If no → Probably standard .NET (check Microsoft docs)

---

## Multi-Tenant vs Single-Tenant Architecture

FreeCRM supports both multi-tenant and single-tenant deployments. Understanding when to use each is crucial.

### What Tenants Are For

Tenants exist to **control who sees what data**. They're useful when:
- Multiple organizations share one deployment
- Different departments need data isolation
- You have a SaaS model with separate client accounts
- Users login and need role-based access control

### When You DON'T Need Tenants

Skip tenant architecture when your app is:
- **Public-facing** - Everyone sees the same content (marketing site, documentation)
- **Single-organization** - One company, no data isolation needed
- **No login required** - Or minimal login (admin-only)
- **Standalone tool** - Like DependencyManager, where all users share data

### How to Tell Which You Need

| Scenario | Use Tenants? |
|----------|--------------|
| CRM for multiple companies | ✅ Yes - each company is a tenant |
| Helpdesk with departments | ✅ Yes - departments can be tenants |
| Public blog/CMS | ❌ No - everyone sees same content |
| Internal dashboard tool | ❌ No - one org, shared data |
| Form builder (nForm) | ✅ Yes - forms belong to specific accounts |
| Network diagram tool | ❌ No - users share dependency docs |

### What Changes Without Tenants

| Aspect | Multi-Tenant | Single-Tenant |
|--------|--------------|---------------|
| Routes | `/{TenantCode}/Settings/Tags` | `/Settings/Tags` |
| Data queries | `WHERE TenantId = @TenantId` | No tenant filter |
| URL validation | `Helpers.ValidateUrl(TenantCode)` | Not needed |
| Navigation | Includes tenant prefix | Direct paths |
| Login | Required (usually) | Optional |

### Example: DependencyManager (Single-Tenant)

DependencyManager doesn't use tenant routing because:
- It's a standalone tool, not a SaaS platform
- All users work on shared dependency documents
- No need to isolate data between organizations
- Simpler URLs: `/EditDependency/{id}` instead of `/acme/EditDependency/{id}`

---

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────┐
│                     FreeCRM Layer                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   Helpers   │  │ DataObjects │  │    DataAccess       │  │
│  │ NavigateTo  │  │    .User    │  │    .GetUser()       │  │
│  │ GetOrPost   │  │    .Tag     │  │    .SaveTag()       │  │
│  │ Text()      │  │    .Tenant  │  │    .DeleteItem()    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────┐│
│  │              BlazorDataModel (State)                    ││
│  │  .User  .TenantId  .Tags  .LoggedIn  .NotifyChanged()  ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Standard .NET Layer                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ HttpClient  │  │  NavManager │  │    IJSRuntime       │  │
│  │ (HTTP)      │  │  (Routing)  │  │    (JS Interop)     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   Blazor    │  │    EF Core  │  │    ASP.NET Core     │  │
│  │ Components  │  │  (Database) │  │    (Web Server)     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

*Category: 006_architecture*
*Last Updated: 2025-12-23*
*Audience: Developers new to FreeCRM who know .NET*
