using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;
using System.Diagnostics;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDContextMenuPage
    {
		private bool _enabled = true;
		private Random _random = new Random(Environment.TickCount);
		private string _events = string.Empty;
		private List<MenuItem> _items = new List<MenuItem>();

		protected override void OnInitialized()
		{
			_items.Add(new MenuItem { Content = "<span style=\"font-size:0.8rem; font-weight: bold;color: #ffa500\">Please choose ... </span>", IsDisabled = true });
			_items.Add(new MenuItem { IsSeparator = true });
			_items.Add(new MenuItem { Text = "One", IconCssClass = "fas fa-dice-one" });
			_items.Add(new MenuItem { Text = "Two", IconCssClass = "fas fa-dice-two" });
			_items.Add(new MenuItem { Text = "Three", IconCssClass = "fas fa-dice-three" });
			_items.Add(new MenuItem { Text = "Four", IconCssClass = "fas fa-dice-four" });
			_items.Add(new MenuItem { Text = "Five", IconCssClass = "fas fa-dice-five" });
			_items.Add(new MenuItem { Text = "Six", IconCssClass = "fas fa-dice-six" });
			_items.Add(new MenuItem { IsSeparator = true });
			_items.Add(new MenuItem { Text = "Help", IconCssClass = "fas fa-question-circle" });
			_items.Add(new MenuItem { Text = "About", IsVisible = false });
			}

		public void BeforeShowHandler(CancelEventArgs args)
		{
			args.Cancel = !_enabled;
			_events += $"before show: {(_enabled ? "show = true" : "show = cancelled")} {Environment.NewLine}";
			// randomly disable one item
			_items.ForEach(x => x.IsDisabled = false);
			_items[_random.Next(2, 8)].IsDisabled = true;
		}
	}
}
