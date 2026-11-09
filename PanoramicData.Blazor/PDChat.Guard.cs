namespace PanoramicData.Blazor;

/// <summary>
/// Keeps a failure in the chat's own work, or in the host's callbacks and chat service, from ending the circuit.
/// </summary>
public partial class PDChat
{
	[Inject] private ILogger<PDChat> Logger { get; set; } = NullLogger<PDChat>.Instance;

	/// <summary>Runs a chat service event's work on this chat's dispatcher, logging a failure.</summary>
	/// <remarks>
	/// A service may raise its events from any thread: a timer, a background poll, or another user's circuit when it is a
	/// singleton. On the dispatcher already, the work runs at once.
	/// </remarks>
	private void RunServiceEvent(Func<Task> work, string eventName)
		=> _ = InvokeAsync(() => RunGuardedAsync(work, eventName));

	/// <summary>Runs work that must not end the circuit, logging a failure other than the page going away.</summary>
	/// <returns><see langword="true"/> if the work completed.</returns>
	private async Task<bool> RunGuardedAsync(Func<Task> work, string operation)
	{
		try
		{
			await work();
			return true;
		}
		catch (Exception ex) when (IsTeardown(ex))
		{
			return false;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "PDChat: {Operation} failed", operation);
			return false;
		}
	}

	/// <summary>A handler for the markup that runs <paramref name="work"/> guarded.</summary>
	private Func<Task> Guarded(Func<Task> work, string operation)
		=> () => RunGuardedAsync(work, operation);

	/// <summary>A handler for the markup that runs <paramref name="work"/> guarded with the event's argument.</summary>
	private Func<T, Task> Guarded<T>(Func<T, Task> work, string operation)
		=> argument => RunGuardedAsync(() => work(argument), operation);

	/// <summary>Whether an exception only means the circuit or the browser is going away.</summary>
	private static bool IsTeardown(Exception ex)
		=> ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException;
}
