using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that an exception thrown by one of the consumer's callbacks raised from JavaScript is neither
/// propagated nor silently swallowed, but logged and passed to <see cref="PDDropDown.ExceptionHandler"/> (#198).
/// </summary>
public partial class PDDropDownTests
{
	private ListLogger<PDDropDown> UseRecordingLogger()
	{
		var logger = new ListLogger<PDDropDown>();
		Services.AddSingleton<ILogger<PDDropDown>>(logger);
		return logger;
	}

	/// <summary>Verifies that an exception thrown by DropDownShown is logged, and the caret still flips.</summary>
	[Fact]
	public async Task An_exception_thrown_by_DropDownShown_is_logged()
	{
		var logger = UseRecordingLogger();
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.DropDownShown, () => throw new InvalidOperationException("shown failed")));

		await component.InvokeAsync(component.Instance.OnDropDownShown);

		logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
			.Which.Exception.Should().BeOfType<InvalidOperationException>()
			.Which.Message.Should().Be("shown failed");
		component.Find("button i.fa-angle-up").Should().NotBeNull();
	}

	/// <summary>Verifies that an exception thrown by DropDownHidden is logged and passed to the ExceptionHandler.</summary>
	[Fact]
	public async Task An_exception_thrown_by_DropDownHidden_is_passed_to_the_exception_handler()
	{
		var logger = UseRecordingLogger();
		var handled = new List<Exception>();
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.DropDownHidden, () => throw new InvalidOperationException("hidden failed"))
			.Add(p => p.ExceptionHandler, (Exception ex) => handled.Add(ex)));
		await component.InvokeAsync(component.Instance.OnDropDownShown);

		await component.InvokeAsync(component.Instance.OnDropDownHidden);

		handled.Should().ContainSingle().Which.Message.Should().Be("hidden failed");
		logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error);
		component.Find("button i.fa-angle-down").Should().NotBeNull();
	}

	/// <summary>Verifies that an exception thrown by KeyPress is passed to the ExceptionHandler rather than propagated.</summary>
	[Fact]
	public async Task An_exception_thrown_by_KeyPress_is_passed_to_the_exception_handler()
	{
		var handled = new List<Exception>();
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.KeyPress, (int code) => throw new ArgumentOutOfRangeException(nameof(code), code, "bad key"))
			.Add(p => p.ExceptionHandler, (Exception ex) => handled.Add(ex)));

		var act = () => component.InvokeAsync(() => component.Instance.OnKeyPressed(27));

		await act.Should().NotThrowAsync();
		handled.Should().ContainSingle().Which.Should().BeOfType<ArgumentOutOfRangeException>();
	}

	/// <summary>Verifies that an ExceptionHandler which itself throws is logged too, and still not propagated.</summary>
	[Fact]
	public async Task An_exception_handler_that_throws_is_logged_and_not_propagated()
	{
		var logger = UseRecordingLogger();
		var component = Render<PDDropDown>(parameters => parameters
			.Add(p => p.KeyPress, (int code) => throw new InvalidOperationException($"key {code}"))
			.Add(p => p.ExceptionHandler, (Exception ex) => throw new InvalidOperationException("handler failed", ex)));

		var act = () => component.InvokeAsync(() => component.Instance.OnKeyPressed(13));

		await act.Should().NotThrowAsync();
		logger.Entries.Where(e => e.Level == LogLevel.Error).Select(e => e.Exception!.Message)
			.Should().Equal("key 13", "handler failed");
	}
}
