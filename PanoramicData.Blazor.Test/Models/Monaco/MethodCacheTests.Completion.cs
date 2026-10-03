using AwesomeAssertions;
using BlazorMonaco.Languages;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Test.Models.Monaco;

/// <summary>
/// Completion item and signature tests for <see cref="MethodCache"/>.
/// </summary>
public partial class MethodCacheTests
{
	/// <summary>Completion items list each method once, noting overloads and description.</summary>
	[Fact]
	public void GetCompletionItems_ListsMethods()
	{
		var cache = new MethodCache();
		cache.AddMethod(Lang, Method("Add", typeof(int), Param("a", typeof(int))));
		cache.AddMethod(Lang, Method("Add", typeof(double)));
		var described = Method("Sub", typeof(int));
		described.Description = "Subtracts";
		cache.AddMethod(Lang, described);

		var items = cache.GetCompletionItems(Lang, string.Empty).ToList();

		items.Select(i => i.LabelAsString).Should().Equal("Add", "Sub");
		items.Should().OnlyContain(i => i.Kind == CompletionItemKind.Function);
		items[0].DocumentationAsString.Should().StartWith("int Add(int a)").And.Contain("(+1 overloads)");
		items[1].DocumentationAsString.Should().Contain("Subtracts");
		items[1].InsertText.Should().Be("Sub");
	}

	/// <summary>Naming a function adds its first overload's parameters as property completions.</summary>
	[Fact]
	public void GetCompletionItems_ForFunction_AddsParameters()
	{
		var cache = new MethodCache();
		var add = Method("Add", typeof(int), Param("a", typeof(int)), Param("b", typeof(int), 1));
		add.Parameters[0].Description = "left";
		cache.AddMethod(Lang, add);

		var items = cache.GetCompletionItems(Lang, "Demo.Funcs.Add").ToList();

		items.Select(i => i.LabelAsString).Should().Equal("Add", "a", "b");
		items[1].Kind.Should().Be(CompletionItemKind.Property);
		items[1].DocumentationAsString.Should().Be("left");
		cache.GetCompletionItems("missing", string.Empty).Should().BeEmpty();
	}

	/// <summary>Signatures are returned for every overload matching the name, with parameter labels.</summary>
	[Fact]
	public void GetSignatures_ReturnsMatchingOverloads()
	{
		var cache = new MethodCache();
		cache.AddMethod(Lang, Method("Add", typeof(int), Param("a", typeof(int))));
		cache.AddMethod(Lang, Method("Add", typeof(double), Param("x", typeof(double))));
		cache.AddMethod(Lang, Method("Sub", typeof(int)));

		var signatures = cache.GetSignatures(Lang, "add").ToList();

		signatures.Select(s => s.Label).Should().Equal("int Add(int a)", "double Add(double x)");
		signatures[0].Parameters.Should().ContainSingle().Which.Label.Should().Be("int a");
		cache.GetSignatures(Lang, " ").Should().BeEmpty();
		cache.GetSignatures("missing", "Add").Should().BeEmpty();
	}
}
