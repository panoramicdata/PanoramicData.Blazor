namespace PanoramicData.Blazor;

public static class Constants
{

	public const string TokenAll = "(All)";

	public const string TokenNone = "(None)";

	public static class Functions
	{
		/// <summary>
		/// Function that returns Field.Description if set, otherwise looks to return description from Display attribute on Item under edit.
		/// </summary>
		/// <typeparam name="T">Data type of instance</typeparam>
		/// <param name="field">The FormField instance.</param>
		/// <param name="form">The Form instance that gives access to the original Item as well as the current changes via the Delta property.</param>
		/// <returns>The fields description.</returns>
		public static string FormFieldDescription<T>(FormField<T> field, PDForm<T>? form) where T : class => field.Description ?? (field.Field?.GetPropertyMemberInfo()?.GetCustomAttribute<DisplayAttribute>()?.Description) ?? string.Empty;

		/// <summary>
		/// Function that determines whether the field contains sensitive information.
		/// </summary>
		/// <typeparam name="T">Data type of instance</typeparam>
		/// <param name="item">The instance.</param>
		/// <param name="form">The Form instance that gives access to current changes via the Delta property.</param>
		/// <returns>false</returns>
		public static bool FormFieldIsSensitive<T>(T? item, PDForm<T>? form) where T : class => false;

		/// <summary>
		/// Function that returns true.
		/// </summary>
		/// <typeparam name="T">Data type of instance</typeparam>
		/// <param name="_">Instance of type.</param>
		/// <returns>true</returns>
		public static bool True<T>(T _) => true;

		/// <summary>
		/// Function that returns false.
		/// </summary>
		/// <typeparam name="T">Data type of instance</typeparam>
		/// <param name="_">Instance of type.</param>
		/// <returns>false</returns>
		public static bool False<T>(T _) => false;
	}
}
