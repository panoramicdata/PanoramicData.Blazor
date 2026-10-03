namespace PanoramicData.Blazor.Models;

/// <summary>
/// FormField: the value and data type of the bound field.
/// </summary>
public partial class FormField<TItem> where TItem : class
{
	/// <summary>
	/// Returns the value to be rendered in the user interface.
	/// </summary>
	/// <param name="item">The current TItem instance where to obtain the current field value.</param>
	/// <returns>A value that can be rendered in the user interface.</returns>
	public object? GetRenderValue(TItem? item)
	{
		if (item == null)
		{
			return null;
		}

		return CompiledFieldFunc?.Invoke(item) switch
		{
			// return simple date time string
			DateTimeOffset dto => dto.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			// return date time string
			DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			var value => value
		};
	}

	/// <summary>
	/// Returns the field data type.
	/// </summary>
	/// <returns></returns>
	public Type? GetFieldType()
	{
		var dataType = Field?.GetPropertyMemberInfo()?.GetMemberUnderlyingType();
		return dataType is null ? null : Nullable.GetUnderlyingType(dataType) ?? dataType;
	}

	/// <summary>
	/// Returns true if the field's underlying property type accepts null values
	/// (i.e. it is a <see langword="string"/>, a nullable value type, or a reference type).
	/// </summary>
	public bool GetFieldIsNullable()
	{
		var memberInfo = Field?.GetPropertyMemberInfo();
		if (memberInfo is PropertyInfo propInfo)
		{
			if (propInfo.PropertyType.FullName == "System.String")
			{
				return true;
			}

			return Nullable.GetUnderlyingType(propInfo.PropertyType) != null;
		}

		return false;
	}
}
