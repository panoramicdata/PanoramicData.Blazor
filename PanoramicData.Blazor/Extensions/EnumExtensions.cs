using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace PanoramicData.Blazor.Extensions
{
	public static class EnumExtensions
	{
		public static string? GetEnumDisplayName(this Enum enumType)
		{
			return enumType.GetType().GetMember(enumType.ToString())
						   ?.First()
						   .GetCustomAttribute<DisplayAttribute>()
						   .Name;
		}
	}
}
