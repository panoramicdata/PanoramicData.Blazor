# PanoramicData.Blazor Component Documentation

This document provides an overview of the Blazor components in this project.

Generated on: 2025-10-15 15:44:39

## PDAnimation

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AnimationTime | double | Gets or sets the unique identifier for the animation. |
| Transition | AnimationTransition | The type of transition to apply to the animation. |

---

## PDAudioButton

This component has no public parameters.

---

## PDAudioChannel

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Label | string | Gets or sets the label for the channel (displayed on the fader). |
| IsEnabled | bool | Gets or sets whether the channel is enabled. |

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
| CssClass | string | Gets or sets the CSS class for the component. |
| IsBusy | bool | Gets or sets whether the busy overlay is active. |
| ChildContent | RenderFragment? | Gets or sets the child content of the component. |
| OverlayCssClass | string | Gets or sets the CSS class for the overlay. |
| OverlayContent | RenderFragment? | Gets or sets the content to be displayed in the overlay. |

---

## PDButton

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Custom content to display instead of the standard text and icon. |
| MouseDown | EventCallback<MouseEventArgs> | An event callback that is invoked when the mouse button is pressed down on the button. |
| MouseEnter | EventCallback<MouseEventArgs> | An event callback that is invoked when the mouse pointer enters the button. |
| OperationIconCssClass | string | Async function to be called when button is clicked. |
| PreventDefault | bool | Gets or sets whether to prevent the default action of the event. |
| StopPropagation | bool | Gets or sets whether to stop the event from propagating further. |

---

## PDCanvas

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Height | int | Gets or sets the height of the canvas. |
| Id | string | Gets or sets the unique identifier for the canvas. |
| Width | int | Gets or sets the width of the canvas. |

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
| ChatDockPosition | PDChatDockPosition | Gets or sets the dock position of the chat window. |
| CollapsedIcon | string | Gets or sets the icon to display when the chat window is collapsed. |
| OnChatMinimized | EventCallback | A function to select a user icon for a given message. |
| OnChatRestored | EventCallback | An event callback that is invoked when the chat window is restored. |
| OnChatMaximized | EventCallback | An event callback that is invoked when the chat window is maximized. |
| OnMuteToggled | EventCallback | An event callback that is invoked when the mute setting is toggled. |
| OnChatCleared | EventCallback | An event callback that is invoked when the chat is cleared. |
| OnMessageSent | EventCallback<ChatMessage> | An event callback that is invoked when a message is sent. |
| OnMessageReceivedEvent | EventCallback<ChatMessage> | An event callback that is invoked when a message is received. |
| OnAutoRestored | EventCallback | An event callback that is invoked when the chat window is automatically restored. |

---

## PDChatContainer

This component has no public parameters.

---

## PDClickableImage

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ImageSource | string | Gets or sets the source URL of the image. |
| Alt | string | Gets or sets the alternate text for the image. |
| Title | string | Gets or sets the title of the image. |
| CssStyles | string | Gets or sets the CSS styles for the image. |

---

## PDClipboard

This component has no public parameters.

---

## PDColumn

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| FilterIcon | string | Gets or sets the CSS class for the filter icon. |
| FilterKey | string | Gets or sets the key to use for filtering. |
| FilterOptions | FilterOptions | Gets or sets the options for the filter. |
| FilterShowSuggestedValues | bool | Gets or sets whether to show suggested values in the filter. |
| FilterSuggestedValues | IEnumerable<object> | Gets or sets the suggested values for the filter. |
| FilterMaxValues | int? | Gets or sets the maximum number of values to show in the filter. |
| Title | string? | If set will override the FieldExpression's name |

---

## PDComboBox

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| SelectedItem | TItem? | Gets or sets the list of items to be displayed in the combo box. |
| Placeholder | string | Gets or sets the placeholder text for the input. |
| MaxResults | int | A function to specify the sort order for the items. |
| IsDisabled | bool | Gets or sets whether the combo box is disabled. |
| IsReadOnly | bool | Gets or sets whether the combo box is read-only. |
| NoResultsText | string | Gets or sets the text to display when no results are found. |
| ItemTemplate | RenderFragment<TItem>? | A template for rendering each item in the dropdown. |
| NoResultsTemplate | RenderFragment<string>? | A template to display when no results are found. |
| ShowSelectedItemOnTop | bool | Gets or sets whether to show the selected item at the top of the filtered list. |

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
| Blur | EventCallback | An event callback that is invoked when the component loses focus. |
| ShowTime | bool | Gets or sets whether to show the time part of the value. |
| TimeStepSecs | int | Gets or sets the step in seconds for the time input. |
| Value | DateTime | Gets or sets the current value. |
| ValueChanged | EventCallback<DateTime> | An event callback that is invoked when the value changes. |

---

## PDDateTimeOffset

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Blur | EventCallback | An event callback that is invoked when the component loses focus. |
| ShowOffset | bool | Gets or sets whether to show the offset from UTC. |
| ShowTime | bool | Gets or sets whether to show the time part of the value. |
| TimeStepSecs | int | Gets or sets the step in seconds for the time input. |
| Value | DateTimeOffset | Gets or sets the current value. |
| ValueChanged | EventCallback<DateTimeOffset> | An event callback that is invoked when the value changes. |

---

## PDDragContainer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment | Gets or sets the child content of the component. |
| Items | IEnumerable<TItem> | Gets or sets the collection of items in the container. |
| SelectionChanged | EventCallback<IEnumerable<TItem>> | An event callback that is invoked when the selection changes. |

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
| CanChangeOrder | bool | Gets or sets whether the order of items can be changed. |
| CanDrag | bool | Gets or sets whether items can be dragged. |
| Id | string | Gets or sets the unique identifier for the panel. |
| ItemOrderChanged | EventCallback<DragOrderChangeArgs<TItem>> | An event callback that is invoked when the order of items changes. |
| Template | RenderFragment<TItem>? | A template for rendering each item. |
| PlaceholderTemplate | RenderFragment<TItem>? | A template for rendering the placeholder when an item is being dragged. |

---

## PDDropDown

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Click | EventCallback<MouseEventArgs> | An event callback that is invoked when the dropdown is clicked. |
| ChildContent | RenderFragment? | Gets or sets the child content of the dropdown. |
| CloseOption | CloseOptions | Gets or sets when the dropdown should close. |
| CssClass | string | Gets or sets the CSS class for the dropdown. |
| DropdownDirection | Directions | Gets or sets the direction the dropdown will open. |
| DropDownHidden | EventCallback | An event callback that is invoked when the dropdown is hidden. |
| DropDownShown | EventCallback | An event callback that is invoked when the dropdown is shown. |
| IsEnabled | bool | Gets or sets whether the dropdown is enabled. |
| IconCssClass | string | Gets or sets the CSS class for the icon. |
| Id | string | Gets or sets the unique identifier for the dropdown. |
| KeyPress | EventCallback<int> | An event callback that is invoked when a key is pressed. |
| PreventDefault | bool | Gets or sets whether to prevent the default action of the event. |
| ShowCaret | bool | Gets or sets whether to show the caret. |
| ShowOnMouseEnter | bool | Gets or sets whether to show the dropdown on mouse enter. |
| Size | ButtonSizes | Gets or sets the size of the dropdown button. |
| StopPropagation | bool | Gets or sets whether to stop the event from propagating further. |
| Text | string | Gets or sets the text to be displayed on the dropdown button. |
| TextCssClass | string | Gets or sets the CSS class for the text. |
| ToolTip | string | Gets or sets the tooltip for the dropdown. |
| Visible | bool | Gets or sets whether the dropdown is visible. |

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
| ExceptionHandler | EventCallback<Exception> | An event callback that is invoked when an exception occurs. |
| Item | FileExplorerItem? | Gets or sets the file item to be previewed. |
| PreviewProvider | IPreviewProvider | Gets or sets the preview provider for the file. |

---

## PDFilter

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CssClass | string | Gets or sets the CSS class for the component. |
| Filter | Filter | Gets or sets the filter object. |
| FilterChanged | EventCallback<Filter> | An event callback that is invoked when the filter changes. |
| IconCssClass | string | A function to fetch the values for the filter. |
| DataType | FilterDataTypes | Gets or sets the data type for the filter. |
| Nullable | bool | Gets or sets whether the value can be null. |
| Options | FilterOptions | Gets or sets the filter options. |
| ShowValues | bool | Gets or sets whether to show the values for the filter. |
| Size | ButtonSizes | Gets or sets the size of the filter button. |

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
| DebounceWait | int | Gets or sets the debounce wait period in milliseconds for value changes. |
| Field | FormField<TItem> |  |
| Form | PDForm<TItem> |  |
| Id | string | Gets or sets the unique identifier for the editor. |

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
| Click | EventCallback<MouseEventArgs> | An event callback that is invoked when the label is clicked. |
| ChildContent | RenderFragment? | Gets or sets the child content of the label. |
| CssClass | string | Gets or sets the CSS class for the label. |
| DataItem | object? | Gets or sets the data item associated with the label. |
| IconCssClass | string | Gets or sets the CSS class for the icon. |
| MouseDown | EventCallback<MouseEventArgs> | An event callback that is invoked when the mouse button is pressed down on the label. |
| MouseEnter | EventCallback<MouseEventArgs> | An event callback that is invoked when the mouse pointer enters the label. |
| PreventDefault | bool | Gets or sets whether to prevent the default action of the event. |
| SelectedChanged | EventCallback<ISelectable> | An event callback that is invoked when the selection state of the data item changes. |
| StopPropagation | bool | Gets or sets whether to stop the event from propagating further. |
| Text | string | Gets or sets the text to be displayed on the label. |
| TextCssClass | string | Gets or sets the CSS class for the text. |
| ToolTip | string | Gets or sets the tooltip for the label. |

---

## PDLinkButton

This component has no public parameters.

---

## PDList

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AllCheckBoxWhenPartial | SelectionBehaviours | Determines the behavior of the 'All' checkbox when the selection is partial. |
| Apply | EventCallback<Selection<TItem>> | An event callback that is invoked when the 'Apply' button is clicked. |
| Cancel | EventCallback | An event callback that is invoked when the 'Cancel' button is clicked. |
| ClearSelectionOnFilter | bool | Gets or sets whether to clear the selection when the filter text changes. |
| DataProvider | IDataProviderService<TItem>? | Gets or sets the data provider service for the list. |
| DefaultToSelectAll | bool | Gets or sets whether to select all items by default. |
| ItemTemplate | RenderFragment<TItem>? | A function to determine whether an item should be included in the filtered list. |
| Selection | Selection<TItem> | Gets or sets the current selection. |
| SelectionChanged | EventCallback<Selection<TItem>> | An event callback that is invoked when the selection changes. |
| SelectionMode | TableSelectionMode | Gets or sets the selection mode for the list. |
| ShowAllCheckBox | bool | Gets or sets whether to show the 'All' checkbox. |
| ShowApplyCancelButtons | bool | Gets or sets whether to show the 'Apply' and 'Cancel' buttons. |
| ShowCheckBoxes | bool | Gets or sets whether to show checkboxes for each item. |
| ShowFilter | bool | Gets or sets whether to show the filter input. |
| SortDirection | SortDirection | Gets or sets the sort direction for the list. |

---

## PDLocalStorageStateManager

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the child content of the component. |

---

## PDLog

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| LogLevel | LogLevel | Gets or sets the minimum log level to display. |
| Capacity | int | Gets or sets the maximum number of log entries to keep. |
| Rows | int | Gets or sets the number of rows to display. |
| ShowTimestamp | bool | Gets or sets whether to show the timestamp for each log entry. |
| ShowIcon | bool | Gets or sets whether to show the icon for each log entry. |
| ShowException | bool | Gets or sets whether to show the exception for each log entry. |
| UtcTimestampFormat | string | Gets or sets the format for the UTC timestamp. |

---

## PDMenuItem

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Key | string | Gets or sets the unique identifier of the menu item. |
| Text | string | Gets or sets the text to display on the menu item. |
| IsVisible | bool | Gets or sets whether this item is displayed. |
| IsDisabled | bool | Gets or sets whether this item is displayed but disabled. |
| IconCssClass | string | Gets or sets CSS classes to display an icon for the menu item. |
| Content | string | Gets or sets custom markup to be displayed for the item. |
| IsSeparator | bool | Gets or sets whether this item is rendered as a separator. |
| ShortcutKey | ShortcutKey | Sets the short cut keys that will perform a click on this button. |

---

## PDMessage

This component has no public parameters.

---

## PDMessages

This component has no public parameters.

---

## PDMixingDesk

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the child content (typically PDAudioChannel components). |
| CssClass | string | Gets or sets additional CSS classes. |
| MinHeight | string | Gets or sets the minimum height of the mixing desk. |

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
| Id | string | Gets or sets the unique identifier for the editor. |
| Language | string | Gets or sets the programming language for the editor. |
| ShowSuggestions | bool | Gets or sets whether to show code suggestions. |
| Theme | string | Gets or sets the theme for the editor. |
| Value | string | Gets or sets the content of the editor. |
| ValueChanged | EventCallback<string> | An event callback that is invoked when the content of the editor changes. |
| UpdateValueOnBlur | bool | Gets or sets whether the Value parameter is updated only when the editor loses focus. |
| InitializeCache | Action<MethodCache>? | An action to initialize the method cache for language completions. |
| InitializeOptions | Action<StandaloneEditorConstructionOptions>? | An async function to initialize the method cache for language completions. |
| InitializeLanguage | Action<Language>? | An action to initialize a custom language. |
| RegisterLanguages | Action<List<Language>>? | An async function to initialize a custom language. |
| SelectionChanged | EventCallback<Selection> | An async function to update the method cache. |

---

## PDNavLink

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ActiveClass | string? | Gets or sets the CSS class name applied to the NavLink when the current route matches the NavLink href. |
| ChildContent | RenderFragment? | Gets or sets the child content of the component. |
| Match | NavLinkMatch | Gets or sets a value representing the URL matching behavior. |

---

## PDPager

This component has no public parameters.

---

## PDProgressBar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| BarContent | RenderFragment<PDProgressBar>? | Gets or sets the content to be displayed within the progress bar. |
| DecimalPlaces | ushort | Gets or sets the number of decimal places to display in the percentage. |
| Height | string | Gets or sets the height of the progress bar. |
| Total | double | Gets or sets the total value of the progress bar. |
| Value | double | Gets or sets the current value of the progress bar. |

---

## PDQuestVisualizer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Quests | List<Quest> | Gets or sets the list of quests to be visualized. |
| QuestActions | List<QuestAction> | Gets or sets the list of quest actions to be visualized. |
| QuestHeight | int | Gets or sets the height of each quest lane. |
| QuestMargin | int | Gets or sets the margin between each quest lane. |
| QuestActionRadius | int | Gets or sets the radius of the quest action nodes. |

---

## PDRange

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Height | double | Gets or sets the height of the component. |
| Invert | bool | Gets or sets whether to invert the range. |
| Options | RangeOptions | Gets or sets the options for the range component. |
| Range | NumericRange | Gets or sets the numeric range. |
| ShowLabels | bool | Gets or sets whether to show labels. |
| TickMajor | double | Gets or sets the major tick interval. |
| Max | double | A function to format the major tick labels. |
| Min | double | Gets or sets the minimum value of the range. |
| MinGap | double | Gets or sets the minimum gap between the start and end of the range. |
| RangeChanged | EventCallback<NumericRange> | An event callback that is invoked when the range changes. |
| Step | double | Gets or sets the step value for the range. |
| TrackHeight | double | Gets or sets the height of the track. |
| Width | double | Gets or sets the width of the component. |

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
| DateFormat | string | Gets or sets the format for displaying dates. |
| DataPoint | DataPoint | Gets or sets the data point to be rendered. |
| Height | double | Gets or sets the height of the bar. |
| IsEnabled | bool | Gets or sets whether the component is enabled. |
| MaxValue | double | Gets or sets the maximum value for the bar. |
| Options | TimelineOptions | Gets or sets the timeline options. |
| X | double | Gets or sets the X coordinate of the bar. |

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
| Table | PDTable<TItem>? | A reference to the PDTable component. |
| CanChangeOrder | bool | Gets or sets whether the user can change the order of the columns. |
| CanChangeVisible | bool | Gets or sets whether the user can change the visibility of the columns. |

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
| DisableAfter | DateTime | Gets or sets the date and time after which the timeline is disabled. |
| DisableBefore | DateTime | Gets or sets the date and time before which the timeline is disabled. |
| Initialized | EventCallback | An event callback that is invoked when the component has been initialized. |
| IsEnabled | bool | Gets or sets whether the timeline is enabled. |
| Scale | TimelineScale | Gets or sets the current scale of the timeline. |
| ScaleChanged | EventCallback<TimelineScale> | An event callback that is invoked when the timeline scale changes. |
| Refreshed | EventCallback | An event callback that is invoked when the timeline has been refreshed. |
| SelectionChanged | EventCallback<TimeRange?> | An event callback that is invoked when the time selection changes. |
| SelectionChangeEnd | EventCallback | An event callback that is invoked when the time selection change is complete. |
| DataProvider | DataProviderDelegate? | A delegate that provides data points to the timeline. |
| Id | string | Gets or sets the unique identifier for the component. |
| NewMaxDateTimeAvailable | bool | Gets or sets whether a new maximum date/time is available. |
| NewMinDateTimeAvailable | bool | Gets or sets whether a new minimum date/time is available. |
| MaxDateTime | DateTime? | Gets or sets the maximum date and time of the timeline. |
| MinDateTime | DateTime | Gets or sets the minimum date and time of the timeline. |
| Options | TimelineOptions | Gets or sets the options for the timeline. |
| UpdateMaxDate | EventCallback | An event callback that is invoked to update the maximum date. |
| UpdateMinDate | EventCallback | An event callback that is invoked to update the minimum date. |
| OffsetX | int | A function to transform the Y value of data points. |

---

## PDTimelineToolbar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| IsEnabled | bool | Gets or sets whether the toolbar is enabled. |
| ShowRange | bool | Gets or sets whether to show the date/time range of the timeline. |
| ShowScale | bool | Gets or sets whether to show the scale controls. |
| ShowSelection | bool | Gets or sets whether to show the current selection details. |
| ShowZoomButtons | bool | Gets or sets whether to show the zoom in/out buttons. |
| Timeline | PDTimeline? | A reference to the PDTimeline component. |

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
| CloseOption | CloseOptions | Determines when the dropdown should close. |
| ShowOnMouseEnter | bool | Gets or sets whether the dropdown is shown on mouse enter. |

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
| Errors | object? | Gets or sets the collection of validation errors. |

---

## PDZoomBar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string | Gets or sets the unique identifier for the component. |
| Options | ZoomBarOptions | Gets or sets the options for the zoom bar. |
| Value | ZoombarValue | Gets or sets the current zoom and pan value. |
| ValueChanged | EventCallback<ZoombarValue> | An event callback that is invoked when the zoom or pan value changes. |
| Width | int | Gets or sets the width of the zoom bar canvas. |

---

