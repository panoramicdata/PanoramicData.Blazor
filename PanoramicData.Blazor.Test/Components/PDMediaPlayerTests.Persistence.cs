using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System.Text.Json;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDMediaPlayer.PersistKey"/> (remembering the speed and position) and for disposal.
/// </summary>
public partial class PDMediaPlayerTests
{
	private const string Key = "player-state";

	private static string Saved(double? rate, params (string Src, double Seconds)[] positions)
	{
		var state = new MediaPlayerPersistedState { Rate = rate };
		foreach (var (src, seconds) in positions.Reverse())
		{
			state.SetPosition(src, TimeSpan.FromSeconds(seconds));
		}

		return JsonSerializer.Serialize(state);
	}

	private MediaPlayerPersistedState? LastWrite()
	{
		var json = _module.Invocations.Where(i => i.Identifier == "setItem").Select(i => (string)i.Arguments[1]!).LastOrDefault();
		return json is null ? null : JsonSerializer.Deserialize<MediaPlayerPersistedState>(json);
	}

	private int WriteCount => CountOf("setItem");

	/// <summary>A saved speed and position are restored: the speed at once, the position (over StartAt) when the media loads.</summary>
	[Fact]
	public async Task Persist_RestoresSpeedAndPosition()
	{
		_module.Setup<string?>("getItem", i => (string)i.Arguments[0]! == Key).SetResult(Saved(1.75, (Src, 1234.5), ("/other.wav", 10)));
		var rates = new List<double>();
		var cut = RenderPlayer(p => p
			.Add(x => x.PersistKey, Key)
			.Add(x => x.StartAt, TimeSpan.FromSeconds(30))
			.Add(x => x.PlaybackRateChanged, r => rates.Add(r)));

		cut.Instance.CurrentPlaybackRate.Should().Be(1.75);
		rates.Should().Equal(1.75);
		_module.Invocations.First(i => i.Identifier == "setRate").Arguments[1].Should().Be(1.75);
		_module.Invocations.Select(i => i.Identifier).Should().ContainInOrder("getItem", "attach");

		await RaiseAsync(cut, "loadedmetadata", State(duration: 3600, rate: 1.75));

		SeekTargets().Should().Equal(1234.5);
	}

	/// <summary>The position is saved at most once per PersistInterval while playing, at once on pause, and forgotten at the end.</summary>
	[Fact]
	public async Task Persist_SavesThePositionThrottled()
	{
		var clock = new ManualClock();
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.PersistKey, Key)
			.Add(x => x.Clock, clock)
			.Add(x => x.PersistInterval, TimeSpan.FromSeconds(10)));

		await RaiseAsync(cut, "timeupdate", State(time: 100));
		WriteCount.Should().Be(1);
		LastWrite()!.GetPosition(Src).Should().Be(TimeSpan.FromSeconds(100));

		clock.Advance(TimeSpan.FromSeconds(5));
		await RaiseAsync(cut, "timeupdate", State(time: 105));
		WriteCount.Should().Be(1);

		clock.Advance(TimeSpan.FromSeconds(6));
		await RaiseAsync(cut, "timeupdate", State(time: 111));
		WriteCount.Should().Be(2);

		await RaiseAsync(cut, "pause", State(time: 112));
		WriteCount.Should().Be(3);
		LastWrite()!.GetPosition(Src).Should().Be(TimeSpan.FromSeconds(112));

		await RaiseAsync(cut, "ended", State(time: 3600));
		WriteCount.Should().Be(4);
		LastWrite()!.GetPosition(Src).Should().BeNull();

		// nothing left to forget
		await RaiseAsync(cut, "ended", State(time: 3600));
		WriteCount.Should().Be(4);
	}

	/// <summary>A position of zero is forgotten rather than saved.</summary>
	[Fact]
	public async Task Persist_ZeroPosition_IsForgotten()
	{
		_module.Setup<string?>("getItem", _ => true).SetResult(Saved(null, (Src, 50)));
		var cut = await RenderReadyAsync(p => p.Add(x => x.PersistKey, Key));

		await RaiseAsync(cut, "pause", State(time: 0));

		LastWrite()!.GetPosition(Src).Should().BeNull();
	}

	/// <summary>A chosen speed is saved.</summary>
	[Fact]
	public async Task Persist_SavesTheSpeed()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.PersistKey, Key));

		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(1.25));

		LastWrite()!.Rate.Should().Be(1.25);
	}

	/// <summary>Without a key, or for a live stream, nothing is read or saved.</summary>
	[Fact]
	public async Task Persist_NotForLiveOrWithoutAKey()
	{
		var cut = await RenderReadyAsync();
		await RaiseAsync(cut, "pause", State(time: 10));
		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(2));
		CountOf("getItem").Should().Be(0);
		WriteCount.Should().Be(0);

		var live = RenderPlayer(p => p.Add(x => x.PersistKey, Key));
		await RaiseAsync(live, "loadedmetadata", State(duration: null, infinite: true));
		await RaiseAsync(live, "pause", State(time: 10));
		WriteCount.Should().Be(0);
	}

	/// <summary>Unreadable storage is ignored and playback carries on; a failed save is ignored too.</summary>
	[Fact]
	public async Task Persist_StorageFailures_AreIgnored()
	{
		_module.Setup<string?>("getItem", _ => true).SetResult("{not json");
		_module.SetupVoid("setItem", _ => true).SetException(new JSException("quota"));
		var cut = await RenderReadyAsync(p => p.Add(x => x.PersistKey, Key).Add(x => x.StartAt, TimeSpan.FromSeconds(9)));

		await RaiseAsync(cut, "pause", State(time: 10));

		SeekTargets().Should().Equal(9.0);
		cut.Instance.CurrentPlaybackRate.Should().Be(1);
	}

	/// <summary>A cascaded state manager, such as PDLocalStorageStateManager, is used instead of the script's storage.</summary>
	[Fact]
	public async Task Persist_UsesACascadedStateManager()
	{
		var store = new MemoryStateManager();
		store.Data[Key] = new MediaPlayerPersistedState { Rate = 2 };
		var cut = Render<PDMediaPlayer>(p => p
			.Add(x => x.Src, Src)
			.Add(x => x.PersistKey, Key)
			.AddCascadingValue<IAsyncStateManager>(store));
		await RaiseAsync(cut, "loadedmetadata", State());

		await RaiseAsync(cut, "pause", State(time: 42));

		store.Initialized.Should().BeTrue();
		cut.Instance.CurrentPlaybackRate.Should().Be(2);
		((MediaPlayerPersistedState)store.Data[Key]).GetPosition(Src).Should().Be(TimeSpan.FromSeconds(42));
		CountOf("getItem").Should().Be(0);
		WriteCount.Should().Be(0);
	}

	/// <summary>A state manager that fails is ignored.</summary>
	[Fact]
	public async Task Persist_FailingStateManager_IsIgnored()
	{
		var store = new MemoryStateManager { Fail = true };
		var cut = Render<PDMediaPlayer>(p => p
			.Add(x => x.Src, Src)
			.Add(x => x.PersistKey, Key)
			.AddCascadingValue<IAsyncStateManager>(store));
		await RaiseAsync(cut, "loadedmetadata", State());

		await RaiseAsync(cut, "pause", State(time: 42));

		cut.Instance.IsReady.Should().BeTrue();
	}

	/// <summary>Changing the key loads what the new key holds, resuming its position when the media is already loaded.</summary>
	[Fact]
	public async Task Persist_KeyChange_LoadsTheNewKey()
	{
		_module.Setup<string?>("getItem", i => (string)i.Arguments[0]! == "other").SetResult(Saved(1.5, (Src, 77)));
		var cut = await RenderReadyAsync();

		cut.Render(p => p.Add(x => x.PersistKey, "other"));

		cut.Instance.CurrentPlaybackRate.Should().Be(1.5);
		SeekTargets().Should().Equal(77.0);

		cut.Render(p => p.Add(x => x.PersistKey, (string?)null));
		await RaiseAsync(cut, "pause", State(time: 10));
		WriteCount.Should().Be(0);
	}

	/// <summary>Disposing saves the position, detaches the script, releases the object reference and stops the player using the script.</summary>
	[Fact]
	public async Task Dispose_ReleasesEverything()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.PersistKey, Key));
		var reference = (DotNetObjectReference<PDMediaPlayer>)_module.VerifyInvoke("attach").Arguments[1]!;
		await RaiseAsync(cut, "timeupdate", State(time: 50));
		var writes = WriteCount;

		await cut.Instance.DisposeAsync();
		await cut.Instance.DisposeAsync();

		WriteCount.Should().Be(writes + 1);
		_module.Invocations.Where(i => i.Identifier == "detach").Should().ContainSingle()
			.Which.Arguments[0].Should().BeOfType<ElementReference>();
		var useReference = () => reference.Value;
		useReference.Should().Throw<ObjectDisposedException>();

		await cut.InvokeAsync(() => cut.Instance.SeekAsync(TimeSpan.FromSeconds(5)));
		await RaiseAsync(cut, "timeupdate", State(time: 99));
		SeekTargets().Should().BeEmpty();
		cut.Instance.CurrentPosition.Should().Be(TimeSpan.FromSeconds(5));
	}

	/// <summary>Disposing after the circuit has gone does not throw.</summary>
	[Fact]
	public async Task Dispose_AfterTheCircuitHasGone_DoesNotThrow()
	{
		_module.SetupVoid("detach", _ => true).SetException(new JSDisconnectedException("gone"));
		var cut = RenderPlayer();

		var dispose = () => DisposeComponentsAsync();

		await dispose.Should().NotThrowAsync();
		_module.VerifyInvoke("detach");
	}

	/// <summary>Disposing a player whose script never loaded completes.</summary>
	[Fact]
	public async Task Dispose_WithoutTheScript_Completes()
	{
		var player = new PDMediaPlayer();

		await player.DisposeAsync();

		CountOf("detach").Should().Be(0);
	}

	/// <summary>A clock whose time only moves when told to.</summary>
	private sealed class ManualClock : TimeProvider
	{
		private DateTimeOffset _now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

		public void Advance(TimeSpan by) => _now += by;

		public override DateTimeOffset GetUtcNow() => _now;
	}

	/// <summary>An in-memory state manager that can be made to fail.</summary>
	private sealed class MemoryStateManager : IAsyncStateManager
	{
		public Dictionary<string, object> Data { get; } = [];

		public bool Initialized { get; private set; }

		public bool Fail { get; init; }

		public Task InitializeAsync()
		{
			Initialized = true;
			return Task.CompletedTask;
		}

		public Task<T?> LoadStateAsync<T>(string key)
			=> Fail ? throw new StateException("broken") : Task.FromResult(Data.TryGetValue(key, out var value) ? (T?)value : default);

		public Task SaveStateAsync(string key, object state)
		{
			if (Fail)
			{
				throw new StateException("broken");
			}

			Data[key] = state;
			return Task.CompletedTask;
		}

		public Task RemoveStateAsync(string key)
		{
			Data.Remove(key);
			return Task.CompletedTask;
		}
	}
}
