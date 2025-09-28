# PD Studio Component Implementation Plan

## Overview
This document outlines the plan for implementing a new **PD Studio** component - a versatile development environment with splitter-based layout that can serve as an abstract base for specialized studios like SQL Studio or Query Studio.

## Component Architecture

### Main Component Structure
```
PDStudio
├── PDSplitter (Main vertical splitter)
│   ├── Top Panel (Horizontal splitter)
│   │   ├── Left Panel
│   │   │   ├── PDToolbar (Above Monaco Editor)
│   │   │   └── Monaco Editor
│   │   └── Right Panel: Results Display (HTML output)
│   └── Bottom Panel: Debug Console (Terminal-like feed - full width)
```

## Component Breakdown

### 1. PDStudio.razor (Main Component)
- **Purpose**: Root component that orchestrates the entire studio layout
- **Layout**: Uses nested PDSplitter components for flexible layout management
- **Responsibilities**:
  - Manage overall layout and splitter configuration
  - Coordinate communication between child components
  - Handle resize events and maintain panel proportions
  - Provide configuration options for different studio types

### 2. Top Panel Structure
#### 2.1 Left Section - Monaco Editor
- **Components**: PDMenuBar + PDToolbar + PDMonacoEditor
- **Features**:
  - **PDMenuBar** (new component) with standard menu items:
    - **File Menu**: Open, Save, Save As, New, etc.
    - **Logging Menu**: Change default log level (Debug, Info, Warning, Error)
    - Additional menus as needed
  - **PDToolbar** with essential buttons:
    - **Play Button**: Execute code (transforms to Cancel when running)
    - **Cancel Button**: Stop execution (only visible when running)  
    - **Hide/Show Console Button**: Toggle console visibility
    - Additional buttons to be defined later
  - **Monaco Editor Behavior**:
    - **Configurable during execution**: `IsEditingEnabledDuringExecution` option (default: true)
    - When false: **Spinner overlay blocks interaction** with Monaco Editor  
    - When true: Editor remains editable during execution
    - **Language from PDStudioOptions**: Current language, with plan for future file-based detection
  - **Language Support**: Queriable via IPDStudioService interface
    - PostgreSQL, MSSQL, NCalc, JSON, PlainText, and others
  - Syntax highlighting based on studio type
  - Code completion and IntelliSense
  - Integration with IPDStudioService for code execution

#### 2.2 Right Section - Results Display
- **Component**: PDStudioResults (new component)
- **Features**:
  - **Status Bar**: Same height as toolbar, shows execution progress/status
  - **iframe HTML rendering**: Safe HTML content display using iframe
  - Real-time updates during code execution via IPDStudioService
  - Scrollable content area within iframe
  - Future extensibility for different result types
  - Error state handling
  - Loading indicators during execution

### 3. Bottom Panel - PDLog Component (Toggleable - Full Width)
- **Component**: PDLog (existing component - ILogger integration)
- **Features**:
  - **Initial Visibility**: Controlled by `IsLoggingVisible` option in PDStudioOptions
  - **Runtime Toggle**: Can be hidden/shown via toolbar button
  - Integration with ILogger interface (PDLog logger provider)
  - **Configurable Log Level**: Via menu option (Logging menu)
  - **Default Log Level**: Debug level selected by default
  - Displays execution logs, errors, warnings, debug info
  - Auto-scroll to bottom for new messages
  - Timestamps for log entries
  - Color-coded message types (Info, Warning, Error, Debug)
  - Clear log functionality
  - Spans full width underneath both left and right panels
  - Receives streaming log information from IPDStudioService events

## New Components to Create

### 1. IPDStudioService Interface
**File**: `PanoramicData.Blazor/Interfaces/IPDStudioService.cs`

**Key Features**:
- Standard interface for code execution services
- Support for streaming log information to ILogger
- Methods for executing code and updating results
- Cancellation token support for stopping execution

### 2. PDStudio.razor
**Files**: 
- `PDStudio.razor` - Main component markup
- `PDStudio.razor.cs` - Component logic
- `PDStudio.razor.css` - Styling

- **Key Features**: 
  - **Initial Visibility**: Logging panel visibility controlled by `IsLoggingVisible` option
  - **Runtime Toggle**: Show/Hide logging via toolbar button
  - Execution state management (running/stopped)
  - Play/Cancel button state management
  - Editor behavior control: Configurable read-only state during execution

### 2. PDStudioConsole.razor
**Files**:
- `PDStudioConsole.razor` - Console component markup
- `PDStudioConsole.razor.cs` - Console logic
- `PDStudioConsole.razor.css` - Console styling

**Key Features**:
- Message collection with timestamps
- Auto-scroll behavior
- Message filtering by type
- Clear console functionality

### 3. PDStudioResults.razor
**Files**: 
- `PDStudioResults.razor` - Results display markup
- `PDStudioResults.razor.cs` - Results logic  
- `PDStudioResults.razor.css` - Results styling

**Key Features**:
- iframe HTML content rendering: Secure HTML display using iframe
- Status Bar: Current execution status display (no progress percentage)  
- Real-time updates during code execution via IPDStudioService events
- Loading states and execution indicators
- Error handling
- Responsive design
- Integration with cancellation tokens

### 5. Standard Menu and Toolbar Configuration
**Menu Structure** (PDMenu):
- **File Menu**:
  - New, Open, Save, Save As
  - Recent Files
  - Export options
- **Logging Menu**:
  - Set Log Level: Debug, Info, Warning, Error
  - Clear Log
  - Export Log

**Toolbar Buttons** (PDToolbar):
- **Play Button** (▶️): Execute code - transforms to Cancel when running
- **Cancel Button** (⏹️): Stop execution - only visible when code is running
- **Hide/Show Console Button**: Toggle console panel visibility
- **Additional buttons**: To be defined later as needed

**Key Features**:
- Dynamic button states based on execution status
- Console visibility toggle functionality
- Menu-driven log level configuration
- Language support queriable via IPDStudioService
- Integration with IPDStudioService for execution control

## Implementation Structure

### Layout Configuration
```html
<PDStudio CssClass="studio-container"
          StudioService="@StudioService"
          Logger="@Logger"
          Options="@StudioOptions">
    <PDSplitter Direction="SplitDirection.Vertical" 
                Sizes="@GetMainSplitSizes()">
        
        <!-- Top Panel: Editor + Results -->
        <PDSplitPanel>
            <PDSplitter Direction="SplitDirection.Horizontal" 
                        Sizes="@StudioOptions.TopSplitSizes">
                
                <!-- Left Section: Menu + Toolbar + Monaco Editor -->
                <PDSplitPanel>
                    <PDMenuBar>
                        <PDMenuBarItem Text="File">
                            <PDMenuItem Text="New" Click="OnNewClick" />
                            <PDMenuItem Text="Open" Click="OnOpenClick" />
                            <PDMenuItem Text="Save" Click="OnSaveClick" />
                            <PDMenuItem Text="Save As" Click="OnSaveAsClick" />
                        </PDMenuBarItem>
                        <PDMenuBarItem Text="Logging">
                            <PDMenuItem Text="Debug" Click="@(() => SetLogLevel(LogLevel.Debug))" />
                            <PDMenuItem Text="Info" Click="@(() => SetLogLevel(LogLevel.Information))" />
                            <PDMenuItem Text="Warning" Click="@(() => SetLogLevel(LogLevel.Warning))" />
                            <PDMenuItem Text="Error" Click="@(() => SetLogLevel(LogLevel.Error))" />
                        </PDMenuBarItem>
                    </PDMenuBar>
                    
                    <PDToolbar Size="ButtonSizes.Small">
                        <PDToolbarButton Key="play" 
                                        Text="@(_isExecuting ? "Cancel" : "Play")"
                                        IconCssClass="@(_isExecuting ? "fas fa-stop" : "fas fa-play")"
                                        CssClass="@(_isExecuting ? "btn-danger" : "btn-success")"
                                        Click="OnPlayCancelClick" />
                        <PDToolbarSeparator />
                        <PDToolbarButton Key="console" 
                                        Text="@(StudioOptions.IsLoggingVisible ? "Hide Console" : "Show Console")" 
                                        IconCssClass="@(StudioOptions.IsLoggingVisible ? "fas fa-eye-slash" : "fas fa-eye")"
                                        Click="OnToggleConsoleClick" />
                    </PDToolbar>
                    
                    <PDMonacoEditor @ref="EditorRef" 
                                   Language="@StudioOptions.Language"
                                   Theme="@StudioOptions.Theme">
                        @if (_isExecuting && !StudioOptions.IsEditingEnabledDuringExecution)
                        {
                            <div class="monaco-editor-overlay">
                                <div class="spinner-border" role="status">
                                    <span class="visually-hidden">Executing...</span>
                                </div>
                            </div>
                        }
                    </PDMonacoEditor>
                </PDSplitPanel>
                
                <!-- Right Section: Results with Status Bar -->
                <PDSplitPanel>
                    <PDStudioResults @ref="ResultsRef" 
                                    Content="@ResultsContent"
                                    IsExecuting="@_isExecuting"
                                    ExecutionProgress="@_executionProgress"
                                    ExecutionStatus="@_executionStatus" />
                </PDSplitPanel>
                
            </PDSplitter>
        </PDSplitPanel>
        
        <!-- Bottom Panel: PDLog Component (Toggleable) -->
        @if (StudioOptions.IsLoggingVisible)
        {
            <PDSplitPanel>
                <PDLog @ref="LogRef" 
                       Logger="@Logger"
                       LogLevel="@StudioOptions.DefaultLogLevel"
                       ShowTimestamp="true"
                       ShowIcon="true"
                       ShowLogLevel="true"
                       WordWrap="true" />
            </PDSplitPanel>
        }
        
    </PDSplitter>
</PDStudio>
```

## Demo Page Implementation

### PDStudioDemo.razor
**Location**: `PanoramicData.Blazor.Demo/Pages/PDStudioDemo.razor`

**Features**:
- Demonstrate basic studio functionality
- Show HTML output generation
- Example console logging
- Multiple language support examples

## Configuration Properties

## Configuration Properties

### PDStudio Parameters
```csharp
[Parameter] public string CssClass { get; set; } = "";
[Parameter] public IPDStudioService? StudioService { get; set; }
[Parameter] public ILogger? Logger { get; set; }
[Parameter] public PDStudioOptions Options { get; set; } = new();
[Parameter] public EventCallback<string> OnCodeExecuted { get; set; }
[Parameter] public EventCallback<bool> OnExecutionStateChanged { get; set; }
[Parameter] public EventCallback<bool> OnLoggingVisibilityChanged { get; set; }
[Parameter] public RenderFragment? EditorToolbarContent { get; set; }
```

### PDStudioOptions Class
```csharp
public class PDStudioOptions
{
    public string Language { get; set; } = "html";
    public string Theme { get; set; } = "light";
    public double[] MainSplitSizes { get; set; } = new[] { 75.0, 25.0 }; // Top panel vs Log
    public double[] TopSplitSizes { get; set; } = new[] { 50.0, 50.0 }; // Editor vs Results (50/50)
    public bool IsLoggingVisible { get; set; } = true; // Debug/Log panel visibility at initialization
    public LogLevel DefaultLogLevel { get; set; } = LogLevel.Debug; // Default log level
    public bool ShowMenu { get; set; } = true; // Menu visibility
    public bool ShowToolbar { get; set; } = true; // Toolbar visibility
    public bool ShowStatusBar { get; set; } = true; // Status bar visibility
    public bool IsEditingEnabledDuringExecution { get; set; } = true; // Editor usability during execution
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}
```

### IPDStudioService Interface
```csharp
public interface IPDStudioService
{
    // Language Support
    IEnumerable<string> GetSupportedLanguages();
    string GetDefaultLanguage();
    bool IsLanguageSupported(string language);
    
    // Execution
    Task<string> ExecuteCodeAsync(string code, string language, CancellationToken cancellationToken = default);
    Task<string> ExecuteCodeAsync(string code, string language, IProgress<string>? resultsProgress = null, CancellationToken cancellationToken = default);
    
    // Events
    event EventHandler<StudioExecutionEventArgs>? ExecutionEvent;
    
    // Status
    bool IsExecuting { get; }
    string CurrentStatus { get; }
}

public class StudioExecutionEventArgs : EventArgs
{
    public StudioExecutionEventType EventType { get; set; }
    public string Output { get; set; } = "";
    public string Status { get; set; } = "";
    public double Progress { get; set; } // 0.0 to 1.0
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsComplete { get; set; }
}

public enum StudioExecutionEventType
{
    Started,
    UpdateOutput,
    OutputComplete,
    Progress,
    Log,
    Error,
    Cancelled,
    Completed
}

public enum SupportedLanguage
{
    PostgreSQL,
    MSSQL,
    NCalc,
    JSON,
    PlainText,
    HTML,
    JavaScript,
    CSS,
    XML
}
```

## Styling Approach

### CSS Classes Structure
```css
.pd-studio {
    /* Main studio container */
}

.pd-studio-editor {
    /* Monaco editor area */
}

.pd-studio-console {
    /* Console area styling */
}

.pd-studio-results {
    /* Results display area */
}

.pd-studio-editor-toolbar {
    /* Editor toolbar styling (positioned above Monaco Editor only) */
}
```

## Future Extensibility

### Abstract Base Design
The PDStudio component is designed to be abstract enough to support:

1. **SQL Studio**: 
   - Language: SQL
   - Results: Table/Grid display
   - Console: SQL execution logs

2. **Query Studio**:
   - Language: Custom query language
   - Results: JSON/Data visualization
   - Console: Query performance metrics

3. **Script Studio**:
   - Language: JavaScript/Python
   - Results: Script output
   - Console: Runtime logs

### Extension Points
- Custom result renderers
- Language-specific toolbars
- Studio-specific console message types
- Pluggable execution engines

## PDMenuBar Component Specification (Phase 0)

### Core Requirements
- **Horizontal Layout**: Traditional menu bar (File, Edit, View, etc.)
- **Multiple Menus**: Support multiple top-level menu items
- **Multi-level Sub-menus**: Nested menus (File → Recent Files → file1.txt)
- **Keyboard Shortcuts**: Full keyboard navigation and shortcut support
- **Accessibility**: ARIA labels, screen reader support
- **Themeable**: Consistent with existing PD component styling

### PDMenuBar Component Features
```csharp
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public string CssClass { get; set; } = "";
[Parameter] public bool IsEnabled { get; set; } = true;
[Parameter] public EventCallback<MenuBarItemClickEventArgs> ItemClick { get; set; }
[Parameter] public bool ShowKeyboardShortcuts { get; set; } = true;
[Parameter] public string Theme { get; set; } = "light";
```

### PDMenuBarItem Component Features
```csharp
[Parameter] public string Text { get; set; } = "";
[Parameter] public string Key { get; set; } = "";
[Parameter] public string IconCssClass { get; set; } = "";
[Parameter] public bool IsEnabled { get; set; } = true;
[Parameter] public bool IsVisible { get; set; } = true;
[Parameter] public bool IsSeparator { get; set; }
[Parameter] public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey(); // Use existing model
[Parameter] public RenderFragment? ChildContent { get; set; } // For sub-menus
[Parameter] public EventCallback<MenuBarItemClickEventArgs> Click { get; set; }
```

### Keyboard Shortcuts Support
**Uses Existing GlobalEventService and ShortcutKey Model**:
- **IGlobalEventService**: Register/unregister shortcuts globally  
- **ShortcutKey Model**: Existing model with Key, Code, Alt, Ctrl, Shift properties
- **Integration**: PDMenuBarItem uses existing ShortcutKey parameter
- **Display**: Shortcuts shown in menu (Ctrl+S, Alt+F, etc.)

```csharp
// Reuse existing ShortcutKey model from PanoramicData.Blazor.Models
[Parameter] public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

// Use existing IGlobalEventService for registration  
[Inject] private IGlobalEventService GlobalEventService { get; set; } = null!;
```

### Keyboard Navigation Features
- **Alt Key Activation**: Alt key activates menu bar, shows underlined shortcuts
- **Arrow Key Navigation**: Left/Right between menus, Up/Down within menus
- **Enter Key**: Activates selected menu item
- **Escape Key**: Closes open menus, deactivates menu bar
- **Letter Keys**: Quick access when menu is active (Alt+F opens File menu)
- **Shortcut Keys**: Global shortcuts (Ctrl+S, Ctrl+O) work when registered

### PDMenuBar Demo Page Features
The demo page will showcase:

1. **Basic Menu Structure**:
   ```html
   <PDMenuBar>
       <PDMenuBarItem Text="File" Key="F">
   <PDMenuBarItem Text="New" ShortcutKey="@(ShortcutKey.Create("ctrl-n"))" />
           <PDMenuBarItem Text="Load Example 1" Click="OnLoadExample1" />
           <PDMenuBarItem Text="Load Example 2" Click="OnLoadExample2" />
           <PDMenuBarItem IsSeparator="true" />
           <PDMenuBarItem Text="Open" ShortcutKey="@(new ShortcutKey { Ctrl = true, Key = "o" })" IsEnabled="false" />
           <PDMenuBarItem Text="Save" ShortcutKey="@(ShortcutKey.Create("ctrl-s"))" IsEnabled="false" />
       </PDMenuBarItem>
   </PDMenuBar>
   ```

2. **Multiple Menu Examples**:
   - **File Menu**: New, Load Example 1, Load Example 2, Open (disabled), Save (disabled)  
   - **Edit Menu**: Cut, Copy, Paste (all disabled for future implementation)
   - **View Menu**: Zoom In, Zoom Out, Full Screen (disabled for future)
   - **Help Menu**: Database Structure, Sample Queries, About

3. **Mock SQL Database Help**:
   - **Database Structure**: Documentation of mock tables and relationships
   - **Sample Queries**: Selectable working SQL examples
   - **Load Examples**: "Load Example 1" and "Load Example 2" working SQL queries

3. **Keyboard Shortcut Demonstrations**:
   - Visual shortcut display (Ctrl+S, Alt+F4, etc.)
   - Functional shortcut execution
   - Alt+key menu activation

4. **Accessibility Features**:
   - Tab navigation
   - Screen reader compatibility  
   - High contrast support

5. **Theming Examples**:
   - Light and dark theme support
   - Custom CSS styling examples

6. **Interactive Examples**:
   - Event handling demonstrations
   - Enabled/disabled state management
   - Dynamic menu updates

### JavaScript Integration (PDMenuBar.razor.js)
```javascript
// Keyboard navigation (Arrow keys, Enter, Escape)
// Alt key handling for menu activation
// Global shortcut integration with existing GlobalEventService
// Focus management and menu positioning
// Bootstrap dropdown integration
// Accessibility features (ARIA, screen readers)
```
## Questions for Clarification - ANSWERED ✅

1. **PDMenuBar Styling**: ✅ **ANSWERED**
   - **Bootstrap-based styling** with localized CSS in the style of existing components ✅

2. **Shortcut Key Registration**: ✅ **ANSWERED** 
   - **Reuse existing GlobalEventService** and ShortcutKey model ✅
   - Use IGlobalEventService.RegisterShortcutKey/UnregisterShortcutKey ✅

3. **File Operations**: ✅ **ANSWERED**
   - **File menu operations should not work** at this point - for future implementation ✅
   - **Mock File menu items**: "Load Example 1", "Load Example 2" for SQL demo ✅

4. **Language Detection**: ✅ **ANSWERED**  
   - **Language should be part of PDStudioOptions** ✅
   - **Plan for future**: Language can be changed when files are loaded from filesystem ✅

5. **Mock SQL Engine**: ✅ **ANSWERED**
   - **Basic SELECT only** for initial implementation ✅
   - **Simple mock database** with two joined tables ✅
   - **Demo help section** with database structure and working SQL queries ✅

6. **Event Subscription**: ✅ **ANSWERED**
   - **Components should auto-subscribe** to service events ✅

7. **Status Bar Content**: ✅ **ANSWERED**
   - **Current status only** - no progress percentage ✅

8. **Editor Read-Only Visual Indicator**: ✅ **ANSWERED**
   - **Simple spinner overlay** over PDMonaco that blocks user interaction ✅

## Implementation Priority

### Phase 0 (PDMenuBar Foundation) - **NEW PREREQUISITE**
1. **PDMenuBar component** with horizontal menu bar layout
2. **PDMenuBarItem component** for menu items with dropdown support
3. **Keyboard shortcuts support** (Alt+key navigation, arrow keys, Enter, Escape)
4. **Multi-level sub-menu support** (nested menus)
5. **PDMenuBar demo page** with comprehensive examples
6. **Menu styling and accessibility** features

### Phase 1 (MVP)
1. **IPDStudioService interface** and basic demo implementation
2. PDStudio main component with service injection
3. Standard toolbar with Play/Cancel functionality
4. PDStudioResults with real-time updates
5. PDLog integration for streaming logs
6. Basic demo page with service implementation

### Phase 2 (Enhancement)
1. Advanced PDLog features (filtering, log levels)
2. Enhanced execution state management
3. Advanced result display options
4. State persistence and auto-save

### Phase 3 (Extensibility)
1. **Abstract studio interfaces** for different studio types
2. **Plugin architecture** for custom execution engines
3. Advanced result renderers and visualizations
4. **Multi-language studio support**

## Files to Create

### Core Interface
1. `PanoramicData.Blazor/Interfaces/IPDStudioService.cs` - Main service interface

### New Menu Components (Phase 0 - Foundation)
2. `PanoramicData.Blazor/PDMenuBar.razor` - Horizontal menu bar component
3. `PanoramicData.Blazor/PDMenuBar.razor.cs`
4. `PanoramicData.Blazor/PDMenuBar.razor.css` - Bootstrap + localized CSS
5. `PanoramicData.Blazor/PDMenuBar.razor.js` - Keyboard navigation and shortcuts
6. `PanoramicData.Blazor/PDMenuBarItem.razor` - Menu bar item component  
7. `PanoramicData.Blazor/PDMenuBarItem.razor.cs`
8. `PanoramicData.Blazor/PDMenuBarItem.razor.css`

**Note**: Uses existing ShortcutKey model and IGlobalEventService - no new shortcut classes needed.

### Menu Demo (Phase 0)
9. `PanoramicData.Blazor.Demo/Pages/PDMenuBarDemo.razor` - Comprehensive menu demo
10. `PanoramicData.Blazor.Demo/Pages/PDMenuBarDemo.razor.cs`

### Core Components (Phase 1)
11. `PanoramicData.Blazor/PDStudio.razor`
12. `PanoramicData.Blazor/PDStudio.razor.cs`
13. `PanoramicData.Blazor/PDStudio.razor.css`
14. `PanoramicData.Blazor/PDStudioResults.razor`
15. `PanoramicData.Blazor/PDStudioResults.razor.cs`
16. `PanoramicData.Blazor/PDStudioResults.razor.css`

### Supporting Models/Classes (Phase 1)
17. `PanoramicData.Blazor/Models/PDStudioOptions.cs`
18. `PanoramicData.Blazor/Models/StudioExecutionEventArgs.cs`
19. `PanoramicData.Blazor/Models/SupportedLanguage.cs`
20. `PanoramicData.Blazor/Services/DefaultPDStudioService.cs` - Base implementation

### Studio Demo Implementation (Phase 1)  
21. `PanoramicData.Blazor.Demo/Pages/PDStudioDemo.razor`
22. `PanoramicData.Blazor.Demo/Pages/PDStudioDemo.razor.cs`
23. `PanoramicData.Blazor.Demo/Services/MockSQLStudioService.cs` - Basic SELECT simulation
24. `PanoramicData.Blazor.Demo/Services/DemoStudioService.cs` - Multi-language demo

**Note**: **Phase 0** introduces the new **PDMenuBar** component with full menu functionality that PDStudio will depend on.

---

**Ready for Review**: This plan provides a comprehensive roadmap for implementing the PD Studio component. Please review and provide feedback before proceeding with implementation.