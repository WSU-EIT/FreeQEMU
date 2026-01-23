# FreeCRM: Razor Page Templates

> Skeleton templates and patterns for CRUD pages in FreeCRM-based Blazor applications.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Routing Patterns](#routing-multi-tenant-vs-single-tenant) | ~35 | Multi-tenant vs single-tenant URLs |
| [List Page Template](#list-page-template) | ~100 | Filterable list with pagination |
| [Edit Page Template](#edit-page-template) | ~300 | Create/edit with validation |
| [Permission Checking](#permission-checking-patterns) | ~500 | Admin and role checks |
| [Loading States](#loading-state-patterns) | ~550 | Loading spinners and messages |
| [Save/Delete Patterns](#savedelete-patterns) | ~600 | Standard CRUD operations |
| [SignalR Integration](#signalr-integration-pattern) | ~700 | Real-time updates |

**Source:** FreeCRM base template (Tags implementation)

---

## Routing: Multi-Tenant vs Single-Tenant

FreeCRM supports two routing patterns depending on your application type:

### Multi-Tenant Routing (Default)

Use when your application has:
- Multiple organizations/companies sharing one deployment
- User login with different permission levels
- Data isolation between departments or clients

```razor
@* Dual route pattern - supports both direct and tenant-prefixed URLs *@
@page "/Settings/ExampleItems"
@page "/{TenantCode}/Settings/ExampleItems"
```

This allows both `/Settings/ExampleItems` and `/acme-corp/Settings/ExampleItems` to work.

### Single-Tenant Routing (Simpler)

Use when your application is:
- A public-facing site with no login (or minimal login)
- A single-organization internal tool
- An app where everyone sees the same data

```razor
@* Simple route - no tenant code needed *@
@page "/Settings/ExampleItems"
@page "/Settings/EditExampleItem/{id}"
```

**Key differences for single-tenant apps:**
- Remove the `@page "/{TenantCode}/..."` route directives
- Remove `[Parameter] public string? TenantCode { get; set; }`
- Remove `Model.TenantCodeFromUrl = TenantCode;` from `OnAfterRenderAsync`
- Remove `await Helpers.ValidateUrl(TenantCode);` calls
- Use `Helpers.NavigateTo()` normally (it handles both modes)

**Example: DependencyManager uses single-tenant routing** because it's designed as a standalone tool where all users see the same dependency documents.

---

## ExampleItem - Complete Template

This guide uses `ExampleItem` as a reference implementation demonstrating every common field type and UI pattern. Use this as a starting point when adding new entities to FreeCRM-based projects.

---

## Part 1: Data Object

### DataObjects.ExampleItems.cs

```csharp
namespace CRM;

public partial class DataObjects
{
    public partial class ExampleItem : ActionResponseObject
    {
        // Primary Key
        public Guid ExampleItemId { get; set; }

        // Tenant (required for multi-tenant)
        public Guid TenantId { get; set; }

        // ===== STRING FIELDS =====
        public string? Name { get; set; }                    // varchar(255) - required
        public string? Description { get; set; }             // varchar(max) / text
        public string? Code { get; set; }                    // varchar(50) - short code
        public string? Notes { get; set; }                   // varchar(max) - multiline

        // ===== BOOLEAN FIELDS =====
        public bool Enabled { get; set; }                    // bit NOT NULL
        public bool? IsActive { get; set; }                  // bit NULL (nullable bool)
        public bool IsDefault { get; set; }                  // bit NOT NULL DEFAULT 0

        // ===== NUMERIC FIELDS =====
        public int SortOrder { get; set; }                   // int NOT NULL
        public int? Priority { get; set; }                   // int NULL
        public long TotalCount { get; set; }                 // bigint NOT NULL
        public long? MaxValue { get; set; }                  // bigint NULL
        public decimal Amount { get; set; }                  // decimal(18,2) NOT NULL
        public decimal? Price { get; set; }                  // decimal(18,2) NULL
        public double Percentage { get; set; }               // float NOT NULL
        public double? Rate { get; set; }                    // float NULL

        // ===== GUID FIELDS =====
        public Guid CategoryId { get; set; }                 // uniqueidentifier NOT NULL
        public Guid? ParentId { get; set; }                  // uniqueidentifier NULL
        public Guid? AssignedUserId { get; set; }            // uniqueidentifier NULL (FK to Users)

        // ===== DATE/TIME FIELDS =====
        public DateTime StartDate { get; set; }              // datetime2 NOT NULL
        public DateTime? EndDate { get; set; }               // datetime2 NULL
        public DateOnly? EffectiveDate { get; set; }         // date NULL
        public TimeOnly? ScheduledTime { get; set; }         // time NULL

        // ===== ENUM FIELDS =====
        public ExampleItemStatus Status { get; set; }        // int (enum stored as int)
        public ExampleItemType? ItemType { get; set; }       // int NULL

        // ===== STANDARD AUDIT FIELDS =====
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // ===== NAVIGATION / COMPUTED =====
        public string? CategoryName { get; set; }            // Populated from join
        public string? AssignedUserName { get; set; }        // Populated from join
        public List<ExampleItemDetail> Details { get; set; } = new List<ExampleItemDetail>();
    }

    public enum ExampleItemStatus
    {
        Draft = 0,
        Active = 1,
        Completed = 2,
        Archived = 3
    }

    public enum ExampleItemType
    {
        Standard = 0,
        Premium = 1,
        Custom = 2
    }

    // Child/detail object example
    public partial class ExampleItemDetail
    {
        public Guid ExampleItemDetailId { get; set; }
        public Guid ExampleItemId { get; set; }
        public string? Value { get; set; }
        public int SortOrder { get; set; }
    }
}
```

---

## Part 2: List Page

### Pages/Settings/ExampleItems/ExampleItems.razor

```razor
@page "/Settings/ExampleItems"
@page "/{TenantCode}/Settings/ExampleItems"
@implements IDisposable
@using Blazored.LocalStorage
@inject IJSRuntime jsRuntime
@inject HttpClient Http
@inject ILocalStorageService LocalStorage
@inject BlazorDataModel Model

@if (Model.Loaded && Model.View == _pageName) {
    @if (_loading) {
        <h1 class="page-title">
            <Language Tag="ExampleItems" IncludeIcon="true" />
        </h1>
        <LoadingMessage />
    } else {
        <div class="@Model.StickyMenuClass">
            <h1 class="page-title">
                <Language Tag="ExampleItems" IncludeIcon="true" />
                <StickyMenuIcon />
            </h1>
            <div class="mb-2">
                <a href="@(Helpers.BuildUrl("Settings/AddExampleItem"))" role="button" class="btn btn-success">
                    <Language Tag="AddNewExampleItem" IncludeIcon="true" />
                </a>
            </div>
        </div>

        @* ===== FILTER TOGGLES ===== *@
        <div class="mb-2">
            <div class="form-check form-switch">
                <input type="checkbox" id="exampleitems-IncludeDeletedRecords" class="form-check-input"
                       @bind="Model.User.UserPreferences.IncludeDeletedItems" />
                <label for="exampleitems-IncludeDeletedRecords" class="form-check-label">
                    <Language Tag="IncludeDeletedRecords" />
                </label>
            </div>
        </div>

        <div class="mb-2">
            <div class="form-check form-switch">
                <input type="checkbox" id="exampleitems-EnabledItemsOnly" class="form-check-input"
                       @bind="Model.User.UserPreferences.EnabledItemsOnly" />
                <label for="exampleitems-EnabledItemsOnly" class="form-check-label">
                    <Language Tag="EnabledItemsOnly" />
                </label>
            </div>
        </div>

        @* ===== OPTIONAL: STATUS FILTER ===== *@
        <div class="mb-2">
            <label for="exampleitems-StatusFilter"><Language Tag="FilterByStatus" /></label>
            <select id="exampleitems-StatusFilter" class="form-select" style="max-width:200px;" @bind="_statusFilter">
                <option value=""><Language Tag="All" /></option>
                <option value="0"><Language Tag="StatusDraft" /></option>
                <option value="1"><Language Tag="StatusActive" /></option>
                <option value="2"><Language Tag="StatusCompleted" /></option>
                <option value="3"><Language Tag="StatusArchived" /></option>
            </select>
        </div>

        @* ===== DATA TABLE ===== *@
        @if (FilteredItems.Any()) {
            <table class="table table-sm">
                <thead>
                    <tr class="table-dark">
                        <th style="width:1%;"></th>
                        <th><Language Tag="Name" ReplaceSpaces="true" /></th>
                        <th><Language Tag="Code" ReplaceSpaces="true" /></th>
                        <th><Language Tag="Status" ReplaceSpaces="true" /></th>
                        <th class="right"><Language Tag="Amount" ReplaceSpaces="true" /></th>
                        <th class="center" style="width:1%;"><Language Tag="Enabled" ReplaceSpaces="true" /></th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in FilteredItems.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)) {
                        string itemClass = String.Empty;
                        if (item.Deleted) {
                            itemClass = "item-deleted";
                        } else if (!item.Enabled) {
                            itemClass = "disabled";
                        }

                        <tr class="@itemClass">
                            <td>
                                <button type="button" class="btn btn-xs btn-primary nowrap"
                                        @onclick="@(() => EditItem(item.ExampleItemId))">
                                    <Language Tag="Edit" IncludeIcon="true" />
                                </button>
                            </td>
                            <td>@item.Name</td>
                            <td>@item.Code</td>
                            <td>@Helpers.Text("Status" + item.Status.ToString())</td>
                            <td class="right">@item.Amount.ToString("C")</td>
                            <td class="center">@((MarkupString)Helpers.BooleanToIcon(item.Enabled))</td>
                        </tr>
                    }
                </tbody>
            </table>
        } else {
            <Language Tag="NoItemsToShow" />
        }
    }
}

@code {
    [Parameter] public string? TenantCode { get; set; }

    protected bool _loadedData = false;
    protected bool _loading = true;
    protected string _statusFilter = "";

    protected string _pageName = "exampleitems";

    // Computed property for filtered list
    protected IEnumerable<DataObjects.ExampleItem> FilteredItems {
        get {
            var items = Model.ExampleItems.AsEnumerable();

            // See if we should filter by enabled status
            if (Model.User.UserPreferences.EnabledItemsOnly) {
                items = items.Where(x => x.Enabled);
            }

            // See if we should include deleted records
            if (!Model.User.UserPreferences.IncludeDeletedItems) {
                items = items.Where(x => !x.Deleted);
            }

            // See if we should filter by status
            if (!String.IsNullOrWhiteSpace(_statusFilter) && int.TryParse(_statusFilter, out int status)) {
                items = items.Where(x => (int)x.Status == status);
            }

            return items;
        }
    }

    public void Dispose()
    {
        Model.OnChange -= OnDataModelUpdated;
        Model.OnSignalRUpdate -= SignalRUpdate;

        Model.Subscribers_OnChange.Remove(_pageName);
        Model.Subscribers_OnSignalRUpdate.Remove(_pageName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            Model.TenantCodeFromUrl = TenantCode;
        }

        if (Model.Loaded && Model.LoggedIn) {
            // See if this feature is enabled and user has permission
            if (!Model.FeatureEnabledExampleItems || !Model.User.Admin) {
                Helpers.NavigateToRoot();
                return;
            }

            await Helpers.ValidateUrl(TenantCode);

            if (!_loadedData) {
                _loadedData = true;
                await LoadData();
            }
        }
    }

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

    protected void OnDataModelUpdated()
    {
        if (Model.View == _pageName) {
            StateHasChanged();
        }
    }

    protected void EditItem(Guid ExampleItemId)
    {
        Helpers.NavigateTo("Settings/EditExampleItem/" + ExampleItemId.ToString());
    }

    protected async Task LoadData()
    {
        _loading = true;
        await Helpers.LoadExampleItems();
        _loading = false;

        StateHasChanged();
    }

    protected void SignalRUpdate(DataObjects.SignalRUpdate update)
    {
        if (update.UpdateType == DataObjects.SignalRUpdateType.ExampleItem &&
            Model.View == _pageName &&
            update.UserId != Model.User.UserId) {
            StateHasChanged();
        }
    }
}
```

---

## Part 3: Edit Page

### Pages/Settings/ExampleItems/EditExampleItem.razor

```razor
@page "/Settings/EditExampleItem/{id}"
@page "/{TenantCode}/Settings/EditExampleItem/{id}"
@page "/Settings/AddExampleItem"
@page "/{TenantCode}/Settings/AddExampleItem"
@implements IDisposable
@using Blazored.LocalStorage
@inject IJSRuntime jsRuntime
@inject HttpClient Http
@inject ILocalStorageService LocalStorage
@inject BlazorDataModel Model
@inject Radzen.DialogService DialogService

@if (Model.Loaded && Model.View == _pageName) {
    @if (_loading) {
        <h1 class="page-title">
            <Language Tag="@_title" IncludeIcon="true" />
        </h1>
        <LoadingMessage />
    } else {
        @* ===== DELETED RECORD VIEW ===== *@
        @if (_item.Deleted) {
            <h1 class="page-title">
                <Language Tag="@_title" IncludeIcon="true" />
            </h1>
            <UndeleteMessage DeletedAt="_item.DeletedAt"
                             LastModifiedBy="@_item.LastModifiedBy"
                             OnUndelete="(() => _item.Deleted = false)" />
        } else {
            @* ===== STICKY MENU ===== *@
            <div class="@Model.StickyMenuClass">
                <h1 class="page-title">
                    <Language Tag="@_title" IncludeIcon="true" />
                    <StickyMenuIcon />
                </h1>
                <div class="btn-group mb-2" role="group">
                    <a href="@(Helpers.BuildUrl("Settings/ExampleItems"))" class="btn btn-dark">
                        <Language Tag="Back" IncludeIcon="true" />
                    </a>

                    @if (!_item.Deleted) {
                        <button type="button" class="btn btn-success" @onclick="Save">
                            <Language Tag="Save" IncludeIcon="true" />
                        </button>

                        @if (!_newItem) {
                            <DeleteConfirmation OnConfirmed="Delete"
                                                CancelText="@Helpers.ConfirmButtonTextCancel"
                                                DeleteText="@Helpers.ConfirmButtonTextDelete"
                                                ConfirmDeleteText="@Helpers.ConfirmButtonTextConfirmDelete" />
                        }
                    }
                </div>
            </div>

            <RequiredIndicator />

            @* ===== TABS (optional - for complex forms) ===== *@
            <ul class="nav nav-tabs" role="tablist">
                <li class="nav-item" role="presentation">
                    <button class="nav-link active" id="tabGeneralButton" data-bs-toggle="tab"
                            data-bs-target="#tabGeneral" type="button" role="tab">
                        <Language Tag="General" />
                    </button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="tabDetailsButton" data-bs-toggle="tab"
                            data-bs-target="#tabDetails" type="button" role="tab">
                        <Language Tag="Details" />
                    </button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="tabAdvancedButton" data-bs-toggle="tab"
                            data-bs-target="#tabAdvanced" type="button" role="tab">
                        <Language Tag="Advanced" />
                    </button>
                </li>
            </ul>

            <div class="tab-content">
                @* ===== TAB: GENERAL ===== *@
                <div id="tabGeneral" class="tab-pane active" role="tabpanel">

                    @* ----- BOOLEAN: Switch ----- *@
                    <div class="mb-2 form-check form-switch">
                        <input type="checkbox" role="switch" class="form-check-input"
                               id="edit-item-Enabled" @bind="_item.Enabled" />
                        <label for="edit-item-Enabled" class="form-check-label">
                            <Language Tag="Enabled" />
                        </label>
                    </div>

                    @* ----- STRING: Required Text Input ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Name">
                            <Language Tag="Name" Required="true" />
                        </label>
                        <input type="text" id="edit-item-Name"
                               @bind="_item.Name" @bind:event="oninput"
                               class="@Helpers.MissingValue(_item.Name, "form-control")" />
                    </div>

                    @* ----- STRING: Optional Text Input ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Code"><Language Tag="Code" /></label>
                        <input type="text" id="edit-item-Code" class="form-control"
                               @bind="_item.Code" @bind:event="oninput" />
                    </div>

                    @* ----- STRING: With Info Note ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Description"><Language Tag="Description" /></label>
                        <span class="note">- <Language Tag="DescriptionInfo" /></span>
                        <input type="text" id="edit-item-Description" class="form-control"
                               @bind="_item.Description" />
                    </div>

                    @* ----- STRING: Textarea (multiline) ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Notes"><Language Tag="Notes" /></label>
                        <textarea id="edit-item-Notes" class="form-control" rows="4"
                                  @bind="_item.Notes"></textarea>
                    </div>

                    @* ----- ENUM: Dropdown Select ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Status"><Language Tag="Status" Required="true" /></label>
                        <select id="edit-item-Status" class="form-select" @bind="_item.Status">
                            <option value="@DataObjects.ExampleItemStatus.Draft">
                                <Language Tag="StatusDraft" />
                            </option>
                            <option value="@DataObjects.ExampleItemStatus.Active">
                                <Language Tag="StatusActive" />
                            </option>
                            <option value="@DataObjects.ExampleItemStatus.Completed">
                                <Language Tag="StatusCompleted" />
                            </option>
                            <option value="@DataObjects.ExampleItemStatus.Archived">
                                <Language Tag="StatusArchived" />
                            </option>
                        </select>
                    </div>

                    @* ----- GUID: Foreign Key Dropdown ----- *@
                    <div class="mb-2">
                        <label for="edit-item-CategoryId">
                            <Language Tag="Category" Required="true" />
                        </label>
                        <select id="edit-item-CategoryId"
                                class="@Helpers.MissingValue(_item.CategoryId, "form-select")"
                                @bind="_item.CategoryId">
                            <option value="@Guid.Empty"><Language Tag="SelectCategory" /></option>
                            @foreach (var cat in Model.Categories.Where(x => x.Enabled).OrderBy(x => x.Name)) {
                                <option value="@cat.CategoryId">@cat.Name</option>
                            }
                        </select>
                    </div>

                    @* ----- GUID: Nullable Foreign Key ----- *@
                    <div class="mb-2">
                        <label for="edit-item-AssignedUserId"><Language Tag="AssignedTo" /></label>
                        <select id="edit-item-AssignedUserId" class="form-select" @bind="_assignedUserId">
                            <option value=""><Language Tag="Unassigned" /></option>
                            @foreach (var user in Model.Users.Where(x => x.Enabled).OrderBy(x => x.DisplayName)) {
                                <option value="@user.UserId">@user.DisplayName</option>
                            }
                        </select>
                    </div>
                </div>

                @* ===== TAB: DETAILS ===== *@
                <div id="tabDetails" class="tab-pane" role="tabpanel">

                    @* ----- DECIMAL: Currency Input ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Amount"><Language Tag="Amount" /></label>
                        <div class="input-group" style="max-width:200px;">
                            <span class="input-group-text">$</span>
                            <input type="number" step="0.01" id="edit-item-Amount" class="form-control"
                                   @bind="_item.Amount" />
                        </div>
                    </div>

                    @* ----- DECIMAL: Nullable Currency ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Price"><Language Tag="Price" /></label>
                        <div class="input-group" style="max-width:200px;">
                            <span class="input-group-text">$</span>
                            <input type="number" step="0.01" id="edit-item-Price" class="form-control"
                                   @bind="_item.Price" />
                        </div>
                    </div>

                    @* ----- DOUBLE: Percentage Input ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Percentage"><Language Tag="Percentage" /></label>
                        <div class="input-group" style="max-width:150px;">
                            <input type="number" step="0.1" id="edit-item-Percentage" class="form-control"
                                   @bind="_item.Percentage" />
                            <span class="input-group-text">%</span>
                        </div>
                    </div>

                    @* ----- INT: Number Input ----- *@
                    <div class="mb-2">
                        <label for="edit-item-SortOrder"><Language Tag="SortOrder" /></label>
                        <input type="number" step="1" id="edit-item-SortOrder" class="form-control"
                               style="max-width:100px;" @bind="_item.SortOrder" />
                    </div>

                    @* ----- INT: Nullable Number ----- *@
                    <div class="mb-2">
                        <label for="edit-item-Priority"><Language Tag="Priority" /></label>
                        <input type="number" step="1" id="edit-item-Priority" class="form-control"
                               style="max-width:100px;" @bind="_item.Priority" />
                    </div>

                    @* ----- DATETIME: Date Picker ----- *@
                    <div class="mb-2">
                        <label for="edit-item-StartDate"><Language Tag="StartDate" Required="true" /></label>
                        <input type="datetime-local" id="edit-item-StartDate" class="form-control"
                               style="max-width:250px;" @bind="_item.StartDate" />
                    </div>

                    @* ----- DATETIME: Nullable Date ----- *@
                    <div class="mb-2">
                        <label for="edit-item-EndDate"><Language Tag="EndDate" /></label>
                        <input type="datetime-local" id="edit-item-EndDate" class="form-control"
                               style="max-width:250px;" @bind="_item.EndDate" />
                    </div>

                    @* ----- DATEONLY: Date Only Picker ----- *@
                    <div class="mb-2">
                        <label for="edit-item-EffectiveDate"><Language Tag="EffectiveDate" /></label>
                        <input type="date" id="edit-item-EffectiveDate" class="form-control"
                               style="max-width:200px;" @bind="_item.EffectiveDate" />
                    </div>

                    @* ----- TIMEONLY: Time Picker ----- *@
                    <div class="mb-2">
                        <label for="edit-item-ScheduledTime"><Language Tag="ScheduledTime" /></label>
                        <input type="time" id="edit-item-ScheduledTime" class="form-control"
                               style="max-width:150px;" @bind="_item.ScheduledTime" />
                    </div>
                </div>

                @* ===== TAB: ADVANCED ===== *@
                <div id="tabAdvanced" class="tab-pane" role="tabpanel">

                    @* ----- BOOLEAN: Nullable (tri-state) ----- *@
                    <div class="mb-2">
                        <label for="edit-item-IsActive"><Language Tag="IsActive" /></label>
                        <select id="edit-item-IsActive" class="form-select" style="max-width:150px;"
                                @bind="_isActiveString">
                            <option value=""><Language Tag="NotSet" /></option>
                            <option value="true"><Language Tag="Yes" /></option>
                            <option value="false"><Language Tag="No" /></option>
                        </select>
                    </div>

                    @* ----- CARD: Grouped Section ----- *@
                    <div class="card mt-4">
                        <div class="card-header bg-primary text-bg-primary">
                            <Language Tag="AdditionalOptions" />
                        </div>
                        <div class="card-body">
                            <div class="mb-2 form-check form-switch">
                                <input type="checkbox" role="switch" class="form-check-input"
                                       id="edit-item-IsDefault" @bind="_item.IsDefault" />
                                <label for="edit-item-IsDefault" class="form-check-label">
                                    <Language Tag="IsDefault" />
                                </label>
                            </div>
                        </div>
                    </div>

                    @* ----- CARD: Danger Zone ----- *@
                    <div class="card mt-4">
                        <div class="card-header bg-danger text-bg-danger">
                            <Language Tag="DangerZone" />
                        </div>
                        <div class="card-body">
                            <div class="alert alert-danger">
                                <Language Tag="DangerZoneWarning" />
                            </div>
                            <div class="mb-2">
                                <label for="edit-item-MaxValue"><Language Tag="MaxValue" /></label>
                                <input type="number" id="edit-item-MaxValue" class="form-control"
                                       style="max-width:150px;" @bind="_item.MaxValue" />
                            </div>
                        </div>
                    </div>

                    @* ----- MULTI-SELECT: List of related items ----- *@
                    <div class="mb-2">
                        <label for="edit-item-RelatedItems"><Language Tag="RelatedItems" /></label>
                        <select id="edit-item-RelatedItems" class="form-select" multiple size="5"
                                @bind="_selectedRelatedItems">
                            @foreach (var related in Model.RelatedItems.OrderBy(x => x.Name)) {
                                <option value="@related.RelatedItemId">@related.Name</option>
                            }
                        </select>
                    </div>
                </div>
            </div>

            @* ===== APP MODULE EXTENSION POINT ===== *@
            <EditExampleItem_App Area="edit" Value="_item" />
        }
    }
}

@code {
    [Parameter] public string? id { get; set; }
    [Parameter] public string? TenantCode { get; set; }

    protected bool _loading = true;
    protected bool _loadedData = false;
    protected bool _newItem = false;
    protected string _title = "";
    protected DataObjects.ExampleItem _item = new DataObjects.ExampleItem();

    protected string _pageName = "editexampleitem";

    // Helper properties for nullable guid binding
    protected string _assignedUserId {
        get => _item.AssignedUserId?.ToString() ?? "";
        set => _item.AssignedUserId = String.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
    }

    // Helper property for nullable bool binding
    protected string _isActiveString {
        get => _item.IsActive?.ToString().ToLower() ?? "";
        set => _item.IsActive = String.IsNullOrWhiteSpace(value) ? null : bool.Parse(value);
    }

    // Multi-select binding
    protected string[] _selectedRelatedItems { get; set; } = Array.Empty<string>();

    protected EditExampleItem_App AppModule = new EditExampleItem_App();

    public void Dispose()
    {
        Model.OnChange -= OnDataModelUpdated;
        Model.OnSignalRUpdate -= SignalRUpdate;

        Model.Subscribers_OnChange.Remove(_pageName);
        Model.Subscribers_OnSignalRUpdate.Remove(_pageName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            Model.TenantCodeFromUrl = TenantCode;
        }

        if (Model.Loaded && Model.LoggedIn) {
            // See if this feature is enabled and user has permission
            if (!Model.FeatureEnabledExampleItems || !Model.User.Admin) {
                Helpers.NavigateToRoot();
                return;
            }

            await Helpers.ValidateUrl(TenantCode);

            if (!_loadedData || Helpers.StringValue(Model.NavigationId) != Helpers.StringValue(id)) {
                _loadedData = true;
                await LoadItem();
            }
        }
    }

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

    protected void OnDataModelUpdated()
    {
        if (Model.View == _pageName) {
            StateHasChanged();
        }
    }

    protected async Task Delete()
    {
        Model.ClearMessages();
        Model.Message_Deleting();

        var deleted = await Helpers.GetOrPost<DataObjects.BooleanResponse>("api/Data/DeleteExampleItem/" + id);

        Model.ClearMessages();

        if (deleted != null) {
            if (deleted.Result) {
                // Remove from local cache
                Model.ExampleItems = Model.ExampleItems.Where(x => x.ExampleItemId.ToString() != id).ToList();
                Helpers.NavigateTo("Settings/ExampleItems");
            } else {
                Model.ErrorMessages(deleted.Messages);
            }
        } else {
            Model.UnknownError();
        }
    }

    protected async Task LoadItem()
    {
        // First, load any required lookup data
        if (!Model.Categories.Any()) {
            await Helpers.LoadCategories();
        }

        if (!String.IsNullOrWhiteSpace(id)) {
            // Edit existing item
            Model.NavigationId = id;
            Model.ViewIsEditPage = true;

            _loading = true;
            _newItem = false;
            _title = "EditExampleItem";

            var getItem = await Helpers.GetOrPost<DataObjects.ExampleItem>("api/Data/GetExampleItem/" + id);
            if (getItem != null) {
                _item = getItem;
            } else {
                Model.UnknownError();
            }
        } else {
            // Add new item
            _newItem = true;
            _title = "AddNewExampleItem";

            _item = new DataObjects.ExampleItem {
                TenantId = Model.TenantId,
                ExampleItemId = Guid.Empty,
                Enabled = true,
                Status = DataObjects.ExampleItemStatus.Draft,
                StartDate = DateTime.Now,
                SortOrder = 0,
            };
        }

        _loading = false;
        this.StateHasChanged();

        await Helpers.DelayedFocus("edit-item-Name");
    }

    protected async Task Save()
    {
        Model.ClearMessages();

        List<string> errors = new List<string>();
        string focus = "";

        // ===== VALIDATION =====

        // Required string
        if (String.IsNullOrWhiteSpace(_item.Name)) {
            errors.Add(Helpers.MissingRequiredField("Name"));
            if (focus == "") { focus = "edit-item-Name"; }
        }

        // Required guid
        if (_item.CategoryId == Guid.Empty) {
            errors.Add(Helpers.MissingRequiredField("Category"));
            if (focus == "") { focus = "edit-item-CategoryId"; }
        }

        // Custom validation example
        if (_item.EndDate.HasValue && _item.EndDate < _item.StartDate) {
            errors.Add(Helpers.Text("EndDateMustBeAfterStartDate"));
            if (focus == "") { focus = "edit-item-EndDate"; }
        }

        // App module validation
        var saveApp = AppModule.Save(_item);
        if (!saveApp.Result) {
            if (saveApp.Messages.Any()) {
                errors.AddRange(saveApp.Messages);
            }
            if (focus == "" && !String.IsNullOrWhiteSpace(saveApp.Focus)) {
                focus = saveApp.Focus;
            }
        }

        if (errors.Any()) {
            Model.ErrorMessages(errors);
            await Helpers.DelayedFocus(focus);
            return;
        }

        // ===== SAVE =====
        Model.Message_Saving();

        var saved = await Helpers.GetOrPost<DataObjects.ExampleItem>("api/Data/SaveExampleItem", _item);

        Model.ClearMessages();

        if (saved != null) {
            if (saved.ActionResponse.Result) {
                Helpers.NavigateTo("Settings/ExampleItems");
            } else {
                Model.ErrorMessages(saved.ActionResponse.Messages);
            }
        } else {
            Model.UnknownError();
        }
    }

    protected void SignalRUpdate(DataObjects.SignalRUpdate update)
    {
        if (Model.View == _pageName &&
            update.UpdateType == DataObjects.SignalRUpdateType.ExampleItem &&
            update.ItemId == _item.ExampleItemId &&
            update.UserId != Model.User.UserId) {

            switch (update.Message.ToLower()) {
                case "deleted":
                    Helpers.NavigateTo("Settings/ExampleItems");
                    Model.Message_RecordDeleted("", update.UserDisplayName);
                    break;

                case "saved":
                    var item = Helpers.DeserializeObject<DataObjects.ExampleItem>(update.ObjectAsString);
                    if (item != null) {
                        _item = item;
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

## Part 4: Input Field Quick Reference

### Field Type → HTML Input Mapping

| C# Type | HTML Input | Blazor Binding | CSS Class |
|---------|------------|----------------|-----------|
| `string` (required) | `<input type="text">` | `@bind="_item.Name"` | `Helpers.MissingValue()` |
| `string` (optional) | `<input type="text">` | `@bind="_item.Code"` | `form-control` |
| `string` (multiline) | `<textarea>` | `@bind="_item.Notes"` | `form-control` |
| `bool` | `<input type="checkbox">` | `@bind="_item.Enabled"` | `form-check-input` |
| `bool?` | `<select>` with null/true/false | custom property | `form-select` |
| `int` | `<input type="number">` | `@bind="_item.SortOrder"` | `form-control` |
| `int?` | `<input type="number">` | `@bind="_item.Priority"` | `form-control` |
| `decimal` | `<input type="number" step="0.01">` | `@bind="_item.Amount"` | `form-control` |
| `double` | `<input type="number" step="0.1">` | `@bind="_item.Rate"` | `form-control` |
| `Guid` (required FK) | `<select>` | `@bind="_item.CategoryId"` | `Helpers.MissingGuid()` |
| `Guid?` (optional FK) | `<select>` | custom string property | `form-select` |
| `DateTime` | `<input type="datetime-local">` | `@bind="_item.StartDate"` | `form-control` |
| `DateTime?` | `<input type="datetime-local">` | `@bind="_item.EndDate"` | `form-control` |
| `DateOnly?` | `<input type="date">` | `@bind="_item.EffectiveDate"` | `form-control` |
| `TimeOnly?` | `<input type="time">` | `@bind="_item.ScheduledTime"` | `form-control` |
| `enum` | `<select>` | `@bind="_item.Status"` | `form-select` |

---

## Part 5: Common UI Patterns

### Pattern: Sticky Menu
```razor
<div class="@Model.StickyMenuClass">
    <h1 class="page-title">
        <Language Tag="PageTitle" IncludeIcon="true" />
        <StickyMenuIcon />
    </h1>
    <div class="btn-group mb-2" role="group">
        <!-- buttons here -->
    </div>
</div>
```

### Pattern: Filter Toggles
```razor
<div class="mb-2">
    <div class="form-check form-switch">
        <input type="checkbox" id="filter-toggle" class="form-check-input" @bind="_filterValue" />
        <label for="filter-toggle" class="form-check-label"><Language Tag="FilterLabel" /></label>
    </div>
</div>
```

### Pattern: Card Section
```razor
<div class="card mt-4">
    <div class="card-header bg-primary text-bg-primary"><Language Tag="SectionTitle" /></div>
    <div class="card-body">
        <!-- content here -->
    </div>
</div>
```

### Pattern: Danger Card
```razor
<div class="card mt-4">
    <div class="card-header bg-danger text-bg-danger"><Language Tag="DangerZone" /></div>
    <div class="card-body">
        <div class="alert alert-danger"><Language Tag="WarningMessage" /></div>
        <!-- dangerous options here -->
    </div>
</div>
```

### Pattern: Row Layout
```razor
<div class="row mb-4">
    <div class="col col-sm-12 col-md-6">
        <!-- left column -->
    </div>
    <div class="col col-sm-12 col-md-6">
        <!-- right column -->
    </div>
</div>
```

### Pattern: Conditional Content
```razor
@if (_showAdvancedOptions) {
    <div class="indented">
        <!-- advanced options -->
    </div>
}
```

### Pattern: Module Markers
```razor
<!-- {{ModuleItemStart:ModuleName}} -->
<div>Module-specific content</div>
<!-- {{ModuleItemEnd:ModuleName}} -->
```

---

## Part 6: Lifecycle & Events Checklist

### List Page Checklist
- [ ] `@implements IDisposable`
- [ ] `_loadedData` and `_loading` flags
- [ ] `_pageName` constant
- [ ] `Dispose()` removes event handlers and subscribers
- [ ] `OnAfterRenderAsync` checks permissions and loads data
- [ ] `OnInitialized` adds event handlers and sets Model.View
- [ ] `OnDataModelUpdated` calls `StateHasChanged()` if correct view
- [ ] `SignalRUpdate` handles real-time updates

### Edit Page Checklist
- [ ] All routes: Edit with id, Edit with TenantCode+id, Add, Add with TenantCode
- [ ] `_newItem` flag to distinguish add vs edit
- [ ] `_title` changes based on mode
- [ ] `LoadItem()` handles both new and existing
- [ ] `Delete()` removes from cache and navigates
- [ ] `Save()` validates, shows saving message, handles response
- [ ] `SignalRUpdate` handles deleted and saved messages

---

*Category: 008_components*
*Last Updated: 2025-12-23*
*Source: FreeCRM base template*
