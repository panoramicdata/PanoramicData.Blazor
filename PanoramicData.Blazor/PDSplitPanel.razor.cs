using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDSplitPanel
    {
		private static int _idSequence;

		/// <summary>
		/// The parent PDSplitter instance.
		/// </summary>
		[CascadingParameter(Name = "Splitter")]
		public PDSplitter Splitter { get; set; } = null!;

		/// <summary>
		/// Sets the default panel size.
		/// </summary>
		/// <remarks>The value is proportional to the sum of all panel sizes.</remarks>
		[Parameter] public int Size { get; set; } = 1;

		/// <summary>
		/// Sets the minimum panel size in pixels.
		/// </summary>
		[Parameter] public int MinSize { get; set; } = 100;

		/// <summary>
		/// Child HTML content.
		/// </summary>
		[Parameter] public RenderFragment ChildContent { get; set; } = null!;

		/// <summary>
		/// Gets the unique identifier of this panel.
		/// </summary>
		public string Id { get; private set; } = string.Empty;

		protected override void OnInitialized()
		{
			Id = $"pdsp{++_idSequence}";
			Splitter.AddPanel(this);
		}
	}
}
