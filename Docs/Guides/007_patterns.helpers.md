# FreeCRM: Helpers & Utilities Guide

> Complete reference for the `Helpers` static class that provides essential utilities for Blazor client applications.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Overview](#overview) | ~35 | Initialization and key principles |
| [Top 25 Most Used Helpers](#top-25-most-used-helpers) | ~50 | Quick reference by usage frequency |
| [Navigation Helpers](#navigation-helpers) | ~85 | `NavigateTo`, `BuildUrl`, `ValidateUrl` |
| [Text & Language](#text--language-mechanics) | ~160 | Localization with `Text()` and `<Language>` |
| [HTTP Helpers](#http-helpers) | ~230 | `GetOrPost<T>()` for API calls |
| [Form Validation](#form-validation-helpers) | ~330 | `MissingValue()`, `MissingRequiredField()` |
| [UI Helpers](#ui-helpers) | ~420 | Focus, icons, console logging |
| [Serialization](#data-serialization-helpers) | ~530 | JSON serialize/deserialize/duplicate |
| [Formatting](#formatting-helpers) | ~590 | Date, currency, file size formatting |
| [String Helpers](#string--value-helpers) | ~660 | Null-safe conversions, type checking |
| [Quick Reference Card](#quick-reference-card) | ~730 | Copy-paste examples |
| [Extending Helpers](#extending-helpers-helpersappcs) | ~800 | Adding project-specific helpers |

**Source:** FreeCRM base template (`Helpers.cs` in Client project)

---

## Overview

The `Helpers` class is a static partial class that must be initialized in `MainLayout.razor` before use:

```csharp
// In MainLayout.razor OnAfterRenderAsync
Helpers.Init(jsRuntime, Model, Http, LocalStorage, DialogService, TooltipService, NavManager);
```

Once initialized, all helpers are available throughout the application via `Helpers.MethodName()`.

**Key Principle**: If there's a Helper for it, use it. Don't reinvent utilities that already exist in the shared library.

---

## Top 25 Most Used Helpers

Based on usage analysis across FreeCRM-main (public) and private repos nForm, Helpdesk4, and TrusselBuilder:

| Rank | Helper | Total Uses | Purpose |
|------|--------|------------|---------|
| 1 | `Text()` | 954 | Language/localization |
| 2 | `GetOrPost<T>()` | 457 | HTTP GET/POST operations |
| 3 | `DelayedFocus()` | 380 | Focus management |
| 4 | `NavigateTo()` | 332 | Page navigation |
| 5 | `MissingRequiredField()` | 227 | Validation messages |
| 6 | `MissingValue()` | 189 | Input validation styling |
| 7 | `StringValue()` | 178 | Null-safe string conversion |
| 8 | `NavigateToRoot()` | 163 | Navigate to app root |
| 9 | `ValidateUrl()` | 152 | URL validation |
| 10 | `BuildUrl()` | 133 | URL generation |
| 11 | `StringLower()` | 103 | Lowercase conversion |
| 12 | `GuidValue()` | 88 | Null-safe GUID conversion |
| 13 | `DeserializeObject<T>()` | 78 | JSON deserialization |
| 14 | `GetObjectPropertyValue<T>()` | 74 | Dynamic property access |
| 15 | `ConfirmButtonTextCancel` | 73 | Localized "Cancel" |
| 16 | `DuplicateObject<T>()` | 66 | Deep clone objects |
| 17 | `ConfirmButtonTextConfirmDelete` | 66 | Localized "Confirm Delete" |
| 18 | `ConfirmButtonTextDelete` | 63 | Localized "Delete" |
| 19 | `Icon()` | 49 | Icon rendering |
| 20 | `FormatDateTime()` | 49 | Date/time formatting |
| 21 | `BooleanToIcon()` | 46 | Boolean to icon conversion |
| 22 | `ReloadModel()` | 45 | Refresh data model |
| 23 | `SerializeObject()` | 44 | JSON serialization |
| 24 | `BaseUri` | 34 | Base URI property |
| 25 | `ConsoleLog()` | 33 | Debug logging |

---

## Navigation Helpers

### NavigateTo

Navigates to a page within the application, automatically prepending the tenant URL.

```csharp
// Signature
public static void NavigateTo(string subUrl, bool forceReload = false)

// Usage Examples
Helpers.NavigateTo("Settings/Tags");           // Goes to /TenantCode/Settings/Tags
Helpers.NavigateTo("Settings/EditTag/" + id);  // Goes to /TenantCode/Settings/EditTag/{id}
Helpers.NavigateTo("https://external.com");    // External URLs work too
Helpers.NavigateTo("Login", true);             // Force full page reload
```

**When to use**: Any time you need to navigate programmatically. Don't use `NavManager.NavigateTo()` directly.

### NavigateToRoot

Navigates to the application root (home page).

```csharp
// Signature
public static void NavigateToRoot(bool forceReload = false)

// Usage Examples
Helpers.NavigateToRoot();       // Go to home page
Helpers.NavigateToRoot(true);   // Go to home with full reload
```

**Common Pattern** - Permission check redirect:

```csharp
if (!Model.User.Admin) {
    Helpers.NavigateToRoot();
    return;
}
```

### BuildUrl

Generates a full application URL for use in `href` attributes.

```csharp
// Signature
public static string BuildUrl(string? subUrl = "")

// Usage Examples
<a href="@Helpers.BuildUrl("Settings/Tags")" class="btn btn-dark">
    <Language Tag="Back" IncludeIcon="true" />
</a>

<a href="@Helpers.BuildUrl("Data/" + Model.Form.FormId.ToString())" class="btn btn-primary">
    View Data
</a>
```

**Key Difference**:
- `BuildUrl()` returns a string for `href` attributes
- `NavigateTo()` performs actual navigation

### ValidateUrl

Validates the current URL matches the tenant context. Call this in `OnAfterRenderAsync`:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (Model.Loaded && Model.LoggedIn) {
        await Helpers.ValidateUrl(TenantCode);
        // ... rest of your code
    }
}
```

---

## Text & Language Mechanics

### Helpers.Text()

Returns localized text for a language tag. This is the core localization function.

```csharp
// Signature
public static string Text(string? text,
    bool ReplaceSpaces = false,
    List<string>? ReplaceValues = null,
    bool MarkUndefinedStrings = true,
    TextCase textCase = TextCase.Normal)

// Basic Usage
var label = Helpers.Text("Save");           // Returns "Save" (or localized version)
var title = Helpers.Text("EditTag");        // Returns "Edit Tag"

// With Replacement Values (for parameterized strings)
var error = Helpers.Text("RequiredMissing", false, new List<string> { "Email" });
// If RequiredMissing = "The {0} field is required" -> "The Email field is required"

// With HTML non-breaking spaces
var label = Helpers.Text("LastModified", true);  // Spaces become &nbsp;

// Case transformation
var upper = Helpers.Text("Save", false, null, true, TextCase.Uppercase);
```

### Language Component

For use in Razor markup, prefer the `<Language>` component over `Helpers.Text()`:

```razor
@* Basic usage *@
<Language Tag="Save" />

@* With icon (looks up icon by same tag name) *@
<Language Tag="Save" IncludeIcon="true" />

@* For required field labels *@
<label>
    <Language Tag="Email" Required="true" />
</label>

@* With case transformation *@
<Language Tag="Status" TransformCase="TextCase.Uppercase" />

@* With custom class *@
<Language Tag="Warning" Class="text-danger" />
```

**When to Use Each**:
- `<Language>` component: In Razor markup for visible UI text
- `Helpers.Text()`: In C# code for validation messages, dynamic strings, or `title` attributes

### Confirmation Button Text Properties

Pre-localized strings for delete confirmation dialogs:

```razor
<DeleteConfirmation
    OnConfirmed="Delete"
    CancelText="@Helpers.ConfirmButtonTextCancel"
    DeleteText="@Helpers.ConfirmButtonTextDelete"
    ConfirmDeleteText="@Helpers.ConfirmButtonTextConfirmDelete" />
```

---

## HTTP Helpers

### GetOrPost<T>

The primary method for all API communication. Automatically handles:
- GET vs POST based on whether data is provided
- Authentication headers (TenantId, Token, Fingerprint)
- JSON serialization/deserialization
- Error logging

```csharp
// Signature
public static async Task<T?> GetOrPost<T>(string url, object? post = null, bool logResults = false)

// GET request
var tag = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/GetTag/" + id);

// POST request (passing an object triggers POST)
var saved = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/SaveTag", _tag);

// With debug logging
var result = await Helpers.GetOrPost<DataObjects.User>("api/Data/GetUser/" + id, null, true);

// For boolean responses
var deleted = await Helpers.GetOrPost<DataObjects.BooleanResponse>("api/Data/DeleteTag/" + id);
```

**Complete Save Pattern**:

```csharp
protected async Task Save()
{
    Model.ClearMessages();

    // 1. Validate
    List<string> errors = new List<string>();
    string focus = "";

    if (String.IsNullOrWhiteSpace(_tag.Name)) {
        errors.Add(Helpers.MissingRequiredField("TagName"));
        if (focus == "") { focus = "edit-tag-Name"; }
    }

    if (errors.Any()) {
        Model.ErrorMessages(errors);
        await Helpers.DelayedFocus(focus);
        return;
    }

    // 2. Show saving message
    Model.Message_Saving();

    // 3. Call API
    var saved = await Helpers.GetOrPost<DataObjects.Tag>("api/Data/SaveTag", _tag);

    // 4. Clear messages and handle response
    Model.ClearMessages();

    if (saved != null) {
        if (saved.ActionResponse.Result) {
            Helpers.NavigateTo("Settings/Tags");
        } else {
            Model.ErrorMessages(saved.ActionResponse.Messages);
        }
    } else {
        Model.UnknownError();
    }
}
```

**Complete Delete Pattern**:

```csharp
protected async Task Delete()
{
    Model.ClearMessages();
    Model.Message_Deleting();

    var deleted = await Helpers.GetOrPost<DataObjects.BooleanResponse>("api/Data/DeleteTag/" + id);

    Model.ClearMessages();

    if (deleted != null) {
        if (deleted.Result) {
            // Update local cache
            Model.Tags = Model.Tags.Where(x => x.TagId.ToString() != id).ToList();
            Helpers.NavigateTo("Settings/Tags");
        } else {
            Model.ErrorMessages(deleted.Messages);
        }
    } else {
        Model.UnknownError();
    }
}
```

---

## Form Validation Helpers

### MissingValue

Returns a CSS class to highlight empty required fields. Use on form inputs.

```csharp
// Signature
public static string MissingValue(string? value, string? defaultClass = "")

// Returns "m-r" class if value is empty, otherwise returns defaultClass
```

**Usage in Forms**:

```razor
@* Text input *@
<input type="text"
       id="edit-tag-Name"
       @bind="_tag.Name"
       @bind:event="oninput"
       class="@Helpers.MissingValue(_tag.Name, "form-control")" />

@* Select dropdown *@
<select id="edit-item-Status"
        class="@Helpers.MissingValue(_item.Status, "form-select")"
        @bind="_item.Status">
    <option value="">-- Select Status --</option>
    @foreach (var status in _statuses) {
        <option value="@status.Value">@status.Name</option>
    }
</select>
```

**Overloads for Different Types**:

```csharp
Helpers.MissingValue(string? value, string? defaultClass = "")
Helpers.MissingValue(Guid? value, string? defaultClass = "")
Helpers.MissingValue(DateTime? value, string? defaultClass = "")
Helpers.MissingValue(decimal? value, string? defaultClass = "")
Helpers.MissingValue(int? value, string? defaultClass = "")
```

### MissingRequiredField

Generates a localized validation error message for a missing field.

```csharp
// Signature
public static string MissingRequiredField(string fieldName)

// Usage
if (String.IsNullOrWhiteSpace(_tag.Name)) {
    errors.Add(Helpers.MissingRequiredField("TagName"));
    if (focus == "") { focus = "edit-tag-Name"; }
}

// Returns something like "The Tag Name field is required"
```

**Complete Validation Pattern**:

```csharp
List<string> errors = new List<string>();
string focus = "";

if (String.IsNullOrWhiteSpace(_item.Name)) {
    errors.Add(Helpers.MissingRequiredField("Name"));
    if (focus == "") { focus = "edit-item-Name"; }
}

if (_item.Price <= 0) {
    errors.Add(Helpers.MissingRequiredField("Price"));
    if (focus == "") { focus = "edit-item-Price"; }
}

if (_item.CategoryId == Guid.Empty) {
    errors.Add(Helpers.MissingRequiredField("Category"));
    if (focus == "") { focus = "edit-item-Category"; }
}

if (errors.Any()) {
    Model.ErrorMessages(errors);
    await Helpers.DelayedFocus(focus);
    return;
}
```

---

## UI Helpers

### DelayedFocus

Sets focus to an element after a brief delay (allows DOM to render first).

```csharp
// Signature
public static async Task DelayedFocus(string elementId)

// Usage
await Helpers.DelayedFocus("edit-tag-Name");

// Common patterns:
// After page loads
_loading = false;
StateHasChanged();
await Helpers.DelayedFocus("edit-tag-Name");

// After validation error
if (errors.Any()) {
    Model.ErrorMessages(errors);
    await Helpers.DelayedFocus(focus);
    return;
}
```

### DelayedSelect

Sets focus and selects all text in an input element.

```csharp
// Signature
public static async Task DelayedSelect(string elementId)

// Usage - useful for inputs where user will likely replace all content
await Helpers.DelayedSelect("edit-item-Quantity");
```

### Icon

Returns icon HTML for a named icon. Icons are mapped in the Helpers.Icons dictionary.

```csharp
// Signature
public static string Icon(string? name, bool WrapInElement = false)

// In C# code
var iconHtml = Helpers.Icon("Save", true);  // Returns <i class="...">save</i>

// In Razor (prefer the Icon component instead)
@((MarkupString)Helpers.Icon("Save", true))
```

**Prefer the Icon Component in Razor**:

```razor
@* Use this *@
<Icon Name="Save" />

@* For buttons with just icons *@
<button type="button" class="btn btn-dark" title="@Helpers.Text("Reload")">
    <Icon Name="Reload" />
</button>

@* Language component with icon *@
<Language Tag="Save" IncludeIcon="true" />
```

### BooleanToIcon

Returns an icon based on a boolean value (useful for displaying status in tables).

```csharp
// Signature
public static string BooleanToIcon(bool? value, string? icon = "")

// Usage in table cells
<td>@((MarkupString)Helpers.BooleanToIcon(_item.IsActive))</td>
<td>@((MarkupString)Helpers.BooleanToIcon(_item.IsPrimary, "Star"))</td>
```

### BooleanToCheckboxIcons

Returns checkbox-style icons for true/false display.

```csharp
// Signature
public static string BooleanToCheckboxIcons(bool? value, string? iconChecked = "", string? iconUnchecked = "")

// Usage
<td>@((MarkupString)Helpers.BooleanToCheckboxIcons(_item.Enabled))</td>
```

### ConsoleLog

Debug logging via JavaScript interop.

```csharp
// Signatures
public static async Task ConsoleLog(string message)
public static async Task ConsoleLog(string message, params object[] objects)
public static async Task ConsoleLog(params object[] objects)

// Usage
await Helpers.ConsoleLog("Save completed");
await Helpers.ConsoleLog("Tag data", _tag);
await Helpers.ConsoleLog("Before/After", oldValue, newValue);
```

---

## Data Serialization Helpers

### SerializeObject

Converts an object to JSON string.

```csharp
// Signature
public static string SerializeObject(object? Object, bool formatOutput = false)

// Usage
string json = Helpers.SerializeObject(_tag);
string prettyJson = Helpers.SerializeObject(_tag, true);  // Formatted/indented
```

### DeserializeObject<T>

Converts a JSON string to an object.

```csharp
// Signature
public static T? DeserializeObject<T>(string? SerializedObject)

// Usage
var tag = Helpers.DeserializeObject<DataObjects.Tag>(jsonString);

// Common in SignalR handlers
var tag = Helpers.DeserializeObject<DataObjects.Tag>(update.ObjectAsString);
if (tag != null) {
    _tag = tag;
    StateHasChanged();
}
```

### DuplicateObject<T>

Creates a deep clone of an object (via serialize/deserialize).

```csharp
// Signature
public static T? DuplicateObject<T>(object? o)

// Usage - create a copy before editing
var editCopy = Helpers.DuplicateObject<DataObjects.Tag>(_originalTag);

// Usage - duplicate a record
var newItem = Helpers.DuplicateObject<DataObjects.Item>(_item);
newItem.ItemId = Guid.Empty;  // Clear ID for new record
newItem.Name = _item.Name + " (Copy)";
```

---

## Formatting Helpers

### FormatDateTime

Formats a DateTime for display.

```csharp
// Signature
public static string FormatDateTime(DateTime? date, bool ReplaceSpaces = false, bool ToLocalTimezone = true)

// Usage
<td>@Helpers.FormatDateTime(_item.LastModified)</td>

// With non-breaking spaces (for table headers that shouldn't wrap)
<th>@Helpers.FormatDateTime(_item.Added, true)</th>
```

### FormatDate

Formats just the date portion.

```csharp
// Signature
public static string FormatDate(DateTime? date, bool ReplaceSpaces = false, bool ToLocalTimezone = true)

// Usage
<td>@Helpers.FormatDate(_item.DueDate)</td>
```

### FormatTime

Formats just the time portion.

```csharp
// Signature
public static string FormatTime(DateTime? date, bool Compressed = false, bool ToLocalTimezone = true)

// Usage
<td>@Helpers.FormatTime(_appointment.StartTime)</td>
<td>@Helpers.FormatTime(_appointment.StartTime, true)</td>  // Compressed format
```

### FormatCurrency

Formats a currency value.

```csharp
// Signature
public static string FormatCurrency(string? value, bool ReplaceSpaces = false)

// Usage
<td>@Helpers.FormatCurrency(_invoice.Total)</td>
```

### BytesToFileSizeLabel

Converts byte count to human-readable file size.

```csharp
// Signature
public static string BytesToFileSizeLabel(long? bytes, List<string>? labels = null)

// Usage
<span>@Helpers.BytesToFileSizeLabel(_file.Size)</span>  // Returns "1.5mb"
```

---

## String & Value Helpers

### StringValue

Null-safe string conversion. Returns empty string instead of null.

```csharp
// Signature
public static string StringValue(string? input)

// Usage - comparing strings safely
if (Helpers.StringValue(Model.NavigationId) != Helpers.StringValue(id)) {
    await LoadData();
}
```

### GuidValue

Null-safe GUID conversion. Returns Guid.Empty instead of null.

```csharp
// Signatures
public static Guid GuidValue(Guid? value)
public static Guid GuidValue(string? value)

// Usage
var userId = Helpers.GuidValue(userIdString);
if (userId != Guid.Empty) {
    // Valid GUID
}
```

### IsGuid / IsDate / IsInt / IsNumeric

Type checking helpers.

```csharp
if (Helpers.IsGuid(idString)) {
    var id = new Guid(idString);
}

if (Helpers.IsDate(dateString)) {
    // Parse date
}

if (Helpers.IsNumeric(value)) {
    // Safe to parse as number
}
```

### MaxStringLength

Truncates a string with optional ellipsis.

```csharp
// Signature
public static string MaxStringLength(string? input, int maxLength = 100, bool addEllipses = true)

// Usage
<td title="@_item.Description">@Helpers.MaxStringLength(_item.Description, 50)</td>
```

### LinesInString

Counts lines in a string (useful for textarea sizing).

```csharp
// Signature
public static int LinesInString(string? input, int MinimumLines = 0)

// Usage
int rows = Helpers.LinesInString(_item.Notes, 3);
<textarea rows="@rows" @bind="_item.Notes"></textarea>
```

---

## Quick Reference Card

### Navigation
```csharp
Helpers.NavigateTo("Settings/Tags")           // Go to page
Helpers.NavigateToRoot()                      // Go to home
Helpers.BuildUrl("Settings/Tags")             // Get URL string for href
await Helpers.ValidateUrl(TenantCode)         // Validate tenant URL
```

### Text & Language
```csharp
Helpers.Text("Save")                          // Get localized text
Helpers.ConfirmButtonTextCancel               // "Cancel"
Helpers.ConfirmButtonTextDelete               // "Delete"
Helpers.ConfirmButtonTextConfirmDelete        // "Confirm Delete"
```

### HTTP
```csharp
await Helpers.GetOrPost<T>("api/endpoint")           // GET request
await Helpers.GetOrPost<T>("api/endpoint", data)     // POST request
```

### Validation
```csharp
Helpers.MissingValue(value, "form-control")   // CSS class for empty fields
Helpers.MissingRequiredField("FieldName")     // Error message for field
```

### UI
```csharp
await Helpers.DelayedFocus("element-id")      // Set focus after render
await Helpers.DelayedSelect("element-id")     // Focus and select all
Helpers.Icon("Save", true)                    // Get icon HTML
Helpers.BooleanToIcon(value)                  // Boolean to checkmark
```

### Data
```csharp
Helpers.SerializeObject(obj)                  // Object to JSON
Helpers.DeserializeObject<T>(json)            // JSON to object
Helpers.DuplicateObject<T>(obj)               // Deep clone
```

### Formatting
```csharp
Helpers.FormatDateTime(date)                  // Format date+time
Helpers.FormatDate(date)                      // Format date only
Helpers.FormatCurrency(value)                 // Format currency
Helpers.BytesToFileSizeLabel(bytes)           // "1.5mb"
```

### Strings
```csharp
Helpers.StringValue(str)                      // Null-safe string
Helpers.GuidValue(guid)                       // Null-safe GUID
Helpers.MaxStringLength(str, 50)              // Truncate with ellipsis
Helpers.IsGuid(str) / IsDate(str)             // Type checking
```

---

## Extending Helpers: Helpers.App.cs

The core `Helpers.cs` file is part of the FreeCRM-main base template and **should not be modified**. To add project-specific helpers, use the `Helpers.App.cs` partial class file.

### Pattern

```csharp
// Helpers.App.cs - Your project-specific helpers
namespace YourProject.Client;

public static partial class Helpers
{
    // Add your app-specific helpers here

    /// <summary>
    /// Example: Format a dependency relationship for display.
    /// </summary>
    public static string DependencyRelationshipToString(DataObjects.DependencyRelationship relationship)
    {
        return relationship switch {
            DataObjects.DependencyRelationship.Hosts => "Hosts",
            DataObjects.DependencyRelationship.Instance => "Instance Of",
            DataObjects.DependencyRelationship.Requires => "Requires",
            _ => ""
        };
    }

    /// <summary>
    /// Example: Get color for a relationship type.
    /// </summary>
    public static string DependencyRelationshipColor(DataObjects.DependencyRelationship relationship)
    {
        return relationship switch {
            DataObjects.DependencyRelationship.Hosts => "#28a745",
            DataObjects.DependencyRelationship.Instance => "#6c757d",
            DataObjects.DependencyRelationship.Requires => "#dc3545",
            _ => "#000"
        };
    }
}
```

### Why This Pattern?

- **Base template stays clean** - FreeCRM-main updates won't conflict with your code
- **Autocomplete works** - `Helpers.YourMethod()` appears alongside core helpers
- **Partial class merging** - C# combines both files into one class at compile time

### Common App-Specific Helpers

Projects often add helpers for:
- Domain-specific formatting (relationships, statuses, types)
- Custom validation logic
- Project-specific UI rendering
- Integration with app-specific components

---

## Important Notes

1. **Always use Helpers** - Don't roll your own navigation, HTTP, or validation logic
2. **Initialize first** - `Helpers.Init()` must be called in MainLayout before any helpers work
3. **Language component in Razor** - Use `<Language Tag="..." />` in markup, `Helpers.Text()` in code
4. **BuildUrl vs NavigateTo** - `BuildUrl` returns strings for `href`, `NavigateTo` performs navigation
5. **MissingValue pattern** - Always combine with `MissingRequiredField` for complete validation
6. **GetOrPost handles everything** - Auth headers, JSON, errors - don't use HttpClient directly

---

*Category: 007_patterns*
*Last Updated: 2025-12-23*
*Source: FreeCRM base template*
