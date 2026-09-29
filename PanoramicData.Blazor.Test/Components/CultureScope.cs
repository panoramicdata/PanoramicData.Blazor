using System.Globalization;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Sets the current culture and UI culture for the life of the scope, restoring the previous ones on dispose.
/// </summary>
/// <remarks>
/// The culture is held per thread and flows with the execution context, so a test that renders and fires
/// events from inside the scope sees it throughout, without affecting tests running in parallel.
/// </remarks>
internal sealed class CultureScope : IDisposable
{
	private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
	private readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;

	/// <summary>Switches to the named culture.</summary>
	/// <param name="name">The culture name, for example "de-DE".</param>
	public CultureScope(string name)
	{
		var culture = CultureInfo.GetCultureInfo(name);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
	}

	/// <summary>Restores the cultures in force when the scope was created.</summary>
	public void Dispose()
	{
		CultureInfo.CurrentCulture = _previousCulture;
		CultureInfo.CurrentUICulture = _previousUiCulture;
	}
}
