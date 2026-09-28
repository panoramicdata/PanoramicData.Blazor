using AwesomeAssertions;
using Microsoft.Extensions.Primitives;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="UriExtensions"/>.</summary>
public class UriExtensionsTests
{
	/// <summary>New parameters are added to the path's query string.</summary>
	[Fact]
	public void GetQueryString_AddsParameters()
	{
		var uri = new Uri("https://example.com/items");

		var result = uri.GetQueryString(new Dictionary<string, StringValues> { ["page"] = "2" });

		result.Should().Be("/items?page=2");
	}

	/// <summary>Existing parameters are kept, and a supplied parameter replaces one with the same name.</summary>
	[Fact]
	public void GetQueryString_ReplacesAndKeepsParameters()
	{
		var uri = new Uri("https://example.com/items?page=1&sort=name");

		var result = uri.GetQueryString(new Dictionary<string, StringValues> { ["page"] = "3" });

		result.Should().Be("/items?page=3&sort=name");
	}

	/// <summary>A multi-valued parameter is written once per value.</summary>
	[Fact]
	public void GetQueryString_WritesEachValue()
	{
		var uri = new Uri("https://example.com/");

		var result = uri.GetQueryString(new Dictionary<string, StringValues> { ["tag"] = new StringValues(["a", "b"]) });

		result.Should().Be("/?tag=a&tag=b");
	}
}
