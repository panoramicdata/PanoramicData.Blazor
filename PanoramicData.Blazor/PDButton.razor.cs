using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PanoramicData.Blazor
{
	public partial class PDButton : IDisposable
	{
		#region Inject
		[Inject] IGlobalEventService GlobalEventService { get; set; } = null!;
		#endregion

		/// <summary>
		/// Extra attributes to apply to the button.
		/// </summary>
		[Parameter] public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

		/// <summary>
		/// CSS Class for button.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = string.Empty;

		/// <summary>
		/// CSS Class for icon to be displayed on button.
		/// </summary>
		[Parameter] public string IconCssClass { get; set; } = string.Empty;


		/// <summary>
		/// Unique identifier for button.
		/// </summary>
		[Parameter] public string Id { get; set; } = string.Empty;

		/// <summary>
		/// Determines whether the button is enabled and can be clicked?
		/// </summary>
		[Parameter] public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Sets a callback for when user clicks button.
		/// </summary>
		[Parameter] public EventCallback<MouseEventArgs> Click { get; set; }

		/// <summary>
		/// Sets the short cut keys that will perform a click on this button.
		/// In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive)
		/// </summary>
		[Parameter] public string ShortcutKey { get; set; } = string.Empty;

		/// <summary>
		/// Sets the text displayed on the button.
		/// </summary>
		[Parameter] public string Text { get; set; } = string.Empty;

		/// <summary>
		/// CSS Class for text to be displayed on button.
		/// </summary>
		[Parameter] public string TextCssClass { get; set; } = string.Empty;

		/// <summary>
		/// Sets the text displayed on the buttons tooltip.
		/// </summary>
		[Parameter] public string ToolTip { get; set; } = string.Empty;

		protected override void OnInitialized()
		{
			GlobalEventService.KeyDownEvent += GlobalEventService_KeyUpEvent;
		}

		private string TextWithShortcutShown
		{
			get
			{
				if (string.IsNullOrEmpty(Text))
				{
					return Text;
				}
				var ampIdx = Text.IndexOf("&&");
				if (ampIdx == -1)
				{
					return Text;
				}
				var sb = new StringBuilder();
				sb.Append("<span>")
					.Append(Text.Substring(0, ampIdx))
					.Append("<u>")
					.Append(Text.Substring(ampIdx + 2, 1))
					.Append("</u>")
					.Append(Text.Substring(ampIdx + 3))
					.Append("</span>");
				return sb.ToString();
			}
		}

		private string ToolTipWithShortcutShown
		{
			get
			{
				if (string.IsNullOrEmpty(ToolTip) || string.IsNullOrWhiteSpace(ShortcutKey))
				{
					return ToolTip;
				}
				var sb = new StringBuilder();
				var temp = ShortcutKey.ToLower();
				for (var i = 0; i < temp.Length; i++)
				{
					if (sb.Length == 0 || sb[sb.Length - 1] == '-')
					{
						sb.Append(temp[i].ToString().ToUpper());
					}
					else
					{
						sb.Append(temp[i]);
					}
				}
				return $"{ToolTip.Replace("&&", "")} ({sb})";
			}
		}

		private async void GlobalEventService_KeyUpEvent(object sender, KeyboardInfo e)
		{
			if (!string.IsNullOrWhiteSpace(ShortcutKey))
			{
				var codes = ShortcutKey.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
				var ctrlKey = codes.Any(x => string.Equals(x, "ctrl", StringComparison.OrdinalIgnoreCase));
				var shiftKey = codes.Any(x => string.Equals(x, "shift", StringComparison.OrdinalIgnoreCase));
				var altKey = codes.Any(x => string.Equals(x, "alt", StringComparison.OrdinalIgnoreCase));
				var key = codes.LastOrDefault();
				if (key != null && e.AltKey == altKey && e.CtrlKey == ctrlKey && e.AltKey == altKey && e.Key.ToLower() == key.ToLower())
				{
					await InvokeAsync(async () => await Click.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true)).ConfigureAwait(true);
				}
			}
		}

		public void Dispose()
		{
			GlobalEventService.KeyUpEvent -= GlobalEventService_KeyUpEvent;
		}
	}
}
