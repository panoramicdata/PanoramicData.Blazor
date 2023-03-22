namespace PanoramicData.Blazor;

public partial class PDFormFieldEditor<TItem> where TItem : class
{
	[Parameter]
	public int DebounceWait { get; set; }

	[EditorRequired]
	[Parameter]
	public FormField<TItem> Field { get; set; } = null!;

	[EditorRequired]
	[Parameter]
	public PDForm<TItem> Form { get; set; } = null!;

	public string GetEditorClass(FormField<TItem> field)
	{
		return $"{(Form?.Errors.ContainsKey(field.GetName() ?? "") == true ? "invalid" : "")} {field.DisplayOptions?.CssClass}";
	}

	private OptionInfo[] GetEnumValues(FormField<TItem> field)
	{
		var options = new List<OptionInfo>();
		var memberInfo = field.Field?.GetPropertyMemberInfo();
		if (memberInfo is PropertyInfo propInfo)
		{
			string[] names = Enum.GetNames(propInfo.PropertyType);
			Array values = Enum.GetValues(propInfo.PropertyType);
			for (var i = 0; i < values.Length; i++)
			{
				var displayName = propInfo.PropertyType.GetMember($"{names[i]}")
							   ?.First()
							   .GetCustomAttribute<DisplayAttribute>()
							   ?.Name ?? names[i];
				options.Add(new OptionInfo
				{
					Text = displayName,
					Value = values.GetValue(i),
					IsSelected = Form?.GetFieldStringValue(field) == values.GetValue(i)?.ToString()
				});
			}
		}
		return options.ToArray();
	}

	private static Dictionary<string, object> GetNumericAttributes(FormField<TItem> field)
	{
		var dict = new Dictionary<string, object>();
		if (field.MaxValue.HasValue)
		{
			dict.Add("max", field.MaxValue.Value);
		}
		if (field.MinValue.HasValue)
		{
			dict.Add("min", field.MinValue.Value);
		}
		return dict;
	}

	public bool IsReadOnly(FormField<TItem> field) =>
		(Form?.Mode == FormModes.Create && field.ReadOnlyInCreate(Form?.GetItemWithUpdates())) ||
		(Form?.Mode == FormModes.Edit && field.ReadOnlyInEdit(Form?.GetItemWithUpdates())) ||
		Form?.Mode == FormModes.Delete ||
		Form?.Mode == FormModes.Cancel ||
		Form?.Mode == FormModes.ReadOnly;

	public async Task OnSelectInputChanged(ChangeEventArgs args, FormField<TItem> field)
	{
		if (Form != null && args.Value != null)
		{
			await Form.SetFieldValueAsync(field, args.Value).ConfigureAwait(true);
		}
	}

	private async Task UpdateDateTimeValue(ChangeEventArgs args, FormField<TItem> field)
	{
		try
		{
			await Form!.SetFieldValueAsync(field, DateTime.SpecifyKind(Convert.ToDateTime(args.Value), DateTimeKind.Utc)).ConfigureAwait(true);
		}
		catch
		{
			Form!.SetFieldErrors(field.GetName() ?? "", "Invalid Date");
		}
	}

	private async Task UpdateDateTimeOffsetValue(ChangeEventArgs args, FormField<TItem> field)
	{
		try
		{
			await Form!.SetFieldValueAsync(field, DateTimeOffset.Parse(args.Value?.ToString() ?? String.Empty)).ConfigureAwait(true);
		}
		catch
		{
			Form!.SetFieldErrors(field.GetName() ?? "", "Invalid Date");
		}
	}

	private async Task UpdateValueViaCastAsync(ChangeEventArgs args, FormField<TItem> field)
	{
		try
		{
			var fieldType = field.GetFieldType();
			if (fieldType != null)
			{
				await Form!.SetFieldValueAsync(field, Convert.ChangeType(args.Value ?? string.Empty, fieldType, CultureInfo.InvariantCulture)).ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

}
