namespace PanoramicData.Blazor;

/// <summary>
/// Renders and manages editing UI for a single <see cref="FormField{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">Form model type.</typeparam>
public partial class PDFormFieldEditor<TItem> : IDisposable where TItem : class
{
	private bool _disposedValue;
	private bool _hasValue = true;
	private EventHandler<object?>? _fieldValueChangedHandler;

	// Debounce support
	private CancellationTokenSource? _monacoDebounceCts;

	private IJSObjectReference? _commonModule;

	/// <summary>
	/// Gets the Monaco editor, when the field is edited with one, set by the markup.
	/// </summary>
	internal StandaloneCodeEditor? MonacoEditor { get; set; }

	/// <summary>
	/// Gets the element containing the editor, set by the markup.
	/// </summary>
	internal ElementReference EditorDiv { get; set; }

	/// <summary>
	/// Gets or sets JavaScript runtime used by this editor.
	/// </summary>
	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the debounce wait period in milliseconds for value changes.
	/// </summary>
	[Parameter]
	public int DebounceWait { get; set; }

	/// <summary>
	/// Gets or sets the form field to be edited.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public FormField<TItem> Field { get; set; } = null!;

	/// <summary>
	/// Gets or sets the parent form.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public PDForm<TItem> Form { get; set; } = null!;

	/// <summary>
	/// Gets or sets the unique identifier for the editor.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"field-editor-{GenericTypeIds.NextFormFieldEditorId()}";

	/// <summary>
	/// Gets CSS classes for the editor container based on validation and field options.
	/// </summary>
	/// <param name="field">Field metadata.</param>
	/// <returns>CSS class string.</returns>
	public string GetEditorClass(FormField<TItem> field)
		=> $"{(Form?.Errors.ContainsKey(field.GetName() ?? "") == true ? "invalid" : "")} {field.DisplayOptions?.CssClass}";

	private OptionInfo[] GetEnumValues(FormField<TItem> field)
	{
		if (field.Field?.GetPropertyMemberInfo() is not PropertyInfo || field.GetFieldType() is not Type enumType)
		{
			return [];
		}

		var selectedValue = Form?.GetFieldStringValue(field);
		var values = Enum.GetValues(enumType);
		return [.. Enum.GetNames(enumType).Select((name, i) => new OptionInfo
		{
			Text = GetEnumDisplayName(enumType, name),
			Value = values.GetValue(i),
			IsSelected = selectedValue == values.GetValue(i)?.ToString()
		})];
	}

	private static string GetEnumDisplayName(Type enumType, string name)
		=> enumType.GetMember(name)[0].GetCustomAttribute<DisplayAttribute>()?.Name ?? name;

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

	/// <summary>
	/// Determines whether the field should be rendered read-only for the current form mode.
	/// </summary>
	/// <param name="field">Field metadata.</param>
	/// <returns>True if read-only.</returns>
	public bool IsReadOnly(FormField<TItem> field) => !_hasValue || IsReadOnlyInMode(field);

	private bool IsReadOnlyInMode(FormField<TItem> field) => Form?.Mode switch
	{
		FormModes.Create => field.ReadOnlyInCreate(Form.GetItemWithUpdates()),
		FormModes.Edit => field.ReadOnlyInEdit(Form.GetItemWithUpdates()),
		FormModes.Delete or FormModes.Cancel or FormModes.ReadOnly => true,
		_ => false
	};

	/// <summary>
	/// Applies parameter-driven editor state.
	/// </summary>
	protected override void OnParametersSet()
	{
		// mark as having value (writable) - unless is nullable type, nulls are allowed and value is null
		_hasValue = Field == null ||
					Field.DisplayOptions?.AllowNulls == false ||
					!Field.GetFieldIsNullable() ||
					Form.GetFieldValue(Field, true) != null;
	}

	private async Task OnHasNullValueChanged(bool hasValue)
	{
		if (Field is null)
		{
			return;
		}

		_hasValue = hasValue;
		if (!_hasValue && Field.GetFieldIsNullable())
		{
			await Form.SetFieldValueAsync(Field, null).ConfigureAwait(true);
			return;
		}

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

	/// <summary>
	/// Registers with the parent form and subscribes to field reset/value events.
	/// </summary>
	protected override void OnInitialized()
	{
		base.OnInitialized();
		if (Form is not null)
		{
			Form.RegisterFieldEditor(this);
			Form.ResetRequested += Form_ResetRequested;
		}

		_fieldValueChangedHandler = async (_, value) => await OnFieldValueChangedAsync(value);
		Field.ValueChanged += _fieldValueChangedHandler;
	}

	/// <summary>
	/// Loads JavaScript helpers after first render.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
		}
	}

	private async Task OnFieldValueChangedAsync(object? value)
	{
		// For most editors the value will be reflected in the UI immediately due to
		// data binding - however the Monaco Editor requires a manual update
		if (MonacoEditor != null && Field.DisplayOptions is FieldStringOptions fso && fso.Editor == FieldStringOptions.Editors.Monaco)
		{
			await SetMonacoValueAsync(value?.ToString() ?? string.Empty);
		}
	}

	private async void Form_ResetRequested(object? sender, EventArgs e)
	{
		// reset data to any Monaco editors
		if (MonacoEditor != null && Form != null && Field != null)
		{
			var value = Form.GetFieldStringValue(Field);
			var model = await MonacoEditor.GetModel();
			// when re-creating Monaco Editor (i.e toggling to/from ReadOnly)
			// this can cause an crash - do NOT ResetChanges on Form.SetEditItem
			await model.SetValue(value);
		}
	}

	private async Task OnMonacoEditorBlurAsync()
	{
		if (MonacoEditor != null && Form != null && Field != null)
		{
			// Only update if a de-bounce is outstanding
			if (DebounceWait > 0 && _monacoDebounceCts != null)
			{
				await CancelPendingUpdateAsync();

				var model = await MonacoEditor.GetModel();
				var value = await model.GetValue(EndOfLinePreference.LF, true);
				await Form.SetFieldValueAsync(Field, value);
			}

			Field.SuppressErrors = false;
		}
	}

	private async Task OnMonacoEditorKeyUpAsync()
	{
		if (MonacoEditor is null || Form is null || Field is null)
		{
			return;
		}

		// Cancel any pending update
		await CancelPendingUpdateAsync();

		_monacoDebounceCts = new CancellationTokenSource();
		var token = _monacoDebounceCts.Token;

		try
		{
			await Task.Delay(DebounceWait > 0 ? DebounceWait : 0, token);
			if (!token.IsCancellationRequested)
			{
				// Cancel and dispose after use
				await CancelPendingUpdateAsync();

				var model = await MonacoEditor.GetModel();
				var value = await model.GetValue(EndOfLinePreference.LF, true);
				await Form.SetFieldValueAsync(Field, value, false);
			}
		}
		catch (TaskCanceledException)
		{
			// Ignore, another key-up event occurred
		}
	}

	private async Task CancelPendingUpdateAsync()
	{
		if (_monacoDebounceCts != null)
		{
			await _monacoDebounceCts.CancelAsync();
			_monacoDebounceCts.Dispose();
			_monacoDebounceCts = null;
		}
	}

	private async Task OnMonacoInitAsync()
	{
		if (MonacoEditor != null && Form != null)
		{
			var value = Form.GetFieldStringValue(Field);
			await SetMonacoValueAsync(value);
		}
	}

	/// <summary>
	/// Handles select input changes and writes the updated value to the form.
	/// </summary>
	/// <param name="args">Change event args.</param>
	/// <param name="field">Target field.</param>
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
			if (MonacoEditor != null && Form != null)
			{
				var model = await MonacoEditor.GetModel();
				var oldValue = await model.GetValue(EndOfLinePreference.LF, true);
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

	private async Task UpdateValueViaCastAsync(ChangeEventArgs args, FormField<TItem> field)
	{
		try
		{
			var fieldType = field.GetFieldType();
			if (fieldType is null)
			{
				return;
			}

			await Form!.SetFieldValueAsync(field, ConvertEnteredValue(args.Value, fieldType, field)).ConfigureAwait(true);
		}
		catch
		{
			// Nothing to do...
		}
	}

	/// <summary>
	/// Converts an entered value to the field's type. GetFieldType has already unwrapped Nullable&lt;T&gt;, so whether
	/// the field can hold null is asked of the field itself. An empty entry clears a nullable value type; strings
	/// are kept as typed.
	/// </summary>
	private static object? ConvertEnteredValue(object? value, Type fieldType, FormField<TItem> field)
		=> fieldType != typeof(string) && field.GetFieldIsNullable() && string.IsNullOrEmpty(value?.ToString())
			? null
			: Convert.ChangeType(value ?? string.Empty, fieldType, CultureInfo.InvariantCulture);

	/// <summary>
	/// Clears inline editor styles for this field editor.
	/// </summary>
	public async Task ResetEditorCssAsync()
	{
		_commonModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
		await _commonModule.InvokeVoidAsync("clearInlineStyle", EditorDiv);
	}

	#region IDisposable

	/// <summary>
	/// Disposes managed resources.
	/// </summary>
	/// <param name="disposing">True when called from Dispose.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				Field.ValueChanged -= _fieldValueChangedHandler;
				Form.ResetRequested -= Form_ResetRequested;
				Form?.UnregisterFieldEditor(this);
				_monacoDebounceCts?.Cancel();
				_monacoDebounceCts?.Dispose();
				_monacoDebounceCts = null;
			}

			_disposedValue = true;
		}
	}

	/// <summary>
	/// Disposes this editor instance.
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}