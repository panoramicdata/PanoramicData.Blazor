using BlazorMonaco;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Shared
{
	public partial class DemoSourceView
	{
		private string ActiveTab { get; set; } = "Demo";
		private const string _sourceBaseUrl = "https://raw.githubusercontent.com/panoramicdata/PanoramicData.Blazor/main/PanoramicData.Blazor.Demo";
		private readonly HttpClient _httpClient = new();
		private readonly Dictionary<string, SourceFile> _sourceFiles = new();
		private string _activeSourceFile = string.Empty;
		private MonacoEditor Editor { get; set; } = null!;

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
			if (firstRender)
			{
				foreach (var sourceFile in SourceFiles.Split(',', StringSplitOptions.RemoveEmptyEntries))
				{
					var trimmedSourceFile = sourceFile.Trim();
					var name = trimmedSourceFile[(trimmedSourceFile.LastIndexOf('/') + 1)..];
					_sourceFiles.Add(name, new SourceFile
					{
						Name = name,
						Url = GetUrl(trimmedSourceFile)
					});
					if (string.IsNullOrWhiteSpace(_activeSourceFile))
					{
						_activeSourceFile = name;
					}
				}
				// add _Host.cshtml to every page
				_sourceFiles.Add("_Host.cshtml", new SourceFile
				{
					Name = "_Host.cshtml",
					Url = "https://raw.githubusercontent.com/panoramicdata/PanoramicData.Blazor/main/PanoramicData.Blazor.Web/Pages/_Host.cshtml"
				});
				// load first source file
				if (!string.IsNullOrWhiteSpace(_activeSourceFile))
				{
					var entry = _sourceFiles[_activeSourceFile];
					try
					{
						entry.Content = await _httpClient.GetStringAsync(entry.Url).ConfigureAwait(true);
					}
					catch
					{
					}
				}
			}
		}

		private string SourceCode
		{
			get
			{
				if (_sourceFiles.ContainsKey(_activeSourceFile))
				{
					return _sourceFiles[_activeSourceFile].Content;
				}
				return "";
			}
		}

		public string GetUrl(string url)
		{
			return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : $"{_sourceBaseUrl}/{url}";
		}

		private async Task<string> LoadSourceAsync(string url)
		{
			try
			{
				return await _httpClient.GetStringAsync(GetUrl(url)).ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				return $"Failed to load source: {ex.Message}";
			}
		}

		private async Task OnFileClick(string name)
		{
			if (_sourceFiles.ContainsKey(name))
			{
				var sourceFile = _sourceFiles[name];
				if (string.IsNullOrWhiteSpace(sourceFile.Content))
				{
					sourceFile.Content = await LoadSourceAsync(sourceFile.Url).ConfigureAwait(true);
				}
				var extnChanged = System.IO.Path.GetExtension(name) != System.IO.Path.GetExtension(_activeSourceFile);
				_activeSourceFile = name;

				await Editor.SetValue(SourceCode).ConfigureAwait(true);

				if (extnChanged)
				{
					var model = await Editor.GetModel().ConfigureAwait(true);
					await MonacoEditor.SetModelLanguage(model, GetLanguageForFile(_activeSourceFile)).ConfigureAwait(true);
				}
			}
		}

		private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor _)
		{
			return new StandaloneEditorConstructionOptions
			{
				AutomaticLayout = true,
				Language = GetLanguageForFile(_activeSourceFile),
				Value = SourceCode,
				ReadOnly = true
			};
		}

		private string GetLanguageForFile(string filename)
		{
			return System.IO.Path.GetExtension(filename) switch
			{
				".cs" => "csharp",
				".css" => "css",
				".html" => "html",
				".cshtml" => "razor",
				".razor" => "razor",
				_ => "csharp"
			};
		}
	}
}
