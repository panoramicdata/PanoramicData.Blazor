using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFileModal"/> opens in open and save modes, reflects the user's selection and
/// typed filename in its OK button, and reports the chosen path, confirming before an overwrite.
/// </summary>
public partial class PDFileModalTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	private readonly List<string> _results = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDFileModalTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>Verifies that button texts, titles and icons can be replaced.</summary>
	[Fact]
	public async Task Texts_and_icons_can_be_replaced()
	{
		var component = RenderModal(p => p
			.Add(x => x.OpenTitle, "Pick")
			.Add(x => x.OpenButtonText, "Choose")
			.Add(x => x.GetItemIconCssClass, item => $"icon-{item.Name}"));

		await ShowOpenAsync(component);

		component.Find(".modal-title").TextContent.Should().Be("Pick");
		OkButton(component).TextContent.Trim().Should().Be("Choose");
		Row(component, "/readme.txt").QuerySelector(".icon-readme\\.txt").Should().NotBeNull();
	}

	/// <summary>Verifies that refreshing asks the data provider for the folder contents again.</summary>
	[Fact]
	public async Task Refreshing_requests_the_contents_again()
	{
		var provider = new FileProvider();
		var component = RenderModal(provider: provider);
		await ShowOpenAsync(component);
		var before = provider.Requests;

		await component.InvokeAsync(component.Instance.RefreshFileExplorerAsync);

		provider.Requests.Should().BeGreaterThan(before);
	}

	private IRenderedComponent<PDFileModal> RenderModal(
		Action<ComponentParameterCollectionBuilder<PDFileModal>>? configure = null,
		FileProvider? provider = null)
		=> Render<PDFileModal>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, provider ?? new FileProvider())
				.Add(p => p.ModalHidden, (string result) => _results.Add(result));
			configure?.Invoke(parameters);
		});

	private static async Task ShowOpenAsync(IRenderedComponent<PDFileModal> component)
	{
		await component.InvokeAsync(() => component.Instance.ShowOpenAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);
	}

	/// <summary>Types a filename as a user does: the text box reports its value on key up.</summary>
	private static async Task TypeFilenameAsync(IRenderedComponent<PDFileModal> component, string text)
	{
		await FilenameBox(component).InputAsync(new() { Value = text });
		await FilenameBox(component).KeyUpAsync(new KeyboardEventArgs { Key = "a", Code = "KeyA" });
	}

	private static IElement Row(IRenderedComponent<PDFileModal> component, string path)
		=> component.Find($"tr.pdtablerow[id='{path}']");

	private static IElement Footer(IRenderedComponent<PDFileModal> component)
		=> component.Find($"#{component.Instance.Modal.Id} > .modal-dialog > .modal-content > .modal-footer");

	private static IElement FooterButton(IRenderedComponent<PDFileModal> component, string key)
		=> Footer(component).QuerySelector($"#pd-tbr-btn-{key}")!;

	private static IElement OkButton(IRenderedComponent<PDFileModal> component) => FooterButton(component, "OK");

	private static IElement FilenameBox(IRenderedComponent<PDFileModal> component)
		=> Footer(component).QuerySelector("input[type=text]")!;

	private static IElement ConfirmDialog(IRenderedComponent<PDFileModal> component)
		=> component.FindAll(".modal").First(m => m.QuerySelector(".modal-title")?.TextContent == "Confirm Overwrite");

	private static IElement ConfirmButton(IRenderedComponent<PDFileModal> component, string key)
		=> ConfirmDialog(component).QuerySelector($".modal-footer #pd-tbr-btn-{key}")!;
}
