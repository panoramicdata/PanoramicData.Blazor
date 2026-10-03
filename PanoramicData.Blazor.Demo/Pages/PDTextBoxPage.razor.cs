namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTextBoxPage
{
	protected string Text3 { get; set; } = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc eget lectus a urna interdum eleifend. Phasellus pellentesque, lorem non dictum congue, nulla nunc gravida lectus, at maximus sem orci eget sem. Donec viverra suscipit libero, ut dapibus magna vestibulum quis. Vivamus sodales malesuada nunc quis iaculis. Donec non lectus velit. Proin mollis leo a ultrices semper. Ut ac lacinia nisi, eget aliquam quam. Curabitur pellentesque laoreet tristique. In vestibulum varius placerat. Nam sit amet venenatis elit. Morbi a pretium dolor. In massa justo, blandit efficitur lectus at, lobortis cursus nisi. Ut lacinia bibendum pellentesque. Vivamus blandit ante in libero finibus tincidunt. Donec iaculis egestas dui eget feugiat. Curabitur in tempus odio.";
	private string _textSelection = "";

	protected PDTextArea? TextArea1 { get; set; }
	protected PDTextArea? TextArea2 { get; set; }
	protected PDTextArea? TextArea3 { get; set; }
	private string _textArea = string.Empty;
	protected string TextBox2 { get; set; } = string.Empty;
	protected string TextBox3 { get; set; } = string.Empty;
	protected string TextBox4 { get; set; } = string.Empty;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private bool Visible { get; set; } = true;

	private bool Enabled { get; set; } = true;

	private string Value { get; set; } = "Hello World!";

	private void OnGetTextAreaSelection()
	{
		if (TextArea3 != null)
		{
			var selection = TextArea3.GetSelection();
			_textSelection = $"{selection.Value} ({selection.Start} - {selection.End})";
		}
	}

	private async Task OnSetTextAreaSelection()
	{
		if (TextArea3 != null)
		{
			await TextArea3.SetSelectionAsync(316, 338);
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
		if (TextArea1 != null)
		{
			await TextArea1.SetValueAsync(value);
		}

		if (TextArea2 != null)
		{
			await TextArea2.SetValueAsync(value);
		}
	}

	private void OnTextAreaSelectionChanged(TextAreaSelection args)
	{
		EventManager?.Add(new Event("SelectionChanged",
			new EventArgument("Start", args.Start),
			new EventArgument("End", args.End),
			new EventArgument("Value", args.Value)));
	}
}
