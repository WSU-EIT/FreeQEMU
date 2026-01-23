# FreeCRM: SignalR Real-Time Updates Guide

> Complete guide to implementing SignalR real-time communication in FreeCRM-based projects.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Overview](#overview) | ~30 | What SignalR provides |
| [Architecture](#architecture) | ~45 | Component overview and data flow |
| [Server-Side Implementation](#server-side-implementation) | ~70 | Hub, types, and broadcasting |
| [Client-Side Implementation](#client-side-implementation) | ~200 | Connection and handlers |
| [Best Practices](#best-practices) | ~370 | Common patterns and tips |
| [Adding Custom Update Types](#adding-custom-update-types) | ~440 | Extending for your app |
| [Troubleshooting](#troubleshooting) | ~495 | Common issues |

---

## Overview

**Source:** FreeCRM base template (all projects)
**Status:** Core feature used in ALL FreeCRM projects

SignalR is the backbone of real-time updates in FreeCRM applications. It enables instant notification of data changes across all connected clients, ensuring users always see the latest information without manual page refreshes.

**Key Features:**
- Tenant-scoped broadcasts (updates only reach users in the same tenant)
- Type-safe update handling with `SignalRUpdateType` enum
- Automatic reconnection with stateful reconnect
- Clean subscription/unsubscription lifecycle management

---

## Architecture

### Core Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `signalrHub.cs` | Server/Hubs/ | Hub implementation for broadcasting |
| `DataObjects.SignalR.cs` | DataObjects/ | Update type definitions |
| `DataAccess.SignalR.cs` | DataAccess/ | Server-side update methods |
| `MainLayout.razor` | Client/Layout/ | Connection setup and global handlers |
| Page components | Client/Pages/ | Page-specific handlers |

### Data Flow

```
[Server Action] → DataAccess.SignalR → Hub.Clients.Group → [All Clients in Tenant]
                                                               ↓
                                                    MainLayout.ProcessSignalRUpdate
                                                               ↓
                                                    Model.SignalRUpdate (event)
                                                               ↓
                                                    Page.SignalRUpdate (handler)
```

---

## Server-Side Implementation

### 1. Hub Definition (`signalrHub.cs`)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CRM.Server.Hubs
{
    public partial interface IsrHub
    {
        Task SignalRUpdate(DataObjects.SignalRUpdate update);
    }

    [Authorize]
    public partial class crmHub : Hub<IsrHub>
    {
        private List<string> tenants = new List<string>();

        public async Task JoinTenantId(string TenantId)
        {
            // Before adding a user to a Tenant group remove them from any groups they were in before.
            if (tenants != null && tenants.Count() > 0) {
                foreach (var tenant in tenants) {
                    try {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenant);
                    } catch { }
                }
            }

            if (!tenants.Contains(TenantId)) {
                tenants.Add(TenantId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, TenantId);
        }

        public async Task SignalRUpdate(DataObjects.SignalRUpdate update)
        {
            if (update.TenantId.HasValue) {
                // This is a tenant-specific update. Send only to those people in that tenant group.
                await Clients.Group(update.TenantId.Value.ToString()).SignalRUpdate(update);
            } else {
                // This is a non-tenant-specific update.
                await Clients.All.SignalRUpdate(update);
            }
        }
    }
}
```

### 2. Update Type Definitions (`DataObjects.SignalR.cs`)

```csharp
public class SignalRUpdate
{
    public SignalRUpdateType UpdateType { get; set; }
    public Guid ItemId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; } = "";
    public string ObjectAsString { get; set; } = "";
}

public enum SignalRUpdateType
{
    Unknown = 0,
    Appointment = 1,
    Asset = 2,
    Department = 3,
    DepartmentGroup = 4,
    EmailTemplate = 5,
    File = 6,
    Language = 7,
    LastAccessTime = 8,
    Location = 9,
    Service = 10,
    Setting = 11,
    Tag = 12,
    Tenant = 13,
    UDF = 14,
    Undelete = 15,
    User = 16,
    UserGroup = 17,
    UserPreferences = 18,
    // Add app-specific types here...
}
```

### 3. Sending Updates (`DataAccess.SignalR.cs`)

```csharp
public async Task SignalRUpdate(SignalRUpdate update)
{
    // See if this update should include the object as a string.
    bool includeObject = false;

    switch (update.UpdateType) {
        case SignalRUpdateType.Department:
        case SignalRUpdateType.DepartmentGroup:
        case SignalRUpdateType.Tag:
        case SignalRUpdateType.User:
        case SignalRUpdateType.UserGroup:
        case SignalRUpdateType.UserPreferences:
            includeObject = true;
            break;
    }

    if (includeObject && String.IsNullOrWhiteSpace(update.ObjectAsString)) {
        // Object was not provided - the ObjectAsString should contain the serialized object
    }

    await _hubContext.Clients.Group(update.TenantId.ToString()).SignalRUpdate(update);
}
```

### 4. Calling from Data Access Methods

```csharp
// Example: After saving a user
public async Task<DataObjects.User> SaveUser(DataObjects.User User, Guid CurrentUserId)
{
    // ... save logic ...

    // Send SignalR update
    await SignalRUpdate(new DataObjects.SignalRUpdate {
        UpdateType = DataObjects.SignalRUpdateType.User,
        TenantId = User.TenantId,
        ItemId = User.UserId,
        UserId = CurrentUserId,
        Message = "saved",
        ObjectAsString = Newtonsoft.Json.JsonConvert.SerializeObject(User)
    });

    return User;
}

// Example: After deleting a record
public async Task DeleteItem(Guid ItemId, Guid TenantId, Guid CurrentUserId)
{
    // ... delete logic ...

    await SignalRUpdate(new DataObjects.SignalRUpdate {
        UpdateType = DataObjects.SignalRUpdateType.Asset,
        TenantId = TenantId,
        ItemId = ItemId,
        UserId = CurrentUserId,
        Message = "deleted"
    });
}
```

---

## Client-Side Implementation

### 1. Connection Setup (`MainLayout.razor`)

```csharp
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

@code {
    private bool hubConfigured = false;
    private HubConnection? hubConnection;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Model.Loaded && Model.LoggedIn && !hubConfigured) {
            hubConfigured = true;

            // Build the connection with automatic reconnect
            hubConnection = new HubConnectionBuilder()
                .WithUrl(Model.ApplicationUrl + "crmHub")
                .WithStatefulReconnect()
                .WithAutomaticReconnect()
                .Build();

            // Register the update handler
            hubConnection.On<DataObjects.SignalRUpdate>("SignalRUpdate", async (update) => {
                await ProcessSignalRUpdate(update);
            });

            // Start the connection
            await hubConnection.StartAsync();

            // Join the tenant group
            await hubConnection.InvokeAsync("JoinTenantId", Model.TenantId);
        }
    }

    public bool IsSignalRConnected =>
        hubConnection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null) {
            await hubConnection.DisposeAsync();
        }
    }
}
```

### 2. Global Update Processing

```csharp
protected async Task ProcessSignalRUpdate(DataObjects.SignalRUpdate update)
{
    // See if this update is for the current tenant
    if (update != null && (update.TenantId == null || update.TenantId == Model.TenantId)) {
        var itemId = update.ItemId;
        string message = update.Message.ToLower();
        var userId = update.UserId;

        switch (update.UpdateType) {
            case DataObjects.SignalRUpdateType.Department:
                await Helpers.LoadDepartments();
                break;

            case DataObjects.SignalRUpdateType.Tag:
                await Helpers.LoadTags();
                break;

            case DataObjects.SignalRUpdateType.User:
                var user = Helpers.DeserializeObject<DataObjects.User>(update.ObjectAsString);
                if (user != null) {
                    // Update the user in the local model
                    var existingUser = Model.Users.FirstOrDefault(x => x.UserId == user.UserId);
                    if (existingUser != null) {
                        existingUser = user;
                    } else {
                        Model.Users.Add(user);
                    }

                    // See if the current user was updated
                    if (Model.User.UserId == user.UserId) {
                        Model.User = user;

                        if (!user.Enabled) {
                            // This user has been disabled, so log them out
                            Helpers.NavigateTo("Logout");
                        }
                    }
                }
                break;

            case DataObjects.SignalRUpdateType.Setting:
                if (userId != Model.User.UserId) {
                    await Helpers.ReloadModel();
                }
                break;

            // Handle other types...
        }

        // Trigger the model event for page-level handlers
        Model.SignalRUpdate(update);
    }
}
```

### 3. Page-Level Subscription Pattern

Every page that needs real-time updates follows this pattern:

```csharp
@implements IDisposable

@code {
    protected string _pageName = "mypage";

    protected override void OnInitialized()
    {
        // Subscribe to SignalR updates
        if (!Model.Subscribers_OnSignalRUpdate.Contains(_pageName)) {
            Model.Subscribers_OnSignalRUpdate.Add(_pageName);
            Model.OnSignalRUpdate += SignalRUpdate;
        }

        Model.View = _pageName;
    }

    public void Dispose()
    {
        // Unsubscribe from SignalR updates
        Model.OnSignalRUpdate -= SignalRUpdate;
        Model.Subscribers_OnSignalRUpdate.Remove(_pageName);
    }

    protected async void SignalRUpdate(DataObjects.SignalRUpdate update)
    {
        // Only process updates relevant to this page
        if (update.UpdateType == DataObjects.SignalRUpdateType.Asset
            && Model.View == _pageName
            && update.UserId != Model.User.UserId)
        {
            // See if the updated item is in our current view
            if (!Filter.Loading && Filter.Records != null && Filter.Records.Any()) {
                bool itemInFilter = false;

                foreach (var record in Filter.Records) {
                    Guid itemId = Helpers.GetObjectPropertyValue<Guid>(record, "Id");
                    if (itemId == update.ItemId) {
                        itemInFilter = true;
                        break;
                    }
                }

                if (itemInFilter) {
                    await LoadFilter();
                }
            }
        }
    }
}
```

---

## Best Practices

### 1. Always Check Update Source

```csharp
// Don't react to your own updates
if (update.UserId != Model.User.UserId) {
    await LoadFilter();
}
```

### 2. Check Current View Before Processing

```csharp
// Only process if user is viewing this page
if (Model.View == _pageName) {
    await ProcessUpdate();
}
```

### 3. Avoid Redundant API Calls

```csharp
// Check if the item is in the current view before reloading
bool itemInView = Filter.Records?.Any(r =>
    Helpers.GetObjectPropertyValue<Guid>(r, "Id") == update.ItemId) ?? false;

if (itemInView) {
    await LoadFilter();
}
```

### 4. Include Object in Update When Appropriate

```csharp
// For simple updates that clients need to process immediately
await SignalRUpdate(new DataObjects.SignalRUpdate {
    UpdateType = DataObjects.SignalRUpdateType.User,
    TenantId = TenantId,
    ItemId = UserId,
    UserId = CurrentUserId,
    Message = "saved",
    ObjectAsString = Newtonsoft.Json.JsonConvert.SerializeObject(user)
});
```

### 5. Clean Up Subscriptions

```csharp
public void Dispose()
{
    Model.OnSignalRUpdate -= SignalRUpdate;
    Model.Subscribers_OnSignalRUpdate.Remove(_pageName);
}
```

---

## Common Update Types

| Type | Message Values | Usage |
|------|---------------|-------|
| `User` | "saved", "deleteduserphoto", "saveduserphoto" | User profile changes |
| `Tenant` | "saved", "deleted" | Tenant configuration |
| `Setting` | "applicationsettingsupdate" | App settings changes |
| `Department` | "saved", "deleted" | Department changes |
| `Tag` | "saved", "deleted" | Tag changes |
| `File` | "saved", "deleted" | File uploads |
| `Undelete` | "user", "department", "tag", etc. | Restore deleted records |
| `LastAccessTime` | - | User activity tracking |
| `UserPreferences` | - | User preference sync |

---

## Adding Custom Update Types

### 1. Add to Enum

```csharp
// DataObjects.SignalR.cs
public enum SignalRUpdateType
{
    // ... existing types ...
    MyCustomType = 100,  // Use high numbers for app-specific types
}
```

### 2. Send Update from Server

```csharp
// DataAccess.MyFeature.cs
await SignalRUpdate(new DataObjects.SignalRUpdate {
    UpdateType = DataObjects.SignalRUpdateType.MyCustomType,
    TenantId = TenantId,
    ItemId = ItemId,
    UserId = CurrentUserId,
    Message = "mycustommessage",
    ObjectAsString = Newtonsoft.Json.JsonConvert.SerializeObject(myObject)
});
```

### 3. Handle in MainLayout (Optional)

```csharp
// MainLayout.razor ProcessSignalRUpdate
case DataObjects.SignalRUpdateType.MyCustomType:
    // Handle at global level if needed
    break;
```

### 4. Handle in Page Component

```csharp
protected async void SignalRUpdate(DataObjects.SignalRUpdate update)
{
    if (update.UpdateType == DataObjects.SignalRUpdateType.MyCustomType
        && Model.View == _pageName)
    {
        var myObject = Helpers.DeserializeObject<MyObject>(update.ObjectAsString);
        // Process update...
    }
}
```

---

## Troubleshooting

### Connection Issues

```csharp
// Check connection state
if (hubConnection?.State != HubConnectionState.Connected) {
    // Handle disconnected state
}
```

### Debug Logging

```csharp
hubConnection.On<DataObjects.SignalRUpdate>("SignalRUpdate", async (update) => {
    await Helpers.ConsoleLog("SignalR Update Received", update);
    await ProcessSignalRUpdate(update);
});
```

### Update Not Received

1. Check tenant group membership
2. Verify `TenantId` is set correctly in update
3. Confirm subscription in `OnInitialized`
4. Check for disposal issues

---

## Program.cs Configuration

```csharp
// Add SignalR services
builder.Services.AddSignalR();

// Map the hub endpoint
app.MapHub<crmHub>("/crmHub");
```

---

*Category: 007_patterns*
*Last Updated: 2025-12-23*
*Source: FreeCRM base template*
