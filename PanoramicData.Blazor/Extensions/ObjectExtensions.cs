using System;

namespace PanoramicData.Blazor.Extensions
{
	public static class ObjectExtensions
	{
		public static object Cast(this object value, Type type)
		{
			object typedValue = value;
			if (type.IsEnum)
			{
				typedValue = Enum.Parse(type, value.ToString());
			}
			else if(type.FullName == "System.DateTime")
			{
				typedValue = DateTime.Parse(value.ToString());
			}
			else if (type.FullName == "System.DateTimeOffset")
			{
				typedValue = DateTimeOffset.Parse(value.ToString());
			}
			else
			{
				typedValue = Convert.ChangeType(value, type);
			}
			return typedValue;
		}
	}
}
