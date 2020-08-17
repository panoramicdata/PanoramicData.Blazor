using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PanoramicData.Blazor.Web.Pages
{
    public partial class PDToolbarPage
    {
		private string _events = string.Empty;
		private bool _showButtons = true;
		private bool _enableButtons = true;

		public void OnOpen()
		{
			_events += $"Open Button Clicked{Environment.NewLine}";
		}

		public void OnRename()
		{
			_events += $"Rename Button Clicked{Environment.NewLine}";
		}

		public void OnDownload()
		{
			_events += $"Download Button Clicked{Environment.NewLine}";
		}
	}
}
