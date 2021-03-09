using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDButtonPage
	{
		private int _clickCounter;
		private ShortcutKey _shortcut1 = ShortcutKey.Create("Shift-Ctrl-Digit1");
		private ShortcutKey _shortcut2 = ShortcutKey.Create("Shift-Ctrl-Digit2");
	}
}
