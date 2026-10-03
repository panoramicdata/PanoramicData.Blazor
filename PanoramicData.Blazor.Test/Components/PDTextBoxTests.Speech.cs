using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Speech recognition tests for <see cref="PDTextBox"/>.
/// </summary>
public partial class PDTextBoxTests
{
	/// <summary>The speech button initialises speech recognition in the requested language.</summary>
	[Fact]
	public void SpeechButton_InitialisesSpeech()
	{
		var speech = JSInterop.SetupModule(SpeechModulePath);
		speech.Mode = JSRuntimeMode.Loose;

		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.ShowSpeechButton, true)
			.Add(p => p.SpeechLang, "en-GB"));

		speech.VerifyInvoke("initSpeech").Arguments.Should().Equal("en-GB");
		component.Find("button i.fa-microphone").Should().NotBeNull();
	}

	/// <summary>Clicking the speech button starts listening and flashes; clicking again aborts.</summary>
	[Fact]
	public async Task SpeechButton_StartsListening_ThenAbortsOnSecondClick()
	{
		var speech = JSInterop.SetupModule(SpeechModulePath);
		speech.Mode = JSRuntimeMode.Loose;
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.ShowSpeechButton, true)
			.Add(p => p.ShowClearButton, false));

		await component.Find("button").ClickAsync(new MouseEventArgs());
		component.WaitForAssertion(() => speech.Invocations["startListenForSpeech"].Should().ContainSingle());
		component.Find("button").ClassList.Should().Contain("flash");
		speech.Invocations["abortListenForSpeech"].Should().ContainSingle();

		await component.Find("button").ClickAsync(new MouseEventArgs());
		speech.Invocations["abortListenForSpeech"].Should().HaveCount(2);
		speech.Invocations["startListenForSpeech"].Should().ContainSingle();

		await component.InvokeAsync(component.Instance.OnListeningStopped);
		component.Find("button").ClassList.Should().NotContain("flash");
	}

	/// <summary>A speech result becomes the value and raises ValueChanged.</summary>
	[Fact]
	public async Task SpeechResult_BecomesTheValue()
	{
		var component = Render<PDTextBox>(parameters => parameters
			.Add(p => p.ValueChanged, (string v) => _changes.Add(v)));

		await component.InvokeAsync(() => component.Instance.OnSpeechResult("spoken words"));
		await component.InvokeAsync(component.Instance.OnListeningStarted);

		_changes.Should().Equal("spoken words");
		component.Find("input").GetAttribute("value").Should().Be("spoken words");
	}

	/// <summary>Disposing terminates speech recognition.</summary>
	[Fact]
	public async Task Dispose_TerminatesSpeech()
	{
		var speech = JSInterop.SetupModule(SpeechModulePath);
		speech.Mode = JSRuntimeMode.Loose;
		var component = Render<PDTextBox>(parameters => parameters.Add(p => p.ShowSpeechButton, true));

		await component.InvokeAsync(() => component.Instance.DisposeAsync().AsTask());

		speech.Invocations["termSpeech"].Should().ContainSingle();
	}
}
