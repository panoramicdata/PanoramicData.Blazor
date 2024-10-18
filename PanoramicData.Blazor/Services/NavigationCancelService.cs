namespace PanoramicData.Blazor.Services;

public class NavigationCancelService(IJSRuntime jsRuntime) : INavigationCancelService
{
	private readonly ValueTask<IJSObjectReference>? _loadCommonJsTask = jsRuntime.InvokeAsync<IJSObjectReference>("import", "/_content/PanoramicData.Blazor/js/common.js");

	/// <summary>
	/// Event raised before a navigation occurs.
	/// </summary>
	public event EventHandler<BeforeNavigateEventArgs> BeforeNavigate = default!;

	/// <summary>
	/// Determines whether the intended operation should proceed or be canceled.
	/// </summary>
	/// <param name="target">Optional data for intended operation. May be a target URL or operation name etc.</param>
	/// <returns>true if the operation should proceed otherwise false.</returns>
	public async Task<bool> ProceedAsync(string target = "")
	{
		// ask listening code if operation should be canceled
		var args = new BeforeNavigateEventArgs { Target = target };
		BeforeNavigate?.Invoke(this, args);
		if (args.Cancel && _loadCommonJsTask != null)
		{
			// allow user option to override and perform operation regardless
			var commonModule = await _loadCommonJsTask.GetValueOrDefault().ConfigureAwait(true);
			if (commonModule != null)
			{
				return await commonModule.InvokeAsync<bool>("confirm", "Changes have been made, continue and lose those changes?").ConfigureAwait(true);
			}
		}

		return true;
	}
}
