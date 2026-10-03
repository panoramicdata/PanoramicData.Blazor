using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tab renaming tests for <see cref="PDTabSet"/>.
/// </summary>
public partial class PDTabSetTests
{
	/// <summary>Double-clicking a renamable tab shows the rename box; Enter commits and raises OnTabRenamed.</summary>
	[Fact]
	public void Rename_with_enter_commits_the_new_title()
	{
		var renamed = new List<string>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabRenamingEnabled, true)
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.OnTabRenamed, (PDTab tab) => renamed.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Icon = "fas fa-1" })));

		component.Find("button.pdtabset-tab i.pdtabset-tab-icon").Should().NotBeNull();
		component.Find("button.pdtabset-tab .pdtabset-tab-close").Should().NotBeNull();
		component.Find("button.pdtabset-tab").DoubleClick();

		var input = component.Find("input.pdtabset-tab-rename-input");
		input.GetAttribute("value").Should().Be("One");
		input.Input("Renamed");
		input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

		renamed.Should().Equal("Renamed");
		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}

	/// <summary>Leaving the rename box commits the new title and ends the rename.</summary>
	[Fact]
	public void Rename_on_blur_commits_the_new_title()
	{
		var renamed = new List<string>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabRenamingEnabled, true)
			.Add(p => p.OnTabRenamed, (PDTab tab) => renamed.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.Find("button.pdtabset-tab").DoubleClick();
		var input = component.Find("input.pdtabset-tab-rename-input");
		input.Input("Blurred");
		input.Blur();

		renamed.Should().Equal("Blurred");
		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}

	/// <summary>A commit with no OnTabRenamed handler still ends the rename.</summary>
	[Fact]
	public void Rename_without_a_handler_ends_the_rename()
	{
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabRenamingEnabled, true)
			.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.Find("button.pdtabset-tab").DoubleClick();
		component.Find("input.pdtabset-tab-rename-input").Input("Other");
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}

	/// <summary>Escape abandons the rename, and committing an unchanged title raises nothing.</summary>
	[Fact]
	public void Rename_escape_and_unchanged_commit_raise_nothing()
	{
		var renamed = 0;
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.OnTabRenamed, (PDTab _) => renamed++)
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Renaming = true })));

		component.Find("button.pdtabset-tab").DoubleClick();
		component.Find("input.pdtabset-tab-rename-input").Input("Abandoned");
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "Escape" });
		component.Find(".pdtabset-tab-title").TextContent.Should().Be("One");

		component.Find("button.pdtabset-tab").DoubleClick();
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "a" });
		component.FindAll("input.pdtabset-tab-rename-input").Should().ContainSingle();
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		renamed.Should().Be(0);
		component.Find(".pdtabset-tab-title").TextContent.Should().Be("One");
	}

	/// <summary>An input event with no value clears the pending title.</summary>
	[Fact]
	public void Rename_input_with_null_value_clears_the_pending_title()
	{
		var component = Render<PDTabSet>(parameters => parameters.Add(p => p.ChildContent, Tabs(new Spec("One"))));
		var tab = component.FindComponent<PDTab>().Instance;

		PDTabSet.OnRenameTabInput(tab, new ChangeEventArgs { Value = null });

		tab.TempTitle.Should().BeEmpty();
	}

	/// <summary>A tab that cannot be renamed ignores a request to start renaming.</summary>
	[Fact]
	public async Task StartRenamingTab_is_ignored_when_renaming_is_disabled()
	{
		var component = Render<PDTabSet>(parameters => parameters.Add(p => p.ChildContent, Tabs(new Spec("One"))));
		var tab = component.FindComponent<PDTab>().Instance;

		await component.InvokeAsync(() => component.Instance.StartRenamingTab(tab));

		tab.IsRenaming.Should().BeFalse();
		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}
}
