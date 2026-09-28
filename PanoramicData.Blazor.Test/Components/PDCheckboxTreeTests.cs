using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDCheckboxTree{TItem}"/> puts a checkbox on every tree node and reports the checked
/// set as node keys.
/// </summary>
public class PDCheckboxTreeTests : BunitContext
{
	private readonly ListDataProviderService<Folder> _provider = new(
	[
		new("root", null, "Root"),
		new("a", "root", "Alpha"),
		new("b", "root", "Bravo")
	]);

	/// <summary>Sets up the rendering context.</summary>
	public PDCheckboxTreeTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDCheckboxTree<Folder>> RenderPicker(Action<ComponentParameterCollectionBuilder<PDCheckboxTree<Folder>>>? configure = null)
	{
		var component = Render<PDCheckboxTree<Folder>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.KeyField, f => f.Id)
				.Add(p => p.ParentKeyField, f => f.ParentId ?? string.Empty)
				.Add(p => p.TextField, f => f.Name)
				.Add(p => p.CssClass, "picker");
			configure?.Invoke(parameters);
		});

		component.WaitForAssertion(() => component.FindAll("input.pdcheckboxtree-checkbox").Should().NotBeEmpty());
		component.WaitForState(() => component.Instance.Tree is not null);
		return component;
	}

	private static void ExpandRoot(IRenderedComponent<PDCheckboxTree<Folder>> component)
	{
		component.Find("i.fa-plus-square.pd-pointer").Click();
		component.WaitForAssertion(() => component.FindAll("input.pdcheckboxtree-checkbox").Should().HaveCount(3));
	}

	private static IElement Checkbox(IRenderedComponent<PDCheckboxTree<Folder>> component, string text)
		=> component.FindAll("label.pdcheckboxtree-item")
			.Single(l => l.QuerySelector(".pdcheckboxtree-text")!.TextContent == text)
			.QuerySelector("input")!;

	/// <summary>
	/// Verifies that every node gets a checkbox and its text, inside the identified container.
	/// </summary>
	[Fact]
	public void EveryNode_GetsACheckboxAndItsText()
	{
		var component = RenderPicker();
		ExpandRoot(component);

		component.Find("div.pdcheckboxtree").ClassList.Should().Contain("picker");
		component.FindAll(".pdcheckboxtree-text").Select(s => s.TextContent).Should().Equal("Root", "Alpha", "Bravo");
	}

	/// <summary>
	/// Verifies that the keys passed in are rendered checked and the others unchecked.
	/// </summary>
	[Fact]
	public void CheckedKeys_AreRenderedChecked()
	{
		var component = RenderPicker(p => p.Add(x => x.CheckedKeys, ["a"]));
		ExpandRoot(component);

		Checkbox(component, "Alpha").HasAttribute("checked").Should().BeTrue();
		Checkbox(component, "Bravo").HasAttribute("checked").Should().BeFalse();
	}

	/// <summary>
	/// Verifies that checking and unchecking a box updates the checked keys and raises the change each time.
	/// </summary>
	[Fact]
	public void CheckingAndUnchecking_UpdatesAndRaisesTheCheckedKeys()
	{
		var raised = new List<List<string>>();
		var component = RenderPicker(p => p
			.Add(x => x.CheckedKeys, ["a"])
			.Add(x => x.CheckedKeysChanged, keys => raised.Add([.. keys])));
		ExpandRoot(component);

		Checkbox(component, "Bravo").Change(true);
		Checkbox(component, "Alpha").Change(false);

		raised.Should().HaveCount(2);
		raised[0].Should().BeEquivalentTo(["a", "b"]);
		raised[1].Should().BeEquivalentTo(["b"]);
		component.Instance.CheckedKeys.Should().BeEquivalentTo(["b"]);
	}

	/// <summary>
	/// Verifies that <see cref="PDCheckboxTree{TItem}.IsCheckDisabled"/> disables only the matching checkbox.
	/// </summary>
	[Fact]
	public void IsCheckDisabled_DisablesTheMatchingCheckbox()
	{
		var component = RenderPicker(p => p.Add(x => x.IsCheckDisabled, f => f.Id == "b"));
		ExpandRoot(component);

		Checkbox(component, "Bravo").HasAttribute("disabled").Should().BeTrue();
		Checkbox(component, "Alpha").HasAttribute("disabled").Should().BeFalse();
		component.FindAll("label.pdcheckboxtree-item-disabled").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that an implied item is shown checked and disabled without its key being part of the checked set.
	/// </summary>
	[Fact]
	public void IsCheckImplied_ShowsCheckedAndDisabled_WithoutJoiningTheCheckedKeys()
	{
		var component = RenderPicker(p => p
			.Add(x => x.CheckedKeys, ["root"])
			.Add(x => x.IsCheckImplied, f => f.ParentId == "root"));
		ExpandRoot(component);

		var alpha = Checkbox(component, "Alpha");
		alpha.HasAttribute("checked").Should().BeTrue();
		alpha.HasAttribute("disabled").Should().BeTrue();
		component.Instance.CheckedKeys.Should().Equal("root");
	}

	/// <summary>
	/// Verifies that disabling the whole picker disables every checkbox.
	/// </summary>
	[Fact]
	public void Disabled_DisablesEveryCheckbox()
	{
		var component = RenderPicker(p => p.Add(x => x.IsEnabled, false));
		ExpandRoot(component);

		component.FindAll("input.pdcheckboxtree-checkbox").Should().OnlyContain(i => i.HasAttribute("disabled"));
	}

	/// <summary>
	/// Verifies that an icon function adds an icon for each item, given the item.
	/// </summary>
	[Fact]
	public void IconCssClass_RendersAnIconPerItem()
	{
		var component = RenderPicker(p => p.Add(x => x.IconCssClass, (f, _) => $"icon-{f.Id}"));
		ExpandRoot(component);

		component.FindAll("label.pdcheckboxtree-item i").Select(i => i.ClassName).Should().Equal("icon-root", "icon-a", "icon-b");
	}

	/// <summary>
	/// Verifies that an invisible picker renders nothing.
	/// </summary>
	[Fact]
	public void Invisible_RendersNothing()
	{
		var component = Render<PDCheckboxTree<Folder>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.IsVisible, false));

		component.Markup.Trim().Should().BeEmpty();
		component.Instance.Tree.Should().BeNull();
	}

	/// <summary>
	/// Verifies that a null set of checked keys is treated as none checked.
	/// </summary>
	[Fact]
	public void NullCheckedKeys_AreTreatedAsNone()
	{
		var component = RenderPicker(p => p.Add(x => x.CheckedKeys, (List<string>)null!));
		ExpandRoot(component);

		component.FindAll("input.pdcheckboxtree-checkbox").Should().OnlyContain(i => !i.HasAttribute("checked"));
	}

	/// <summary>A folder in the tree.</summary>
	/// <param name="Id">Key.</param>
	/// <param name="ParentId">Parent key, or null at the top.</param>
	/// <param name="Name">Display text.</param>
	public sealed record Folder(string Id, string? ParentId, string Name);
}
