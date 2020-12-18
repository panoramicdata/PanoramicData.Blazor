using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDDragDropSeparator
	{
		[CascadingParameter] public PDDragContext? DragContext { get; set; }

		[Parameter] public int Height { get; set; } = 3;

		[Parameter] public bool? Before { get; set; }

		[Parameter] public string CssClass { get; set; } = string.Empty;

		[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

		public bool DragOver { get; set; }

		private void OnDragOver()
		{
			DragOver = true;
		}

		private void OnDragLeave()
		{
			DragOver = false;
		}

		private async Task OnDropAsync(MouseEventArgs args)
		{
			DragOver = false;
			var dropArgs = new DropEventArgs(this, DragContext?.Payload, args.CtrlKey, Before);
			await Drop.InvokeAsync(dropArgs).ConfigureAwait(true);
		}
	}
}
