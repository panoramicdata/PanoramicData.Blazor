namespace PanoramicData.Blazor.Interfaces;

public interface IAsyncStateManager
{
	Task InitializeAsync();

	Task<T?> LoadStateAsync<T>(string key);

	Task RemoveStateAsync(string key);

	Task SaveStateAsync(string key, object state);
}
