using System;

namespace PanoramicData.Blazor.Interfaces
{
	public interface IKeyedCollection<T>
	{
		void Add(string key, T value);

		bool ContainsKey(string key);

		T Get(string key);

		T Get(string key, T defaultValue);
	}
}
