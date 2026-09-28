using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="CachedResult{T}"/>.</summary>
public class CachedResultTests
{
	/// <summary>The constructor captures the key and result.</summary>
	[Fact]
	public void Constructor_CapturesKeyAndResult()
	{
		var result = new CachedResult<int>("answer", 42);

		result.Key.Should().Be("answer");
		result.Result.Should().Be(42);
	}

	/// <summary>A new result expires immediately, because its expiry defaults to the moment it was created.</summary>
	[Fact]
	public void New_HasExpired()
	{
		var result = new CachedResult<string>("k", "v");

		result.Expiry.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
		result.HasExpired.Should().BeTrue();
	}

	/// <summary>A result with a future expiry has not expired.</summary>
	[Fact]
	public void FutureExpiry_HasNotExpired()
	{
		var result = new CachedResult<string>("k", "v") { Expiry = DateTimeOffset.UtcNow.AddMinutes(5) };

		result.HasExpired.Should().BeFalse();
	}

	/// <summary>A result with a past expiry has expired.</summary>
	[Fact]
	public void PastExpiry_HasExpired()
	{
		var result = new CachedResult<string>("k", "v") { Expiry = DateTimeOffset.UtcNow.AddMinutes(-5) };

		result.HasExpired.Should().BeTrue();
	}
}
