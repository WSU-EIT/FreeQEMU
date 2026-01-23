# FreeCRM: Digital Signature Capture Guide

> Complete guide to implementing digital signature capture using jSignature in FreeCRM-based projects.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Overview](#overview) | ~30 | What this component provides |
| [Component Architecture](#component-architecture) | ~45 | Files and data format |
| [Implementation](#implementation) | ~65 | Complete Razor and JS code |
| [Usage Examples](#usage-examples) | ~180 | Basic, styled, and form integration |
| [Required Dependencies](#required-dependencies) | ~250 | jSignature setup |
| [Key Patterns](#key-patterns-demonstrated) | ~290 | DotNetObjectReference, two-way binding |
| [Troubleshooting](#troubleshooting) | ~350 | Common issues |

**Source:** Private repo "nForm"
**Reusability:** HIGH - Easily adaptable for any form-based application

---

## Overview

The Signature component provides touch-friendly digital signature capture using the jSignature library. It demonstrates key Blazor patterns including:

- DotNetObjectReference for JavaScript→C# callbacks
- Colocated `.razor.js` JavaScript modules
- Two-way binding with `@bind-Value`
- Proper disposal patterns

---

## Component Architecture

### Files Required

| File | Purpose |
|------|---------|
| `Signature.razor` | Blazor component with C# logic |
| `Signature.razor.js` | Colocated JavaScript module |
| `jSignature.min.js` | jSignature library (wwwroot) |
| `jSignature.css` | Signature styling (optional) |

### Data Format

Signatures are stored in jSignature's **base30** format:
- Compact string representation
- Format: `image/jsignature;base30,{data}`
- Suitable for database storage

---

## Implementation

### 1. Blazor Component (`Signature.razor`)

```csharp
@implements IDisposable
@inject IJSRuntime jsRuntime

@if (IncludeClearButton) {
    <button type="button" class="@ClearButtonClass" @onclick="Clear" disabled="@(String.IsNullOrWhiteSpace(Value))">
        <i class="@ClearButtonIcon"></i> @ClearButtonText
    </button>
}
<div class="signature @Class" id="@SignatureId"></div>

@code {
    [Parameter] public string Class { get; set; } = "";

    /// <summary>
    /// OPTIONAL: An Id for the signature editor. If empty a random Id will be generated.
    /// </summary>
    [Parameter] public string Id { get; set;} = "";

    /// <summary>
    /// OPTIONAL: Show a clear button to clear the signature.
    /// </summary>
    [Parameter] public bool IncludeClearButton { get; set; } = false;

    /// <summary>
    /// OPTIONAL: If showing the clear button this is the class to display on the button.
    /// </summary>
    [Parameter] public string ClearButtonClass { get; set; } = "btn btn-sm btn-dark";

    /// <summary>
    /// OPTIONAL: If showing the clear button this is the text to display on the button.
    /// </summary>
    [Parameter] public string? ClearButtonText { get; set; } = "Clear";

    /// <summary>
    /// OPTIONAL: If showing the clear button this is the class to display for the icon.
    /// </summary>
    [Parameter] public string? ClearButtonIcon { get; set; } = "fas fa-eraser";

    /// <summary>
    /// The signature value (base30 format)
    /// </summary>
    [Parameter] public string Value { get; set; } = "";

    /// <summary>
    /// Internal method for 2-way binding with @bind-Value
    /// </summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    protected string _id = Guid.NewGuid().ToString().Replace("-", "").ToLower();

    private IJSObjectReference jsModule = null!;
    protected DotNetObjectReference<Signature>? dotNetHelper;
    protected bool _setupComplete = false;

    public void Dispose() {
        dotNetHelper?.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) {
            // Create the DotNetObjectReference for JS callbacks
            dotNetHelper = DotNetObjectReference.Create(this);

            // Import the colocated JavaScript module
            jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./Shared/Signature.razor.js");

            // Pass the reference to JavaScript
            await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);

            // Validate existing value format
            if (!String.IsNullOrWhiteSpace(Value)) {
                if (!Value.ToLower().StartsWith("image/jsignature;base30,")) {
                    Value = "";
                    ValueHasChanged();
                }
            }

            // Initialize the signature pad
            await jsModule.InvokeVoidAsync("SetupSignature", SignatureId, Value);
        }
    }

    public async Task Clear()
    {
        await jsModule.InvokeVoidAsync("ClearSignature", SignatureId);

        Value = String.Empty;
        ValueHasChanged();
    }

    /// <summary>
    /// Get the current signature value
    /// </summary>
    public string GetValue()
    {
        return Value;
    }

    protected string SignatureId {
        get {
            return !String.IsNullOrWhiteSpace(Id) ? Id : _id;
        }
    }

    /// <summary>
    /// Called from JavaScript when the signature changes
    /// </summary>
    [JSInvokable]
    public void SignatureUpdated(string value)
    {
        Value = value;
        ValueHasChanged();
    }

    /// <summary>
    /// Programmatically set the signature value
    /// </summary>
    public async Task SetValue(string value)
    {
        Value = value;
        ValueHasChanged();
    }

    protected void ValueHasChanged()
    {
        ValueChanged.InvokeAsync(Value);
        StateHasChanged();
    }
}
```

### 2. JavaScript Module (`Signature.razor.js`)

```javascript
let dotNetHelper;

export function ClearSignature(elementId) {
    $("#" + elementId).jSignature("clear");
}

export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

export function SetupSignature(elementId, value) {
    let signatureElement = document.getElementById(elementId);

    // Only initialize once
    if (signatureElement != undefined && signatureElement != null
        && !signatureElement.classList.contains("signature-added")) {

        signatureElement.classList.add("signature-added");

        // Initialize jSignature with signature line
        $("#" + elementId).jSignature({ signatureLine: true });

        // Load existing value if provided
        if (value != undefined && value != null && value != "") {
            $("#" + elementId).jSignature("setData", value, "base30");
        }

        // Bind change event to update Blazor
        $("#" + elementId).bind("change", function () {
            let hasSignature = $("#" + elementId).jSignature("getData", "native");

            if (hasSignature != null && hasSignature.length > 0) {
                let values = $("#" + elementId).jSignature("getData", "base30");
                let value = "";
                if (values != null && values.length > 1) {
                    value = values[0] + "," + values[1];
                }

                // Call back to Blazor
                dotNetHelper.invokeMethodAsync("SignatureUpdated", value);
            }
        });
    }
}
```

---

## Usage Examples

### Basic Usage

```razor
<Signature @bind-Value="mySignature" />

@code {
    private string mySignature = "";
}
```

### With Clear Button

```razor
<Signature @bind-Value="customerSignature"
           IncludeClearButton="true"
           ClearButtonText="Clear Signature"
           ClearButtonClass="btn btn-danger btn-sm" />

@code {
    private string customerSignature = "";
}
```

### Styled Signature Pad

```razor
<Signature @bind-Value="contractSignature"
           Class="signature-large border rounded"
           Id="contract-signature-pad"
           IncludeClearButton="true" />

<style>
    .signature-large {
        width: 100%;
        height: 200px;
        background: #f9f9f9;
    }
</style>
```

### Pre-loaded Signature

```razor
@* Load an existing signature from database *@
<Signature @bind-Value="existingSignature" />

@code {
    private string existingSignature = "image/jsignature;base30,3E13Z5Y1...";
}
```

### Form Integration

```razor
<EditForm Model="Contract" OnValidSubmit="SaveContract">
    <div class="mb-3">
        <label>Customer Name</label>
        <InputText @bind-Value="Contract.CustomerName" class="form-control" />
    </div>

    <div class="mb-3">
        <label>Signature <span class="required">*</span></label>
        <Signature @bind-Value="Contract.Signature"
                   IncludeClearButton="true"
                   Class="form-control p-0" />
        @if (String.IsNullOrWhiteSpace(Contract.Signature)) {
            <div class="text-danger">Signature is required</div>
        }
    </div>

    <button type="submit" class="btn btn-primary"
            disabled="@String.IsNullOrWhiteSpace(Contract.Signature)">
        Submit Contract
    </button>
</EditForm>

@code {
    private ContractModel Contract = new();

    private async Task SaveContract()
    {
        // Contract.Signature contains the base30 signature data
        await SaveToDatabase(Contract);
    }
}
```

---

## Required Dependencies

### 1. Add jSignature Library

Download jSignature from: https://github.com/brinley/jSignature

Add to `wwwroot/lib/jSignature/`:
- `jSignature.min.js`
- `flashcanvas.js` (optional, for IE support)

### 2. Include jQuery (Required by jSignature)

```html
<!-- index.html or _Host.cshtml -->
<script src="lib/jquery/jquery.min.js"></script>
<script src="lib/jSignature/jSignature.min.js"></script>
```

### 3. CSS Styling (Optional)

```css
/* Add to your stylesheet */
.signature {
    border: 1px solid #ccc;
    border-radius: 4px;
    min-height: 150px;
    background: white;
}

.signature canvas {
    width: 100%;
    height: 100%;
}
```

---

## Key Patterns Demonstrated

### 1. DotNetObjectReference Pattern

This pattern enables JavaScript to call C# methods:

```csharp
// Create reference in Blazor
dotNetHelper = DotNetObjectReference.Create(this);
await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);

// Mark method as callable from JS
[JSInvokable]
public void SignatureUpdated(string value) { }
```

```javascript
// Store and use in JavaScript
let dotNetHelper;

export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

// Call back to Blazor
dotNetHelper.invokeMethodAsync("SignatureUpdated", value);
```

### 2. Colocated JavaScript Module Pattern

Place `.razor.js` file next to the component:
```
Shared/
  Signature.razor
  Signature.razor.js
```

Import using relative path:
```csharp
jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
    "./Shared/Signature.razor.js");
```

### 3. Two-Way Binding Pattern

```csharp
[Parameter] public string Value { get; set; } = "";
[Parameter] public EventCallback<string> ValueChanged { get; set; }

protected void ValueHasChanged()
{
    ValueChanged.InvokeAsync(Value);
    StateHasChanged();
}
```

Usage:
```razor
<Signature @bind-Value="mySignature" />
```

### 4. Proper Disposal

```csharp
@implements IDisposable

public void Dispose() {
    dotNetHelper?.Dispose();
}
```

---

## Converting Signatures

### To Image (Server-Side)

```csharp
// Convert base30 to PNG
public byte[] SignatureToImage(string base30Data)
{
    // Use jSignature's SVG output or a server-side conversion library
    // This requires additional server-side processing
}
```

### For Display

```razor
@if (!String.IsNullOrWhiteSpace(SignatureValue)) {
    <div class="signature-display">
        @* Re-render the signature for viewing *@
        <Signature Value="@SignatureValue" />
    </div>
}
```

---

## Troubleshooting

### Signature Not Appearing

1. Check that jSignature library is loaded
2. Verify jQuery is available
3. Check browser console for errors
4. Ensure element has height (CSS)

### Signature Not Saving

1. Verify `@bind-Value` syntax
2. Check that `SignatureUpdated` is marked `[JSInvokable]`
3. Ensure `dotNetHelper` is set before signature changes

### Clear Button Not Working

1. Check that `jsModule` is initialized
2. Verify element ID matches

---

*Category: 008_components*
*Last Updated: 2025-12-23*
*Source: Private repo "nForm"*
