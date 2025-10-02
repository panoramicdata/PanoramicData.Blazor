# PanoramicData.Blazor Component Documentation

This document provides an overview of the Blazor components in this project.

## PDAnimation

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AnimationTime | double |  |
| Transition | AnimationTransition | The type of transition to apply to the animation. |

---

## PDAudioButton

This component has no public parameters.

---

## PDAudioPad

This component has no public parameters.

---

## PDBlockOverlay

This component has no public parameters.

---

## PDBusyOverlay

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CssClass | string |  |
| IsBusy | bool |  |
| ChildContent | RenderFragment? |  |
| OverlayCssClass | string |  |
| OverlayContent | RenderFragment? |  |

---

## PDButton

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Custom content to display instead of the standard text and icon. |
| MouseDown | EventCallback<MouseEventArgs> |  |
| MouseEnter | EventCallback<MouseEventArgs> |  |
| OperationIconCssClass | string | Async function to be called when button is clicked. |
| PreventDefault | bool |  |
| StopPropagation | bool |  |

---

## PDCanvas

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Height | int |  |
| Id | string |  |
| Width | int |  |

---

## PDCard

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| IsAnimated | bool | Whether this card is animated when it is rendered or not. |
| DraggingEnabled | bool | Whether Dragging is enabled for this card. |
| Template | RenderFragment<TCard>? | The Template to render for the card. |

---

## PDCardDeck

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DataFunction | Func<Task<DataResponse<TCard>>> | Unique identifier for this Card Deck. If not set, a unique ID will be generated. |
| IsAnimated | bool | Whether the deck has animations enabled or not. Defaults to false. |
| CardTemplate | RenderFragment<TCard>? | Template for rendering each individual Card within this Deck |
| DeckTemplate | RenderFragment<PDCardDeck<TCard>>? | Template for rendering this Deck |
| MultipleSelection | bool | Global CSS Class to outline the styling of each Card |

---

## PDCardDeckGroup

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DataProvider | IDataProviderService<TCard> | Unique identifier for this card deck group. |
| ChildContent | RenderFragment? |  |

---

## PDCardDeckLoadingIcon

This component has no public parameters.

---

## PDChat

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Container | PDChatContainer? |  |
| ChatDockPosition | PDChatDockPosition |  |
| CollapsedIcon | string |  |
| OnChatMinimized | EventCallback |  |
| OnChatRestored | EventCallback |  |
| OnChatMaximized | EventCallback |  |
| OnMuteToggled | EventCallback |  |
| OnChatCleared | EventCallback |  |
| OnMessageSent | EventCallback<ChatMessage> |  |
| OnMessageReceivedEvent | EventCallback<ChatMessage> |  |
| OnAutoRestored | EventCallback |  |

---

## PDChatContainer

This component has no public parameters.

---

## PDClickableImage

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ImageSource | string |  |
| Alt | string |  |
| Title | string |  |
| CssStyles | string |  |

---

## PDClipboard

This component has no public parameters.

---

## PDColumn

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| FilterIcon | string |  |
| FilterKey | string |  |
| FilterOptions | FilterOptions |  |
| FilterShowSuggestedValues | bool |  |
| FilterSuggestedValues | IEnumerable<object> |  |
| FilterMaxValues | int? |  |
| Title | string? | If set will override the FieldExpression's name |

---

## PDComboBox

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| SelectedItem | TItem? |  |
| Placeholder | string |  |
| MaxResults | int |  |
| IsDisabled | bool |  |
| IsReadOnly | bool |  |
| NoResultsText | string |  |
| ItemTemplate | RenderFragment<TItem>? |  |
| NoResultsTemplate | RenderFragment<string>? |  |
| ShowSelectedItemOnTop | bool |  |

---

## PDConfirm

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CancelText | string | Gets the text displayed on the Cancel button. |
| ChildContent | RenderFragment? | Sets the content displayed in the modal dialog body. |
| Message | string | Gets the message to be displayed if the ChildContent not supplied. |
| NoText | string | Gets the text displayed on the No button. |
| ShowCancel | bool | Gets whether to show the Cancel button? |
| YesText | string | Gets the text displayed on the Yes button. |

---

## PDContextMenu

This component has no public parameters.

---

## PDDateTime

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Blur | EventCallback |  |
| ShowTime | bool |  |
| TimeStepSecs | int |  |
| Value | DateTime |  |
| ValueChanged | EventCallback<DateTime> |  |

---

## PDDateTimeOffset

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Blur | EventCallback |  |
| ShowOffset | bool |  |
| ShowTime | bool |  |
| TimeStepSecs | int |  |
| Value | DateTimeOffset |  |
| ValueChanged | EventCallback<DateTimeOffset> |  |

---

## PDDragContainer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment |  |
| Items | IEnumerable<TItem> |  |
| SelectionChanged | EventCallback<IEnumerable<TItem>> |  |

---

## PDDragContext

This component has no public parameters.

---

## PDDragDropSeparator

This component has no public parameters.

---

## PDDragPanel

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CanChangeOrder | bool |  |
| CanDrag | bool |  |
| Id | string |  |
| ItemOrderChanged | EventCallback<DragOrderChangeArgs<TItem>> |  |
| Template | RenderFragment<TItem>? |  |
| PlaceholderTemplate | RenderFragment<TItem>? |  |

---

## PDDropDown

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Click | EventCallback<MouseEventArgs> |  |
| ChildContent | RenderFragment? |  |
| CloseOption | CloseOptions |  |
| CssClass | string |  |
| DropdownDirection | Directions |  |
| DropDownHidden | EventCallback |  |
| DropDownShown | EventCallback |  |
| IsEnabled | bool |  |
| IconCssClass | string |  |
| Id | string |  |
| KeyPress | EventCallback<int> |  |
| PreventDefault | bool |  |
| ShowCaret | bool |  |
| ShowOnMouseEnter | bool |  |
| Size | ButtonSizes |  |
| StopPropagation | bool |  |
| Text | string |  |
| TextCssClass | string |  |
| ToolTip | string |  |
| Visible | bool |  |

---

## PDDropZone

This component has no public parameters.

---

## PDFader

This component has no public parameters.

---

## PDField

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Title | string? | If set will override the Field's name |
| Group | string | Gets or sets a function that returns the description for the field. |

---

## PDFileExplorer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ColumnConfig | List<PDColumnConfig> | Sets the Table column configuration. |
| TableContextItems | List<MenuItem> | Sets the Table context menu items. |
| ToolbarItems | List<ToolbarItem> | Sets the Table context menu items. |

---

## PDFileModal

This component has no public parameters.

---

## PDFilePreview

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ExceptionHandler | EventCallback<Exception> |  |
| Item | FileExplorerItem? |  |
| PreviewProvider | IPreviewProvider |  |

---

## PDFilter

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CssClass | string |  |
| Filter | Filter |  |
| FilterChanged | EventCallback<Filter> |  |
| IconCssClass | string |  |
| DataType | FilterDataTypes |  |
| Nullable | bool |  |
| Options | FilterOptions |  |
| ShowValues | bool |  |
| Size | ButtonSizes |  |

---

## PDFlag

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Width | string |  |

---

## PDForm

This component has no public parameters.

---

## PDFormBody

This component has no public parameters.

---

## PDFormCheckBox

This component has no public parameters.

---

## PDFormFieldEditor

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DebounceWait | int |  |
| Field | FormField<TItem> |  |
| Form | PDForm<TItem> |  |
| Id | string |  |

---

## PDFormFooter

This component has no public parameters.

---

## PDFormHeader

This component has no public parameters.

---

## PDGlobalListener

This component has no public parameters.

---

## PDGraph

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string | Gets or sets the unique identifier for this component. |
| CssClass | string | Gets or sets the CSS class for styling. |
| IsVisible | bool | Gets or sets whether the component is visible. |
| DataProvider | IDataProviderService<GraphData>? | Gets or sets the data provider for the graph data. |
| VisualizationConfig | GraphVisualizationConfig | Gets or sets the visualization configuration. |
| ClusteringConfig | GraphClusteringConfig | Gets or sets the clustering configuration. |
| ConvergenceThreshold | double | Gets or sets the convergence threshold for the physics simulation. Lower values make physics run longer. |
| Damping | double | Gets or sets the damping factor for the physics simulation. Higher values mean faster settling. |
| NodeClick | EventCallback<GraphNode> | Gets or sets a callback that is invoked when a node is clicked. |
| EdgeClick | EventCallback<GraphEdge> | Gets or sets a callback that is invoked when an edge is clicked. |
| Size | double | Gets or sets a callback that is invoked when the selection changes. |

---

## PDGraphControls

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| IsReadOnly | bool | Gets or sets whether the controls are read-only. |
| VisualizationConfig | GraphVisualizationConfig | Gets or sets the visualization configuration. |
| ClusteringConfig | GraphClusteringConfig | Gets or sets the clustering configuration. |
| AvailableDimensions | List<string> | Gets or sets the available dimension names for mapping. |
| Damping | double | Gets or sets the damping factor for the physics simulation. |

---

## PDGraphInfo

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| SplitDirection | SplitDirection | Gets or sets the split direction for the controls and selection info panels. |
| ShowControls | bool | Gets or sets whether to show the controls panel. |
| ReadOnlyControls | bool | Gets or sets whether the controls are read-only. |
| VisualizationConfig | GraphVisualizationConfig | Gets or sets the visualization configuration. |
| ClusteringConfig | GraphClusteringConfig | Gets or sets the clustering configuration. |
| SelectedNode | GraphNode? | Gets or sets the currently selected node. |
| SelectedEdge | GraphEdge? | Gets or sets the currently selected edge. |

---

## PDGraphSelectionInfo

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| SelectedNode | GraphNode? | Gets or sets the currently selected node. |
| SelectedEdge | GraphEdge? | Gets or sets the currently selected edge. |

---

## PDGraphViewer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DataProvider | IDataProviderService<GraphData>? | Gets or sets the data provider for the graph data. |
| SplitDirection | SplitDirection | Gets or sets the split panel direction (Horizontal or Vertical). |
| ShowInfo | bool | Gets or sets whether to show the information panel. |
| ShowControls | bool | Gets or sets whether to show the controls panel within the info panel. |
| ReadOnlyControls | bool | Gets or sets whether the controls are read-only. |
| VisualizationConfig | GraphVisualizationConfig | Gets or sets the visualization configuration for mapping dimensions to visual properties. |
| ClusteringConfig | GraphClusteringConfig | Gets or sets the clustering configuration. |
| ConvergenceThreshold | double | Gets or sets the convergence threshold for the physics simulation. |
| Damping | double | Gets or sets the damping factor for the physics simulation. Higher values mean faster settling. |
| NodeClick | EventCallback<GraphNode> | Gets or sets a callback that is invoked when a node is clicked. |
| EdgeClick | EventCallback<GraphEdge> | Gets or sets a callback that is invoked when an edge is clicked. |

---

## PDImage

This component has no public parameters.

---

## PDKnob

This component has no public parameters.

---

## PDLabel

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Click | EventCallback<MouseEventArgs> |  |
| ChildContent | RenderFragment? |  |
| CssClass | string |  |
| DataItem | object? |  |
| IconCssClass | string |  |
| MouseDown | EventCallback<MouseEventArgs> |  |
| MouseEnter | EventCallback<MouseEventArgs> |  |
| PreventDefault | bool |  |
| SelectedChanged | EventCallback<ISelectable> |  |
| StopPropagation | bool |  |
| Text | string |  |
| TextCssClass | string |  |
| ToolTip | string |  |

---

## PDLinkButton

This component has no public parameters.

---

## PDList

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AllCheckBoxWhenPartial | SelectionBehaviours |  |
| Apply | EventCallback<Selection<TItem>> |  |
| Cancel | EventCallback |  |
| ClearSelectionOnFilter | bool |  |
| DataProvider | IDataProviderService<TItem>? |  |
| DefaultToSelectAll | bool |  |
| ItemTemplate | RenderFragment<TItem>? |  |
| Selection | Selection<TItem> |  |
| SelectionChanged | EventCallback<Selection<TItem>> |  |
| SelectionMode | TableSelectionMode |  |
| ShowAllCheckBox | bool |  |
| ShowApplyCancelButtons | bool |  |
| ShowCheckBoxes | bool |  |
| ShowFilter | bool |  |
| SortDirection | SortDirection |  |

---

## PDLocalStorageStateManager

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? |  |

---

## PDLog

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| LogLevel | LogLevel |  |
| Capacity | int |  |
| Rows | int |  |
| ShowTimestamp | bool |  |
| ShowIcon | bool |  |
| ShowException | bool |  |
| UtcTimestampFormat | string |  |

---

## PDMenuItem

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Key | string |  |
| Text | string |  |
| IsVisible | bool |  |
| IsDisabled | bool |  |
| IconCssClass | string |  |
| Content | string |  |
| IsSeparator | bool |  |
| ShortcutKey | ShortcutKey |  |

---

## PDMessage

This component has no public parameters.

---

## PDMessages

This component has no public parameters.

---

## PDModal

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Buttons | List<ToolbarItem> | Sets the buttons displayed in the modal dialog footer. |

---

## PDMonacoEditor

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string |  |
| Language | string |  |
| ShowSuggestions | bool |  |
| Theme | string |  |
| Value | string |  |
| ValueChanged | EventCallback<string> |  |
| UpdateValueOnBlur | bool |  |
| InitializeCache | Action<MethodCache>? |  |
| InitializeOptions | Action<StandaloneEditorConstructionOptions>? |  |
| InitializeLanguage | Action<Language>? |  |
| RegisterLanguages | Action<List<Language>>? |  |
| SelectionChanged | EventCallback<Selection> |  |

---

## PDNavLink

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ActiveClass | string? | Gets or sets the CSS class name applied to the NavLink when the current route matches the NavLink href. |
| ChildContent | RenderFragment? |  |
| Match | NavLinkMatch | Gets or sets a value representing the URL matching behavior. |

---

## PDPager

This component has no public parameters.

---

## PDProgressBar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| BarContent | RenderFragment<PDProgressBar>? |  |
| DecimalPlaces | ushort |  |
| Height | string |  |
| Total | double |  |
| Value | double |  |

---

## PDQuestVisualizer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Quests | List<Quest> |  |
| QuestActions | List<QuestAction> |  |
| QuestHeight | int |  |
| QuestMargin | int |  |
| QuestActionRadius | int |  |

---

## PDRange

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Height | double |  |
| Invert | bool |  |
| Options | RangeOptions |  |
| Range | NumericRange |  |
| ShowLabels | bool |  |
| TickMajor | double |  |
| Max | double |  |
| Min | double |  |
| MinGap | double |  |
| RangeChanged | EventCallback<NumericRange> |  |
| Step | double |  |
| TrackHeight | double |  |
| Width | double |  |

---

## PDResizePane

No code-behind file found for this component.

---

## PDSplitPanel

This component has no public parameters.

---

## PDSplitter

This component has no public parameters.

---

## PDStackedBar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DateFormat | string |  |
| DataPoint | DataPoint |  |
| Height | double |  |
| IsEnabled | bool |  |
| MaxValue | double |  |
| Options | TimelineOptions |  |
| X | double |  |

---

## PDStudio

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DataProvider | IDataProviderService<GraphData>? | Gets or sets the data provider for the graph data. |

---

## PDStudioResults

This component has no public parameters.

---

## PDTab

This component has no public parameters.

---

## PDTable

This component has no public parameters.

---

## PDTableColumnSelector

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Table | PDTable<TItem>? |  |
| CanChangeOrder | bool |  |
| CanChangeVisible | bool |  |

---

## PDTabSet

This component has no public parameters.

---

## PDTextArea

This component has no public parameters.

---

## PDTextBox

This component has no public parameters.

---

## PDTimeline

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DisableAfter | DateTime |  |
| DisableBefore | DateTime |  |
| Initialized | EventCallback |  |
| IsEnabled | bool |  |
| Scale | TimelineScale |  |
| ScaleChanged | EventCallback<TimelineScale> |  |
| Refreshed | EventCallback |  |
| SelectionChanged | EventCallback<TimeRange?> |  |
| SelectionChangeEnd | EventCallback |  |
| DataProvider | DataProviderDelegate? |  |
| Id | string |  |
| NewMaxDateTimeAvailable | bool |  |
| NewMinDateTimeAvailable | bool |  |
| MaxDateTime | DateTime? |  |
| MinDateTime | DateTime |  |
| Options | TimelineOptions |  |
| UpdateMaxDate | EventCallback |  |
| UpdateMinDate | EventCallback |  |
| OffsetX | int |  |

---

## PDTimelineToolbar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| IsEnabled | bool |  |
| ShowRange | bool |  |
| ShowScale | bool |  |
| ShowSelection | bool |  |
| ShowZoomButtons | bool |  |
| Timeline | PDTimeline? |  |

---

## PDToggleSwitch

This component has no public parameters.

---

## PDToolbar

This component has no public parameters.

---

## PDToolbarButton

This component has no public parameters.

---

## PDToolbarDropdown

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CloseOption | CloseOptions |  |
| ShowOnMouseEnter | bool |  |

---

## PDToolbarItem

This component has no public parameters.

---

## PDToolbarLinkButton

This component has no public parameters.

---

## PDToolbarPlaceholder

This component has no public parameters.

---

## PDToolbarSeparator

This component has no public parameters.

---

## PDToolbarTextbox

This component has no public parameters.

---

## PDTree

This component has no public parameters.

---

## PDTreeNode

This component has no public parameters.

---

## PDValidationSummary

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Errors | object? |  |

---

## PDZoomBar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string |  |
| Options | ZoomBarOptions |  |
| Value | ZoombarValue |  |
| ValueChanged | EventCallback<ZoombarValue> |  |
| Width | int |  |

---

