﻿namespace PanoramicData.Blazor.Models;

/// <summary>
/// The CachedResult class implements a very simple cached item.
/// </summary>
public class CachedResult<T>(string key, T result)
{

	/// <summary>
	/// Gets or sets the expiry time of the cache.
	/// </summary>
	public DateTimeOffset Expiry { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets whether the cache has expired.
	/// </summary>
	public bool HasExpired => DateTimeOffset.UtcNow >= Expiry;

	/// <summary>
	/// Gets or sets a unique key value for the collection of items.
	/// </summary>
	public string Key { get; } = key;

	/// <summary>
	/// Gets the cached result.
	/// </summary>
	public T Result { get; } = result;
}
