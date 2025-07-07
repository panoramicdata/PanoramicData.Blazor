namespace PanoramicData.Blazor;

public partial class PDFormFieldEditor<TItem> : IDisposable where TItem : class
{
	private static int _idSeq;
	private bool _disposedValue;
	private bool _hasValue = true;
	private StandaloneCodeEditor? _monacoEditor;

	// Debounce support
	private CancellationTokenSource? _monacoDebounceCts;

	[Parameter]
	public int DebounceWait { get; set; }

	[EditorRequired]
	[Parameter]
	public FormField<TItem> Field { get; set; } = null!;

	[EditorRequired]
	[Parameter]
	public PDForm<TItem> Form { get; set; } = null!;

	[Parameter]
	public string Id { get; set; } = $"field-editor-{++_idSeq}";

	private Dictionary<string, object> GetNullEditorAttributes()
	{
		return new Dictionary<string, object>
		{
			{ "class", "form-check-input ms-nullable me-1 mb-2" },
			{ "type", "checkbox" },
			{ "checked", _hasValue }
		};
	}

	public string GetEditorClass(FormField<TItem> field)
		=> $"{(Form?.Errors.ContainsKey(field.GetName() ?? "") == true ? "invalid" : "")} {field.DisplayOptions?.CssClass}";

	private OptionInfo[] GetEnumValues(FormField<TItem> field)
	{
		var options = new List<OptionInfo>();
		if (field.Field?.GetPropertyMemberInfo() is PropertyInfo propInfo)
		{
			var enumType = field.GetFieldType();
			if (enumType != null)
			{
				string[] names = Enum.GetNames(enumType);
				Array values = Enum.GetValues(enumType);
				for (var i = 0; i < values.Length; i++)
				{

					var displayName = enumType.GetMember($"{names[i]}")
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
		}

		return [.. options];
	}

	private static StandaloneEditorConstructionOptions GetMonacoOptionsReadOnly(FieldStringOptions fso, StandaloneCodeEditor editor)
	{
		var opt = fso.MonacoOptions(editor);
		opt.ReadOnly = true;
		return opt;
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

	private string GetResizeableCssCls()
	{
		if (Field?.DisplayOptions is FieldStringOptions fso && fso.Resize)
		{
			return $"resize-h {fso.ResizeCssCls}";
		}

		return string.Empty;
	}

	public bool IsReadOnly(FormField<TItem> field) =>
		!_hasValue ||
		(Form?.Mode == FormModes.Create && field.ReadOnlyInCreate(Form?.GetItemWithUpdates())) ||
		(Form?.Mode == FormModes.Edit && field.ReadOnlyInEdit(Form?.GetItemWithUpdates())) ||
		Form?.Mode == FormModes.Delete ||
		Form?.Mode == FormModes.Cancel ||
		Form?.Mode == FormModes.ReadOnly;

	protected override void OnParametersSet()
	{
		// mark as having value (writable) - unless is nullable type, nulls are allowed and value is null
		_hasValue = Field == null ||
					Field.DisplayOptions?.AllowNulls == false ||
					Field.GetFieldIsNullable() == false ||
					Form.GetFieldValue(Field, true) != null;
	}

	private async Task OnHasNullValueChanged(bool hasValue)
	{
		if (Field != null)
		{
			_hasValue = hasValue;
			if (!_hasValue && Field.GetFieldIsNullable())
			{
				await Form.SetFieldValueAsync(Field, null).ConfigureAwait(true);
			}
			else
			{
				if (Field.GetFieldType() is Type dt)
				{
					object defaultValue = dt.FullName switch
					{
						"System.String" => string.Empty,
						"System.Boolean" => false,
						"System.DateTime" => DateTime.Today,
						"System.DateTimeOffset" => DateTime.Today,
						"System.Guid" => Guid.Empty,
						_ => 0
					};
					await Form.SetFieldValueAsync(Field, defaultValue).ConfigureAwait(true);
				}
			}
		}
	}

	protected override void OnInitialized()
	{
		Form.ResetRequested += Form_ResetRequested;
		Field.ValueChanged += Field_ValueChanged;
	}

	private async void Field_ValueChanged(object? sender, object? value)
	{
		// for most editors the value will be reflected in the UI immediately due to
		// data binding - however the Monaco Editor requires a manual update
		if (_monacoEditor != null && Field.DisplayOptions is FieldStringOptions fso && fso.Editor == FieldStringOptions.Editors.Monaco)
		{
			await SetMonacoValueAsync(value?.ToString() ?? string.Empty);
		}
	}

	private async void Form_ResetRequested(object? sender, EventArgs e)
	{
		// reset data to any Monaco editors
		if (_monacoEditor != null && Form != null && Field != null)
		{
			var value = Form.GetFieldStringValue(Field);
			var model = await _monacoEditor.GetModel();
			// when re-creating Monaco Editor (i.e toggling to/from ReadOnly)
			// this can cause an crash - do NOT ResetChanges on Form.SetEditItem
			await model.SetValue(value);
		}
	}

	private async Task OnMonacoEditorBlurAsync()
	{
		if (_monacoEditor != null && Form != null && Field != null)
		{
			 // Only update if a debounce is outstanding
			if (DebounceWait > 0 && _monacoDebounceCts != null)
			{
				_monacoDebounceCts.Cancel();
				_monacoDebounceCts.Dispose();
				_monacoDebounceCts = null;

				var model = await _monacoEditor.GetModel();
				var value = await model.GetValue(EndOfLinePreference.CRLF, true);
				await Form.SetFieldValueAsync(Field, value);
			}

			Field.SuppressErrors = false;
		}
	}

	private async Task OnMonacoEditorKeyUpAsync(BlazorMonaco.KeyboardEvent args)
	{
		if (_monacoEditor != null && Form != null && Field != null)
		{
			// Cancel any pending update
			_monacoDebounceCts?.Cancel();
			_monacoDebounceCts?.Dispose();
			_monacoDebounceCts = new CancellationTokenSource();
			var token = _monacoDebounceCts.Token;

			try
			{
				await Task.Delay(DebounceWait > 0 ? DebounceWait : 0, token);
				if (!token.IsCancellationRequested)
				{
					// Cancel and dispose after use
					_monacoDebounceCts.Cancel();
					_monacoDebounceCts.Dispose();
					_monacoDebounceCts = null;

					var model = await _monacoEditor.GetModel();
					var value = await model.GetValue(EndOfLinePreference.CRLF, true);
					await Form.SetFieldValueAsync(Field, value, false);
				}
			}
			catch (TaskCanceledException)
			{
				// Ignore, another keyup event occurred
			}
		}
	}

	private async Task OnMonacoInitAsync()
	{
		if (_monacoEditor != null && Form != null)
		{
			var value = Form.GetFieldStringValue(Field);
			await SetMonacoValueAsync(value);
		}
	}

	public async Task OnSelectInputChanged(ChangeEventArgs args, FormField<TItem> field)
	{
		if (Form != null && args.Value != null)
		{
			await Form.SetFieldValueAsync(field, args.Value).ConfigureAwait(true);
		}
	}

	private async Task SetMonacoValueAsync(string value)
	{
		try
		{
			if (_monacoEditor != null && Form != null)
			{
				var model = await _monacoEditor.GetModel();
				var oldValue = await model.GetValue(EndOfLinePreference.CRLF, true);
				// only update if it is different to what we have or it will move cursor to the beginning of the editor
				if (oldValue != value)
				{
					await model.SetValue(value);
				}
			}
		}
		catch
		{
			// Do nothing
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
			await Form!.SetFieldValueAsync(field, DateTimeOffset.Parse(args.Value?.ToString() ?? string.Empty)).ConfigureAwait(true);
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
				// handle nullable types
				object? newValue = null;
				if (Nullable.GetUnderlyingType(fieldType) is Type ut)
				{
					if (args.Value is null)
					{
						newValue = null;
					}
					else if (ut.Name == "System.String")
					{
						newValue = args.Value.ToString();
					}
					else if (args.Value.ToString() == string.Empty)
					{
						newValue = null;
					}
					else
					{
						newValue = Convert.ChangeType(args.Value, ut, CultureInfo.InvariantCulture);
					}
				}
				else
				{
					newValue = Convert.ChangeType(args.Value ?? string.Empty, fieldType, CultureInfo.InvariantCulture);
				}

				await Form!.SetFieldValueAsync(field, newValue).ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	#region IDisposable

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				Field.ValueChanged -= Field_ValueChanged;
				Form.ResetRequested -= Form_ResetRequested;
				_monacoDebounceCts?.Cancel();
				_monacoDebounceCts?.Dispose();
				_monacoDebounceCts = null;
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			_disposedValue = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~PDFormFieldEditor()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}