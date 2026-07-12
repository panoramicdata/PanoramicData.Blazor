namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines an asynchronous store for component state that survives between page loads.
/// </summary>
public interface IAsyncStateManager
{
	/// <summary>
	/// Performs any required initialisation of the state store, such as loading persisted state.
	/// </summary>
	Task InitializeAsync();

	/// <summary>
	/// Loads a previously saved state value by key.
	/// </summary>
	/// <typeparam name="T">The expected type of the stored value.</typeparam>
	/// <param name="key">The unique key under which the state was saved.</param>
	/// <returns>The stored value cast to <typeparamref name="T"/>, or <c>null</c> when not found.</returns>
	Task<T?> LoadStateAsync<T>(string key);

	/// <summary>
	/// Removes the stored state entry for the given key.
	/// </summary>
	/// <param name="key">The unique key of the state entry to remove.</param>
	Task RemoveStateAsync(string key);

	/// <summary>
	/// Saves a state value under the given key, replacing any existing value.
	/// </summary>
	/// <param name="key">The unique key under which to save the state.</param>
	/// <param name="state">The state value to persist.</param>
	Task SaveStateAsync(string key, object state);
}
