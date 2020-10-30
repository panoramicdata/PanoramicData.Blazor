using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Demo.Shared
{
	public partial class DemoSourceView
    {
		private string ActiveTab { get; set; } = "Demo";
		private const string SourceBaseUrl = "https://raw.githubusercontent.com/panoramicdata/PanoramicData.Blazor/main/PanoramicData.Blazor.Demo/Pages";
		private HttpClient _httpClient = new HttpClient();
		private Dictionary<string, string> _sourceFiles = new Dictionary<string, string>();
		private string _activeSourceFile = string.Empty;

		/// <summary>
		/// Sets the child content that the drop zone wraps.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		/// <summary>
		/// Sets the source code pages used in the demo.
		/// </summary>
		[Parameter] public string SourceFiles { get; set; } = string.Empty;

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if(firstRender)
			{
				foreach (var sourceFile in SourceFiles.Split(',', StringSplitOptions.RemoveEmptyEntries))
				{
					var filename = sourceFile.Trim();
					var content = await LoadSourceAsync(filename).ConfigureAwait(true);
					_sourceFiles.Add(filename, content);
					if(string.IsNullOrWhiteSpace(_activeSourceFile))
					{
						_activeSourceFile = filename;
					}
				}
			}
		}

		private string SourceCode
		{
			get
			{
				if(_sourceFiles.ContainsKey(_activeSourceFile))
				{
					return _sourceFiles[_activeSourceFile];
				}
				return "";
			}
		}

		private async Task<string> LoadSourceAsync(string relativeUrl)
		{
			try
			{
				var url = $"{SourceBaseUrl}/{relativeUrl}";
				return await _httpClient.GetStringAsync(url).ConfigureAwait(true);
			}
			catch(Exception ex)
			{
				return $"Failed to load source: {ex.Message}";
			}
		}
	}
}
