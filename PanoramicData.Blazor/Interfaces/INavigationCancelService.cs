namespace PanoramicData.Blazor.Interfaces;

public interface INavigationCancelService
{
	/// <summary>
	/// Event raised before a navigation occurs.
	/// </summary>
	event EventHandler<BeforeNavigateEventArgs> BeforeNavigate;

	/// <summary>
	/// Determines whether the intended operation should proceed or be cancelled.
	/// </summary>
	/// <param name="target">Optional data for intended operation. May be a target URL or operation name etc.</param>
	/// <returns>true if the operation should proceed otherwise false.</returns>
	Task<bool> ProceedAsync(string target = "");
}
