using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDWidgetDocumentation
{
	private readonly string _sampleHtml = "<p><strong>Hello</strong> from a widget.</p>";

	private const string _htmlExample = """
		<PDWidget Title="HTML Content"
		          WidgetType="PDWidgetType.Html"
		          Content="<p><strong>Hello</strong></p>"
		          Icon="fas fa-code" />
		""";

	private const string _clockExample = """
		<PDWidget Title="Local Time"
		          WidgetType="PDWidgetType.Clock"
		          Icon="fas fa-clock" />

		@* UTC clock: *@
		<PDWidget Title="UTC"
		          WidgetType="PDWidgetType.Clock"
		          ClockTimeZone="TimeZoneInfo.Utc" />
		""";

	private const string _customExample = """
		<PDWidget Title="Custom Content"
		          WidgetType="PDWidgetType.Custom">
		    <div class="p-3 text-center">
		        <span class="fas fa-puzzle-piece fa-2x"></span>
		        <p>Any Blazor content here</p>
		    </div>
		</PDWidget>
		""";
}
