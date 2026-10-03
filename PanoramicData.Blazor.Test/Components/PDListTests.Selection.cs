using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Click and check box selection tests for <see cref="PDList{TItem}"/>.
/// </summary>
public partial class PDListTests
{
	/// <summary>
	/// Verifies that in single selection a click selects one item, another click moves the selection, and
	/// clicking the selected item again changes nothing.
	/// </summary>
	[Fact]
	public void SingleSelection_SelectsOneItem()
	{
		var list = RenderList(TableSelectionMode.Single);

		Item(list, "Apple").Click();
		Item(list, "Banana").Click();
		Item(list, "Banana").Click();

		list.Instance.Selection.Items.Should().Equal(Banana);
		Item(list, "Banana").ClassList.Should().Contain("selected");
		_selections.Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that in single selection with checkboxes, clicking the checked item unchecks it.
	/// </summary>
	[Fact]
	public void SingleSelectionWithCheckBoxes_TogglesTheItem()
	{
		var list = RenderList(TableSelectionMode.Single, p => p.Add(x => x.ShowCheckBoxes, true));

		Item(list, "Apple").Click();
		CheckedItems(list).Should().Equal("Apple");
		Item(list, "Apple").Click();

		CheckedItems(list).Should().BeEmpty();
		list.Instance.Selection.Items.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that in multiple selection a plain click replaces the selection, a control click adds or
	/// removes an item, and a shift click selects the range from the last item clicked.
	/// </summary>
	[Fact]
	public void MultipleSelection_SupportsControlAndShiftClicks()
	{
		var list = RenderList(TableSelectionMode.Multiple);

		Item(list, "Apple").Click();
		Item(list, "Apple").Click();
		Item(list, "Cherry").Click(new MouseEventArgs { CtrlKey = true });
		list.Instance.Selection.Items.Should().Equal(Apple, Cherry);

		Item(list, "Apple").Click(new MouseEventArgs { CtrlKey = true });
		list.Instance.Selection.Items.Should().Equal(Cherry);

		Item(list, "Banana").Click(new MouseEventArgs { ShiftKey = true });
		list.Instance.Selection.Items.Should().Equal(Apple, Banana);

		Item(list, "Apple").Click();
		list.Instance.Selection.Items.Should().Equal(Apple);
	}

	/// <summary>
	/// Verifies that a shift range covering every item becomes an all selection.
	/// </summary>
	[Fact]
	public void MultipleSelection_ShiftRangeOfEverything_SelectsAll()
	{
		var list = RenderList(TableSelectionMode.Multiple);

		Item(list, "Cherry").Click();
		Item(list, "Apple").Click(new MouseEventArgs { ShiftKey = true });

		list.Instance.Selection.AllSelected.Should().BeTrue();
		list.Instance.Selection.Items.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that with checkboxes, checking every item becomes an all selection, and unchecking one from
	/// an all selection leaves the others checked.
	/// </summary>
	[Fact]
	public void MultipleCheckBoxes_ToggleToAndFromAll()
	{
		var list = RenderList(TableSelectionMode.Multiple, p => p.Add(x => x.ShowCheckBoxes, true));

		Item(list, "Apple").Click();
		Item(list, "Banana").Click();
		Item(list, "Cherry").Click();
		list.Instance.Selection.AllSelected.Should().BeTrue();
		CheckedItems(list).Should().Equal("Apple", "Banana", "Cherry");

		Item(list, "Banana").Click();

		list.Instance.Selection.AllSelected.Should().BeFalse();
		list.Instance.Selection.Items.Should().Equal(Apple, Cherry);
	}

	/// <summary>
	/// Verifies that the all checkbox selects everything from none, clears an all selection, and treats a
	/// partial selection according to <see cref="PDList{TItem}.AllCheckBoxWhenPartial"/>.
	/// </summary>
	[Theory]
	[InlineData(SelectionBehaviours.SelectAll, true)]
	[InlineData(SelectionBehaviours.ClearAll, false)]
	public void AllCheckBox_CyclesTheSelection(SelectionBehaviours whenPartial, bool partialSelectsAll)
	{
		var list = RenderList(TableSelectionMode.Multiple, p => p
			.Add(x => x.ShowCheckBoxes, true)
			.Add(x => x.ShowAllCheckBox, true)
			.Add(x => x.AllCheckBoxWhenPartial, whenPartial));
		IElement All() => list.FindAll("li.list-item")[0];

		All().TextContent.Trim().Should().Be("(All)");
		All().Click();
		list.Instance.Selection.AllSelected.Should().BeTrue();
		All().QuerySelector("i")!.ClassList.Should().Contain("fa-check-square");
		All().Click();
		list.Instance.Selection.AllSelected.Should().BeFalse();

		Item(list, "Apple").Click();
		All().QuerySelector("i")!.ClassList.Should().Contain("fa-minus-square");
		All().Click();

		list.Instance.Selection.AllSelected.Should().Be(partialSelectsAll);
		list.Instance.Selection.Items.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a disabled list ignores clicks and is styled as disabled.
	/// </summary>
	[Fact]
	public void Disabled_IgnoresClicks()
	{
		var list = RenderList(TableSelectionMode.Single, p => p.Add(x => x.IsEnabled, false).Add(x => x.IsVisible, false));

		Item(list, "Apple").Click();

		list.Instance.Selection.Items.Should().BeEmpty();
		list.Find("div.pd-list").ClassList.Should().Contain(["disabled", "d-none"]);
		_selections.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking an item in a list without selection clears any selection and raises nothing.
	/// </summary>
	[Fact]
	public void NoSelectionMode_ClickClearsAndRaisesNothing()
	{
		var list = RenderList(TableSelectionMode.None, p => p.Add(x => x.Selection, new Selection<Fruit> { Items = [Apple] }));

		Item(list, "Banana").Click();

		list.Instance.Selection.Items.Should().BeEmpty();
		_selections.Should().BeEmpty();
	}
}
