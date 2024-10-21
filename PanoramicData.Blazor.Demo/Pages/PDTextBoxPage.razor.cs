namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTextBoxPage
{
	private string _text3 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc eget lectus a urna interdum eleifend. Phasellus pellentesque, lorem non dictum congue, nulla nunc gravida lectus, at maximus sem orci eget sem. Donec viverra suscipit libero, ut dapibus magna vestibulum quis. Vivamus sodales malesuada nunc quis iaculis. Donec non lectus velit. Proin mollis leo a ultrices semper. Ut ac lacinia nisi, eget aliquam quam. Curabitur pellentesque laoreet tristique. In vestibulum varius placerat. Nam sit amet venenatis elit. Morbi a pretium dolor. In massa justo, blandit efficitur lectus at, lobortis cursus nisi. Ut lacinia bibendum pellentesque. Vivamus blandit ante in libero finibus tincidunt. Donec iaculis egestas dui eget feugiat. Curabitur in tempus odio.";
	private string _textSelection = "";

	private PDTextArea? _textArea1;
	private PDTextArea? _textArea2;
	private PDTextArea? _textArea3;
	private string _textArea = string.Empty;
	private string _textBox2 = string.Empty;
	private string _textBox3 = string.Empty;
	private string _textBox4 = string.Empty;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private bool Visible { get; set; } = true;

	private bool Enabled { get; set; } = true;

	private string Value { get; set; } = "Hello World!";

	private void OnGetTextAreaSelection()
	{
		if (_textArea3 != null)
		{
			var selection = _textArea3.GetSelection();
			_textSelection = $"({selection.Start} - {selection.End}) {selection.Value})";
		}
	}

	private async Task OnSetTextAreaSelection()
	{
		if (_textArea3 != null)
		{
			await _textArea3.SetSelectionAsync(316, 338);
		}
	}

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

	private async Task OnTextAreaSelectionChanged(TextAreaSelection args)
	{
		EventManager?.Add(new Event("SelectionChanged",
			new EventArgument("Start", args.Start),
			new EventArgument("End", args.End),
			new EventArgument("Value", args.Value)));
	}
}
