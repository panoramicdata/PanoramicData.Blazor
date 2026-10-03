namespace PanoramicData.Blazor;

/// <summary>
/// Live following of the current time.
/// </summary>
public partial class PDTimeline
{
	/// <summary>
	/// Gets whether the timeline is actively following the current time.
	/// </summary>
	public bool IsFollowingNow => _isFollowingNow;

	/// <summary>
	/// Resumes live following at the right-hand edge. A fixed <see cref="MaxDateTime"/> cannot be followed.
	/// </summary>
	public async Task ResumeFollowNowAsync()
	{
		_followNowSuspendedByUser = false;
		FollowNow = true;
		_lastFollowNowParameter = true;
		await SetFollowNowAsync(true, true).ConfigureAwait(true);
	}

	/// <summary>
	/// Suspends live following, leaving the current viewport and selection in place.
	/// </summary>
	public async Task SuspendFollowNowAsync()
	{
		_followNowSuspendedByUser = true;
		FollowNow = false;
		await SetFollowNowAsync(false, true).ConfigureAwait(true);
	}

	/// <summary>
	/// Advances a following timeline to the current scale boundary, if that boundary has moved on.
	/// </summary>
	public Task RefreshFollowNowAsync() => RefreshFollowNowAsync(false);

	/// <summary>
	/// Advances a following timeline to the current scale boundary.
	/// </summary>
	/// <param name="force">True to update even when the rounded boundary has not changed.</param>
	public async Task RefreshFollowNowAsync(bool force)
	{
		if (!CanRefreshFollowNow)
		{
			return;
		}

		var boundary = RoundedMaxDateTime;
		if (!force && boundary <= _lastFollowNowBoundary)
		{
			return;
		}

		if (FollowNowSelection && _followNowSelectionDuration is null && _selectionRange is not null)
		{
			_followNowSelectionDuration = _selectionRange.EndTime - _selectionRange.StartTime;
		}

		_lastFollowNowBoundary = boundary;
		var refreshAllData = Options.General.RightAlign && _totalColumns < _viewportColumns;
		await SetScale(Scale, true, boundary, TimelinePositions.End, refreshAllData).ConfigureAwait(true);
		await RollFollowNowSelectionAsync(boundary).ConfigureAwait(true);
		StateHasChanged();
	}

	private bool CanRefreshFollowNow
		=> _isFollowingNow && MaxDateTime is null && MinDateTime != DateTime.MinValue && _canvasWidth > 0;

	private async Task RollFollowNowSelectionAsync(DateTime boundary)
	{
		if (FollowNowSelection && _followNowSelectionDuration > TimeSpan.Zero)
		{
			var selection = TimelineFollowNowCalculator.CreateRollingSelection(
				boundary,
				_followNowSelectionDuration.Value,
				RoundedMinDateTime);
			await SetSelection(selection.StartTime, selection.EndTime).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Records the latest <see cref="FollowNow"/> parameter value.
	/// </summary>
	/// <returns>True when the parameter has changed since it was last seen.</returns>
	private bool UpdateFollowNowParameter()
	{
		if (_lastFollowNowParameter == FollowNow)
		{
			return false;
		}

		_lastFollowNowParameter = FollowNow;
		if (FollowNow)
		{
			_followNowSuspendedByUser = false;
		}

		return true;
	}

	private async Task ApplyFollowNowParameterAsync(bool parameterChanged)
	{
		if (!FollowNow)
		{
			if (_isFollowingNow)
			{
				await SetFollowNowAsync(false, false).ConfigureAwait(true);
			}

			return;
		}

		if (MaxDateTime is not null)
		{
			await SetFollowNowAsync(false, parameterChanged || _isFollowingNow).ConfigureAwait(true);
		}
		else if (ShouldStartFollowingNow(parameterChanged))
		{
			await SetFollowNowAsync(true, false).ConfigureAwait(true);
		}
		else if (FollowNowTimerSettingsChanged)
		{
			StartFollowNowTimer();
		}
	}

	private bool ShouldStartFollowingNow(bool parameterChanged)
		=> MinDateTime != DateTime.MinValue && !_followNowSuspendedByUser && (parameterChanged || !_isFollowingNow);

	private bool FollowNowTimerSettingsChanged
		=> _isFollowingNow
			&& (_activeFollowNowRefreshInterval != FollowNowRefreshInterval || !ReferenceEquals(_activeFollowNowClock, Clock));

	private async Task SetFollowNowAsync(bool follow, bool notify)
	{
		var canFollow = follow && MaxDateTime is null && MinDateTime != DateTime.MinValue;
		var changed = _isFollowingNow != canFollow;
		_isFollowingNow = canFollow;

		if (canFollow)
		{
			await StartFollowingNowAsync().ConfigureAwait(true);
		}
		else
		{
			StopFollowingNow();
		}

		if (notify && (changed || FollowNow != canFollow))
		{
			FollowNow = canFollow;
			await FollowNowChanged.InvokeAsync(canFollow).ConfigureAwait(true);
		}

		if (!_disposed)
		{
			StateHasChanged();
		}
	}

	private async Task StartFollowingNowAsync()
	{
		_followNowSelectionDuration = FollowNowSelection && _selectionRange is not null
			? _selectionRange.EndTime - _selectionRange.StartTime
			: null;
		_lastFollowNowBoundary = DateTime.MinValue;
		StartFollowNowTimer();
		await RefreshFollowNowAsync(true).ConfigureAwait(true);
	}

	private void StopFollowingNow()
	{
		_followNowCancellationTokenSource?.Cancel();
		_followNowSelectionDuration = null;
	}

	private void StartFollowNowTimer()
	{
		_followNowCancellationTokenSource?.Cancel();
		_followNowCancellationTokenSource?.Dispose();
		_followNowCancellationTokenSource = new CancellationTokenSource();
		_activeFollowNowRefreshInterval = FollowNowRefreshInterval;
		_activeFollowNowClock = Clock;
		_followNowTask = RunFollowNowTimerAsync(_followNowCancellationTokenSource.Token);
	}

	private async Task RunFollowNowTimerAsync(CancellationToken cancellationToken)
	{
		try
		{
			using var timer = new PeriodicTimer(FollowNowRefreshInterval, Clock);
			while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(true))
			{
				await InvokeAsync(() => RefreshFollowNowAsync()).ConfigureAwait(true);
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// Following was stopped or the timeline disposed: the timer is meant to end here.
		}
	}
}
