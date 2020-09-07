using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDForm<TItem> where TItem : class
    {
		/// <summary>
		/// Gets or sets the child content that the drop zone wraps.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		/// <summary>
		/// Gets or sets the item being created / edited / deleted.
		/// </summary>
		[Parameter] public TItem? Item { get; set; }

		/// <summary>
		/// Gets or sets the current form mode.
		/// </summary>
		public FormModes Mode { get; private set; }

		public void SetMode(FormModes mode)
		{
			Mode = mode;
			StateHasChanged();
		}
	}
}
