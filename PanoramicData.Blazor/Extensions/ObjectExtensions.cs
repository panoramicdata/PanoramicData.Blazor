using System;

namespace PanoramicData.Blazor.Extensions
{
	public static class ObjectExtensions
	{
		/// <summary>
		/// Inspects the incoming data type and casts to the requested type.
		/// </summary>
		/// <param name="value">The object value being cast.</param>
		/// <param name="type">The data type to be cast to.</param>
		/// <returns></returns>
		public static object Cast(this object value, Type type)
		{
			if (type.IsEnum)
			{
				return Enum.Parse(type, value.ToString());
			}
			else if (type.FullName == "System.Guid")
			{
				return Guid.Parse(value.ToString());
			}
			else if (type.FullName == "System.DateTime")
			{
				return DateTime.Parse(value.ToString());
			}
			else if (type.FullName == "System.DateTimeOffset")
			{
				return DateTimeOffset.Parse(value.ToString());
			}
			return Convert.ChangeType(value, type);
		}
	}
}
