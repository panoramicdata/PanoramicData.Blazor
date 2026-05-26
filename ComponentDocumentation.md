# PanoramicData.Blazor Component Documentation

This document provides an overview of the Blazor components in this project.

Generated on: 2026-05-26 12:38:39

## PDAnimation

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | override string | Gets or sets the unique identifier for the animation. |
| Element | required RenderFragment | Gets or sets the element to be animated. |
| AnimationTime | double | The time in seconds for the animation to complete when the element is moved. |
| Transition | AnimationTransition | The type of transition to apply to the animation. |

---

## PDAudioButton

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ActiveColor | string |  |
| InactiveColor | string |  |

---

## PDAudioChannel

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Label | string | Gets or sets the label for the channel (displayed on the fader). |
| IsEnabled | bool | Gets or sets whether the channel is enabled. |
| GainValue | double | Gets or sets gain control value. |
| GainValueChanged | EventCallback<double> | Gets or sets callback fired when gain value changes. |
| GainColor | string | Gets or sets color used by gain control. |
| CompValue | double | Gets or sets compressor control value. |
| CompValueChanged | EventCallback<double> | Gets or sets callback fired when compressor value changes. |
| CompColor | string | Gets or sets color used by compressor control. |
| EqHighValue | double | Gets or sets high-band EQ value. |
| EqHighValueChanged | EventCallback<double> | Gets or sets callback fired when high-band EQ value changes. |
| EqMidValue | double | Gets or sets mid-band EQ value. |
| EqMidValueChanged | EventCallback<double> | Gets or sets callback fired when mid-band EQ value changes. |
| EqLowValue | double | Gets or sets low-band EQ value. |
| EqLowValueChanged | EventCallback<double> | Gets or sets callback fired when low EQ value changes. |
| EqColor | string | Gets or sets color used by EQ controls. |
| DspValue | double | Gets or sets DSP control value. |
| DspValueChanged | EventCallback<double> | Gets or sets callback fired when DSP value changes. |
| DspColor | string | Gets or sets color used by the DSP control. |
| PanValue | double | Gets or sets pan control value. |
| PanValueChanged | EventCallback<double> | Gets or sets callback fired when pan value changes. |
| PanColor | string | Gets or sets color used by pan control. |
| PflValue | double | Gets or sets pre-fade-listen button state value. |
| PflValueChanged | EventCallback<double> | Gets or sets callback fired when PFL value changes. |
| PflActiveColor | string | Gets or sets active color for PFL button. |
| PflInactiveColor | string | Gets or sets inactive color for PFL button. |
| MuteValue | double | Gets or sets mute button state value. |
| MuteValueChanged | EventCallback<double> | Gets or sets callback fired when mute value changes. |
| MuteActiveColor | string | Gets or sets active color for mute button. |
| MuteInactiveColor | string | Gets or sets inactive color for mute button. |
| FaderValue | double | Gets or sets channel fader value. |
| FaderValueChanged | EventCallback<double> | Gets or sets callback fired when fader value changes. |

---

## PDAudioPad

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ActiveColor | string | Gets or sets active color used when pad value is high. |
| InactiveColor | string | Gets or sets inactive color used when pad value is low. |
| DecayMode | DecayMode | Gets or sets decay behavior mode. |
| DecayUpon | DecayUpon | Gets or sets whether activation starts on press or release. |
| DecayHalfLife | TimeSpan | Gets or sets half-life duration used by exponential and linear decay. |
| ZeroBelow | double? | Gets or sets threshold below which values are treated as zero during decay. |
| MinValue | double | Gets or sets minimum output value for this pad. |
| Width | int | Gets or sets the pad width in pixels. |
| Height | int | Gets or sets the pad height in pixels. |
| Symbol | Symbol? | Gets or sets an optional symbol rendered on the pad. |
| SymbolColor | string? | Gets or sets optional symbol color override. |
| LabelColor | string? | Gets or sets optional overlay label color override. |
| EventThrottleMs | int | Throttle interval in milliseconds for decay events (default 100ms). Only applies to Linear and Exponential decay modes to prevent event spam. |
| OnPadValueChanged | EventCallback<PDAudioPadEventArgs> | Event callback fired when the pad value changes. For toggle mode: fires on each toggle. For decay modes: throttled to EventThrottleMs interval. |

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
| Attributes | Dictionary<string, object> | Extra attributes to apply to the button. |
| ChildContent | RenderFragment? | Custom content to display instead of the standard text and icon. |
| CssClass | string | CSS Class for button. |
| IconCssClass | string | CSS Class for icon to be displayed on button. |
| Id | string | Unique identifier for button. |
| IsEnabled | bool | Determines whether the button is enabled and can be clicked? |
| Click | EventCallback<MouseEventArgs> | Sets a callback for when user clicks button. |
| MouseDown | EventCallback<MouseEventArgs> | An event callback that is invoked when the mouse button is pressed down on the button. |
| MouseEnter | EventCallback<MouseEventArgs> | An event callback that is invoked when the mouse pointer enters the button. |
| Operation | Func<MouseEventArgs, Task>? | Async function to be called when button is clicked. |
| OperationIconCssClass | string | CSS Class for icon to be displayed on button when Operation is running. |
| PreventDefault | bool | Gets or sets whether to prevent the default action of the event. |
| StopPropagation | bool | Gets or sets whether to stop the event from propagating further. |
| Size | ButtonSizes? | Gets or sets the button sizes. |
| ShortcutKey | ShortcutKey | Sets the short cut keys that will perform a click on this button. In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive) |
| Target | string | Target where URL content should be opened. |
| Text | string | Sets the text displayed on the button. |
| TextCssClass | string | CSS Class for text to be displayed on button. |
| ToolTip | string | Sets the text displayed on the buttons tooltip. |
| Url | string | Target URL. If set forces the button to be rendered as an Anchor element. |

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
| Css | Func<TCard, string>? | Function returning CSS class that will be applied to the card. |
| Card | required TCard | The card data that is associated with this component. |
| ParentCardDeck | required PDCardDeck<TCard> | The parent card deck that this card belongs to. |

---

## PDCardDeck

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | override string | Unique identifier for this Card Deck. If not set, a unique ID will be generated. |
| DataFunction | Func<Task<DataResponse<TCard>>> |  |
| IsAnimated | bool | Whether the deck has animations enabled or not. Defaults to false. |
| CardTemplate | RenderFragment<TCard>? | Template for rendering each individual Card within this Deck |
| DeckTemplate | RenderFragment<PDCardDeck<TCard>>? | Template for rendering this Deck |
| CardCss | Func<TCard, string>? | Global CSS Class to outline the styling of each Card |
| MultipleSelection | bool | Whether the deck has multiple selection enabled or not. Defaults to false. |

---

## PDCardDeckGroup

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | override string | Unique identifier for this card deck group. |
| DataProvider | IDataProviderService<TCard> |  |
| ChildContent | RenderFragment? |  |
| ValidateCardMove | Func<PDCardDeck<TCard>, PDCardDeck<TCard>, bool>? | Determines if a card move is valid. |
| Transformation | Func<IDataProviderService<TCard>, PDCardDeck<TCard>, PDCardDeck<TCard>, List<TCard>, Task>? | Transformation that is applied to the cards when they are moved within this group. This can be a Reordering operation or Migration (cards moving from one deck to another) |

---

## PDCardDeckLoadingIcon

This component has no public parameters.

---

## PDChat

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChatService | required IChatService |  |
| User | required ChatMessageSender |  |
| ChatDockPosition | PDChatDockPosition | Gets or sets the dock position of the chat window. |
| CollapsedIcon | string | Gets or sets the icon to display when the chat window is collapsed. |
| UserIconSelector | Func<ChatMessage, string?>? | A function to select a user icon for a given message. |
| PriorityIconSelector | Func<ChatMessage, string?>? | A function to select a priority icon for a given message. |
| SoundSelector | Func<ChatMessage, string?>? | A function to select a sound to play for a given message. |
| OnChatMinimized | EventCallback | An event callback that is invoked when the chat window is minimized. |
| OnChatRestored | EventCallback | An event callback that is invoked when the chat window is restored. |
| OnChatMaximized | EventCallback | An event callback that is invoked when the chat window is maximized. |
| OnMuteToggled | EventCallback | An event callback that is invoked when the mute setting is toggled. |
| OnChatCleared | EventCallback | An event callback that is invoked when the chat is cleared. |
| OnMessageSent | EventCallback<ChatMessage> | An event callback that is invoked when a message is sent. |
| OnMessageReceivedEvent | EventCallback<ChatMessage> | An event callback that is invoked when a message is received. |
| OnAutoRestored | EventCallback | An event callback that is invoked when the chat window is automatically restored. |

---

## PDChatContainer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the main content of the container. |
| ChatContent | RenderFragment? | Gets or sets the content of the chat panel. |
| InitialDockMode | PDChatDockMode | Initial dock mode for the chat. If not specified, defaults to Minimized. The container will automatically manage dock mode changes internally. |
| DockModeChanged | EventCallback<PDChatDockMode> | Callback fired when the dock mode changes. Optional - for external monitoring only. |
| GutterSize | int | Gets or sets the size of the gutter between the main content and the chat panel. |
| ChatPanelSize | int | Gets or sets the initial size of the chat panel. |
| TotalSize | int | Gets or sets the total size of the container. |
| ChatMinSize | int | Gets or sets the minimum size of the chat panel. |
| ContentMinSize | int | Gets or sets the minimum size of the main content. |
| ChatService | required IChatService | Gets or sets the chat service for the container. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ButtonText | string | The text to show next to (on the right) of the copy button. If not set, only the button will be shown |
| ButtonTextCssClass | string | The CSS class to apply to the text next to the copy button. If not set, no CSS class will be applied |
| CssClass | string | General CSS Class to apply |
| ReadyToCopyCssClass | string | CSS class to apply when the copy button is ready to be clicked |
| Text | string | Text to be copied. |
| TextCopiedCssClass | string | CSS class to apply when the text has been copied |
| ToolTip | string | Text displayed as a tooltip. |

---

## PDColorPicker

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string | Gets or sets the unique identifier. |
| Value | string | Gets or sets the current color value (hex format). |
| ValueChanged | EventCallback<string> | Event callback raised when the color value changes. |
| ColorSelected | EventCallback<string> | Event callback raised when a color is selected (after confirmation if buttons shown). |
| Size | ButtonSizes? | Gets or sets the button sizes. |
| Text | string | Gets or sets the text displayed on the button. |
| CssClass | string | Gets or sets CSS classes for the button. |
| ItemCssClass | string | Gets or sets CSS classes for the toolbar item. |
| TextCssClass | string | Gets or sets CSS classes for the text. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| ShiftRight | bool | Gets or sets whether the toolbar item is positioned further to the right. |
| Options | ColorPickerOptions | Gets or sets the color picker options. |
| Palette | List<PaletteColor>? | Gets or sets the color palette to display. |
| RecentColors | List<string>? | Gets or sets the recently chosen colors. |
| RecentColorsChanged | EventCallback<List<string>> | Event callback raised when recent colors should be updated. |

---

## PDColumn

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AutoComplete | string | Gets or sets the autocomplete attribute value. |
| CanToggleVisible | bool | Gets or sets whether this column can be shown or hidden by the user. |
| DefaultSortDirection | SortDirection | Gets or sets the default sort direction for this column. |
| Description | string? | Gets or sets a short description of the columns purpose. Overrides DisplayAttribute description if set. |
| DescriptionFunc | Func<FormField<TItem>, PDForm<TItem>?, string> | Gets or sets a function that returns the description for the field. |
| DisplayOptions | FieldDisplayOptions | Gets or sets optional display options. |
| Editable | bool | Gets or sets whether this column is editable. |
| EditTemplate | RenderFragment<TItem?>? | Gets or sets an HTML template for editing. |
| Filterable | bool | Gets or sets whether this column can be filtered. |
| Format | string? | Optional format string for displaying the field value. |
| Field | Expression<Func<TItem, object>>? | A Linq expression that selects the field to be data bound to. |
| FilterIcon | string | Gets or sets the CSS class for the filter icon. |
| FilterKey | string | Gets or sets the key to use for filtering. |
| FilterOptions | FilterOptions | Gets or sets the options for the filter. |
| FilterShowSuggestedValues | bool | Gets or sets whether to show suggested values in the filter. |
| FilterShowSelectAll | bool | Gets or sets whether to show the select all / deselect all row in the filter values list. |
| FilterSuggestedValues | IEnumerable<object> | Gets or sets the suggested values for the filter. |
| FilterMaxValues | int? | Gets or sets the maximum number of values to show in the filter. |
| HeaderTemplate | RenderFragment? | Gets or sets an HTML template for the header content. |
| Helper | FormFieldHelper<TItem>? | Gets or sets an optional helper for filling in the field. |
| HelpText | string? | Optional text for the alt attribute of the cell. |
| HelpUrl | string? | Gets or sets a URL to an external context sensitive help page. |
| Id | string | The Id - this should be unique per column in a table |
| IsPassword | bool | Gets whether this field contains passwords or other sensitive information. |
| IsSensitive | Func<TItem?, PDForm<TItem>?, bool> | Gets or sets a function that determines whether this field contains sensitive values that should not be shown. |
| IsTextArea | bool | Gets or sets whether this field contains longer sections of text. |
| IsVisible | bool | Gets or sets whether the column is visible or not. |
| IsImage | bool | Gets or sets whether this field contains an image If the field is a string, then the string is treated as the image URL |
| MinValue | double? | Gets or sets the minimum value for numeric values. |
| MaxLength | int? | Gets or sets the maximum length for entered text. |
| MaxValue | double? | Gets or sets the maximum value for numeric values. |
| Name | string | Gets or sets an optional name for the column. Useful for calculated columns that have no header text / title. |
| Options | Func<FormField<TItem>, TItem?, OptionInfo[]>? | Gets a function that returns available value choices. |
| OptionsAsync | Func<FormField<TItem>, TItem?, Task<OptionInfo[]>>? | Gets an asynchronous function that returns available value choices. |
| Ordinal | int | Gets or sets the preferred ordinal position of the column (from left to right). |
| ReadOnlyInCreate | Func<TItem?, bool> | Gets or sets a function that determines whether this field is read-only when the linked form mode is Create. |
| ReadOnlyInEdit | Func<TItem?, bool> | Gets or sets a function that determines whether this field is read-only when the linked form mode is Edit. |
| ShowCopyButton | Func<TItem?, bool> | Gets or sets whether a 'copy to clipboard' button is displayed for the field. |
| ShowInList | bool | This sets whether something CAN be shown in the list, use DTTable ColumnsToDisplay to dynamically change which to display from those that CAN be shown in the list |
| ShowInEdit | Func<TItem?, bool> | Gets or sets a function that determines whether this field is visible when the linked form mode is Edit. |
| ShowInCreate | Func<TItem?, bool> | Gets or sets a function that determines whether this field is visible when the linked form mode is Create. |
| ShowInDelete | Func<TItem?, bool> | Gets or sets a function that determines whether this field is visible when the linked form mode is Create. |
| ShowValidationResult | bool | Gets or sets whether the validation result should be shown when displayed in a linked form. |
| Sortable | bool | Gets or sets whether this column can be sorted. |
| TdClass | string? | Optional CSS class for the column cell. |
| ThClass | string? | Optional CSS class for the column header. |
| Title | string? | If set will override the FieldExpression's name. |
| TitleFunc | Func<TItem?, string>? | Gets or sets a function that returns the title for the column. |
| Template | RenderFragment<TItem>? | Gets or sets an HTML template for the fields value. |
| TextAreaRows | int | Gets or sets the number of rows of text displayed by default in a text area., |
| Type | Type? | The data type of the columns field value. |
| UserSelectable | bool? | Gets or sets whether the contents of this cell are user selectable. |

---

## PDComboBox

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Items | required List<TItem> | Gets or sets the list of items to be displayed in the combo box. |
| SelectedItemChanged | required EventCallback<TItem> | An event callback that is invoked when the selected item changes. |
| ItemToString | Func<TItem, string> | A function to convert an item to its string representation. |
| ItemToId | required Func<TItem, string> | A function to get a unique identifier for an item. |
| Filter | required Func<TItem, string, bool> | A function to filter the items based on the search text. |
| SelectedItem | TItem? | Gets or sets the currently selected item. |
| Placeholder | string | Gets or sets the placeholder text for the input. |
| OrderBy | Func<TItem, object>? | A function to specify the sort order for the items. |
| MaxResults | int | Gets or sets the maximum number of results to display. |
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
| ButtonSize | ButtonSizes | Sets the button size in the modal footer. |
| CancelText | string | Gets the text displayed on the Cancel button. |
| ChildContent | RenderFragment? | Sets the content displayed in the modal dialog body. |
| Message | string | Gets the message to be displayed if the ChildContent not supplied. |
| NoText | string | Gets the text displayed on the No button. |
| ShowCancel | bool | Gets whether to show the Cancel button? |
| YesText | string | Gets the text displayed on the Yes button. |

---

## PDContextMenu

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Items | List<MenuItem> | Gets or sets the menu items to be displayed in the context menu. |
| ChildContent | RenderFragment? | Gets or sets the child content that the COntextMenu wraps. |
| UpdateState | EventCallback<MenuItemsEventArgs> | Gets or sets an event that is raised just prior to the context menu being shown and allowing the application to refresh the state of the items. |
| ItemClick | EventCallback<MenuItem> | Gets or sets an event callback delegate fired when the user selects clicks one of the items. |
| Enabled | bool | Sets whether the context menu is enabled or disabled. |
| ShowOnMouseUp | bool | Gets or sets whether the menu is displayed on the mouse up event instead of the default mouse down event. |

---

## PDDashboard

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Tabs | List<PDDashboardTab> | Gets or sets the dashboard tabs. |
| ColumnCount | int | Gets or sets the number of grid columns. Default 12. |
| TileRowHeightPx | int | Gets or sets the height of each grid row in pixels. |
| Css | string? | Gets or sets dashboard-level CSS classes. |
| WidgetHeaderCss | string? | Gets or sets CSS classes applied to all widget headers. Individual widgets can override via HeaderCss. |
| WidgetBorderCss | string? | Gets or sets CSS classes applied to all widget borders/cards. Individual widgets can override via BorderCss. |
| WidgetContentCss | string? | Gets or sets CSS classes applied to all widget content areas. Individual widgets can override via ContentCss. |
| ShowTabs | bool | Gets or sets whether to show the tab bar. |
| StartTab | int | Gets or sets the index of the initially selected tab. |
| IsRotationEnabled | bool | Gets or sets whether automatic tab rotation is enabled. |
| RotationIntervalSeconds | int | Gets or sets the tab auto-rotation interval in seconds. 0 = never rotate. Requires <see cref="IsRotationEnabled"/> to be true. |
| DisplayMode | bool | Gets or sets kiosk/display mode. When true, hides all editing chrome. |
| IsEditable | bool | Gets or sets whether editing controls are enabled. |
| MaximizePercent | int | Gets or sets the percentage of dashboard area used when a tile is maximized. Default 80. |
| ShowMaximize | bool | Gets or sets whether the maximize button is shown in view mode. Individual tiles can override this via their ShowMaximize property. |
| Name | string? | Gets or sets the dashboard display name. |
| ShowName | bool | Gets or sets whether to show the dashboard name in a header row above the tab bar. |
| DisplayModeHeader | DisplayModeHeaderContent | Gets or sets what to display in the header row when in display mode. |
| AllowViewModePropertyEdit | bool | Gets or sets whether users in regular view (not display mode or edit mode) can override property values for their session. |
| ShowEditButton | bool | Gets or sets whether to show the built-in edit mode toggle button. Only shown when <see cref="IsEditable"/> is not forced on externally. Default true. |
| Properties | Dictionary<string, string>? | Gets or sets dashboard-level properties as string key/value pairs. These are cascaded to all widgets and can be overridden at the widget level. |
| OnTileMove | EventCallback<(PDDashboardTile Tile, int NewRow, int NewColumn)> | Fired when a tile is moved via drag-and-drop. |
| OnTileResize | EventCallback<(PDDashboardTile Tile, int NewRowSpan, int NewColumnSpan)> | Fired when a tile is resized via the resize handle. |
| OnTileAdd | EventCallback | Fired when the user requests to add a new tile. If no delegate is provided, a blank <see cref="PDWidget"/> tile is added automatically. |
| OnTileDelete | EventCallback<PDDashboardTile> | Fired when a tile is deleted. The tile has already been removed from the active tab. |
| ConfirmTileDelete | bool | Gets or sets whether tile deletion requires a confirmation dialog. Default true. |
| OnTabAdd | EventCallback<PDDashboardTab> | Fired when a tab is added. |
| OnTabRemove | EventCallback<PDDashboardTab> | Fired when a tab is removed. |
| OnSettingsChanged | EventCallback | Fired when settings change. |
| ActiveTabChanged | EventCallback<int> | Fired when the active tab changes. |
| OnEditModeChanged | EventCallback<bool> | Fired when the IsEditable property changes value. |

---

## PDDateTime

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Blur | EventCallback | An event callback that is invoked when the component loses focus. |
| DateFormat | string | Gets or sets the date format string used for display and parsing. Defaults to "yyyy-MM-dd". When set to the default, the native browser date picker is used. When set to a custom format, a text input is used instead. |
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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment | Gets or sets the child content of the component. |

---

## PDDragDropSeparator

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Height | int | Gets or sets the height of the separator. |
| Before | bool? | Gets or sets whether the separator is before or after the item. |
| CssClass | string | Gets or sets the CSS class for the separator. |
| Drop | EventCallback<DropEventArgs> | An event callback that is invoked when an item is dropped on the separator. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the child content that the drop zone wraps. |
| Clickable | bool | Gets or sets whether the user can click to initiate an upload. |
| CssClass | string | Sets additional CSS classes. |
| Drop | EventCallback<DropZoneEventArgs> | Event raised whenever the user drops files onto the drop zone. |
| KeyDown | EventCallback<KeyboardEventArgs> | Event raised whenever the user finished pressing a key. |
| UploadStarted | EventCallback<DropZoneUploadEventArgs> | Event raised whenever a file upload starts. |
| UploadProgress | EventCallback<DropZoneUploadProgressEventArgs> | Event raised periodically during a file upload. |
| UploadCompleted | EventCallback<DropZoneUploadCompletedEventArgs> | Event raised whenever a file upload completes. |
| AllUploadsReady | EventCallback<UploadsReadyEventArgs> | Event raised when all files are ready to be uploaded. |
| AllUploadsStarted | EventCallback<int> | Event raised before uploads have started. |
| AllUploadsProgress | EventCallback<DropZoneAllProgressEventArgs> | Event raised during batch uploads. |
| AllUploadsComplete | EventCallback | Event raised when all uploads have completed. |
| UploadUrl | string? | Gets or sets the URL where file uploads should be sent. |
| SessionId | string | Gets or sets a unique identifier for the upload session. |
| Timeout | int | Sets the maximum time in seconds to wait for an upload to complete. |
| MaxFileSize | int | Sets the maximum file upload size in MB. |
| AutoScroll | bool | Sets whether to auto scroll when multiple files uploaded. |
| PreviewContainer | string | Optional CSS selector where preview elements are added. |
| PreviewTemplate | string | Optional CSS selector identifying upload item template. |
| Id | string | Gets the unique identifier of this panel. |

---

## PDFader

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Width | int | Gets or sets the width of the fader. |
| Height | int | Gets or sets the height of the fader. |
| FaderColor | string | Gets or sets the color of the fader. |
| CenterLineColor | string | Gets or sets the color of the center line. |
| MarkingColor | string | Gets or sets the color of the markings. |
| MinValue | int | Gets or sets the minimum value of the fader. |
| MaxValue | int | Gets or sets the maximum value of the fader. |
| FaderLabelPosition | PDFaderLabelPosition | Gets or sets the position of the fader labels. |

---

## PDField

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string | The Id - this should be unique per column in a table |
| Field | Expression<Func<TItem, object>>? | A Linq expression that selects the field to be data bound to. |
| Title | string? | If set will override the Field's name |
| TitleFunc | Func<TItem?, string>? | Gets or sets a function that returns the title for the field. |
| AutoComplete | string | Gets or sets the autocomplete attribute value. |
| DisplayOptions | FieldDisplayOptions | Gets or sets optional display options. |
| Description | string? | Gets or sets a short description of the fields purpose. Overrides DisplayAttribute description if set. |
| DescriptionFunc | Func<FormField<TItem>, PDForm<TItem>?, string> | Gets or sets a function that returns the description for the field. |
| Group | string | Gets or sets name of the group the field belongs to. |
| Label | string | Gets or sets text that is displayed in various ways depending on the control type. For example in a textbox will be displayed when no text has been entered as a place holder. |
| ShowCopyButton | Func<TItem?, bool> | Gets or sets whether a 'copy to clipboard' button is displayed for the field. |
| ShowInEdit | Func<TItem?, bool> | Gets or sets a function that determines whether this field is visible when the form mode is Edit. |
| ShowInCreate | Func<TItem?, bool> | Gets or sets a function that determines whether this field is visible when the form mode is Create. |
| ShowInDelete | Func<TItem?, bool> | Gets or sets a function that determines whether this field is visible when the form mode is Create. |
| ReadOnlyInEdit | Func<TItem?, bool> | Gets or sets a function that determines whether this field is read-only when the form mode is Edit. |
| ReadOnlyInCreate | Func<TItem?, bool> | Gets or sets a function that determines whether this field is read-only when the form mode is Create. |
| Options | Func<FormField<TItem>, TItem?, OptionInfo[]>? | Gets a function that returns available value choices. |
| OptionsAsync | Func<FormField<TItem>, TItem?, Task<OptionInfo[]>>? | Gets an asynchronous function that returns available value choices. |
| IsPassword | bool | Gets whether this field contains passwords or other sensitive information. |
| IsSensitive | Func<TItem?, PDForm<TItem>?, bool> | Gets or sets a function that determines whether this field contains sensitive values that should not be shown. |
| IsTextArea | bool | Gets or sets whether this field contains longer sections of text. |
| IsImage | bool | Gets or sets whether this field contains an image If the field is a string, then the string is treated as the image URL |
| TextAreaRows | int | Gets or sets the number of rows of text displayed by default in a text area., |
| MaxLength | int? | Gets or sets the maximum length for entered text. |
| MaxValue | double? | Gets or sets the maximum value allowed for numeric fields. |
| MinValue | double? | Gets or sets the minimum value allowed for numeric fields. |
| ShowValidationResult | bool | Gets or sets whether the validation result should be shown when displayed. |
| EditTemplate | RenderFragment<TItem?>? | Gets or sets an HTML template for editing. |
| Template | RenderFragment<TItem>? | Gets or sets an HTML template for the fields editor. |
| Helper | FormFieldHelper<TItem>? | Gets or sets an optional helper for filling in the field. |
| HelpUrl | string? | Gets or sets a URL to an external context sensitive help page. |

---

## PDFileExplorer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AllowDrag | bool | Determines whether the user may drag items. |
| AllowDrop | bool | Determines whether the user may drop dragged items onto other items. |
| AllowRename | bool | Determines whether the user may rename items. |
| AllowRenameConflicts | bool | Determines whether the to rename items when conflicting with existing items. |
| AutoExpand | bool | Determines whether the first node is automatically expanded on load. |
| BeforeRename | EventCallback<RenameArgs> | Gets or sets a delegate to be called before an item is renamed. |
| ButtonSize | ButtonSizes | Gets or sets the button sizes. |
| ColumnConfig | List<PDColumnConfig> | Sets the Table column configuration. |
| ConflictResolution | ConflictResolutions | Determines the action taken when copying conflicting named items into a folder. |
| CssClass | string | Gets or sets CSS classes to append. |
| CustomMoveCopy | EventCallback<CustomMoveCopyArgs> | Gets or sets callback that allows host app to perform custom move or copy operations. |
| DataProvider | IDataProviderService<FileExplorerItem> | Sets the IDataProviderService instance to use to fetch data. |
| DateFormat | string | Sets the date format. |
| DeleteRequest | EventCallback<DeleteArgs> | Event called whenever the user requests to delete one or more items. |
| DownloadUrlFunc | Func<FileExplorerItem, string?> | Function that calculates and returns the download url for the given item. |
| ExcludedPaths | string[] | An optional array of paths to be excluded. |
| ExceptionHandler | EventCallback<Exception> | Gets or sets a delegate to be called if an exception occurs. |
| FilenamePattern | string | Gets or sets an optional semi-colon delimited list of wild card patterns to filter filenames by. |
| FolderChanged | EventCallback<FileExplorerItem> | Event raised whenever the current folder changes. |
| GetItemBadgeCssClass | Func<FileExplorerItem, IconInfo?>? | Provides an optional function that allows a bagde icon CSS class to be provided for items. |
| GetItemCssClass | Func<FileExplorerItem, string>? | Provides a function that determines the CSS class for a given item. |
| GetItemIconCssClass | Func<FileExplorerItem, string>? | Provides a function that determines the icon CSS class for a given item. |
| GroupFolders | bool | Determines whether folders are always grouped together and shown first. |
| ItemDoubleClick | EventCallback<FileExplorerItem> | Event raised whenever the user double clicks on a file. |
| UploadMaxSize | int | Gets or sets the maximum file upload size in MB. |
| MoveCopyConflict | EventCallback<MoveCopyArgs> | Event called whenever a move or copy operation is subject to conflicts. |
| NewFolderName | string | Gets or sets the default name for new folders. |
| PreviewProvider | IPreviewProvider | Gets or sets an optional File Preview provider. |
| PreviewPanel | FilePreviewModes | Preview Panel mode. |
| ReadOnlyPostfix | string | Gets or sets string to append after a Read-Only items name. |
| ReadOnlyIconClass | string? | Gets or sets the CSS class for the read-only icon (e.g., "fa fa-solid fa-lock"). When set, this icon is used instead of ReadOnlyPostfix text. |
| ReadOnlyIndicatorPosition | ReadOnlyIndicatorPosition | Gets or sets the position of the read-only indicator relative to the file/folder name. Default is After for backward compatibility with the text postfix behavior. Use Before for better visual alignment when multiple items have indicators. |
| Ready | EventCallback | Gets or sets an event callback raised when the component has perform all it initialization. |
| RightClickSelectsItem | bool | Gets or sets whether right clicking on an item selects it? |
| ShowNavigateUpButton | bool | Determines whether the navigate up to the parent folder button is visible or not. |
| ShowParentFolder | bool | Determines where sub-folders show an entry (..) to allow navigation to the parent folder. |
| ShowToolbar | bool | Determines whether the toolbar is visible. |
| ShowUploadProgressDialog | bool | Determines whether the upload progress dialog is shown. |
| UploadProgressDialogThreshold | int | Determines when the upload progress dialog is shown. |
| SelectionChanged | EventCallback<FileExplorerItem[]> | Event raises whenever the selection changes. |
| SelectionMode | TableSelectionMode | Sets the allowed selection modes. |
| ShowContextMenu | bool | Determines whether the context menu is available. |
| ShowFiles | bool | Determines whether file entries should be listed. |
| SizeFormat | string | Sets the size (humanizer) format. |
| TableContextMenuClick | EventCallback<MenuItemEventArgs> | Event raised whenever the user clicks on a context menu item from the table. |
| TableContextItems | List<MenuItem> | Sets the Table context menu items. |
| TableDownloadRequest | EventCallback<TableSelectionEventArgs<FileExplorerItem>> | Event raised when user requests to download one or more files. |
| ToolbarClick | EventCallback<string> | Event raised whenever the user clicks on a toolbar button. |
| ToolbarItems | List<ToolbarItem> | Sets the Table context menu items. |
| TreeContextMenuClick | EventCallback<MenuItemEventArgs> | Event raised whenever the user clicks on a context menu item from the tree. |
| TreeContextItems | List<MenuItem> | Sets the Tree context menu items. |
| TreeSort | Comparison<FileExplorerItem>? | Optional sort function to use on sibling tree nodes. |
| UploadCompleted | EventCallback<DropZoneUploadCompletedEventArgs> | Event raised whenever a file upload completes. |
| UploadProgress | EventCallback<DropZoneUploadProgressEventArgs> | Event raised periodically during a file upload. |
| UploadRequest | EventCallback<DropZoneEventArgs> | Event raised whenever the user drops one or more files on to the file explorer. |
| UploadStarted | EventCallback<DropZoneUploadEventArgs> | Event raised whenever a file upload starts. |
| UploadUrl | string? | URL where files are uploaded. |
| UploadTimeout | int | Upload timeout in seconds. |
| UpdateTableContextState | EventCallback<MenuItemsEventArgs> | Event raised whenever the table context menu may need updating. |
| UpdateToolbarState | EventCallback<List<ToolbarItem>> | Event raised whenever the toolbar may need updating. |
| UpdateTreeContextState | EventCallback<MenuItemsEventArgs> | Event raised whenever the tree context menu may need updating. |

---

## PDFileModal

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CloseOnEscape | bool | Gets or sets whether the modal should close when the escape key is pressed. |
| DataProvider | IDataProviderService<FileExplorerItem> | Gets or sets the data provider for the file explorer. |
| ExcludedPaths | string[] | Gets or sets a collection of paths to exclude from the file explorer. |
| Height | string | Gets or sets the CSS height of the file explorer area. |
| GetItemIconCssClass | Func<FileExplorerItem, string>? | A function to get the CSS class for a given file explorer item. |
| OpenButtonText | string | Gets or sets the text for the 'Open' button. |
| OpenButtonIconCssClass | string | Gets or sets the icon CSS class for the 'Open' button. |
| SaveButtonText | string | Gets or sets the text for the 'Save' button. |
| SaveButtonIconCssClass | string | Gets or sets the icon CSS class for the 'Save' button. |
| ShowContextMenu | bool | Gets or sets whether to show the context menu in the file explorer. |
| ShowNavigateUpButton | bool | Gets or sets whether to show the 'Navigate Up' button in the file explorer. |
| ShowToolbar | bool | Gets or sets whether to show the toolbar in the file explorer. |
| OpenTitle | string | Gets or sets the title of the modal when in 'Open' mode. |
| SaveTitle | string | Gets or sets the title of the modal when in 'Save' mode. |
| Size | ModalSizes | Gets or sets the size of the modal. |
| HideOnBackgroundClick | bool | Gets or sets whether the modal should hide when the background is clicked. |
| ModalHidden | EventCallback<string> | An event callback that is invoked when the modal is hidden. |
| TreeSort | Comparison<FileExplorerItem>? | Optional sort function to use on sibling tree nodes. |
| ReadOnlyIconClass | string? | Gets or sets the CSS class for the read-only icon (e.g., "fa fa-solid fa-lock"). When set, this icon is used instead of ReadOnlyPostfix text. |
| ReadOnlyIndicatorPosition | ReadOnlyIndicatorPosition | Gets or sets the position of the read-only indicator relative to the filename. Default is After for backward compatibility. |

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
| FetchValuesAsync | Func<Filter, Task<string[]>>? | A function to fetch the values for the filter. |
| IconCssClass | string | Gets or sets the CSS class for the icon. |
| DataType | FilterDataTypes | Gets or sets the data type for the filter. |
| Nullable | bool | Gets or sets whether the value can be null. |
| Options | FilterOptions | Gets or sets the filter options. |
| ShowValues | bool | Gets or sets whether to show the values for the filter. |
| ShowSelectAll | bool | Gets or sets whether to show the select all / deselect all row above the values list. |
| Size | ButtonSizes | Gets or sets the size of the filter button. |

---

## PDFlag

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CountryCode | required string |  |
| Width | string | Gets or sets the width of the flag. |

---

## PDForm

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AutoApplyDelta | bool | Should edit deltas be automatically applied to the model? |
| ChildContent | RenderFragment? | Gets or sets the child content that the drop zone wraps. |
| CssClass | string | CSS classes to be added to the containing DIV element. |
| ConfirmCancel | bool | Should the user be prompted to confirm cancel when changes have been made? |
| ConfirmOnUnload | bool | Should the user be prompted to confirm on page unload when changes have been made? |
| Id | string | Gets or sets the item being created / edited / deleted. |
| Item | TItem? | Gets or sets the item being created / edited / deleted. |
| DataProvider | IDataProviderService<TItem> | Gets or sets the IDataProviderService instance to use to save data. |
| Deleted | EventCallback<TItem> | Event raised whenever the current item is successfully deleted. |
| Created | EventCallback<TItem> | Event raised when the current item has been successfully created. |
| FieldUpdated | EventCallback<FieldUpdateArgs<TItem>> | Event raised when the current item has been successfully updated. |
| Updated | EventCallback<TItem> | Event raised when the current item has been successfully updated. |
| Error | EventCallback<string> | Event raised whenever an error occurs. |
| HideForm | bool | Should the form be hidden after a Save operation? |
| DefaultMode | FormModes | Sets the default mode of the form. |
| HelpTextMode | HelpTextMode | Sets how help text is displayed. |
| CustomValidate | EventCallback<CustomValidateArgs<TItem>> | Gets or sets a delegate to be called for each field validated. |
| ExceptionHandler | EventCallback<Exception> | Gets or sets a delegate to be called if an exception occurs. |
| SuppressInitialErrors | bool | Should any errors (i.e mandatory fields) be suppressed until the first edit occurs? |

---

## PDFormBody

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| DebounceWait | int | Sets the debounce wait period in milliseconds. |
| Table | PDTable<TItem>? | Gets or sets a linked PDTable instance that can be used to provide field definitions. |
| ChildContent | RenderFragment | Child HTML content. |
| ShowValidationIndicator | bool | Gets or sets whether the validation indicator should be shown for fields. |
| TitleWidth | int | Gets or sets the width, in Pixels, of the Title box. |

---

## PDFormCheckBox

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CssClass | string | Gets or sets the CSS class for the checkbox. |
| Disabled | bool | Gets or sets whether the checkbox is disabled. |
| Label | string | Gets or sets the label for the checkbox. |
| LabelBefore | bool | Gets or sets whether the label should be displayed before the checkbox. |
| Value | bool | Gets or sets the current value of the checkbox. |
| ValueChanged | EventCallback<bool> | An event callback that is invoked when the checkbox value changes. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Click | EventCallback<string> | Event raised whenever the user clicks on a button. |
| ErrorCountMessage | string | Error count message: placeholders => {0} = count {1} = ''/'s' {2} = field titles |
| Size | ButtonSizes? | Gets or sets the button sizes. |
| ShowSave | bool | Should the Save button be shown? |
| ShowCancel | bool | Should the Cancel button be shown? |
| ShowCancelWhenReadOnly | bool | Should the Cancel button be shown when in Read-Only mode? |
| ShowDelete | bool | Should the Delete button be shown (only applicable when in Edit mode)? |
| ShowErrorCount | bool | Should the number of errors be shown (when > 0). |
| SaveButtonText | string | Sets the text shown on the save button. |
| SaveButtonCssClass | string | Sets the icon CSS classes for the save button. |
| SaveButtonIconCssClass | string | Sets the icon CSS classes for the save button icon. |
| CancelButtonText | string | Sets the text shown on the cancel button. |
| CloseButtonText | string | Sets the text shown on the cancel button when the form is in ReadOnly mode. |
| CancelButtonCssClass | string | Sets the icon CSS classes for the cancel button. |
| CancelButtonIconCssClass | string | Sets the icon CSS classes for the cancel button icon. |
| DeleteButtonText | string | Sets the text shown on the delete button. |
| DeleteButtonCssClass | string | Sets the icon CSS classes for the delete button. |
| DeleteButtonIconCssClass | string | Sets the icon CSS classes for the delete button icon. |
| YesButtonText | string | Sets the text shown on the yes button. |
| YesButtonCssClass | string | Sets the icon CSS classes for the yes button. |
| YesButtonIconCssClass | string | Sets the icon CSS classes for the yes button icon. |
| NoButtonText | string | Sets the text shown on the no button. |
| NoButtonCssClass | string | Sets the icon CSS classes for the no button. |
| NoButtonIconCssClass | string | Sets the icon CSS classes for the no button icon. |

---

## PDFormHeader

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the custom child content to be displayed in the header. |
| ItemDescription | Func<TItem, string>? | provides a function that will get a short description of the item being edited. |
| HelpText | string | Gets or sets the help text for the form. |
| CancelTitle | string | Gets or sets the title for when the form is in cancel mode. |
| CreateTitle | string | Gets or sets the title for a create form. if omitted then an automatic title is generated. |
| EditTitle | string | Gets or sets the title for an edit form. if omitted then an automatic title is generated. |
| DeleteTitle | string | Gets or sets the title for a delete form. if omitted then an automatic title is generated. |

---

## PDGlobalListener

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the child content of the component. |

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
| SelectionChanged | EventCallback<(GraphNode? Node, GraphEdge? Edge)> | Gets or sets a callback that is invoked when the selection changes. |

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
| ConfigurationChanged | EventCallback<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double damping)> | Gets or sets a callback that is invoked when the configuration changes. |

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
| ConfigurationChanged | EventCallback<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping)> | Gets or sets a callback that is invoked when the configuration changes. |

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
| SelectionChanged | EventCallback<(GraphNode? Node, GraphEdge? Edge)> | Gets or sets a callback that is invoked when the selection changes. |
| ConfigurationChanged | EventCallback<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping)> | Gets or sets a callback that is invoked when the configuration changes. |

---

## PDImage

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string | Gets or sets the unique identifier for the image. |
| CssClass | string | Gets or sets the CSS class for the image. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| Width | string | Sets the width of the containing div element. |
| Value | string | Gets or sets the source URL of the image. |

---

## PDKnob

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Mode | PDKnobMode | Gets or sets the mode of the knob, which determines its behavior and appearance. |
| MaxDisplay | int | Gets or sets the maximum display value for the knob. |
| MinLabel | string? | Gets or sets the minimum display value for custom range labels. Only applies when MinLabel and MaxLabel are also set. |
| MaxLabel | string? | Gets or sets the maximum display value for custom range labels. Only applies when MinLabel and MaxLabel are also set. |
| SizePx | int | Gets or sets the size of the knob in pixels. |
| CapColor | string | Gets or sets the color of the knob's cap. |
| ActiveColor | string | Gets or sets the color of the active part of the knob. |
| ShowTicks | bool | Gets or sets whether to show tick marks around the knob. |
| StartAngle | double | Gets or sets the start angle of the knob's rotation in degrees. |
| EndAngle | double | Gets or sets the end angle of the knob's rotation in degrees. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Size | ButtonSizes? | Gets or sets the button sizes. |
| Attributes | Dictionary<string, object> | Extra attributes to apply to the button. |
| CssClass | string | CSS Class for button. |
| IconCssClass | string | CSS Class for icon to be displayed on button. |
| Id | string | Unique identifier for button. |
| IsEnabled | bool | Determines whether the button is enabled and can be clicked? |
| ShortcutKey | ShortcutKey | Sets the short cut keys that will perform a click on this button. In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive) |
| Target | string | Sets where to display the linked URL, as the name for a browsing context (a tab, window, or &lt;iframe&gt;). The following keywords have special meanings for where to load the URL: _self: the current browsing context. (Default) _blank: usually a new tab, but users can configure browsers to open a new window instead. _parent: the parent browsing context of the current one. If no parent, behaves as _self. _top: the topmost browsing context (the "highest" context that’s an ancestor of the current one). If no ancestors, behaves as _self. |
| Text | string | Sets the text displayed on the button. |
| TextCssClass | string | CSS Class for text to be displayed on button. |
| ToolTip | string | Sets the text displayed on the buttons tooltip. |
| Url | string | Sets the destination URL. |

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
| FilterIncludeFunction | Func<TItem, string, bool>? | A function to determine whether an item should be included in the filtered list. |
| Id | override string |  |
| ItemKeyFunction | Func<TItem, object>? | A function to get the key for a given item. |
| ItemTemplate | RenderFragment<TItem>? | A template for rendering each item in the list. |
| Selection | Selection<TItem> | Gets or sets the current selection. |
| SelectionChanged | EventCallback<Selection<TItem>> | An event callback that is invoked when the selection changes. |
| SelectionMode | TableSelectionMode | Gets or sets the selection mode for the list. |
| ShowAllCheckBox | bool | Gets or sets whether to show the 'All' checkbox. |
| ShowApplyCancelButtons | bool | Gets or sets whether to show the 'Apply' and 'Cancel' buttons. |
| ShowCheckBoxes | bool | Gets or sets whether to show checkboxes for each item. |
| ShowFilter | bool | Gets or sets whether to show the filter input. |
| SortDirection | SortDirection | Gets or sets the sort direction for the list. |
| SortExpression | Expression<Func<TItem, object>>? | An expression to specify the sort order for the list. |
| TextExpression | Expression<Func<TItem, string>>? | An expression to specify the text to be displayed for each item. |

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
| CssClass | string | Optional CSS class to apply to the "class" attribute on the log container. |
| LogLevel | LogLevel | Gets or sets the minimum log level to display. |
| Capacity | int | Gets or sets the maximum number of log entries to keep. |
| Rows | int | Gets or sets the number of rows to display. |
| ShowTimestamp | bool | Gets or sets whether to show the timestamp for each log entry. |
| ShowIcon | bool | Gets or sets whether to show the icon for each log entry. |
| ShowException | bool | Gets or sets whether to show the exception for each log entry. |
| UtcTimestampFormat | string | Gets or sets the format for the UTC timestamp. |
| WordWrap | bool | Gets or sets whether to wrap long lines. |
| Tail | bool | Gets or sets whether to automatically scroll to the bottom of the log. |
| UseLocalTime | bool | Gets or sets whether to display timestamps in local time. |
| Reverse | bool | Gets or sets whether to display log entries in reverse chronological order. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Message | ChatMessage | Gets or sets the chat message to display. |
| UserIconSelector | Func<ChatMessage, string?>? | A function to select a user icon for a given message. |
| UseFullWidthMessages | bool | Gets or sets whether messages should use the full width of the container. |
| MessageMetadataDisplayMode | MessageMetadataDisplayMode | Gets or sets how message metadata is displayed. |
| ShowMessageUserIcon | bool | Gets or sets whether to show the user icon for each message. |
| ShowMessageUserName | bool | Gets or sets whether to show the user name for each message. |
| ShowMessageTimestamp | bool | Gets or sets whether to show the timestamp for each message. |
| MessageTimestampFormat | string | Gets or sets the format for the message timestamp. |

---

## PDMessages

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Messages | List<ChatMessage>? | Gets or sets the list of chat messages to display. |
| CurrentInput | string | Gets or sets the current user input. |
| CurrentInputChanged | EventCallback<string> | An event callback that is invoked when the user input changes. |
| IsLive | bool | Gets or sets whether the message stream is live. |
| CanSend | bool | Gets or sets whether the user can send a message. |
| OnSendClicked | EventCallback | An event callback that is invoked when the send button is clicked. |
| UserIconSelector | Func<ChatMessage, string?>? | A function to select a user icon for a given message. |
| UseFullWidthMessages | bool | Gets or sets whether messages should use the full width of the container. |
| MessageMetadataDisplayMode | MessageMetadataDisplayMode | Gets or sets how message metadata is displayed. |
| ShowMessageUserIcon | bool | Gets or sets whether to show the user icon for each message. |
| ShowMessageUserName | bool | Gets or sets whether to show the user name for each message. |
| ShowMessageTimestamp | bool | Gets or sets whether to show the timestamp for each message. |
| MessageTimestampFormat | string | Gets or sets the format for the message timestamp. |

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
| ButtonSize | ButtonSizes | Sets the modal button sizes in the footer |
| CssClass | string | Sets additional CSS classes. |
| HeaderCssClass | string | Sets additional CSS classes. |
| BodyCssClass | string | Sets additional CSS classes. |
| Footer | RenderFragment? | Sets the content displayed in the modal dialog footer. |
| Header | RenderFragment? | Sets the content displayed in the modal dialog header. |
| Hidden | EventCallback | An event callback that is invoked when the modal is hidden. |
| Title | string | Sets the title shown in the modal dialog header. |
| ChildContent | RenderFragment? | Sets the content displayed in the modal dialog body. |
| Shown | EventCallback | An event callback that is invoked when the modal is shown. |
| Size | ModalSizes | Sets the size of the modal dialog. |
| Buttons | List<ToolbarItem> | Sets the buttons displayed in the modal dialog footer. |
| ButtonClick | EventCallback<string> | Event triggered whenever the user clicks on a button. |
| CloseOnEscape | bool | Close the modal when the user presses the escape key? |
| ShowClose | bool | Display the close button in the top right of the modal? |
| CenterVertically | bool | Sets the title shown in the modal dialog header. |
| HideOnBackgroundClick | bool | Should clicking on the background hide the modal? |
| Id | string | Gets the unique identifier of the modal. |
| ShowFooter | bool | Gets or sets whether the modal footer is rendered. |

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
| InitializeCacheAsync | Func<MethodCache, Task>? | An async function to initialize the method cache for language completions. |
| InitializeOptions | Action<StandaloneEditorConstructionOptions>? | An action to initialize the editor options. |
| InitializeLanguage | Action<Language>? | An action to initialize a custom language. |
| InitializeLanguageAsync | Func<Language, Task>? | An async function to initialize a custom language. |
| RegisterLanguages | Action<List<Language>>? | An action to register custom languages. |
| UpdateCacheAsync | Func<MethodCache, string, string, Task>? | An async function to update the method cache. |
| SelectionChanged | EventCallback<Selection> | An event callback that is invoked when the selection changes in the editor. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CssClass | string | Additional CSS that can be applied to a pager component. |
| IsEnabled | bool | Determines whether the component is enabled or not. |
| NoItemsText | string | Gets or sets the text to be displayed when no items are available. |
| PageCriteria | PageCriteria | Sets the initial page count. |
| PageSizeChoices | uint[] | Gets or sets the possible page sizes offered to the user. |
| ShowPageChangeButtons | bool | Determines whether the navigation buttons are displayed. |
| ShowPageDescription | bool | Determines whether the description of the current page items is displayed. |
| ShowPageSizeChoices | bool | Determines whether the page size choices are displayed. |
| Size | ButtonSizes? | Gets or sets the button sizes. |

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
| TickMajorLabelFn | Func<double, string>? | A function to format the major tick labels. |
| Max | double | Gets or sets the maximum value of the range. |
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

## PDSection

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| CssClass | string | Gets or sets additional CSS classes applied to the outer container element. |
| HeaderCssClass | string | Gets or sets additional CSS classes applied to the header button element. |
| BodyCssClass | string | Gets or sets additional CSS classes applied to the body wrapper element. |
| TitleCssClass | string | Gets or sets additional CSS classes applied to the title element. |
| SecondaryTitleCssClass | string | Gets or sets additional CSS classes applied to the secondary title element. |
| Title | string | Gets or sets the primary title text. Ignored when <see cref="TitleTemplate"/> is set. |
| SecondaryTitle | string | Gets or sets optional secondary title text rendered beside the primary title. Ignored when <see cref="TitleTemplate"/> is set. |
| HeadingLevel | int? | Gets or sets the heading level (1-6) used to render the title as an H element. When null (default) the title is rendered as a plain span. |
| TitleTemplate | RenderFragment? | Gets or sets a custom render fragment for the title area. When set, <see cref="Title"/>, <see cref="SecondaryTitle"/> and <see cref="HeadingLevel"/> are ignored. |
| HeaderActions | RenderFragment? | Gets or sets an optional render fragment rendered in the right-hand side of the header. Click events on this area do not propagate to the toggle handler. |
| ChildContent | RenderFragment? | Gets or sets the body content shown when the section is expanded. |
| IsCollapsed | bool | Gets or sets whether the section is collapsed. Supports two-way binding. |
| IsCollapsedChanged | EventCallback<bool> | Raised when <see cref="IsCollapsed"/> changes, enabling two-way binding. |
| Toggled | EventCallback<bool> | Raised after the section is toggled, providing the new collapsed state. |
| ExpanderTooltip | string | Gets or sets the tooltip shown on the header toggle button. |
| Id | string | Gets or sets the HTML id attribute. Auto-generated if not provided. |
| IsDisabled | bool | Gets or sets whether the section header toggle is disabled. The body content remains visible but cannot be collapsed. |

---

## PDSplitPanel

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Size | int | Sets the default panel size. |
| MinSize | int | Sets the minimum panel size in pixels. |
| ChildContent | RenderFragment | Child HTML content. |
| CssClass | string | Sets extra CSS classes to append. |

---

## PDSplitter

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Direction | SplitDirection | Gets or sets the direction to split the contained panels. |
| ExpandToMin | bool | Gets or sets whether to expand panels to their min size, possibly overriding the default percentage size. |
| GutterSize | int | Sets the gutter sizes in pixels. |
| GutterAlign | string | Gets or sets the gutter alignment between elements. |
| SnapOffset | int | Sets the snap to minimum size offset in pixels. |
| DragInterval | int | Sets the number of pixels to drag. |
| ChildContent | RenderFragment | Child HTML content. |
| CssClass | string | Provides additional CSS classes for the containing element. |

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
| YValueTransform | Func<double, double> | A function to transform the Y value of data points. |

---

## PDStatusRollUp

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Node | PDStatusRollUpNode? | Gets or sets the status tree root node. |
| Label | string? | Gets or sets an optional text label rendered beside the icon. |
| RedIconClass | string | Gets or sets the CSS icon class used when Status is Red. |
| AmberIconClass | string | Gets or sets the CSS icon class used when Status is Amber. |
| GreenIconClass | string | Gets or sets the CSS icon class used when Status is Green. |
| GrayIconClass | string | Gets or sets the CSS icon class used when Status is Gray (unknown). |
| TriggerIconSize | string? | Gets or sets the CSS font-size for the trigger icon (e.g. "1rem", "16px"). When null (default), inherits naturally. |
| TriggerTitle | string | Gets or sets the tooltip text shown when hovering over the trigger icon before the popup opens. |
| OnBeforeExpand | Func<PDStatusRollUpNode, Task<PDStatusRollUpNode?>>? | Optional callback invoked just before a node's popup is shown (including drill-downs). Receives the node about to be expanded; return an updated node to replace it, or null to use the existing node unchanged. When not set the component behaves exactly as before. |

---

## PDStudio

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| StudioService | IPDStudioService? | Gets or sets the studio service for code execution. |
| Options | PDStudioOptions | Gets or sets the configuration options. |
| OnCodeExecuted | EventCallback<string> | Event callback for when code is executed. |
| OnExecutionStateChanged | EventCallback<bool> | Event callback for when execution state changes. |
| OnLoggingVisibilityChanged | EventCallback<bool> | Event callback for when logging visibility changes. |
| DataProvider | IDataProviderService<GraphData>? | Gets or sets the data provider for the graph data. |
| EditorToolbarContent | RenderFragment? | Custom content for the editor toolbar. |
| InitializeMonacoOptions | Action<StandaloneEditorConstructionOptions>? | Gets or sets a callback to initialize Monaco editor options. |
| InitializeMethodCache | Action<MethodCache>? | Gets or sets a callback to initialize the method cache for language completions. |
| RegisterLanguages | Action<List<Language>>? | Gets or sets a callback to register custom languages for Monaco editor. |
| InitializeLanguageAsync | Func<Language, Task>? | Gets or sets a callback to initialize custom language configurations. |
| UpdateMethodCacheAsync | Func<MethodCache, string, string, Task>? | Gets or sets a callback to update method cache asynchronously. |

---

## PDStudioResults

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Content | string | Gets or sets the HTML content to display in the results iframe. |
| IsExecuting | bool | Gets or sets whether execution is currently in progress. |
| ExecutionStatus | string | Gets or sets the current execution status message. |
| ShowStatusBar | bool | Gets or sets whether to show the status bar. |
| ContentChanged | EventCallback<string> | Event callback when content changes. |

---

## PDTab

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | Guid | Gets or sets the unique identifier for the tab. |
| Title | string | Gets or sets the title of the tab. |
| ChildContent | RenderFragment? | Gets or sets the child content of the tab. |
| CssClass | string | Gets or sets the CSS class for the tab. |
| IsClosingEnabled | bool? | Gets or sets whether the tab can be closed. This overrides the parent TabSet's setting. |
| IsRenamingEnabled | bool? | Gets or sets whether the tab can be renamed. This overrides the parent TabSet's setting. |
| OnSelected | EventCallback | An event callback that is invoked when the tab is selected. |

---

## PDTable

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AllowColumnResize | bool | Gets or sets whether columns can be resized. |
| AllowColumnSort | bool | Gets or sets whether columns can be sorted. |
| AllowCreate | bool | Gets or sets whether new items can be created. |
| AllowDelete | bool | Gets or sets whether items can be deleted. |
| AllowDrag | bool | Gets or sets whether rows can be dragged. |
| AllowDrop | bool | Gets or sets whether items can be dropped onto the table. |
| AllowEdit | bool | Gets or sets whether items can be edited. |
| AllowMultiSelect | bool | Gets or sets whether multiple rows can be selected. |
| AllowPaging | bool | Gets or sets whether paging is enabled. |
| AllowSelection | bool | Gets or sets whether rows can be selected. |
| BeforeCreate | EventCallback<DataRequest<TItem>> | An event callback that is invoked before a new item is created. |
| BeforeDelete | EventCallback<TItem> | An event callback that is invoked before an item is deleted. |
| ButtonSize | ButtonSizes | Gets or sets the size of the buttons in the toolbar. |
| ChildContent | RenderFragment? | Gets or sets the child content of the component, which is typically a set of PDColumn components. |
| AfterEdit | EventCallback<TableAfterEditEventArgs<TItem>> | Callback fired after an item edit ends. |
| AfterEditCommitted | EventCallback<TableAfterEditCommittedEventArgs<TItem>> | Callback fired after an item edit ends and has been successfully saved. |
| AfterFetch | EventCallback | Callback fired after a fetch has completed |
| AutoLoad | bool | Determines whether items are fetched from the DataProvider when the component is first rendered. |
| BeforeEdit | EventCallback<TableBeforeEditEventArgs<TItem>> | Callback fired before an item edit begins. |
| BeforeFetch | EventCallback | Callback fired before a fetch is started |
| Click | EventCallback<TItem> | Callback fired whenever the user clicks on a given item. |
| ColumnsConfig | List<PDColumnConfig>? | Allows an application defined configuration to be applied to the available columns at runtime. |
| CssClass | string | Gets or sets the CSS class to apply to the container element. |
| DataProvider | IDataProviderService<TItem> | Gets or sets the IDataProviderService instance to use to fetch data. |
| DoubleClick | EventCallback<TItem> | Callback fired whenever the user double-clicks on a given item. |
| DownloadUrlFunc | Func<TItem, string?> | Function that calculates and returns the download url attribute for each row. |
| Drop | EventCallback<DropEventArgs> | An event callback that is invoked when an item is dropped onto the table. |
| ExceptionHandler | EventCallback<Exception> | Gets or sets a delegate to be called if an exception occurs. |
| ExportButton | bool | Gets or sets whether the export button is shown. |
| FooterTemplate | RenderFragment? | A template for the table footer. |
| HeaderTemplate | RenderFragment? | A template for the table header. |
| Height | string | Gets or sets the height of the table. |
| Id | string | Gets or sets the unique identifier for the component. |
| IsLoading | bool | Gets or sets whether the table is currently loading data. |
| FilterMaxValues | int | Gets or sets the maximum number of possible filter values to show. |
| IsEnabled | bool | Gets or sets whether table interaction is enabled. |
| ItemsLoaded | Action<List<TItem>>? | Action called whenever data items are loaded. |
| KeyDown | EventCallback<KeyboardEventArgs> | Callback fired whenever the user presses a key down. |
| KeyField | Func<TItem, object>? | A LINQ expression that selects the item field that contains the key value. |
| NoDataMessage | string | Gets or sets the message to be displayed when no data is available. |
| PagerCssClass | string | Gets or sets the CSS class to apply to the Pager, if present |
| RowClass | Func<TItem, string>? | Gets or sets a function that calculates and returns CSS Classes for the row (TR element). |
| RowIsEnabled | Func<TItem, bool> | Gets or sets a function that determines whether the given row is enabled or not. |
| TableClass | string | Gets or sets the CSS class to apply to the tables container element. |
| THeadClass | string | Gets or sets the CSS class to apply to the table header element. |
| PageChanged | EventCallback<PageCriteria> | Callback fired whenever the component changes the currently displayed page. |
| PageSizeChanged | EventCallback<PageCriteria> | Callback fired whenever the page size changes. |
| PageCriteria | PageCriteria? | Gets or sets the default page criteria. |
| PagerPosition | PagerPositions | Gets or sets whether the pager (if shown) is positioned at the top or bottom of the table. |
| PageSizeChoices | uint[] | Gets or sets the possible page sizes offered to the user. |
| Ready | EventCallback | Gets or sets an event callback raised when the component has perform all it initialization. |
| RetainSelectionOnPage | bool | Gets or sets whether the selection is maintained across pages. |
| RightClickSelectsRow | bool | Gets whether right-clicking selects a row versus left-clicking. |
| SaveChanges | bool | Gets whether the table will save changes via the DataProvider (if set). |
| SearchText | string? | Search text to be passed to IDataProvider when querying for data. |
| SearchTextChanged | EventCallback<string?> | Event callback for when search text has changed. |
| SelectionChanged | EventCallback | Callback fired whenever the current selection changes. |
| SelectionMode | TableSelectionMode | Gets or sets whether selection is enabled and the method in which it works. |
| ShowCheckboxes | bool | Gets or sets whether the checkboxes should be shown for multiple selection. |
| ShowOverlay | bool | Gets or sets whether the Overlay Service is used when fetching data. |
| ShowPager | bool | Gets or sets whether the pager is displayed. |
| Size | ButtonSizes? | Gets or sets the button and form control sizes. |
| SortChanged | EventCallback<SortCriteria> | Event callback fired whenever the sort criteria has changed. |
| SortCriteria | SortCriteria | Gets or sets the default sort criteria. |
| UserSelectable | bool | Gets or sets whether the contents of all cells are user selectable by default. |
| EditOnDoubleClick | bool | Gets or sets whether editing begins on double click instead of single click selection. |
| MaxHeight | string? | Gets or sets the maximum height of the table container. When set, enables scrollable mode. |
| StickyHeader | bool | Gets or sets whether the table header (thead) stays visible at the top while the body scrolls. Only applies when MaxHeight is set. |
| StickyPager | bool | Gets or sets whether the pager stays at the bottom outside the scroll area. Only applies when MaxHeight is set. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the child content of the component. |
| CssClass | string | Gets or sets the CSS class for the component. |
| IsTabClosingEnabled | bool | Gets or sets whether tabs can be closed. |
| TabMinWidth | string | Gets or sets the minimum width of a tab. |
| TabMaxWidth | string | Gets or sets the maximum width of a tab. |
| IsTabAdditionEnabled | bool | Gets or sets whether new tabs can be added. |
| CreateTabPosition | CreateTabPosition | Gets or sets the position of the create tab button. |
| IsTabRenamingEnabled | bool | Gets or sets whether tabs can be renamed. |
| OnTabSelected | EventCallback<PDTab> | An event callback that is invoked when a tab is selected. |
| OnTabClosed | EventCallback<PDTab> | An event callback that is invoked when a tab is closed. |
| OnTabAdded | EventCallback | An event callback that is invoked when a new tab is added. |
| OnTabRenamed | EventCallback<PDTab> | An event callback that is invoked when a tab is renamed. |

---

## PDTextArea

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Blur | EventCallback | Event raised when the text area loses focus. |
| CssClass | string | Gets or sets CSS classes for the text box. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| IsReadOnly | bool | Gets or sets whether the content is read only. |
| Width | string | Sets the width of the containing div element. |
| MaxLength | int | Sets the maximum length of the input. |
| Placeholder | string | Gets or sets placeholder text for the text box. |
| SelectionChanged | EventCallback<TextAreaSelection> | Event raised whenever the text value changes. |
| Value | string | Sets the initial text value. |
| ValueChanged | EventCallback<string> | Event raised whenever the text value changes. |
| Keypress | EventCallback<KeyboardEventArgs> | Event raised whenever a key is pressed. |
| Rows | int | Sets the number of rows displayed. |
| ShowClearButton | bool | Gets or sets whether the clear button is displayed. |
| DebounceWait | int | Sets the de-bounce wait period in milliseconds. |
| Cleared | EventCallback | Event raised when the user clicks on the clear button. |

---

## PDTextBox

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AutoComplete | string | Gets or sets the autocomplete attribute value. |
| Blur | EventCallback | Event raised when the text box loses focus. |
| Size | ButtonSizes? | Gets or sets the textbox sizes. |
| CssClass | string | Gets or sets CSS classes for the text box. |
| Type | PDInputType | Gets or sets the input type. |
| KeypressEvent | bool | Gets whether keypress events are raised. |
| SpeechLang | string | Gets or sets the speech recognition language. Leave empty for browser default. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsReadOnly | bool | Gets or sets whether the content is read only. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| Width | string | Sets the width of the containing div element. |
| Placeholder | string | Gets or sets placeholder text for the text box. |
| Value | string | Sets the initial text value. |
| ValueChanged | EventCallback<string> | Event raised whenever the text value changes. |
| BindEvent | string | Gets or sets the event that triggers binding, e.g. oninput or onchange. |
| Keypress | EventCallback<KeyboardEventArgs> | Event raised whenever a key is pressed. |
| ShowClearButton | bool | Gets or sets whether the clear button is displayed. |
| ShowSpeechButton | bool | Gets or sets whether the user may use speech to populate the textbox. |
| DebounceWait | int | Sets the debounce wait period in milliseconds. |
| Cleared | EventCallback | Event raised when the user clicks on the clear button. |

---

## PDTiles

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string | Gets or sets the unique identifier for the component. |
| CssClass | string | Gets or sets the CSS class for the component. |
| Style | string | Gets or sets additional inline styles. |
| Width | string | Gets or sets the width of the component. |
| Height | string | Gets or sets the height of the component. |
| Options | TileGridOptions | Gets or sets the grid options. |
| ConnectorOptions | TileConnectorOptions | Gets or sets the connector options. |
| Tiles | List<TileDefinition>? | Gets or sets custom tile definitions with per-tile overrides. |
| Connectors | List<TileConnector>? | Gets or sets custom connector definitions. |
| Logos | List<string> | Gets or sets the list of logo paths to use. |
| ChildContent | RenderFragment? | Gets or sets the child content to render on top of the tiles. |
| TileClick | EventCallback<TileClickEventArgs> | Event callback invoked when a tile is clicked. |
| ConnectorClick | EventCallback<ConnectorClickEventArgs> | Event callback invoked when a connector is clicked. |

---

## PDTilesJavaScript

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Id | string | Gets or sets the unique identifier for the component. |
| CssClass | string | Gets or sets the CSS class for the component. |
| Style | string | Gets or sets additional inline styles. |
| Width | string | Gets or sets the width of the component. |
| Height | string | Gets or sets the height of the component. |
| Options | TileGridOptions | Gets or sets the grid options. |
| ConnectorOptions | TileConnectorOptions | Gets or sets the connector options. |
| Logos | List<string> | Gets or sets the list of logo paths to use. |
| TileClick | EventCallback<TileClickEventArgs> | Event callback invoked when a tile is clicked. |
| ConnectorClick | EventCallback<ConnectorClickEventArgs> | Event callback invoked when a connector is clicked. |

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
| YValueTransform | Func<double, double> | A function to transform the Y value of data points. |

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

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| BorderWidth | int? | Gets or sets the border width of the switch. |
| Height | int? | Gets or sets the height of the switch. |
| Id | override string | Gets or sets the unique identifier for the component. |
| Label | string | Gets or sets the label text for the switch. |
| LabelBefore | bool? | Gets or sets whether the label should be displayed before the switch. |
| OffText | string? | Gets or sets the text to display when the switch is in the 'off' state. |
| OnText | string? | Gets or sets the text to display when the switch is in the 'on' state. |
| Options | PDToggleSwitchOptions | Gets or sets the options for the toggle switch. |
| Rounded | bool? | Gets or sets whether the switch is rounded. |
| Value | bool | Gets or sets the current value of the switch. |
| ValueChanged | EventCallback<bool> | An event callback that is invoked when the switch value changes. |
| Width | int? | Gets or sets the width of the switch. |

---

## PDToolbar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ButtonSize | ButtonSizes | Gets or sets the button sizes. |
| ChildContent | RenderFragment | Child HTML content. |
| Items | List<ToolbarItem>? | Sets a list of application controlled toolbar items. |
| ButtonClick | EventCallback<KeyedEventArgs<MouseEventArgs>> | Event raised whenever the user clicks on a toolbar button. |
| CssClass | string | Gets or sets additional CSS classes for the toolbar. |

---

## PDToolbarButton

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Size | ButtonSizes? | Gets or sets the button sizes. |
| Key | string | Gets or sets the unique identifier. |
| Text | string | Gets or sets the text displayed on the button. |
| Click | EventCallback<KeyedEventArgs<MouseEventArgs>> | Event raised whenever user clicks on the button. |
| CssClass | string | Gets or sets CSS classes for the button. |
| ItemCssClass | string | Gets or sets CSS classes for the toolbar item. |
| IconCssClass | string | Gets or sets CSS classes for an optional icon. |
| TextCssClass | string | Gets or sets CSS classes for the text. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| Operation | Func<MouseEventArgs, Task>? | Async function to be called when button is clicked. |
| OperationIconCssClass | string | CSS Class for icon to be displayed on button when Operation is running. |
| ShiftRight | bool | Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item. |
| ShortcutKey | ShortcutKey | Sets the short cut keys that will perform a click on this button. In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive) |
| Target | string | Target where URL content should be opened. |
| Url | string | Target URL. If set forces the button to be rendered as an Anchor element. |

---

## PDToolbarColorPicker

No code-behind file found for this component.

---

## PDToolbarDropdown

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment | Child HTML content. |
| Click | EventCallback<string> | Event raised whenever user clicks on the button. |
| Key | string | Gets or sets the unique identifier. |
| Size | ButtonSizes? | Gets or sets the button sizes. |
| Text | string | Gets or sets the text displayed on the button. |
| CloseOption | CloseOptions | Determines when the dropdown should close. |
| CssClass | string | Gets or sets CSS classes for the button. |
| ItemCssClass | string | Gets or sets CSS classes for the toolbar item. |
| IconCssClass | string | Gets or sets CSS classes for an optional icon. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| Items | List<MenuItem> | Gets or sets the menu items to be displayed in the context menu. |
| ShiftRight | bool | Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item. |
| ShowOnMouseEnter | bool | Gets or sets whether the dropdown is shown on mouse enter. |
| TextCssClass | string | Gets or sets CSS classes for the text. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |

---

## PDToolbarItem

This component has no public parameters.

---

## PDToolbarLinkButton

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Size | ButtonSizes? | Gets or sets the button sizes. |
| Key | string | Gets or sets the unique identifier. |
| Text | string | Gets or sets the text displayed on the button. |
| CssClass | string | Gets or sets CSS classes for the button. |
| ItemCssClass | string | Gets or sets CSS classes for the toolbar item. |
| IconCssClass | string | Gets or sets CSS classes for an optional icon. |
| TextCssClass | string | Gets or sets CSS classes for the text. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| ShiftRight | bool | Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item. |
| ShortcutKey | ShortcutKey | Sets the short cut keys that will perform a click on this button. In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive) |
| Target | string | Sets where to display the linked URL, as the name for a browsing context (a tab, window, or &lt;iframe&gt;). The following keywords have special meanings for where to load the URL: _self: the current browsing context. (Default) _blank: usually a new tab, but users can configure browsers to open a new window instead. _parent: the parent browsing context of the current one. If no parent, behaves as _self. _top: the topmost browsing context (the "highest" context that’s an ancestor of the current one). If no ancestors, behaves as _self. |
| Url | string | Sets the destination URL. |

---

## PDToolbarPlaceholder

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| ChildContent | RenderFragment? | Gets or sets the child content that the drop zone wraps. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| ItemCssClass | string | Gets or sets CSS classes for the toolbar item. |
| ShiftRight | bool | Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item. |

---

## PDToolbarSeparator

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Key | string | Gets or sets the unique identifier. |
| CssClass | string | Gets or sets CSS classes for the separator. |
| ItemCssClass | string | Gets or sets CSS classes for the toolbar item. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| ShiftRight | bool | Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item. |

---

## PDToolbarTextbox

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Type | PDInputType | Gets or sets the text box input type |
| Size | ButtonSizes? | Gets or sets the text box sizes. |
| Key | string | Gets or sets the unique identifier. |
| CssClass | string | Gets or sets CSS classes for the text box. |
| ItemCssClass | string | Gets or sets CSS classes for the toolbar item. |
| KeypressEvent | bool | Gets whether Keypress events are raised. |
| Placeholder | string | Gets or sets placeholder text for the text box. |
| ToolTip | string | Gets or sets the tooltip for the toolbar item. |
| IsVisible | bool | Gets or sets whether the toolbar item is visible. |
| IsEnabled | bool | Gets or sets whether the toolbar item is enabled. |
| ShiftRight | bool | Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item. |
| Width | string | Sets the width of the containing div element. |
| Value | string | Sets the initial text value. |
| ValueChanged | EventCallback<string> | Event raised whenever the text value changes. |
| Keypress | EventCallback<KeyboardEventArgs> | Event raised whenever a key is pressed. |
| ShowClearButton | bool | Gets or sets whether the clear button is displayed. |
| Cleared | EventCallback | Event raised when the user clicks on the clear button. |
| Label | string | Sets an optional label to be displayed before the text box. |
| DebounceWait | int | Sets the de-bounce wait period in milliseconds. |

---

## PDTree

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| AfterEdit | EventCallback<TreeNodeAfterEditEventArgs<TItem>> | Callback fired after a node edit ends. |
| AllowDrag | bool | Gets or sets whether nodes may be dragged. |
| AllowDrop | bool | Gets or sets whether items may be dropped onto nodes. |
| AllowDropInBetween | bool | Gets or sets whether nodes can be dropped before or after other nodes. |
| AllowEdit | bool | Gets or sets whether node edit is allowed. |
| AllowSelection | bool | Gets or sets whether selection is allowed. |
| BeforeEdit | EventCallback<TreeNodeBeforeEditEventArgs<TItem>> | Callback fired before a node edit begins. |
| BeforeSelectionChange | EventCallback<TreeBeforeSelectionChangeEventArgs<TItem>> | Gets or sets an event callback raised just before the selection changes. |
| ClearOnCollapse | bool | Should a node clear its child content on collapse? Doing so will force a re-load of child nodes if it is re-expanded. Only applicable when LoadOnDemand = true. |
| DataProvider | IDataProviderService<TItem> | Gets or sets the <see cref="IDataProviderService{TItem}"/> instance to use to fetch data. |
| Drop | EventCallback<DropEventArgs> | Callback fired whenever a drag operation ends on a node within the tree. |
| ExceptionHandler | EventCallback<Exception> | Gets or sets a delegate to be called if an exception occurs. |
| ExpandOnExpandAll | Predicate<TreeNode<TItem>>? | Predicate used to determine whether a node should be expanded when ExpandAll is called. |
| IconCssClass | Func<TItem, int, string>? | A function that calculates the CSS classes used to show an icon for the given node. |
| IsLeaf | Func<TItem, bool>? | A function that determines whether the given item is a leaf in the tree. |
| ItemsLoaded | EventCallback<List<TItem>> | Callback fired whenever data items are loaded. |
| KeyDown | EventCallback<KeyboardEventArgs> | Callback fired whenever the user presses a key down. |
| KeyField | Func<TItem, object>? | A function that selects the field that contains the key value. |
| LoadOnDemand | bool | Gets or sets whether a non-leaf node will request data where necessary. |
| NodeCollapsed | EventCallback<TreeNode<TItem>> | Callback fired whenever the user collapses a node. |
| NodeExpanded | EventCallback<TreeNode<TItem>> | Callback fired whenever the user expands a node. |
| NodeTemplate | RenderFragment<TreeNode<TItem>>? | Gets or sets the template to render for each node. |
| NodeUpdated | EventCallback<TreeNode<TItem>> | Callback fired whenever a tree node is updated. |
| ParentKeyField | Func<TItem, object>? | A function that selects the field that contains the parent key value. |
| Ready | EventCallback | Gets or sets an event callback raised when the component has performed all its initialization. |
| RightClickSelectsItem | bool | Gets or sets whether right clicking on an item selects it. |
| SelectionChange | EventCallback<TreeNode<TItem>> | Gets or sets an event callback raised whenever the selection changes. |
| ShowLines | bool | Gets or sets whether expanded nodes should show lines to help identify nested levels. |
| ShowRoot | bool | Gets or sets whether the root node is displayed. |
| Sort | Comparison<TItem>? | A function used to determine sort order of child nodes. |
| TextField | Func<TItem, object>? | A function that selects the field to display for the item. |
| ToolTip | Func<TItem, string>? | A function that returns the tool tip text for a node. |

---

## PDTreeNode

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Node | TreeNode<TItem>? | Gets or sets the TreeNode to be rendered. |
| ShowLines | bool | Gets or sets whether the node when expanded, should show a line to help identify its boundary. |
| NodeTemplate | RenderFragment<TreeNode<TItem>>? | Gets or sets the template to render. |
| EndEdit | EventCallback | Event raised at the end of an edit. |
| KeyDown | EventCallback<KeyboardEventArgs> | Event raised whenever a key down event is generated on the tree node. |
| AllowDrag | bool | Gets or sets whether the node may be dragged. |
| AllowDrop | bool | Gets or sets whether items may be dropped onto the node. |
| AllowDropInBetween | bool | Gets or sets whether nodes can be dropped before or after other nodes. |
| Drop | EventCallback<DropEventArgs> | Callback fired whenever a drag operation ends on a node within a DragContext. |

---

## PDValidationSummary

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Errors | object? | Gets or sets the collection of validation errors. |

---

## PDWidget

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| Title | string? | Gets or sets the widget title displayed in the header. |
| TitleChanged | EventCallback<string> | Gets or sets the callback fired when the title is renamed. |
| WidgetType | PDWidgetType | Gets or sets the content type of the widget. |
| Content | string? | Gets or sets the HTML content or URL depending on the widget type. |
| ContentBytes | byte[]? | Gets or sets binary content for image widgets. |
| Configuration | string? | Gets or sets a JSON configuration string for the widget. |
| Css | string? | Gets or sets per-widget CSS classes. |
| Icon | string? | Gets or sets the icon CSS class displayed in the header (e.g. "fas fa-chart-bar"). |
| HeaderCss | string? | Gets or sets CSS classes applied to the widget header. Overrides dashboard-level WidgetHeaderCss. |
| BorderCss | string? | Gets or sets CSS classes applied to the widget border/card. Overrides dashboard-level WidgetBorderCss. |
| ContentCss | string? | Gets or sets CSS classes applied to the widget content area. Overrides dashboard-level WidgetContentCss. |
| RefreshIntervalSeconds | int | Gets or sets the auto-refresh interval in seconds. 0 = disabled. |
| IsEditable | bool | Gets or sets whether editing controls are shown. |
| ShowEditButton | bool | Gets or sets whether to show a built-in ✏ / ✓ edit toggle button in the widget header. Useful for standalone widgets not hosted in a <see cref="PDDashboard"/>. Default false. |
| ShowTitle | bool | Gets or sets whether the title bar is shown. |
| VerticalOverflow | OverflowBehavior | Gets or sets the vertical overflow behavior for widget content. |
| HorizontalOverflow | OverflowBehavior | Gets or sets the horizontal overflow behavior for widget content. |
| ContentAlignment | ContentAlignment | Gets or sets the vertical content alignment. |
| ChildContent | RenderFragment? | Gets or sets the child content for Custom widget type. |
| OnRefresh | EventCallback | Gets or sets the callback fired on content refresh. |
| OnConfigure | EventCallback | Gets or sets the callback fired when the configure button is clicked. |
| ContentChanged | EventCallback<string?> | Fired when content is changed via the configuration panel. |
| WidgetTypeChanged | EventCallback<PDWidgetType> | Fired when the widget type is changed via the configuration panel. |
| FetchContent | Func<string, Task<string>>? | Gets or sets a delegate for fetching URL content. The string parameter is the URL. |
| ClockTimeZone | TimeZoneInfo? | Gets or sets the clock timezone. Defaults to local time. |
| ClockTimeFormat | string | Gets or sets the clock time format string. |
| ClockDateFormat | string | Gets or sets the clock date format string. |
| ImageMimeType | string | Gets or sets the MIME type for image content bytes. |
| Properties | Dictionary<string, string>? | Gets or sets widget-level properties as string key/value pairs. These override any dashboard-level properties with the same key. |

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

