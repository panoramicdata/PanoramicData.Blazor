﻿namespace PanoramicData.Blazor;

public partial class PDBusyOverlay
{
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	[Parameter]
	public bool IsBusy { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	[Parameter]
	public string OverlayCssClass { get; set; } = "fas fa-4x fa-spin fa-circle-notch";

	[Parameter]
	public RenderFragment? OverlayContent { get; set; }
}