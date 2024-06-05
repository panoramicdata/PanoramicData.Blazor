namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTextBoxPage
{
	private string _textArea = string.Empty;
	private PDTextArea? _textArea1;
	private PDTextArea? _textArea2;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private bool Visible { get; set; } = true;
	private bool Enabled { get; set; } = true;
	private string Value { get; set; } = "Hello World!";


	private void OnValueChanged(string value)
	{
		Value = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value", value)));
	}

	private void OnKeypress(KeyboardEventArgs args) => EventManager?.Add(new Event("Keypress", new EventArgument("Code", args.Code)));

	private async Task OnTextAreaChanged(string value)
	{
		_textArea = value;
		if (_textArea1 != null)
		{
			await _textArea1.SetValueAsync(value);
		}
		if (_textArea2 != null)
		{
			await _textArea2.SetValueAsync(value);
		}
	}
}
