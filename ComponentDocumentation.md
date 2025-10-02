# PanoramicData.Blazor Component Documentation

This document provides an overview of the Blazor components in this project.

## PDAnimation

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=AnimationTime; Type=double; Description=}.Name) | $(@{Name=AnimationTime; Type=double; Description=}.Type) |  |
| $(@{Name=Transition; Type=AnimationTransition; Description=The type of transition to apply to the animation.}.Name) | $(@{Name=Transition; Type=AnimationTransition; Description=The type of transition to apply to the animation.}.Type) | The type of transition to apply to the animation. |

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
| $(@{Name=CssClass; Type=string; Description=}.Name) | $(@{Name=CssClass; Type=string; Description=}.Type) |  |
| $(@{Name=IsBusy; Type=bool; Description=}.Name) | $(@{Name=IsBusy; Type=bool; Description=}.Type) |  |
| $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Type) |  |
| $(@{Name=OverlayCssClass; Type=string; Description=}.Name) | $(@{Name=OverlayCssClass; Type=string; Description=}.Type) |  |
| $(@{Name=OverlayContent; Type=RenderFragment?; Description=}.Name) | $(@{Name=OverlayContent; Type=RenderFragment?; Description=}.Type) |  |

---

## PDButton

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=ChildContent; Type=RenderFragment?; Description=Custom content to display instead of the standard text and icon.}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=Custom content to display instead of the standard text and icon.}.Type) | Custom content to display instead of the standard text and icon. |
| $(@{Name=MouseDown; Type=EventCallback<MouseEventArgs>; Description=}.Name) | $(@{Name=MouseDown; Type=EventCallback<MouseEventArgs>; Description=}.Type) |  |
| $(@{Name=MouseEnter; Type=EventCallback<MouseEventArgs>; Description=}.Name) | $(@{Name=MouseEnter; Type=EventCallback<MouseEventArgs>; Description=}.Type) |  |
| $(@{Name=OperationIconCssClass; Type=string; Description=Async function to be called when button is clicked.}.Name) | $(@{Name=OperationIconCssClass; Type=string; Description=Async function to be called when button is clicked.}.Type) | Async function to be called when button is clicked. |
| $(@{Name=PreventDefault; Type=bool; Description=}.Name) | $(@{Name=PreventDefault; Type=bool; Description=}.Type) |  |
| $(@{Name=StopPropagation; Type=bool; Description=}.Name) | $(@{Name=StopPropagation; Type=bool; Description=}.Type) |  |

---

## PDCanvas

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Height; Type=int; Description=}.Name) | $(@{Name=Height; Type=int; Description=}.Type) |  |
| $(@{Name=Id; Type=string; Description=}.Name) | $(@{Name=Id; Type=string; Description=}.Type) |  |
| $(@{Name=Width; Type=int; Description=}.Name) | $(@{Name=Width; Type=int; Description=}.Type) |  |

---

## PDCard

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=IsAnimated; Type=bool; Description=Whether this card is animated when it is rendered or not.}.Name) | $(@{Name=IsAnimated; Type=bool; Description=Whether this card is animated when it is rendered or not.}.Type) | Whether this card is animated when it is rendered or not. |
| $(@{Name=DraggingEnabled; Type=bool; Description=Whether Dragging is enabled for this card.}.Name) | $(@{Name=DraggingEnabled; Type=bool; Description=Whether Dragging is enabled for this card.}.Type) | Whether Dragging is enabled for this card. |
| $(@{Name=Template; Type=RenderFragment<TCard>?; Description=The Template to render for the card.}.Name) | $(@{Name=Template; Type=RenderFragment<TCard>?; Description=The Template to render for the card.}.Type) | The Template to render for the card. |

---

## PDCardDeck

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=DataFunction; Type=Func<Task<DataResponse<TCard>>>; Description=Unique identifier for this Card Deck. If not set, a unique ID will be generated.}.Name) | $(@{Name=DataFunction; Type=Func<Task<DataResponse<TCard>>>; Description=Unique identifier for this Card Deck. If not set, a unique ID will be generated.}.Type) | Unique identifier for this Card Deck. If not set, a unique ID will be generated. |
| $(@{Name=IsAnimated; Type=bool; Description=Whether the deck has animations enabled or not. Defaults to false.}.Name) | $(@{Name=IsAnimated; Type=bool; Description=Whether the deck has animations enabled or not. Defaults to false.}.Type) | Whether the deck has animations enabled or not. Defaults to false. |
| $(@{Name=CardTemplate; Type=RenderFragment<TCard>?; Description=Template for rendering each individual Card within this Deck}.Name) | $(@{Name=CardTemplate; Type=RenderFragment<TCard>?; Description=Template for rendering each individual Card within this Deck}.Type) | Template for rendering each individual Card within this Deck |
| $(@{Name=DeckTemplate; Type=RenderFragment<PDCardDeck<TCard>>?; Description=Template for rendering this Deck}.Name) | $(@{Name=DeckTemplate; Type=RenderFragment<PDCardDeck<TCard>>?; Description=Template for rendering this Deck}.Type) | Template for rendering this Deck |
| $(@{Name=MultipleSelection; Type=bool; Description=Global CSS Class to outline the styling of each Card}.Name) | $(@{Name=MultipleSelection; Type=bool; Description=Global CSS Class to outline the styling of each Card}.Type) | Global CSS Class to outline the styling of each Card |

---

## PDCardDeckGroup

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=DataProvider; Type=IDataProviderService<TCard>; Description=Unique identifier for this card deck group.}.Name) | $(@{Name=DataProvider; Type=IDataProviderService<TCard>; Description=Unique identifier for this card deck group.}.Type) | Unique identifier for this card deck group. |
| $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Type) |  |

---

## PDCardDeckLoadingIcon

This component has no public parameters.

---

## PDChat

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Container; Type=PDChatContainer?; Description=}.Name) | $(@{Name=Container; Type=PDChatContainer?; Description=}.Type) |  |
| $(@{Name=ChatDockPosition; Type=PDChatDockPosition; Description=}.Name) | $(@{Name=ChatDockPosition; Type=PDChatDockPosition; Description=}.Type) |  |
| $(@{Name=CollapsedIcon; Type=string; Description=}.Name) | $(@{Name=CollapsedIcon; Type=string; Description=}.Type) |  |
| $(@{Name=OnChatMinimized; Type=EventCallback; Description=}.Name) | $(@{Name=OnChatMinimized; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=OnChatRestored; Type=EventCallback; Description=}.Name) | $(@{Name=OnChatRestored; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=OnChatMaximized; Type=EventCallback; Description=}.Name) | $(@{Name=OnChatMaximized; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=OnMuteToggled; Type=EventCallback; Description=}.Name) | $(@{Name=OnMuteToggled; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=OnChatCleared; Type=EventCallback; Description=}.Name) | $(@{Name=OnChatCleared; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=OnMessageSent; Type=EventCallback<ChatMessage>; Description=}.Name) | $(@{Name=OnMessageSent; Type=EventCallback<ChatMessage>; Description=}.Type) |  |
| $(@{Name=OnMessageReceivedEvent; Type=EventCallback<ChatMessage>; Description=}.Name) | $(@{Name=OnMessageReceivedEvent; Type=EventCallback<ChatMessage>; Description=}.Type) |  |
| $(@{Name=OnAutoRestored; Type=EventCallback; Description=}.Name) | $(@{Name=OnAutoRestored; Type=EventCallback; Description=}.Type) |  |

---

## PDChatContainer

This component has no public parameters.

---

## PDClickableImage

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=ImageSource; Type=string; Description=}.Name) | $(@{Name=ImageSource; Type=string; Description=}.Type) |  |
| $(@{Name=Alt; Type=string; Description=}.Name) | $(@{Name=Alt; Type=string; Description=}.Type) |  |
| $(@{Name=Title; Type=string; Description=}.Name) | $(@{Name=Title; Type=string; Description=}.Type) |  |
| $(@{Name=CssStyles; Type=string; Description=}.Name) | $(@{Name=CssStyles; Type=string; Description=}.Type) |  |

---

## PDClipboard

This component has no public parameters.

---

## PDColumn

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=FilterIcon; Type=string; Description=}.Name) | $(@{Name=FilterIcon; Type=string; Description=}.Type) |  |
| $(@{Name=FilterKey; Type=string; Description=}.Name) | $(@{Name=FilterKey; Type=string; Description=}.Type) |  |
| $(@{Name=FilterOptions; Type=FilterOptions; Description=}.Name) | $(@{Name=FilterOptions; Type=FilterOptions; Description=}.Type) |  |
| $(@{Name=FilterShowSuggestedValues; Type=bool; Description=}.Name) | $(@{Name=FilterShowSuggestedValues; Type=bool; Description=}.Type) |  |
| $(@{Name=FilterSuggestedValues; Type=IEnumerable<object>; Description=}.Name) | $(@{Name=FilterSuggestedValues; Type=IEnumerable<object>; Description=}.Type) |  |
| $(@{Name=FilterMaxValues; Type=int?; Description=}.Name) | $(@{Name=FilterMaxValues; Type=int?; Description=}.Type) |  |
| $(@{Name=Title; Type=string?; Description=If set will override the FieldExpression's name}.Name) | $(@{Name=Title; Type=string?; Description=If set will override the FieldExpression's name}.Type) | If set will override the FieldExpression's name |

---

## PDComboBox

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=SelectedItem; Type=TItem?; Description=}.Name) | $(@{Name=SelectedItem; Type=TItem?; Description=}.Type) |  |
| $(@{Name=Placeholder; Type=string; Description=}.Name) | $(@{Name=Placeholder; Type=string; Description=}.Type) |  |
| $(@{Name=MaxResults; Type=int; Description=}.Name) | $(@{Name=MaxResults; Type=int; Description=}.Type) |  |
| $(@{Name=IsDisabled; Type=bool; Description=}.Name) | $(@{Name=IsDisabled; Type=bool; Description=}.Type) |  |
| $(@{Name=IsReadOnly; Type=bool; Description=}.Name) | $(@{Name=IsReadOnly; Type=bool; Description=}.Type) |  |
| $(@{Name=NoResultsText; Type=string; Description=}.Name) | $(@{Name=NoResultsText; Type=string; Description=}.Type) |  |
| $(@{Name=ItemTemplate; Type=RenderFragment<TItem>?; Description=}.Name) | $(@{Name=ItemTemplate; Type=RenderFragment<TItem>?; Description=}.Type) |  |
| $(@{Name=NoResultsTemplate; Type=RenderFragment<string>?; Description=}.Name) | $(@{Name=NoResultsTemplate; Type=RenderFragment<string>?; Description=}.Type) |  |
| $(@{Name=ShowSelectedItemOnTop; Type=bool; Description=}.Name) | $(@{Name=ShowSelectedItemOnTop; Type=bool; Description=}.Type) |  |

---

## PDConfirm

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=CancelText; Type=string; Description=Gets the text displayed on the Cancel button.}.Name) | $(@{Name=CancelText; Type=string; Description=Gets the text displayed on the Cancel button.}.Type) | Gets the text displayed on the Cancel button. |
| $(@{Name=ChildContent; Type=RenderFragment?; Description=Sets the content displayed in the modal dialog body.}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=Sets the content displayed in the modal dialog body.}.Type) | Sets the content displayed in the modal dialog body. |
| $(@{Name=Message; Type=string; Description=Gets the message to be displayed if the ChildContent not supplied.}.Name) | $(@{Name=Message; Type=string; Description=Gets the message to be displayed if the ChildContent not supplied.}.Type) | Gets the message to be displayed if the ChildContent not supplied. |
| $(@{Name=NoText; Type=string; Description=Gets the text displayed on the No button.}.Name) | $(@{Name=NoText; Type=string; Description=Gets the text displayed on the No button.}.Type) | Gets the text displayed on the No button. |
| $(@{Name=ShowCancel; Type=bool; Description=Gets whether to show the Cancel button?}.Name) | $(@{Name=ShowCancel; Type=bool; Description=Gets whether to show the Cancel button?}.Type) | Gets whether to show the Cancel button? |
| $(@{Name=YesText; Type=string; Description=Gets the text displayed on the Yes button.}.Name) | $(@{Name=YesText; Type=string; Description=Gets the text displayed on the Yes button.}.Type) | Gets the text displayed on the Yes button. |

---

## PDContextMenu

This component has no public parameters.

---

## PDDateTime

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Blur; Type=EventCallback; Description=}.Name) | $(@{Name=Blur; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=ShowTime; Type=bool; Description=}.Name) | $(@{Name=ShowTime; Type=bool; Description=}.Type) |  |
| $(@{Name=TimeStepSecs; Type=int; Description=}.Name) | $(@{Name=TimeStepSecs; Type=int; Description=}.Type) |  |
| $(@{Name=Value; Type=DateTime; Description=}.Name) | $(@{Name=Value; Type=DateTime; Description=}.Type) |  |
| $(@{Name=ValueChanged; Type=EventCallback<DateTime>; Description=}.Name) | $(@{Name=ValueChanged; Type=EventCallback<DateTime>; Description=}.Type) |  |

---

## PDDateTimeOffset

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Blur; Type=EventCallback; Description=}.Name) | $(@{Name=Blur; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=ShowOffset; Type=bool; Description=}.Name) | $(@{Name=ShowOffset; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowTime; Type=bool; Description=}.Name) | $(@{Name=ShowTime; Type=bool; Description=}.Type) |  |
| $(@{Name=TimeStepSecs; Type=int; Description=}.Name) | $(@{Name=TimeStepSecs; Type=int; Description=}.Type) |  |
| $(@{Name=Value; Type=DateTimeOffset; Description=}.Name) | $(@{Name=Value; Type=DateTimeOffset; Description=}.Type) |  |
| $(@{Name=ValueChanged; Type=EventCallback<DateTimeOffset>; Description=}.Name) | $(@{Name=ValueChanged; Type=EventCallback<DateTimeOffset>; Description=}.Type) |  |

---

## PDDragContainer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=ChildContent; Type=RenderFragment; Description=}.Name) | $(@{Name=ChildContent; Type=RenderFragment; Description=}.Type) |  |
| $(@{Name=Items; Type=IEnumerable<TItem>; Description=}.Name) | $(@{Name=Items; Type=IEnumerable<TItem>; Description=}.Type) |  |
| $(@{Name=SelectionChanged; Type=EventCallback<IEnumerable<TItem>>; Description=}.Name) | $(@{Name=SelectionChanged; Type=EventCallback<IEnumerable<TItem>>; Description=}.Type) |  |

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
| $(@{Name=CanChangeOrder; Type=bool; Description=}.Name) | $(@{Name=CanChangeOrder; Type=bool; Description=}.Type) |  |
| $(@{Name=CanDrag; Type=bool; Description=}.Name) | $(@{Name=CanDrag; Type=bool; Description=}.Type) |  |
| $(@{Name=Id; Type=string; Description=}.Name) | $(@{Name=Id; Type=string; Description=}.Type) |  |
| $(@{Name=ItemOrderChanged; Type=EventCallback<DragOrderChangeArgs<TItem>>; Description=}.Name) | $(@{Name=ItemOrderChanged; Type=EventCallback<DragOrderChangeArgs<TItem>>; Description=}.Type) |  |
| $(@{Name=Template; Type=RenderFragment<TItem>?; Description=}.Name) | $(@{Name=Template; Type=RenderFragment<TItem>?; Description=}.Type) |  |
| $(@{Name=PlaceholderTemplate; Type=RenderFragment<TItem>?; Description=}.Name) | $(@{Name=PlaceholderTemplate; Type=RenderFragment<TItem>?; Description=}.Type) |  |

---

## PDDropDown

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Click; Type=EventCallback<MouseEventArgs>; Description=}.Name) | $(@{Name=Click; Type=EventCallback<MouseEventArgs>; Description=}.Type) |  |
| $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Type) |  |
| $(@{Name=CloseOption; Type=CloseOptions; Description=}.Name) | $(@{Name=CloseOption; Type=CloseOptions; Description=}.Type) |  |
| $(@{Name=CssClass; Type=string; Description=}.Name) | $(@{Name=CssClass; Type=string; Description=}.Type) |  |
| $(@{Name=DropdownDirection; Type=Directions; Description=}.Name) | $(@{Name=DropdownDirection; Type=Directions; Description=}.Type) |  |
| $(@{Name=DropDownHidden; Type=EventCallback; Description=}.Name) | $(@{Name=DropDownHidden; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=DropDownShown; Type=EventCallback; Description=}.Name) | $(@{Name=DropDownShown; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=IsEnabled; Type=bool; Description=}.Name) | $(@{Name=IsEnabled; Type=bool; Description=}.Type) |  |
| $(@{Name=IconCssClass; Type=string; Description=}.Name) | $(@{Name=IconCssClass; Type=string; Description=}.Type) |  |
| $(@{Name=Id; Type=string; Description=}.Name) | $(@{Name=Id; Type=string; Description=}.Type) |  |
| $(@{Name=KeyPress; Type=EventCallback<int>; Description=}.Name) | $(@{Name=KeyPress; Type=EventCallback<int>; Description=}.Type) |  |
| $(@{Name=PreventDefault; Type=bool; Description=}.Name) | $(@{Name=PreventDefault; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowCaret; Type=bool; Description=}.Name) | $(@{Name=ShowCaret; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowOnMouseEnter; Type=bool; Description=}.Name) | $(@{Name=ShowOnMouseEnter; Type=bool; Description=}.Type) |  |
| $(@{Name=Size; Type=ButtonSizes; Description=}.Name) | $(@{Name=Size; Type=ButtonSizes; Description=}.Type) |  |
| $(@{Name=StopPropagation; Type=bool; Description=}.Name) | $(@{Name=StopPropagation; Type=bool; Description=}.Type) |  |
| $(@{Name=Text; Type=string; Description=}.Name) | $(@{Name=Text; Type=string; Description=}.Type) |  |
| $(@{Name=TextCssClass; Type=string; Description=}.Name) | $(@{Name=TextCssClass; Type=string; Description=}.Type) |  |
| $(@{Name=ToolTip; Type=string; Description=}.Name) | $(@{Name=ToolTip; Type=string; Description=}.Type) |  |
| $(@{Name=Visible; Type=bool; Description=}.Name) | $(@{Name=Visible; Type=bool; Description=}.Type) |  |

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
| $(@{Name=Title; Type=string?; Description=If set will override the Field's name}.Name) | $(@{Name=Title; Type=string?; Description=If set will override the Field's name}.Type) | If set will override the Field's name |
| $(@{Name=Group; Type=string; Description=Gets or sets a function that returns the description for the field.}.Name) | $(@{Name=Group; Type=string; Description=Gets or sets a function that returns the description for the field.}.Type) | Gets or sets a function that returns the description for the field. |

---

## PDFileExplorer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=ColumnConfig; Type=List<PDColumnConfig>; Description=Sets the Table column configuration.}.Name) | $(@{Name=ColumnConfig; Type=List<PDColumnConfig>; Description=Sets the Table column configuration.}.Type) | Sets the Table column configuration. |
| $(@{Name=TableContextItems; Type=List<MenuItem>; Description=Sets the Table context menu items.}.Name) | $(@{Name=TableContextItems; Type=List<MenuItem>; Description=Sets the Table context menu items.}.Type) | Sets the Table context menu items. |
| $(@{Name=ToolbarItems; Type=List<ToolbarItem>; Description=Sets the Table context menu items.}.Name) | $(@{Name=ToolbarItems; Type=List<ToolbarItem>; Description=Sets the Table context menu items.}.Type) | Sets the Table context menu items. |

---

## PDFileModal

This component has no public parameters.

---

## PDFilePreview

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=ExceptionHandler; Type=EventCallback<Exception>; Description=}.Name) | $(@{Name=ExceptionHandler; Type=EventCallback<Exception>; Description=}.Type) |  |
| $(@{Name=Item; Type=FileExplorerItem?; Description=}.Name) | $(@{Name=Item; Type=FileExplorerItem?; Description=}.Type) |  |
| $(@{Name=PreviewProvider; Type=IPreviewProvider; Description=}.Name) | $(@{Name=PreviewProvider; Type=IPreviewProvider; Description=}.Type) |  |

---

## PDFilter

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=CssClass; Type=string; Description=}.Name) | $(@{Name=CssClass; Type=string; Description=}.Type) |  |
| $(@{Name=Filter; Type=Filter; Description=}.Name) | $(@{Name=Filter; Type=Filter; Description=}.Type) |  |
| $(@{Name=FilterChanged; Type=EventCallback<Filter>; Description=}.Name) | $(@{Name=FilterChanged; Type=EventCallback<Filter>; Description=}.Type) |  |
| $(@{Name=IconCssClass; Type=string; Description=}.Name) | $(@{Name=IconCssClass; Type=string; Description=}.Type) |  |
| $(@{Name=DataType; Type=FilterDataTypes; Description=}.Name) | $(@{Name=DataType; Type=FilterDataTypes; Description=}.Type) |  |
| $(@{Name=Nullable; Type=bool; Description=}.Name) | $(@{Name=Nullable; Type=bool; Description=}.Type) |  |
| $(@{Name=Options; Type=FilterOptions; Description=}.Name) | $(@{Name=Options; Type=FilterOptions; Description=}.Type) |  |
| $(@{Name=ShowValues; Type=bool; Description=}.Name) | $(@{Name=ShowValues; Type=bool; Description=}.Type) |  |
| $(@{Name=Size; Type=ButtonSizes; Description=}.Name) | $(@{Name=Size; Type=ButtonSizes; Description=}.Type) |  |

---

## PDFlag

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Width; Type=string; Description=}.Name) | $(@{Name=Width; Type=string; Description=}.Type) |  |

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
| $(@{Name=DebounceWait; Type=int; Description=}.Name) | $(@{Name=DebounceWait; Type=int; Description=}.Type) |  |
| $(@{Name=Field; Type=FormField<TItem>; Description=}.Name) | $(@{Name=Field; Type=FormField<TItem>; Description=}.Type) |  |
| $(@{Name=Form; Type=PDForm<TItem>; Description=}.Name) | $(@{Name=Form; Type=PDForm<TItem>; Description=}.Type) |  |
| $(@{Name=Id; Type=string; Description=}.Name) | $(@{Name=Id; Type=string; Description=}.Type) |  |

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
| $(@{Name=Id; Type=string; Description=Gets or sets the unique identifier for this component.}.Name) | $(@{Name=Id; Type=string; Description=Gets or sets the unique identifier for this component.}.Type) | Gets or sets the unique identifier for this component. |
| $(@{Name=CssClass; Type=string; Description=Gets or sets the CSS class for styling.}.Name) | $(@{Name=CssClass; Type=string; Description=Gets or sets the CSS class for styling.}.Type) | Gets or sets the CSS class for styling. |
| $(@{Name=IsVisible; Type=bool; Description=Gets or sets whether the component is visible.}.Name) | $(@{Name=IsVisible; Type=bool; Description=Gets or sets whether the component is visible.}.Type) | Gets or sets whether the component is visible. |
| $(@{Name=DataProvider; Type=IDataProviderService<GraphData>?; Description=Gets or sets the data provider for the graph data.}.Name) | $(@{Name=DataProvider; Type=IDataProviderService<GraphData>?; Description=Gets or sets the data provider for the graph data.}.Type) | Gets or sets the data provider for the graph data. |
| $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration.}.Name) | $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration.}.Type) | Gets or sets the visualization configuration. |
| $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Name) | $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Type) | Gets or sets the clustering configuration. |
| $(@{Name=ConvergenceThreshold; Type=double; Description=Gets or sets the convergence threshold for the physics simulation. Lower values make physics run longer.}.Name) | $(@{Name=ConvergenceThreshold; Type=double; Description=Gets or sets the convergence threshold for the physics simulation. Lower values make physics run longer.}.Type) | Gets or sets the convergence threshold for the physics simulation. Lower values make physics run longer. |
| $(@{Name=Damping; Type=double; Description=Gets or sets the damping factor for the physics simulation. Higher values mean faster settling.}.Name) | $(@{Name=Damping; Type=double; Description=Gets or sets the damping factor for the physics simulation. Higher values mean faster settling.}.Type) | Gets or sets the damping factor for the physics simulation. Higher values mean faster settling. |
| $(@{Name=NodeClick; Type=EventCallback<GraphNode>; Description=Gets or sets a callback that is invoked when a node is clicked.}.Name) | $(@{Name=NodeClick; Type=EventCallback<GraphNode>; Description=Gets or sets a callback that is invoked when a node is clicked.}.Type) | Gets or sets a callback that is invoked when a node is clicked. |
| $(@{Name=EdgeClick; Type=EventCallback<GraphEdge>; Description=Gets or sets a callback that is invoked when an edge is clicked.}.Name) | $(@{Name=EdgeClick; Type=EventCallback<GraphEdge>; Description=Gets or sets a callback that is invoked when an edge is clicked.}.Type) | Gets or sets a callback that is invoked when an edge is clicked. |
| $(@{Name=Size; Type=double; Description=Gets or sets a callback that is invoked when the selection changes.}.Name) | $(@{Name=Size; Type=double; Description=Gets or sets a callback that is invoked when the selection changes.}.Type) | Gets or sets a callback that is invoked when the selection changes. |

---

## PDGraphControls

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=IsReadOnly; Type=bool; Description=Gets or sets whether the controls are read-only.}.Name) | $(@{Name=IsReadOnly; Type=bool; Description=Gets or sets whether the controls are read-only.}.Type) | Gets or sets whether the controls are read-only. |
| $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration.}.Name) | $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration.}.Type) | Gets or sets the visualization configuration. |
| $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Name) | $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Type) | Gets or sets the clustering configuration. |
| $(@{Name=AvailableDimensions; Type=List<string>; Description=Gets or sets the available dimension names for mapping.}.Name) | $(@{Name=AvailableDimensions; Type=List<string>; Description=Gets or sets the available dimension names for mapping.}.Type) | Gets or sets the available dimension names for mapping. |
| $(@{Name=Damping; Type=double; Description=Gets or sets the damping factor for the physics simulation.}.Name) | $(@{Name=Damping; Type=double; Description=Gets or sets the damping factor for the physics simulation.}.Type) | Gets or sets the damping factor for the physics simulation. |

---

## PDGraphInfo

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=SplitDirection; Type=SplitDirection; Description=Gets or sets the split direction for the controls and selection info panels.}.Name) | $(@{Name=SplitDirection; Type=SplitDirection; Description=Gets or sets the split direction for the controls and selection info panels.}.Type) | Gets or sets the split direction for the controls and selection info panels. |
| $(@{Name=ShowControls; Type=bool; Description=Gets or sets whether to show the controls panel.}.Name) | $(@{Name=ShowControls; Type=bool; Description=Gets or sets whether to show the controls panel.}.Type) | Gets or sets whether to show the controls panel. |
| $(@{Name=ReadOnlyControls; Type=bool; Description=Gets or sets whether the controls are read-only.}.Name) | $(@{Name=ReadOnlyControls; Type=bool; Description=Gets or sets whether the controls are read-only.}.Type) | Gets or sets whether the controls are read-only. |
| $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration.}.Name) | $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration.}.Type) | Gets or sets the visualization configuration. |
| $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Name) | $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Type) | Gets or sets the clustering configuration. |
| $(@{Name=SelectedNode; Type=GraphNode?; Description=Gets or sets the currently selected node.}.Name) | $(@{Name=SelectedNode; Type=GraphNode?; Description=Gets or sets the currently selected node.}.Type) | Gets or sets the currently selected node. |
| $(@{Name=SelectedEdge; Type=GraphEdge?; Description=Gets or sets the currently selected edge.}.Name) | $(@{Name=SelectedEdge; Type=GraphEdge?; Description=Gets or sets the currently selected edge.}.Type) | Gets or sets the currently selected edge. |

---

## PDGraphSelectionInfo

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=SelectedNode; Type=GraphNode?; Description=Gets or sets the currently selected node.}.Name) | $(@{Name=SelectedNode; Type=GraphNode?; Description=Gets or sets the currently selected node.}.Type) | Gets or sets the currently selected node. |
| $(@{Name=SelectedEdge; Type=GraphEdge?; Description=Gets or sets the currently selected edge.}.Name) | $(@{Name=SelectedEdge; Type=GraphEdge?; Description=Gets or sets the currently selected edge.}.Type) | Gets or sets the currently selected edge. |

---

## PDGraphViewer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=DataProvider; Type=IDataProviderService<GraphData>?; Description=Gets or sets the data provider for the graph data.}.Name) | $(@{Name=DataProvider; Type=IDataProviderService<GraphData>?; Description=Gets or sets the data provider for the graph data.}.Type) | Gets or sets the data provider for the graph data. |
| $(@{Name=SplitDirection; Type=SplitDirection; Description=Gets or sets the split panel direction (Horizontal or Vertical).}.Name) | $(@{Name=SplitDirection; Type=SplitDirection; Description=Gets or sets the split panel direction (Horizontal or Vertical).}.Type) | Gets or sets the split panel direction (Horizontal or Vertical). |
| $(@{Name=ShowInfo; Type=bool; Description=Gets or sets whether to show the information panel.}.Name) | $(@{Name=ShowInfo; Type=bool; Description=Gets or sets whether to show the information panel.}.Type) | Gets or sets whether to show the information panel. |
| $(@{Name=ShowControls; Type=bool; Description=Gets or sets whether to show the controls panel within the info panel.}.Name) | $(@{Name=ShowControls; Type=bool; Description=Gets or sets whether to show the controls panel within the info panel.}.Type) | Gets or sets whether to show the controls panel within the info panel. |
| $(@{Name=ReadOnlyControls; Type=bool; Description=Gets or sets whether the controls are read-only.}.Name) | $(@{Name=ReadOnlyControls; Type=bool; Description=Gets or sets whether the controls are read-only.}.Type) | Gets or sets whether the controls are read-only. |
| $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration for mapping dimensions to visual properties.}.Name) | $(@{Name=VisualizationConfig; Type=GraphVisualizationConfig; Description=Gets or sets the visualization configuration for mapping dimensions to visual properties.}.Type) | Gets or sets the visualization configuration for mapping dimensions to visual properties. |
| $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Name) | $(@{Name=ClusteringConfig; Type=GraphClusteringConfig; Description=Gets or sets the clustering configuration.}.Type) | Gets or sets the clustering configuration. |
| $(@{Name=ConvergenceThreshold; Type=double; Description=Gets or sets the convergence threshold for the physics simulation.}.Name) | $(@{Name=ConvergenceThreshold; Type=double; Description=Gets or sets the convergence threshold for the physics simulation.}.Type) | Gets or sets the convergence threshold for the physics simulation. |
| $(@{Name=Damping; Type=double; Description=Gets or sets the damping factor for the physics simulation. Higher values mean faster settling.}.Name) | $(@{Name=Damping; Type=double; Description=Gets or sets the damping factor for the physics simulation. Higher values mean faster settling.}.Type) | Gets or sets the damping factor for the physics simulation. Higher values mean faster settling. |
| $(@{Name=NodeClick; Type=EventCallback<GraphNode>; Description=Gets or sets a callback that is invoked when a node is clicked.}.Name) | $(@{Name=NodeClick; Type=EventCallback<GraphNode>; Description=Gets or sets a callback that is invoked when a node is clicked.}.Type) | Gets or sets a callback that is invoked when a node is clicked. |
| $(@{Name=EdgeClick; Type=EventCallback<GraphEdge>; Description=Gets or sets a callback that is invoked when an edge is clicked.}.Name) | $(@{Name=EdgeClick; Type=EventCallback<GraphEdge>; Description=Gets or sets a callback that is invoked when an edge is clicked.}.Type) | Gets or sets a callback that is invoked when an edge is clicked. |

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
| $(@{Name=Click; Type=EventCallback<MouseEventArgs>; Description=}.Name) | $(@{Name=Click; Type=EventCallback<MouseEventArgs>; Description=}.Type) |  |
| $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Type) |  |
| $(@{Name=CssClass; Type=string; Description=}.Name) | $(@{Name=CssClass; Type=string; Description=}.Type) |  |
| $(@{Name=DataItem; Type=object?; Description=}.Name) | $(@{Name=DataItem; Type=object?; Description=}.Type) |  |
| $(@{Name=IconCssClass; Type=string; Description=}.Name) | $(@{Name=IconCssClass; Type=string; Description=}.Type) |  |
| $(@{Name=MouseDown; Type=EventCallback<MouseEventArgs>; Description=}.Name) | $(@{Name=MouseDown; Type=EventCallback<MouseEventArgs>; Description=}.Type) |  |
| $(@{Name=MouseEnter; Type=EventCallback<MouseEventArgs>; Description=}.Name) | $(@{Name=MouseEnter; Type=EventCallback<MouseEventArgs>; Description=}.Type) |  |
| $(@{Name=PreventDefault; Type=bool; Description=}.Name) | $(@{Name=PreventDefault; Type=bool; Description=}.Type) |  |
| $(@{Name=SelectedChanged; Type=EventCallback<ISelectable>; Description=}.Name) | $(@{Name=SelectedChanged; Type=EventCallback<ISelectable>; Description=}.Type) |  |
| $(@{Name=StopPropagation; Type=bool; Description=}.Name) | $(@{Name=StopPropagation; Type=bool; Description=}.Type) |  |
| $(@{Name=Text; Type=string; Description=}.Name) | $(@{Name=Text; Type=string; Description=}.Type) |  |
| $(@{Name=TextCssClass; Type=string; Description=}.Name) | $(@{Name=TextCssClass; Type=string; Description=}.Type) |  |
| $(@{Name=ToolTip; Type=string; Description=}.Name) | $(@{Name=ToolTip; Type=string; Description=}.Type) |  |

---

## PDLinkButton

This component has no public parameters.

---

## PDList

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=AllCheckBoxWhenPartial; Type=SelectionBehaviours; Description=}.Name) | $(@{Name=AllCheckBoxWhenPartial; Type=SelectionBehaviours; Description=}.Type) |  |
| $(@{Name=Apply; Type=EventCallback<Selection<TItem>>; Description=}.Name) | $(@{Name=Apply; Type=EventCallback<Selection<TItem>>; Description=}.Type) |  |
| $(@{Name=Cancel; Type=EventCallback; Description=}.Name) | $(@{Name=Cancel; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=ClearSelectionOnFilter; Type=bool; Description=}.Name) | $(@{Name=ClearSelectionOnFilter; Type=bool; Description=}.Type) |  |
| $(@{Name=DataProvider; Type=IDataProviderService<TItem>?; Description=}.Name) | $(@{Name=DataProvider; Type=IDataProviderService<TItem>?; Description=}.Type) |  |
| $(@{Name=DefaultToSelectAll; Type=bool; Description=}.Name) | $(@{Name=DefaultToSelectAll; Type=bool; Description=}.Type) |  |
| $(@{Name=ItemTemplate; Type=RenderFragment<TItem>?; Description=}.Name) | $(@{Name=ItemTemplate; Type=RenderFragment<TItem>?; Description=}.Type) |  |
| $(@{Name=Selection; Type=Selection<TItem>; Description=}.Name) | $(@{Name=Selection; Type=Selection<TItem>; Description=}.Type) |  |
| $(@{Name=SelectionChanged; Type=EventCallback<Selection<TItem>>; Description=}.Name) | $(@{Name=SelectionChanged; Type=EventCallback<Selection<TItem>>; Description=}.Type) |  |
| $(@{Name=SelectionMode; Type=TableSelectionMode; Description=}.Name) | $(@{Name=SelectionMode; Type=TableSelectionMode; Description=}.Type) |  |
| $(@{Name=ShowAllCheckBox; Type=bool; Description=}.Name) | $(@{Name=ShowAllCheckBox; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowApplyCancelButtons; Type=bool; Description=}.Name) | $(@{Name=ShowApplyCancelButtons; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowCheckBoxes; Type=bool; Description=}.Name) | $(@{Name=ShowCheckBoxes; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowFilter; Type=bool; Description=}.Name) | $(@{Name=ShowFilter; Type=bool; Description=}.Type) |  |
| $(@{Name=SortDirection; Type=SortDirection; Description=}.Name) | $(@{Name=SortDirection; Type=SortDirection; Description=}.Type) |  |

---

## PDLocalStorageStateManager

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Type) |  |

---

## PDLog

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=LogLevel; Type=LogLevel; Description=}.Name) | $(@{Name=LogLevel; Type=LogLevel; Description=}.Type) |  |
| $(@{Name=Capacity; Type=int; Description=}.Name) | $(@{Name=Capacity; Type=int; Description=}.Type) |  |
| $(@{Name=Rows; Type=int; Description=}.Name) | $(@{Name=Rows; Type=int; Description=}.Type) |  |
| $(@{Name=ShowTimestamp; Type=bool; Description=}.Name) | $(@{Name=ShowTimestamp; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowIcon; Type=bool; Description=}.Name) | $(@{Name=ShowIcon; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowException; Type=bool; Description=}.Name) | $(@{Name=ShowException; Type=bool; Description=}.Type) |  |
| $(@{Name=UtcTimestampFormat; Type=string; Description=}.Name) | $(@{Name=UtcTimestampFormat; Type=string; Description=}.Type) |  |

---

## PDMenuItem

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Key; Type=string; Description=}.Name) | $(@{Name=Key; Type=string; Description=}.Type) |  |
| $(@{Name=Text; Type=string; Description=}.Name) | $(@{Name=Text; Type=string; Description=}.Type) |  |
| $(@{Name=IsVisible; Type=bool; Description=}.Name) | $(@{Name=IsVisible; Type=bool; Description=}.Type) |  |
| $(@{Name=IsDisabled; Type=bool; Description=}.Name) | $(@{Name=IsDisabled; Type=bool; Description=}.Type) |  |
| $(@{Name=IconCssClass; Type=string; Description=}.Name) | $(@{Name=IconCssClass; Type=string; Description=}.Type) |  |
| $(@{Name=Content; Type=string; Description=}.Name) | $(@{Name=Content; Type=string; Description=}.Type) |  |
| $(@{Name=IsSeparator; Type=bool; Description=}.Name) | $(@{Name=IsSeparator; Type=bool; Description=}.Type) |  |
| $(@{Name=ShortcutKey; Type=ShortcutKey; Description=}.Name) | $(@{Name=ShortcutKey; Type=ShortcutKey; Description=}.Type) |  |

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
| $(@{Name=Buttons; Type=List<ToolbarItem>; Description=Sets the buttons displayed in the modal dialog footer.}.Name) | $(@{Name=Buttons; Type=List<ToolbarItem>; Description=Sets the buttons displayed in the modal dialog footer.}.Type) | Sets the buttons displayed in the modal dialog footer. |

---

## PDMonacoEditor

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Id; Type=string; Description=}.Name) | $(@{Name=Id; Type=string; Description=}.Type) |  |
| $(@{Name=Language; Type=string; Description=}.Name) | $(@{Name=Language; Type=string; Description=}.Type) |  |
| $(@{Name=ShowSuggestions; Type=bool; Description=}.Name) | $(@{Name=ShowSuggestions; Type=bool; Description=}.Type) |  |
| $(@{Name=Theme; Type=string; Description=}.Name) | $(@{Name=Theme; Type=string; Description=}.Type) |  |
| $(@{Name=Value; Type=string; Description=}.Name) | $(@{Name=Value; Type=string; Description=}.Type) |  |
| $(@{Name=ValueChanged; Type=EventCallback<string>; Description=}.Name) | $(@{Name=ValueChanged; Type=EventCallback<string>; Description=}.Type) |  |
| $(@{Name=UpdateValueOnBlur; Type=bool; Description=}.Name) | $(@{Name=UpdateValueOnBlur; Type=bool; Description=}.Type) |  |
| $(@{Name=InitializeCache; Type=Action<MethodCache>?; Description=}.Name) | $(@{Name=InitializeCache; Type=Action<MethodCache>?; Description=}.Type) |  |
| $(@{Name=InitializeOptions; Type=Action<StandaloneEditorConstructionOptions>?; Description=}.Name) | $(@{Name=InitializeOptions; Type=Action<StandaloneEditorConstructionOptions>?; Description=}.Type) |  |
| $(@{Name=InitializeLanguage; Type=Action<Language>?; Description=}.Name) | $(@{Name=InitializeLanguage; Type=Action<Language>?; Description=}.Type) |  |
| $(@{Name=RegisterLanguages; Type=Action<List<Language>>?; Description=}.Name) | $(@{Name=RegisterLanguages; Type=Action<List<Language>>?; Description=}.Type) |  |
| $(@{Name=SelectionChanged; Type=EventCallback<Selection>; Description=}.Name) | $(@{Name=SelectionChanged; Type=EventCallback<Selection>; Description=}.Type) |  |

---

## PDNavLink

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=ActiveClass; Type=string?; Description=Gets or sets the CSS class name applied to the NavLink when the current route matches the NavLink href.}.Name) | $(@{Name=ActiveClass; Type=string?; Description=Gets or sets the CSS class name applied to the NavLink when the current route matches the NavLink href.}.Type) | Gets or sets the CSS class name applied to the NavLink when the current route matches the NavLink href. |
| $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Name) | $(@{Name=ChildContent; Type=RenderFragment?; Description=}.Type) |  |
| $(@{Name=Match; Type=NavLinkMatch; Description=Gets or sets a value representing the URL matching behavior.}.Name) | $(@{Name=Match; Type=NavLinkMatch; Description=Gets or sets a value representing the URL matching behavior.}.Type) | Gets or sets a value representing the URL matching behavior. |

---

## PDPager

This component has no public parameters.

---

## PDProgressBar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=BarContent; Type=RenderFragment<PDProgressBar>?; Description=}.Name) | $(@{Name=BarContent; Type=RenderFragment<PDProgressBar>?; Description=}.Type) |  |
| $(@{Name=DecimalPlaces; Type=ushort; Description=}.Name) | $(@{Name=DecimalPlaces; Type=ushort; Description=}.Type) |  |
| $(@{Name=Height; Type=string; Description=}.Name) | $(@{Name=Height; Type=string; Description=}.Type) |  |
| $(@{Name=Total; Type=double; Description=}.Name) | $(@{Name=Total; Type=double; Description=}.Type) |  |
| $(@{Name=Value; Type=double; Description=}.Name) | $(@{Name=Value; Type=double; Description=}.Type) |  |

---

## PDQuestVisualizer

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Quests; Type=List<Quest>; Description=}.Name) | $(@{Name=Quests; Type=List<Quest>; Description=}.Type) |  |
| $(@{Name=QuestActions; Type=List<QuestAction>; Description=}.Name) | $(@{Name=QuestActions; Type=List<QuestAction>; Description=}.Type) |  |
| $(@{Name=QuestHeight; Type=int; Description=}.Name) | $(@{Name=QuestHeight; Type=int; Description=}.Type) |  |
| $(@{Name=QuestMargin; Type=int; Description=}.Name) | $(@{Name=QuestMargin; Type=int; Description=}.Type) |  |
| $(@{Name=QuestActionRadius; Type=int; Description=}.Name) | $(@{Name=QuestActionRadius; Type=int; Description=}.Type) |  |

---

## PDRange

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Height; Type=double; Description=}.Name) | $(@{Name=Height; Type=double; Description=}.Type) |  |
| $(@{Name=Invert; Type=bool; Description=}.Name) | $(@{Name=Invert; Type=bool; Description=}.Type) |  |
| $(@{Name=Options; Type=RangeOptions; Description=}.Name) | $(@{Name=Options; Type=RangeOptions; Description=}.Type) |  |
| $(@{Name=Range; Type=NumericRange; Description=}.Name) | $(@{Name=Range; Type=NumericRange; Description=}.Type) |  |
| $(@{Name=ShowLabels; Type=bool; Description=}.Name) | $(@{Name=ShowLabels; Type=bool; Description=}.Type) |  |
| $(@{Name=TickMajor; Type=double; Description=}.Name) | $(@{Name=TickMajor; Type=double; Description=}.Type) |  |
| $(@{Name=Max; Type=double; Description=}.Name) | $(@{Name=Max; Type=double; Description=}.Type) |  |
| $(@{Name=Min; Type=double; Description=}.Name) | $(@{Name=Min; Type=double; Description=}.Type) |  |
| $(@{Name=MinGap; Type=double; Description=}.Name) | $(@{Name=MinGap; Type=double; Description=}.Type) |  |
| $(@{Name=RangeChanged; Type=EventCallback<NumericRange>; Description=}.Name) | $(@{Name=RangeChanged; Type=EventCallback<NumericRange>; Description=}.Type) |  |
| $(@{Name=Step; Type=double; Description=}.Name) | $(@{Name=Step; Type=double; Description=}.Type) |  |
| $(@{Name=TrackHeight; Type=double; Description=}.Name) | $(@{Name=TrackHeight; Type=double; Description=}.Type) |  |
| $(@{Name=Width; Type=double; Description=}.Name) | $(@{Name=Width; Type=double; Description=}.Type) |  |

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
| $(@{Name=DateFormat; Type=string; Description=}.Name) | $(@{Name=DateFormat; Type=string; Description=}.Type) |  |
| $(@{Name=DataPoint; Type=DataPoint; Description=}.Name) | $(@{Name=DataPoint; Type=DataPoint; Description=}.Type) |  |
| $(@{Name=Height; Type=double; Description=}.Name) | $(@{Name=Height; Type=double; Description=}.Type) |  |
| $(@{Name=IsEnabled; Type=bool; Description=}.Name) | $(@{Name=IsEnabled; Type=bool; Description=}.Type) |  |
| $(@{Name=MaxValue; Type=double; Description=}.Name) | $(@{Name=MaxValue; Type=double; Description=}.Type) |  |
| $(@{Name=Options; Type=TimelineOptions; Description=}.Name) | $(@{Name=Options; Type=TimelineOptions; Description=}.Type) |  |
| $(@{Name=X; Type=double; Description=}.Name) | $(@{Name=X; Type=double; Description=}.Type) |  |

---

## PDStudio

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=DataProvider; Type=IDataProviderService<GraphData>?; Description=Gets or sets the data provider for the graph data.}.Name) | $(@{Name=DataProvider; Type=IDataProviderService<GraphData>?; Description=Gets or sets the data provider for the graph data.}.Type) | Gets or sets the data provider for the graph data. |

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
| $(@{Name=Table; Type=PDTable<TItem>?; Description=}.Name) | $(@{Name=Table; Type=PDTable<TItem>?; Description=}.Type) |  |
| $(@{Name=CanChangeOrder; Type=bool; Description=}.Name) | $(@{Name=CanChangeOrder; Type=bool; Description=}.Type) |  |
| $(@{Name=CanChangeVisible; Type=bool; Description=}.Name) | $(@{Name=CanChangeVisible; Type=bool; Description=}.Type) |  |

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
| $(@{Name=DisableAfter; Type=DateTime; Description=}.Name) | $(@{Name=DisableAfter; Type=DateTime; Description=}.Type) |  |
| $(@{Name=DisableBefore; Type=DateTime; Description=}.Name) | $(@{Name=DisableBefore; Type=DateTime; Description=}.Type) |  |
| $(@{Name=Initialized; Type=EventCallback; Description=}.Name) | $(@{Name=Initialized; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=IsEnabled; Type=bool; Description=}.Name) | $(@{Name=IsEnabled; Type=bool; Description=}.Type) |  |
| $(@{Name=Scale; Type=TimelineScale; Description=}.Name) | $(@{Name=Scale; Type=TimelineScale; Description=}.Type) |  |
| $(@{Name=ScaleChanged; Type=EventCallback<TimelineScale>; Description=}.Name) | $(@{Name=ScaleChanged; Type=EventCallback<TimelineScale>; Description=}.Type) |  |
| $(@{Name=Refreshed; Type=EventCallback; Description=}.Name) | $(@{Name=Refreshed; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=SelectionChanged; Type=EventCallback<TimeRange?>; Description=}.Name) | $(@{Name=SelectionChanged; Type=EventCallback<TimeRange?>; Description=}.Type) |  |
| $(@{Name=SelectionChangeEnd; Type=EventCallback; Description=}.Name) | $(@{Name=SelectionChangeEnd; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=DataProvider; Type=DataProviderDelegate?; Description=}.Name) | $(@{Name=DataProvider; Type=DataProviderDelegate?; Description=}.Type) |  |
| $(@{Name=Id; Type=string; Description=}.Name) | $(@{Name=Id; Type=string; Description=}.Type) |  |
| $(@{Name=NewMaxDateTimeAvailable; Type=bool; Description=}.Name) | $(@{Name=NewMaxDateTimeAvailable; Type=bool; Description=}.Type) |  |
| $(@{Name=NewMinDateTimeAvailable; Type=bool; Description=}.Name) | $(@{Name=NewMinDateTimeAvailable; Type=bool; Description=}.Type) |  |
| $(@{Name=MaxDateTime; Type=DateTime?; Description=}.Name) | $(@{Name=MaxDateTime; Type=DateTime?; Description=}.Type) |  |
| $(@{Name=MinDateTime; Type=DateTime; Description=}.Name) | $(@{Name=MinDateTime; Type=DateTime; Description=}.Type) |  |
| $(@{Name=Options; Type=TimelineOptions; Description=}.Name) | $(@{Name=Options; Type=TimelineOptions; Description=}.Type) |  |
| $(@{Name=UpdateMaxDate; Type=EventCallback; Description=}.Name) | $(@{Name=UpdateMaxDate; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=UpdateMinDate; Type=EventCallback; Description=}.Name) | $(@{Name=UpdateMinDate; Type=EventCallback; Description=}.Type) |  |
| $(@{Name=OffsetX; Type=int; Description=}.Name) | $(@{Name=OffsetX; Type=int; Description=}.Type) |  |

---

## PDTimelineToolbar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=IsEnabled; Type=bool; Description=}.Name) | $(@{Name=IsEnabled; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowRange; Type=bool; Description=}.Name) | $(@{Name=ShowRange; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowScale; Type=bool; Description=}.Name) | $(@{Name=ShowScale; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowSelection; Type=bool; Description=}.Name) | $(@{Name=ShowSelection; Type=bool; Description=}.Type) |  |
| $(@{Name=ShowZoomButtons; Type=bool; Description=}.Name) | $(@{Name=ShowZoomButtons; Type=bool; Description=}.Type) |  |
| $(@{Name=Timeline; Type=PDTimeline?; Description=}.Name) | $(@{Name=Timeline; Type=PDTimeline?; Description=}.Type) |  |

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
| $(@{Name=CloseOption; Type=CloseOptions; Description=}.Name) | $(@{Name=CloseOption; Type=CloseOptions; Description=}.Type) |  |
| $(@{Name=ShowOnMouseEnter; Type=bool; Description=}.Name) | $(@{Name=ShowOnMouseEnter; Type=bool; Description=}.Type) |  |

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
| $(@{Name=Errors; Type=object?; Description=}.Name) | $(@{Name=Errors; Type=object?; Description=}.Type) |  |

---

## PDZoomBar

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| $(@{Name=Id; Type=string; Description=}.Name) | $(@{Name=Id; Type=string; Description=}.Type) |  |
| $(@{Name=Options; Type=ZoomBarOptions; Description=}.Name) | $(@{Name=Options; Type=ZoomBarOptions; Description=}.Type) |  |
| $(@{Name=Value; Type=ZoombarValue; Description=}.Name) | $(@{Name=Value; Type=ZoombarValue; Description=}.Type) |  |
| $(@{Name=ValueChanged; Type=EventCallback<ZoombarValue>; Description=}.Name) | $(@{Name=ValueChanged; Type=EventCallback<ZoombarValue>; Description=}.Type) |  |
| $(@{Name=Width; Type=int; Description=}.Name) | $(@{Name=Width; Type=int; Description=}.Type) |  |

---

