# PanoramicData.Blazor - Copilot Instructions

## Overview

**PanoramicData.Blazor** is a comprehensive Blazor component library providing rich UI controls for data visualization, forms, and user interaction.

**Repository**: .NET 9 solution with Blazor WebAssembly, Razor Pages demo applications, and a reusable component library.

---

## 🚨 CRITICAL BUILD RULE

**Build individual projects when making component changes, build the full solution only for verification.**

```sh
# ✅ CORRECT: Build only the component library
dotnet build PanoramicData.Blazor/PanoramicData.Blazor.csproj

# ✅ CORRECT: Run the demo to test changes
dotnet run --project PanoramicData.Blazor.Demo/PanoramicData.Blazor.Demo.csproj

# ✅ CORRECT: Run tests
dotnet test PanoramicData.Blazor.Test/PanoramicData.Blazor.Test.csproj

# ⚠️ USE SPARINGLY: Full solution build
dotnet build PanoramicData.Blazor.sln
```

---

## 📚 Project Structure

### Core Projects

| Project | Purpose | Technology |
|---------|---------|------------|
| **PanoramicData.Blazor** | Main component library | Blazor Components, .NET 9 |
| **PanoramicData.Blazor.Demo** | Demo application (Razor Pages) | Blazor Server, Razor Pages |
| **PanoramicData.Blazor.Web** | Additional demo/web host | Blazor Server |
| **PanoramicData.Blazor.WebAssembly** | WebAssembly demo (Client/Server) | Blazor WASM |
| **PanoramicData.Blazor.Test** | Unit tests | xUnit, bUnit |

### Key Directories

```
PanoramicData.Blazor/
├── Components/          # Individual Blazor components
│   ├── PDTimeline.razor(.cs/.css/.js)
│   ├── PDTable.razor
│   ├── PDTree.razor
│   └── ... (40+ components)
├── Models/             # Data models and DTOs
├── Services/           # Shared services
└── wwwroot/           # Static assets (JS, CSS)

PanoramicData.Blazor.Demo/
├── Pages/             # Demo pages for each component
│   ├── PDTimelinePage.razor
│   └── ...
└── Data/              # Demo data generators
```

---

## 🎯 Component Development Patterns

### Component Structure

Each component typically consists of:
- **`.razor`** - Markup and component structure
- **`.razor.cs`** - C# code-behind (partial class)
- **`.razor.css`** - Scoped CSS styles
- **`.razor.js`** - JavaScript interop (optional)

### Example: PDTimeline Component Files
```
PDTimeline.razor       # SVG markup, event handlers
PDTimeline.razor.cs    # Business logic, state management
PDTimeline.razor.css   # Component-specific styles
PDTimeline.razor.js    # JS interop for DOM measurements
```

### Component Naming Convention
- Components: `PD` prefix (e.g., `PDTimeline`, `PDTable`, `PDTree`)
- Demo pages: Component name + `Page` (e.g., `PDTimelinePage`)
- Models: Descriptive names (e.g., `TimelineOptions`, `TimeRange`, `DataPoint`)

---

## 🔧 Development Workflow

### 1. Making Component Changes

```sh
# 1. Make changes to component in PanoramicData.Blazor/
# 2. Build the component library
dotnet build PanoramicData.Blazor/PanoramicData.Blazor.csproj

# 3. Run demo to verify
dotnet run --project PanoramicData.Blazor.Demo/PanoramicData.Blazor.Demo.csproj

# 4. Navigate to: https://localhost:5001/component-name
```

### 2. Adding New Components

1. Create component files in `PanoramicData.Blazor/`
   - `PDNewComponent.razor`
   - `PDNewComponent.razor.cs` (if needed)
   - `PDNewComponent.razor.css` (if needed)
   - `PDNewComponent.razor.js` (if JS interop needed)

2. Create demo page in `PanoramicData.Blazor.Demo/Pages/`
   - `PDNewComponentPage.razor`
   - `PDNewComponentPage.razor.cs` (if needed)

3. Add navigation link in demo `NavMenu.razor`

### 3. Testing

```sh
# Run all tests
dotnet test PanoramicData.Blazor.Test/PanoramicData.Blazor.Test.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~FilterTests"

# Run with detailed output
dotnet test PanoramicData.Blazor.Test/PanoramicData.Blazor.Test.csproj -v detailed
```

**Note**: Currently, test coverage is minimal (only `FilterTests.cs` exists). Consider adding tests when fixing bugs or adding features.

---

## 📝 Code Style Guidelines

### C# Conventions

```csharp
// ✅ GOOD: Use primary constructors for DI (C# 12+)
public partial class PDTimeline(IJSRuntime jsRuntime) : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = jsRuntime;
}

// ✅ GOOD: Use file-scoped namespaces
namespace PanoramicData.Blazor;

// ✅ GOOD: Use modern C# features
private readonly Dictionary<int, DataPoint> _dataPoints = [];
DataPoint[] tempArray = [.. points.Where(x => x != null)];

// ✅ GOOD: Allman braces (opening brace on new line)
public bool CanZoomIn()
{
    if (IsEnabled)
    {
        return true;
    }
    return false;
}

// ❌ AVOID: K&R style braces
public bool CanZoomIn() {
    if (IsEnabled) {
        return true;
    }
    return false;
}

// ✅ GOOD: Private fields with underscore prefix
private int _selectionStartIndex = -1;
private bool _isChartDragging;

// ✅ GOOD: Async methods with ConfigureAwait(true) in Blazor
await SelectionChanged.InvokeAsync(_selectionRange).ConfigureAwait(true);

// ✅ GOOD: XML documentation for public APIs
/// <summary>
/// Gets or sets the current scale of the timeline.
/// </summary>
[Parameter]
public TimelineScale Scale { get; set; } = TimelineScale.Years;
```

### Blazor-Specific Patterns

```csharp
// ✅ GOOD: Parameter validation
[Parameter]
public DateTime MinDateTime { get; set; }

// ✅ GOOD: EventCallback for component events
[Parameter]
public EventCallback<TimeRange?> SelectionChanged { get; set; }

// ✅ GOOD: IJSRuntime for JavaScript interop
[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

// ✅ GOOD: Dispose pattern for JS modules
public async ValueTask DisposeAsync()
{
    if (_module != null)
    {
        await _module.InvokeVoidAsync("dispose", Id).ConfigureAwait(true);
        await _module.DisposeAsync().ConfigureAwait(true);
    }
    _objRef?.Dispose();
}

// ✅ GOOD: JSInvokable methods for callbacks from JS
[JSInvokable("PanoramicData.Blazor.PDTimeline.OnResize")]
public async Task OnResize()
{
    // Implementation
}
```

### JavaScript Interop

```javascript
// ✅ GOOD: Export functions for Blazor interop
export function initialize(id, options, dotNetRef) {
    const element = document.getElementById(id);
    // Setup
}

export function dispose(id) {
    // Cleanup
}

// ✅ GOOD: Call back to .NET
dotNetRef.invokeMethodAsync('MethodName', arg1, arg2);
```

---

## 🐛 Bug Fix Workflow

### Example: Timeline Selection Bug (Fixed)

**Problem Found**: In `SetSelection()`, when `start < RoundedMinDateTime`, it incorrectly set `start = RoundedMaxDateTime` instead of `RoundedMinDateTime`.

**Fix Process**:
1. ✅ Located the bug in code review
2. ✅ Made the fix: `start = RoundedMinDateTime;`
3. ✅ Built and verified: `dotnet build PanoramicData.Blazor/PanoramicData.Blazor.csproj`
4. ✅ Tested in demo application
5. ⚠️ **Missing**: Unit test to prevent regression

**Best Practice**: Add unit tests for bug fixes to prevent regression.

### Debugging Components

```sh
# 1. Add Debug.WriteLine statements for troubleshooting
System.Diagnostics.Debug.WriteLine($"[DEBUG] Variable: {value}");

# 2. Run demo in debug mode
# In Visual Studio: F5
# Or with dotnet CLI:
dotnet run --project PanoramicData.Blazor.Demo/PanoramicData.Blazor.Demo.csproj

# 3. Check browser developer console for JS errors
# F12 in browser -> Console tab

# 4. Remove debug statements before committing
# Search for: System.Diagnostics.Debug.WriteLine
```

---

## 📦 Component Feature Guide

### Major Components

| Component | Purpose | Key Features |
|-----------|---------|--------------|
| **PDTimeline** | Time-based data visualization | Selection, dragging, zoom, pan, scales |
| **PDTable** | Data grid with sorting/filtering | Virtual scrolling, editing, templates |
| **PDTree** | Hierarchical data display | Lazy loading, selection, drag-drop |
| **PDToolbar** | Command bars | Buttons, separators, placeholders |
| **PDForm** | Dynamic forms | Validation, auto-layout, field types |
| **PDSplitter** | Resizable panels | Horizontal/vertical, min/max sizes |
| **PDMonacoEditor** | Code editor | Syntax highlighting, IntelliSense |

### Common Patterns

#### Selection Management
```csharp
// Selection with TimeRange
public TimeRange? GetSelection() => _selectionRange;

public async Task SetSelection(DateTime start, DateTime end)
{
    // Validate
    if (start < RoundedMinDateTime)
        start = RoundedMinDateTime;
    
    // Notify
    await SelectionChanged.InvokeAsync(new TimeRange { StartTime = start, EndTime = end });
}
```

#### Options Pattern
```csharp
[Parameter]
public TimelineOptions Options { get; set; } = new TimelineOptions();

// Options classes in Models/
public class TimelineOptions
{
    public TimelineGeneralOptions General { get; set; } = new();
    public TimelineBarOptions Bar { get; set; } = new();
    public TimelineSelectionOptions Selection { get; set; } = new();
}
```

---

## 🔍 Common Issues & Solutions

### Issue: Component Not Updating
**Symptom**: Changes to component state don't reflect in UI

**Solution**: Call `StateHasChanged()` after state changes
```csharp
_selectionRange = new TimeRange { StartTime = start, EndTime = end };
StateHasChanged(); // Force UI update
```

### Issue: JavaScript Interop Errors
**Symptom**: `Cannot read property '...' of null` in browser console

**Solution**: Ensure JS module is loaded before calling
```csharp
if (_module != null)
{
    await _module.InvokeVoidAsync("functionName", args);
}
```

### Issue: Event Handlers Not Firing
**Symptom**: Click/pointer events don't trigger methods

**Solution**: 
1. Check event binding syntax: `@onclick="MethodName"`
2. Ensure method signature matches event args
3. Verify component is enabled/not disabled

### Issue: Build Errors After Adding JS File
**Symptom**: JS file not found at runtime

**Solution**: Ensure `.razor.js` file is a sibling to `.razor` file
```
PDComponent.razor
PDComponent.razor.cs
PDComponent.razor.js  ← Must be here, not in wwwroot/
```

---

## 🧪 Testing Guidelines

### Current State
- **Existing Tests**: Minimal (only `FilterTests.cs`)
- **Framework**: xUnit + bUnit (Blazor component testing)
- **Opportunity**: Most components lack unit tests

### Adding Tests (Recommended)

```csharp
using Bunit;
using Xunit;

public class PDTimelineTests : TestContext
{
    [Fact]
    public void SetSelection_BelowMinimum_ClampsToMinimum()
    {
        // Arrange
        var component = RenderComponent<PDTimeline>(parameters => parameters
            .Add(p => p.MinDateTime, new DateTime(2020, 1, 1))
            .Add(p => p.MaxDateTime, new DateTime(2020, 12, 31))
        );
        
        // Act
        await component.Instance.SetSelection(
            new DateTime(2019, 6, 1),  // Before min
            new DateTime(2020, 6, 1)
        );
        
        // Assert
        var selection = component.Instance.GetSelection();
        Assert.Equal(new DateTime(2020, 1, 1), selection.StartTime);
    }
}
```

---

## 📖 Quick Reference

### Most Common Commands

```sh
# Build component library
dotnet build PanoramicData.Blazor/PanoramicData.Blazor.csproj

# Run demo (Razor Pages)
dotnet run --project PanoramicData.Blazor.Demo/PanoramicData.Blazor.Demo.csproj

# Run demo (WASM - slower startup)
dotnet run --project PanoramicData.Blazor.WebAssembly/Server/PanoramicData.Blazor.WebAssembly.Server.csproj

# Run tests
dotnet test PanoramicData.Blazor.Test/PanoramicData.Blazor.Test.csproj

# Search for component usage
# (Useful for understanding component patterns)
dotnet build /t:Rebuild # Ensures search is up to date
```

### File Search Patterns

```sh
# Find all component definitions
# Search: *.razor files in PanoramicData.Blazor/

# Find component demos
# Search: *Page.razor in PanoramicData.Blazor.Demo/Pages/

# Find JavaScript interop
# Search: *.razor.js files

# Find models/DTOs
# Search: PanoramicData.Blazor/Models/
```

---

## 🚨 Before Starting ANY Task

### Mandatory Checklist
- [ ] Understand which component(s) you're modifying
- [ ] Check if component has a demo page to test changes
- [ ] Review existing patterns in similar components
- [ ] Consider if change needs unit tests
- [ ] Verify change doesn't break existing functionality

### Never Claim Victory Prematurely
- **ALWAYS verify** work is complete with targeted testing
- **Check demo page** - does the component still work?
- **Run build** - does it compile without errors?
- **Search for references** - did you update all usages?
- Ask user to confirm rather than assuming success

---

## 🎯 Component Development Checklist

### Adding a New Feature to Existing Component
- [ ] Update `.razor.cs` with new property/method
- [ ] Update `.razor` markup if UI changes needed
- [ ] Update `.razor.js` if JavaScript changes needed
- [ ] Update demo page to showcase new feature
- [ ] Test in demo application
- [ ] Consider adding unit test
- [ ] Update XML documentation

### Creating a New Component
- [ ] Create `PDNewComponent.razor` in `PanoramicData.Blazor/`
- [ ] Create `PDNewComponentPage.razor` in `PanoramicData.Blazor.Demo/Pages/`
- [ ] Add navigation entry in demo `NavMenu.razor`
- [ ] Follow naming conventions (`PD` prefix)
- [ ] Add XML documentation for public API
- [ ] Create options class if component is configurable
- [ ] Implement `IAsyncDisposable` if using JS interop
- [ ] Test in both demo apps (Server and WASM)

### Fixing a Bug
- [ ] Reproduce the bug in demo application
- [ ] Locate the source of the issue
- [ ] Make minimal fix (don't refactor unnecessarily)
- [ ] Test fix in demo application
- [ ] Verify no regressions in related functionality
- [ ] Consider adding unit test to prevent regression
- [ ] Remove any debug logging before commit

---

## 🔗 Useful Patterns from Codebase

### Async Event Callbacks
```csharp
[Parameter]
public EventCallback<TimeRange?> SelectionChanged { get; set; }

// Invoke with ConfigureAwait(true) in Blazor
await SelectionChanged.InvokeAsync(_selectionRange).ConfigureAwait(true);
```

### JavaScript Module Loading
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && JSRuntime is not null)
    {
        _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "import", 
            "./_content/PanoramicData.Blazor/PDComponent.razor.js"
        );
    }
}
```

### Pointer Event Handling
```csharp
private async Task OnPointerDown(PointerEventArgs args)
{
    // Refresh canvas position before drag (handles layout changes)
    if (_commonModule != null)
    {
        _canvasX = await _commonModule.InvokeAsync<double>("getX", _svgElement);
    }
    
    // Capture pointer for smooth dragging
    await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _element);
}
```

### Options Pattern with Defaults
```csharp
[Parameter]
public TimelineOptions Options { get; set; } = new TimelineOptions();

// In Options class
public class TimelineOptions
{
    public TimelineBarOptions Bar { get; set; } = new() 
    { 
        Width = 20, 
        Padding = 2 
    };
}
```

---

## 📚 Additional Resources

### External Dependencies
- **Blazor**: Microsoft's SPA framework using .NET and C#
- **bUnit**: Blazor component testing library
- **Monaco Editor**: VS Code's editor (for PDMonacoEditor component)

### Official Documentation
- Blazor: https://docs.microsoft.com/aspnet/core/blazor/
- bUnit: https://bunit.dev/
- C# 12 Features: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-12

---

## 🎯 Trust These Instructions

This repository has been analyzed for component development patterns. When working on components:

1. **Check the demo pages** - they show intended usage
2. **Follow existing patterns** - consistency matters
3. **Test changes in demo** - don't assume it works
4. **Consider adding tests** - prevent future regressions

**Focus on your specific component** - you don't need to understand all 40+ components at once.

---

## 🧑‍💻 Git Workflow

### Branch Naming
```sh
# Feature branches
feature/descriptive-name

# Bug fix branches  
bug/john/timeline-selection-fix  # Current branch example

# Release branches
release/v1.2.3
```

### Commit Messages
```sh
# ✅ GOOD: Clear, descriptive
git commit -m "Fix SetSelection clamping to use RoundedMinDateTime"
git commit -m "Add unit tests for PDTimeline selection edge cases"
git commit -m "Update PDTimelinePage demo with new features"

# ❌ AVOID: Vague or too brief
git commit -m "fix bug"
git commit -m "updates"
```

### Before Committing
```sh
# 1. Remove debug statements
# Search for: System.Diagnostics.Debug.WriteLine

# 2. Verify build
dotnet build PanoramicData.Blazor/PanoramicData.Blazor.csproj

# 3. Run tests (if any)
dotnet test PanoramicData.Blazor.Test/PanoramicData.Blazor.Test.csproj

# 4. Test in demo
dotnet run --project PanoramicData.Blazor.Demo/PanoramicData.Blazor.Demo.csproj
```

---

## 📖 Component Documentation System

Each component demo page has three tabs: **Demo**, **Source**, and **Documentation**.

### Documentation Tab Structure

The Documentation tab provides comprehensive component documentation with interactive examples.

#### Required Components
- **`DemoSourceView`** - The main wrapper that provides tab navigation
- **`DocExample`** - Reusable component for code + live demo examples
- **`PDComponentDocumentation.razor`** - Separate file for documentation content

#### Creating Documentation for a Component

1. **Create documentation component**: `PanoramicData.Blazor.Demo/Pages/PDComponentDocumentation.razor`

2. **Structure the documentation**:
```razor
<div class="pd-component-documentation">
    <section id="overview">
        <h2 class="doc-section">Overview</h2>
        <!-- Component description, key features -->
    </section>

    <section id="parameters">
        <h2 class="doc-section">Parameters</h2>
        <!-- Tables of all parameters with types and descriptions -->
    </section>

    <section id="examples">
        <h2 class="doc-section">Examples</h2>
        <!-- Progressive examples using DocExample component -->
    </section>

    <section id="events">
        <h2 class="doc-section">Events</h2>
        <!-- EventCallback documentation -->
    </section>
</div>
```

3. **Use DocExample for each example**:
```razor
<DocExample Title="Basic Usage"
            AnchorId="basic-usage"
            Code="@_exampleCode"
            Language="razor"
            DemoStyle="height: 250px;">
    <DemoContent>
        <PDComponent Options="@_exampleOptions" />
    </DemoContent>
    <Description>
        <p>Explanation of what this example demonstrates...</p>
    </Description>
</DocExample>
```

4. **Add to demo page**:
```razor
<DemoSourceView SourceFiles="...">
    <DocumentationContent>
        <PDComponentDocumentation />
    </DocumentationContent>
    <ChildContent>
        <!-- Existing demo content -->
    </ChildContent>
</DemoSourceView>
```

#### Documentation Guidelines

- **Progressive complexity**: Start with minimal examples, build to advanced usage
- **Interactive demos**: Where possible, make demo content interactive
- **Code accuracy**: Ensure example code matches the live demo exactly
- **Deep linking**: Use meaningful `AnchorId` values for URL bookmarking
- **Mobile responsive**: Code stacks above demo on narrow screens
- **Auto-sizing**: Monaco editor auto-sizes to fit code content

#### URL Deep Linking

Demo pages support deep linking to tabs and anchors:
- `/pdtiles?tab=demo` - Demo tab
- `/pdtiles?tab=source` - Source tab
- `/pdtiles?tab=docs` - Documentation tab
- `/pdtiles?tab=docs#row-curves` - Documentation tab, scrolled to anchor

---

**Last Updated**: Based on .NET 9, C# 12, current as of main branch
