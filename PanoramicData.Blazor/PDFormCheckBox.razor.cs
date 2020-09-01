using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
    public partial class PDFormCheckBox
    {
        [Parameter] public bool Value { get; set; }

		[Parameter] public bool Disabled { get; set; }

		private void OnClick()
		{
			if(!Disabled)
			{
				Value = !Value;
			}
		}

		private void OnKeyPress(KeyboardEventArgs args)
		{
			if (!Disabled && args.Code.In("Space", "Enter"))
			{
				Value = !Value;
			}
		}
	}
}
