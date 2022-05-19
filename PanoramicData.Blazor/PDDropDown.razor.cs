using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDDropDown : IDisposable
	{
		private static int _sequence;
		private bool _shown;
		private DotNetObjectReference<PDDropDown> _objRef = null!;

		public enum CloseOptions
		{
			Inside,
			Outside,
			InsideOrOutside,
			Manual
		}

		[Inject]
		public IJSRuntime JSRuntime { get; set; } = null!;

		[Parameter]
		public EventCallback<MouseEventArgs> Click { get; set; }

		[Parameter]
		public RenderFragment? ChildContent { get; set; }

		[Parameter]
		public CloseOptions CloseOption { get; set; } = CloseOptions.Outside;

		[Parameter]
		public string CssClass { get; set; } = string.Empty;

		[Parameter]
		public Directions DropdownDirection { get; set; } = Directions.Down;

		[Parameter]
		public EventCallback DropDownShown { get; set; }


		[Parameter]
		public bool IsEnabled { get; set; } = true;

		[Parameter]
		public string IconCssClass { get; set; } = string.Empty;

		[Parameter]
		public string Id { get; set; } = $"pd-dropdown-{++_sequence}";

		[Parameter]
		public EventCallback<int> KeyPress { get; set; }

		[Parameter]
		public bool PreventDefault { get; set; }

		[Parameter]
		public bool ShowCaret { get; set; } = true;

		[Parameter]
		public ButtonSizes Size { get; set; } = ButtonSizes.Medium;

		[Parameter]
		public bool StopPropagation { get; set; }

		[Parameter]
		public string Text { get; set; } = string.Empty;

		[Parameter]
		public string TextCssClass { get; set; } = string.Empty;

		[Parameter]
		public string ToolTip { get; set; } = string.Empty;

		[Parameter]
		public bool Visible { get; set; } = true;

		private Dictionary<string, object> Attributes
		{
			get
			{
				return new Dictionary<string, object>
				{
					{ "data-toggle", "dropdown" },
					{ "data-bs-toggle", "dropdown" },
					{ "data-bs-auto-close", CloseOption switch
					{
						CloseOptions.Inside  => "inside",
						CloseOptions.Outside  => "outside",
						CloseOptions.InsideOrOutside  => "true",
						_ => "false"
					} }
				};
			}
		}

		public void Dispose()
		{
			if (_objRef != null)
			{
				_objRef.Dispose();
			}
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_objRef = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.initializeDropDown", Id, _objRef).ConfigureAwait(true);
			}
		}

		[JSInvokable]
		public async Task OnDropDownShown()
		{
			_shown = true;
			await DropDownShown.InvokeAsync(null).ConfigureAwait(true);
			StateHasChanged();
		}

		[JSInvokable]
		public void OnDropDownHidden()
		{
			_shown = false;
			StateHasChanged();
		}

		[JSInvokable]
		public async Task OnKeyPressed(int keyCode)
		{
			await KeyPress.InvokeAsync(keyCode).ConfigureAwait(true);
		}

		private string ParentCssClasses
		{
			get
			{
				var dropdownPosition = DropdownDirection switch
				{

					Directions.Up => "dropup",
					Directions.Left => "dropstart",
					Directions.Right => "dropend",
					_ => "dropdown"
				};
				return $"{dropdownPosition} {(Visible ? "" : "d-none")}";
			}
		}

		public async Task ToggleAsync()
		{
			await JSRuntime.InvokeVoidAsync("panoramicData.toggleDropDown", Id).ConfigureAwait(true);
		}
	}
}
