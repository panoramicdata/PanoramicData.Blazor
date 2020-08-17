using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
    public partial class PDToolbar
    {
		/// <summary>
		/// Child HTML content.
		/// </summary>
		[Parameter] public RenderFragment ChildContent { get; set; } = null!;
	}
}
