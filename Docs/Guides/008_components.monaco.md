# FreeCRM: Monaco Editor Guide

> Patterns for using the Monaco code editor in FreeCRM-based Blazor applications.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Overview](#overview) | ~35 | Two approaches to Monaco |
| [Custom Wrapper](#part-1-custom-monacoeditor-wrapper) | ~50 | Simplified component |
| [Direct BlazorMonaco](#part-2-using-blazormonaco-directly) | ~200 | Full control approach |
| [Language Modes](#part-3-language-modes) | ~350 | C#, JSON, SQL, HTML |
| [Diff Editor](#part-4-diff-editor) | ~400 | Comparing versions |
| [Common Patterns](#part-5-common-patterns) | ~450 | Content sync, keyboard shortcuts |
| [Dependencies](#dependencies) | ~550 | Required packages |

**Source:** Private repo "nForm"
**Library:** [BlazorMonaco](https://github.com/niclasmattsson/BlazorMonaco)

> **Note:** Monaco is primarily used in **nForm** for form-building features. Skip this guide if your project doesn't need code editing.

---

## Overview

FreeCRM-based projects use [BlazorMonaco](https://github.com/niclasmattsson/BlazorMonaco) for code editing. There are two approaches:

1. **Custom Wrapper (`MonacoEditor.razor`)** - Simplified component with two-way binding
2. **Direct BlazorMonaco** - Full control over editor configuration

---

## Part 1: Custom MonacoEditor Wrapper

The `MonacoEditor.razor` component provides a simplified interface with built-in features.

### Location
```
nForm.Client/Shared/MonacoEditor.razor
```

### Basic Usage

```razor
@* Simple two-way binding *@
<MonacoEditor
    @bind-Value="MyHtmlContent"
    Language="@MonacoEditor.MonacoLanguage.html" />

@code {
    protected string MyHtmlContent { get; set; } = "<div>Hello World</div>";
}
```

### All Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `@bind-Value` | `string` | `""` | Two-way binding for content |
| `Language` | `string` | `"plaintext"` | Syntax highlighting language |
| `Class` | `string` | `"default-editor"` | CSS class for styling |
| `Id` | `string` | auto-generated | HTML id attribute |
| `MinHeight` | `string` | `"300px"` | Minimum editor height |
| `ReadOnly` | `bool` | `false` | Prevent editing |
| `Timeout` | `int` | `1000` | Debounce delay (ms) for value updates |
| `ValueToDiff` | `string?` | `null` | Enable diff mode against this value |
| `ConstructorOptions` | `StandaloneEditorConstructionOptions?` | `null` | Override default options |
| `ConstructorOptionsForDiffEditor` | `StandaloneDiffEditorConstructionOptions?` | `null` | Override diff editor options |

### Use Case 1: HTML Editor with Binding

```razor
<div class="mb-2">
    <label for="edit-form-ReportTemplate">
        <Language Tag="ReportTemplate" />
    </label>

    <MonacoEditor @ref="_monacoEditor_ReportTemplate"
        Id="edit-form-ReportTemplate"
        Language="@MonacoEditor.MonacoLanguage.html"
        @bind-Value="Model.Form.ReportTemplate" />
</div>

@code {
    private MonacoEditor _monacoEditor_ReportTemplate = null!;
}
```

### Use Case 2: Diff Editor (Compare Two Values)

When `ValueToDiff` is provided, the editor switches to diff mode:

```razor
<MonacoEditor
    Language="@MonacoEditor.MonacoLanguage.csharp"
    Value="@_newCode"
    ValueToDiff="@_originalCode" />

@code {
    protected string _originalCode = "public class Foo { }";
    protected string _newCode = "public class Foo { public int Bar; }";
}
```

### Use Case 3: Read-Only Display

```razor
<MonacoEditor
    Language="@MonacoEditor.MonacoLanguage.json"
    Value="@_jsonData"
    ReadOnly="true"
    MinHeight="200px" />
```

### Use Case 4: Insert Text at Cursor

```razor
<button type="button" class="btn btn-primary" @onclick="InsertVariable">
    Insert Variable
</button>

<MonacoEditor @ref="_editor"
    Language="@MonacoEditor.MonacoLanguage.html"
    @bind-Value="_template" />

@code {
    private MonacoEditor _editor = null!;
    protected string _template = "";

    protected async Task InsertVariable()
    {
        await _editor.InsertValue("{{CustomerName}}");
    }
}
```

### Use Case 5: Get/Set Value Programmatically

```razor
<button type="button" class="btn btn-primary" @onclick="LoadTemplate">Load</button>
<button type="button" class="btn btn-success" @onclick="SaveTemplate">Save</button>

<MonacoEditor @ref="_editor"
    Language="@MonacoEditor.MonacoLanguage.html"
    @bind-Value="_content" />

@code {
    private MonacoEditor _editor = null!;
    protected string _content = "";

    protected async Task LoadTemplate()
    {
        // Set value programmatically
        await _editor.SetValue("<h1>Loaded Template</h1>");
    }

    protected async Task SaveTemplate()
    {
        // Get current value
        string currentContent = await _editor.GetValue();
        await SaveToServer(currentContent);
    }
}
```

### Use Case 6: Access Cursor Position

```razor
<MonacoEditor @ref="_editor"
    Language="@MonacoEditor.MonacoLanguage.html"
    @bind-Value="_content" />

<div>
    Line: @_editor?.EditorCursorPosition.LineNumber,
    Column: @_editor?.EditorCursorPosition.Column
</div>

@code {
    private MonacoEditor _editor = null!;
    protected string _content = "";
}
```

---

## Part 2: Direct BlazorMonaco Usage

For more control, use `BlazorMonaco.Editor.StandaloneCodeEditor` directly.

### Basic Direct Usage

```razor
@using BlazorMonaco
@using BlazorMonaco.Editor

<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_standaloneCodeEditor"
    Id="my-code-editor"
    CssClass="code-editor"
    OnDidChangeModelContent="EditorContentChanged"
    ConstructionOptions="@EditorOptions" />

@code {
    private BlazorMonaco.Editor.StandaloneCodeEditor _standaloneCodeEditor = null!;

    private BlazorMonaco.Editor.StandaloneEditorConstructionOptions EditorOptions(
        BlazorMonaco.Editor.StandaloneCodeEditor editor)
    {
        return new BlazorMonaco.Editor.StandaloneEditorConstructionOptions {
            AutomaticLayout = true,
            Language = "html",
            Value = "<div>Initial Content</div>",
        };
    }

    protected void EditorContentChanged(BlazorMonaco.Editor.ModelContentChangedEvent e)
    {
        // Handle content changes
    }
}
```

### Use Case 1: HTML Form Editor with Snippet Insertion

From `Form.razor`:

```razor
<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_standaloneCodeEditor_Form"
    Id="html-form-editor"
    CssClass="code-editor"
    OnDidChangeModelContent="EditorUpdated_FormHtml"
    OnDidChangeCursorPosition="EditorUpdated_FormHtml_CursorPosition"
    ConstructionOptions="@((BlazorMonaco.Editor.StandaloneCodeEditor editor) => {
        return new BlazorMonaco.Editor.StandaloneEditorConstructionOptions {
            AutomaticLayout = true,
            Language = "html",
            Value = Model.Form.FormHtml,
            AutoClosingBrackets = "always",
            AutoClosingComments = "always",
            AutoClosingQuotes = "always",
            DragAndDrop = true,
            FormatOnPaste = true,
            FormatOnType = true,
            ReadOnly = _readonly,
            RenderWhitespace = "all",
        };
    })" />

@code {
    private BlazorMonaco.Editor.StandaloneCodeEditor _standaloneCodeEditor_Form = null!;
    protected BlazorMonaco.Position _editorPosition = new BlazorMonaco.Position();

    protected void EditorUpdated_FormHtml(BlazorMonaco.Editor.ModelContentChangedEvent e)
    {
        // Debounce updates using a timer
        _timer_HtmlChanged.Stop();
        _timer_HtmlChanged.Start();
    }

    protected void EditorUpdated_FormHtml_CursorPosition(BlazorMonaco.Editor.CursorPositionChangedEvent e)
    {
        _editorPosition = e.Position;
    }
}
```

### Use Case 2: JavaScript Editor with Bracket Colorization

```razor
<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_standaloneCodeEditor_Javascript"
    Id="javascript-editor"
    CssClass="code-editor"
    OnDidChangeModelContent="EditorUpdated_FormJavascript"
    ConstructionOptions="@((BlazorMonaco.Editor.StandaloneCodeEditor editor) => {
        return new BlazorMonaco.Editor.StandaloneEditorConstructionOptions {
            AutomaticLayout = true,
            Language = "javascript",
            Value = Model.Form.Javascript,
            AutoClosingBrackets = "always",
            AutoClosingComments = "always",
            AutoClosingQuotes = "always",
            BracketPairColorization = new BlazorMonaco.Editor.BracketPairColorizationOptions {
                Enabled = true,
                IndependentColorPoolPerBracketType = true,
            },
            DragAndDrop = true,
            FormatOnPaste = true,
            FormatOnType = true,
            ReadOnly = _readonly,
            RenderWhitespace = "all",
        };
    })" />
```

### Use Case 3: C# Code Editor in Workflow

From `Workflow.razor`:

```razor
<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_standaloneCodeEditor_CSharp"
    Id="workflow-CSharp-Code"
    CssClass="code-editor"
    OnDidChangeModelContent="UpdatedEditor_CSharp"
    ConstructionOptions="@((BlazorMonaco.Editor.StandaloneCodeEditor editor) => {
        return new BlazorMonaco.Editor.StandaloneEditorConstructionOptions {
            AutomaticLayout = true,
            Language = "csharp",
            Value = ruleCSharp.Code,
            AutoClosingBrackets = "always",
            AutoClosingComments = "always",
            AutoClosingQuotes = "always",
            DragAndDrop = true,
            FormatOnPaste = true,
            FormatOnType = true,
        };
    })" />

@code {
    private BlazorMonaco.Editor.StandaloneCodeEditor _standaloneCodeEditor_CSharp = null!;

    protected async void UpdatedEditor_CSharp(BlazorMonaco.Editor.ModelContentChangedEvent e)
    {
        ruleCSharp.Code = await _standaloneCodeEditor_CSharp.GetValue();
    }
}
```

### Use Case 4: CSS Editor

```razor
<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_standaloneCodeEditor_CSS"
    Id="css-editor"
    CssClass="code-editor"
    OnDidChangeModelContent="EditorUpdated_FormCss"
    ConstructionOptions="@((BlazorMonaco.Editor.StandaloneCodeEditor editor) => {
        return new BlazorMonaco.Editor.StandaloneEditorConstructionOptions {
            AutomaticLayout = true,
            Language = "css",
            Value = Model.Form.css,
            AutoClosingBrackets = "always",
            AutoClosingComments = "always",
            AutoClosingQuotes = "always",
            BracketPairColorization = new BlazorMonaco.Editor.BracketPairColorizationOptions {
                Enabled = true,
                IndependentColorPoolPerBracketType = true,
            },
            DragAndDrop = true,
            FormatOnPaste = true,
            FormatOnType = true,
            ReadOnly = _readonly,
            RenderWhitespace = "all",
        };
    })" />
```

### Use Case 5: Insert Text at Selection

```razor
protected async Task InsertSnippet(string snippet)
{
    var selection = await _standaloneCodeEditor_Form.GetSelection();

    var edits = new List<BlazorMonaco.Editor.IdentifiedSingleEditOperation>();
    edits.Add(new BlazorMonaco.Editor.IdentifiedSingleEditOperation {
        ForceMoveMarkers = false,
        Range = selection,
        Text = snippet,
    });

    var selectionList = new List<BlazorMonaco.Selection> {
        new BlazorMonaco.Selection {
            StartLineNumber = selection.StartLineNumber,
            EndLineNumber = selection.StartLineNumber,
            StartColumn = selection.StartColumn,
            EndColumn = selection.StartColumn,
            PositionColumn = selection.StartColumn,
            PositionLineNumber = selection.StartLineNumber,
            SelectionStartColumn = selection.StartColumn,
            SelectionStartLineNumber = selection.StartLineNumber,
        },
    };

    await _standaloneCodeEditor_Form.ExecuteEdits("insert-snippet", edits, selectionList);
}
```

---

## Part 3: Available Languages

Use constants from `MonacoEditor.MonacoLanguage`:

### Common Languages

| Language | Constant | Use Case |
|----------|----------|----------|
| Plain Text | `MonacoLanguage.plaintext` | Generic text editing |
| HTML | `MonacoLanguage.html` | Form templates, email templates |
| CSS | `MonacoLanguage.css` | Stylesheets |
| JavaScript | `MonacoLanguage.javascript` | Client-side scripts |
| C# | `MonacoLanguage.csharp` | Workflow code blocks |
| JSON | `MonacoLanguage.json` | Configuration, API responses |
| SQL | `MonacoLanguage.sql` | Database queries |
| XML | `MonacoLanguage.xml` | Config files |
| Markdown | `MonacoLanguage.markdown` | Documentation |
| YAML | `MonacoLanguage.yaml` | Configuration |

### Database Languages

| Language | Constant |
|----------|----------|
| SQL (generic) | `MonacoLanguage.sql` |
| MySQL | `MonacoLanguage.mysql` |
| PostgreSQL | `MonacoLanguage.pgsql` |
| Redis | `MonacoLanguage.redis` |

### Full Language List

```csharp
public static class MonacoLanguage
{
    public const string plaintext = "plaintext";
    public const string html = "html";
    public const string css = "css";
    public const string javascript = "javascript";
    public const string typescript = "typescript";
    public const string csharp = "csharp";
    public const string json = "json";
    public const string xml = "xml";
    public const string sql = "sql";
    public const string mysql = "mysql";
    public const string pgsql = "pgsql";
    public const string markdown = "markdown";
    public const string yaml = "yaml";
    public const string python = "python";
    public const string java = "java";
    public const string cpp = "cpp";
    public const string go = "go";
    public const string rust = "rust";
    public const string ruby = "ruby";
    public const string php = "php";
    public const string shell = "shell";
    public const string powershell = "powershell";
    public const string dockerfile = "dockerfile";
    // ... and 70+ more
}
```

---

## Part 4: Styling

### Default CSS Class

The MonacoEditor wrapper uses `default-editor` class with dynamic min-height:

```css
/* Applied via inline style element */
#editorId.default-editor,
#editorId.default-editor .monaco-diff-editor {
    min-height: 300px;
}
```

### Custom CSS Class

Create custom classes for different sizing needs:

```css
/* In your site.css */
.code-editor {
    min-height: 400px;
    border: 1px solid #ddd;
    border-radius: 4px;
}

.code-editor-small {
    min-height: 150px;
}

.code-editor-large {
    min-height: 600px;
}

.code-editor-fullscreen {
    height: 100vh;
    min-height: 100vh;
}
```

Usage:

```razor
<MonacoEditor
    Class="code-editor-large"
    Language="@MonacoEditor.MonacoLanguage.csharp"
    @bind-Value="_code" />
```

---

## Part 5: Common Configuration Options

### Recommended Default Options

```csharp
new BlazorMonaco.Editor.StandaloneEditorConstructionOptions {
    // Layout
    AutomaticLayout = true,           // Auto-resize with container

    // Language
    Language = "html",

    // Initial content
    Value = "initial content",

    // Auto-closing
    AutoClosingBrackets = "always",   // (), [], {}
    AutoClosingComments = "always",   // /* */
    AutoClosingQuotes = "always",     // "", ''

    // User experience
    DragAndDrop = true,               // Allow dragging text
    FormatOnPaste = true,             // Auto-format pasted code
    FormatOnType = true,              // Auto-format while typing

    // Read-only mode
    ReadOnly = false,

    // Whitespace visualization
    RenderWhitespace = "all",         // Show spaces and tabs
}
```

### Enhanced Options for Code

```csharp
new BlazorMonaco.Editor.StandaloneEditorConstructionOptions {
    AutomaticLayout = true,
    Language = "csharp",
    Value = initialCode,

    // Bracket matching
    BracketPairColorization = new BlazorMonaco.Editor.BracketPairColorizationOptions {
        Enabled = true,
        IndependentColorPoolPerBracketType = true,
    },

    // Line numbers
    LineNumbers = "on",               // "on", "off", "relative"

    // Minimap
    Minimap = new BlazorMonaco.Editor.EditorMinimapOptions {
        Enabled = true,
    },

    // Word wrap
    WordWrap = "on",                  // "on", "off", "wordWrapColumn"

    // Font
    FontSize = 14,
    FontFamily = "Consolas, 'Courier New', monospace",
}
```

### Diff Editor Options

```csharp
new BlazorMonaco.Editor.StandaloneDiffEditorConstructionOptions {
    AutomaticLayout = true,
    OriginalEditable = false,         // Left side read-only
    RenderSideBySide = true,          // Side-by-side vs inline
    UseInlineViewWhenSpaceIsLimited = true,
}
```

---

## Part 6: Debouncing Updates

The custom MonacoEditor uses a timer for debouncing. For direct usage:

```razor
@implements IDisposable

<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_editor"
    OnDidChangeModelContent="EditorUpdated"
    ConstructionOptions="EditorOptions" />

@code {
    private BlazorMonaco.Editor.StandaloneCodeEditor _editor = null!;
    private System.Timers.Timer _debounceTimer = new System.Timers.Timer();

    protected override void OnInitialized()
    {
        _debounceTimer.Interval = 1000; // 1 second
        _debounceTimer.Elapsed += OnDebounceElapsed;
        _debounceTimer.AutoReset = false;
    }

    protected void EditorUpdated(BlazorMonaco.Editor.ModelContentChangedEvent e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    protected async void OnDebounceElapsed(object? source, System.Timers.ElapsedEventArgs e)
    {
        string value = await _editor.GetValue();
        // Process the updated value
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _debounceTimer.Elapsed -= OnDebounceElapsed;
        _debounceTimer.Dispose();
    }
}
```

---

## Part 7: Advanced Features (nForm Complex Patterns)

These advanced patterns are used in nForm's Form.razor for sophisticated editor interactions.

### Feature 1: Snippet Dropdown Insertion

Insert pre-defined HTML snippets from a dropdown into the editor at cursor:

```razor
@* Snippet selector dropdown *@
@if (Model.Snippets.Any(x => x.Enabled)) {
    <div class="input-group input-group-sm mb-2">
        <span class="input-group-text">
            <Language Tag="Snippets" />
        </span>
        <select class="form-select" id="edit-form-insert-snippet" @onchange="InsertSnippet">
            <option value=""></option>
            @foreach (var snippet in Model.Snippets.Where(x => x.Enabled).OrderBy(x => x.Name)) {
                <option value="@snippet.SnippetId">@snippet.Name</option>
            }
        </select>
    </div>
}

<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_standaloneCodeEditor_Form"
    Id="html-form-editor"
    CssClass="code-editor"
    OnDidChangeModelContent="EditorUpdated_FormHtml"
    ConstructionOptions="@EditorOptions" />

@code {
    private BlazorMonaco.Editor.StandaloneCodeEditor _standaloneCodeEditor_Form = null!;

    protected async Task InsertSnippet(ChangeEventArgs e)
    {
        string value = String.Empty;

        if (e.Value != null) {
            try {
                value += e.Value.ToString();
            } catch { }
        }

        var snippet = Model.Snippets.FirstOrDefault(x => x.SnippetId.ToString() == value);
        if (snippet != null && !String.IsNullOrWhiteSpace(snippet.Snippet)) {
            // Get current selection/cursor position
            var selection = await _standaloneCodeEditor_Form.GetSelection();

            // Create the edit operation
            var edits = new List<BlazorMonaco.Editor.IdentifiedSingleEditOperation>();
            edits.Add(new BlazorMonaco.Editor.IdentifiedSingleEditOperation {
                ForceMoveMarkers = false,
                Range = selection,
                Text = snippet.Snippet,
            });

            // Position cursor after inserted text
            var selectionList = new List<BlazorMonaco.Selection> {
                new BlazorMonaco.Selection {
                    StartLineNumber = selection.StartLineNumber,
                    EndLineNumber = selection.StartLineNumber,
                    StartColumn = selection.StartColumn,
                    EndColumn = selection.StartColumn,
                    PositionColumn = selection.StartColumn,
                    PositionLineNumber = selection.StartLineNumber,
                    SelectionStartColumn = selection.StartColumn,
                    SelectionStartLineNumber = selection.StartLineNumber,
                },
            };

            // Execute the edit
            await _standaloneCodeEditor_Form.ExecuteEdits("insert-snippet", edits, selectionList);
        }
    }
}
```

**What this does:** Allows users to select reusable HTML snippets from a dropdown and insert them at the current cursor position in the Monaco editor.

### Feature 2: Delta Decorations (Region Highlighting)

Highlight specific regions in the editor (e.g., selected form fields):

```razor
@code {
    protected DataObjects.HtmlFormField? _selectedField;

    protected async Task ApplyFieldHighlighting(bool scrollToElement = false)
    {
        // First, clear any existing decorations
        await _standaloneCodeEditor_Form.ResetDeltaDecorations();

        if (_selectedField != null) {
            // Find all instances of this field in the HTML
            var fields = Model.HtmlFormFields.Where(x => x.Name == _selectedField.Name).ToList();

            if (fields != null && fields.Count > 0) {
                foreach (var field in fields) {
                    // See if we have valid position data
                    if (field.RowStart.HasValue && field.ColStart.HasValue &&
                        field.RowEnd.HasValue && field.ColEnd.HasValue) {

                        // Create decoration for this field
                        var decorations = new List<BlazorMonaco.Editor.ModelDeltaDecoration> {
                            new BlazorMonaco.Editor.ModelDeltaDecoration {
                                Range = new BlazorMonaco.Range(
                                    field.RowStart.Value,
                                    field.ColStart.Value,
                                    field.RowEnd.Value,
                                    field.ColEnd.Value
                                ),
                                Options = new BlazorMonaco.Editor.ModelDecorationOptions {
                                    InlineClassName = "html-field-selected-element",
                                }
                            }
                        };

                        // Apply the decoration
                        await _standaloneCodeEditor_Form.DeltaDecorations(null, decorations.ToArray());

                        // Optionally scroll to show the highlighted element
                        if (scrollToElement) {
                            await _standaloneCodeEditor_Form.RevealLineInCenter(field.RowStart.Value);
                        }
                    }
                }
            }
        }
    }
}
```

**CSS for decoration:**
```css
/* In your site.css */
.html-field-selected-element {
    background-color: rgba(255, 255, 0, 0.3);
    border: 1px solid #ffc107;
}
```

**What this does:** Highlights form field elements in the HTML editor when selected from an external UI, making it easy to locate fields in complex HTML.

### Feature 3: Bidirectional Cursor-to-UI Sync

Sync cursor position with external UI elements (clickable field badges):

```razor
@* Field listing badges above editor *@
@if (Model.HtmlFormFields.Any()) {
    <div class="mb-2">
        <span class="small me-1"><Language Tag="FormFields" />:</span>
        @foreach (var item in Model.HtmlFormFields) {
            var itemTitle = (item.Required ? Helpers.Text("Required") + " " : "") +
                Helpers.Text("Field") + " '" + item.Name + "'";

            @* Badge highlights when this field is selected *@
            <span class="badge me-1 field-listing @(_selectedField != null && item.Index == _selectedField.Index ? "text-bg-primary" : "text-bg-light")"
                  title="@itemTitle"
                  @onclick="@(() => FieldListingItemClicked(item))">
                @if (item.Required) {
                    <span class="required-indicator"><i class="required-flag"></i></span>
                }
                @item.Name
            </span>
        }
    </div>
}

<BlazorMonaco.Editor.StandaloneCodeEditor
    @ref="_standaloneCodeEditor_Form"
    Id="html-form-editor"
    CssClass="code-editor"
    OnDidChangeModelContent="EditorUpdated_FormHtml"
    OnDidChangeCursorPosition="EditorUpdated_FormHtml_CursorPosition"
    ConstructionOptions="@EditorOptions" />

@code {
    protected BlazorMonaco.Position _editorPosition = new BlazorMonaco.Position();
    protected DataObjects.HtmlFormField? _selectedField;
    private System.Timers.Timer _timer_CursorMoved = new System.Timers.Timer();

    protected override void OnInitialized()
    {
        // Debounce cursor position updates
        _timer_CursorMoved.Interval = 300;
        _timer_CursorMoved.Elapsed += UpdateSelectedFieldFromCursor;
        _timer_CursorMoved.AutoReset = false;
    }

    // Called when user moves cursor in editor
    protected void EditorUpdated_FormHtml_CursorPosition(BlazorMonaco.Editor.CursorPositionChangedEvent e)
    {
        _timer_CursorMoved.Stop();
        _timer_CursorMoved.Start();
        _editorPosition = e.Position;
    }

    // Debounced handler: find which field the cursor is in
    protected void UpdateSelectedFieldFromCursor(Object? source, System.Timers.ElapsedEventArgs e)
    {
        // Find field at current cursor position
        var formField = Model.HtmlFormFields.FirstOrDefault(x =>
            x.RowStart.HasValue && x.RowEnd.HasValue &&
            _editorPosition.LineNumber >= x.RowStart.Value &&
            _editorPosition.LineNumber <= x.RowEnd.Value);

        if (formField != null) {
            _selectedField = formField;
            InvokeAsync(StateHasChanged);
        }
    }

    // Called when user clicks a field badge
    protected async Task FieldListingItemClicked(DataObjects.HtmlFormField field)
    {
        _selectedField = field;
        await ApplyFieldHighlighting(scrollToElement: true);
    }
}
```

**What this does:** Creates a two-way sync between:
- **Badge → Editor:** Click a field badge to highlight and scroll to that field in the editor
- **Editor → Badge:** Move cursor in editor to auto-highlight the corresponding field badge

### Feature 4: JavaScript Function Insertion with Help

Insert predefined JavaScript functions with inline help:

```razor
<div class="alert alert-info">
    <Language Tag="FormJavascriptEditorInfo" />

    <button type="button" class="btn btn-xs btn-dark"
            @onclick="@(() => { _showHelpForJavascript = !_showHelpForJavascript; StateHasChanged(); })">
        <Icon Name="Info" />
    </button>

    @if (_showHelpForJavascript) {
        <div class="mt-3 mb-2">
            Use the function
            <a href="javascript:void('0');" @onclick="@(() => InsertJavascript("nFormOnLoad"))">
                nFormOnLoad()
            </a>
            for any javascript events to run when the page has loaded the form.
        </div>

        <div class="mb-2">
            Use the function
            <a href="javascript:void('0');" @onclick="@(() => InsertJavascript("nFormPreCheck"))">
                nFormPreCheck()
            </a>
            for any javascript to execute before form validation.
        </div>

        <div class="mb-2">
            Use the function
            <a href="javascript:void('0');" @onclick="@(() => InsertJavascript("nFormCheckForm"))">
                nFormCheckForm()
            </a>
            for validation checks prior to submitting
            (return false if validation fails, true if it passes.)
        </div>
    }
</div>

@code {
    protected bool _showHelpForJavascript = false;

    protected async Task InsertJavascript(string functionName)
    {
        string code = functionName switch {
            "nFormOnLoad" => "function nFormOnLoad() {\n    // Your code here\n}\n",
            "nFormPreCheck" => "function nFormPreCheck() {\n    // Runs before validation\n}\n",
            "nFormCheckForm" => "function nFormCheckForm() {\n    // Return false to prevent submit\n    return true;\n}\n",
            _ => ""
        };

        if (!String.IsNullOrEmpty(code)) {
            var selection = await _standaloneCodeEditor_Javascript.GetSelection();
            var edits = new List<BlazorMonaco.Editor.IdentifiedSingleEditOperation> {
                new BlazorMonaco.Editor.IdentifiedSingleEditOperation {
                    ForceMoveMarkers = false,
                    Range = selection,
                    Text = code,
                }
            };
            await _standaloneCodeEditor_Javascript.ExecuteEdits("insert-function", edits, null);
        }
    }
}
```

**What this does:** Provides clickable links in help text that insert predefined JavaScript function templates into the editor.

### Feature 5: Multiple Debounced Editors

Managing multiple editors with separate debounce timers:

```razor
@implements IDisposable

@code {
    private BlazorMonaco.Editor.StandaloneCodeEditor _standaloneCodeEditor_CSS = null!;
    private BlazorMonaco.Editor.StandaloneCodeEditor _standaloneCodeEditor_Form = null!;
    private BlazorMonaco.Editor.StandaloneCodeEditor _standaloneCodeEditor_Javascript = null!;

    // Separate timers for each editor
    protected System.Timers.Timer _timer_CssChanged = new System.Timers.Timer();
    protected System.Timers.Timer _timer_HtmlChanged = new System.Timers.Timer();
    protected System.Timers.Timer _timer_JavascriptChanged = new System.Timers.Timer();
    protected System.Timers.Timer _timer_HtmlChanged_CursorMoved = new System.Timers.Timer();

    protected override void OnInitialized()
    {
        // CSS editor: 1 second debounce
        _timer_CssChanged.Interval = 1000;
        _timer_CssChanged.Elapsed += UpdateTimerCssChanged;
        _timer_CssChanged.AutoReset = false;

        // HTML editor: 1 second debounce for content
        _timer_HtmlChanged.Interval = 1000;
        _timer_HtmlChanged.Elapsed += UpdateTimerHtmlChanged;
        _timer_HtmlChanged.AutoReset = false;

        // HTML editor: 300ms debounce for cursor (faster for UI responsiveness)
        _timer_HtmlChanged_CursorMoved.Interval = 300;
        _timer_HtmlChanged_CursorMoved.Elapsed += UpdateTimerHtmlChanged_CursorMoved;
        _timer_HtmlChanged_CursorMoved.AutoReset = false;

        // JavaScript editor: 1 second debounce
        _timer_JavascriptChanged.Interval = 1000;
        _timer_JavascriptChanged.Elapsed += UpdateTimerJavascriptChanged;
        _timer_JavascriptChanged.AutoReset = false;
    }

    // Individual timer handlers
    protected void UpdateTimerCssChanged(Object? source, System.Timers.ElapsedEventArgs e)
    {
        var task = Task.Run(async () => {
            Model.Form.css = await _standaloneCodeEditor_CSS.GetValue();
            await InvokeAsync(StateHasChanged);
        });
    }

    protected void UpdateTimerHtmlChanged(Object? source, System.Timers.ElapsedEventArgs e)
    {
        var task = Task.Run(async () => {
            Model.Form.FormHtml = await _standaloneCodeEditor_Form.GetValue();
            // Re-parse form fields from updated HTML
            await ParseFormFields();
            await InvokeAsync(StateHasChanged);
        });
    }

    protected void UpdateTimerJavascriptChanged(Object? source, System.Timers.ElapsedEventArgs e)
    {
        var task = Task.Run(async () => {
            Model.Form.Javascript = await _standaloneCodeEditor_Javascript.GetValue();
            await InvokeAsync(StateHasChanged);
        });
    }

    public void Dispose()
    {
        _timer_CssChanged.Elapsed -= UpdateTimerCssChanged;
        _timer_CssChanged.Dispose();

        _timer_HtmlChanged.Elapsed -= UpdateTimerHtmlChanged;
        _timer_HtmlChanged.Dispose();

        _timer_HtmlChanged_CursorMoved.Elapsed -= UpdateTimerHtmlChanged_CursorMoved;
        _timer_HtmlChanged_CursorMoved.Dispose();

        _timer_JavascriptChanged.Elapsed -= UpdateTimerJavascriptChanged;
        _timer_JavascriptChanged.Dispose();
    }
}
```

**What this does:** Manages multiple editors on the same page with independent debounce timers, allowing different update frequencies for content vs cursor position updates.

---

## Part 8: Quick Reference

### When to Use Each Approach

| Scenario | Use |
|----------|-----|
| Simple editing with binding | `MonacoEditor` wrapper |
| Diff/compare view | `MonacoEditor` with `ValueToDiff` |
| Multiple editors, shared logic | Direct `StandaloneCodeEditor` |
| Custom toolbar/snippets | Direct `StandaloneCodeEditor` |
| Maximum performance | Direct `StandaloneCodeEditor` |

### Common Patterns Summary

```razor
@* Simple binding *@
<MonacoEditor Language="@MonacoEditor.MonacoLanguage.html" @bind-Value="_html" />

@* Read-only *@
<MonacoEditor Language="@MonacoEditor.MonacoLanguage.json" Value="@_json" ReadOnly="true" />

@* Diff mode *@
<MonacoEditor Language="@MonacoEditor.MonacoLanguage.csharp" Value="@_new" ValueToDiff="@_original" />

@* Custom height *@
<MonacoEditor Language="@MonacoEditor.MonacoLanguage.sql" @bind-Value="_sql" MinHeight="500px" />

@* With reference for programmatic access *@
<MonacoEditor @ref="_editor" Language="@MonacoEditor.MonacoLanguage.css" @bind-Value="_css" />
```

### NuGet Package

```xml
<PackageReference Include="BlazorMonaco" Version="3.*" />
```

---

*Category: 008_components*
*Last Updated: 2025-12-23*
*Source: Private repo "nForm"*
