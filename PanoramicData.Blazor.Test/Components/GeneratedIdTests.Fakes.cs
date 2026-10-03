using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Components whose constructors race another component's construction, used by the generated id tests.
/// </summary>
public partial class GeneratedIdTests
{
	/// <summary>A dashboard that constructs another component before it is initialised.</summary>
	private sealed class RacedDashboard : PDDashboard
	{
		/// <summary>Constructs the dashboard, then the interloper.</summary>
		public RacedDashboard() => Interloper = new PDComponentBase();

		/// <summary>Gets the component constructed after this one.</summary>
		public PDComponentBase Interloper { get; }
	}

	/// <summary>A widget that constructs another component before it is initialised.</summary>
	private sealed class RacedWidget : PDWidget
	{
		/// <summary>Constructs the widget, then the interloper.</summary>
		public RacedWidget() => Interloper = new PDComponentBase();

		/// <summary>Gets the component constructed after this one.</summary>
		public PDComponentBase Interloper { get; }
	}

	/// <summary>Graph controls that construct another component before they are initialised.</summary>
	private sealed class RacedGraphControls : PDGraphControls<object>
	{
		/// <summary>Constructs the controls, then the interloper.</summary>
		public RacedGraphControls() => Interloper = new PDComponentBase();

		/// <summary>Gets the component constructed after this one.</summary>
		public PDComponentBase Interloper { get; }
	}

	/// <summary>A graph information panel that constructs another component before it is initialised.</summary>
	private sealed class RacedGraphInfo : PDGraphInfo<object>
	{
		/// <summary>Constructs the panel, then the interloper.</summary>
		public RacedGraphInfo() => Interloper = new PDComponentBase();

		/// <summary>Gets the component constructed after this one.</summary>
		public PDComponentBase Interloper { get; }
	}

	/// <summary>A graph selection panel that constructs another component before it is initialised.</summary>
	private sealed class RacedGraphSelectionInfo : PDGraphSelectionInfo<object>
	{
		/// <summary>Constructs the panel, then the interloper.</summary>
		public RacedGraphSelectionInfo() => Interloper = new PDComponentBase();

		/// <summary>Gets the component constructed after this one.</summary>
		public PDComponentBase Interloper { get; }
	}

	/// <summary>A graph viewer that constructs another component before it is initialised.</summary>
	private sealed class RacedGraphViewer : PDGraphViewer<GraphData>
	{
		/// <summary>Constructs the viewer, then the interloper.</summary>
		public RacedGraphViewer() => Interloper = new PDComponentBase();

		/// <summary>Gets the component constructed after this one.</summary>
		public PDComponentBase Interloper { get; }
	}

	/// <summary>A graph selection panel whose initialisation can be run directly, off any renderer.</summary>
	private sealed class InitialisableSelectionInfo : PDGraphSelectionInfo<object>
	{
		/// <summary>Constructs a panel, initialises it and returns the id it chose.</summary>
		/// <returns>The panel's id after initialisation.</returns>
		public static string CreateAndInitialise()
		{
			var panel = new InitialisableSelectionInfo();
			panel.OnInitialized();
			return panel.Id;
		}
	}
}
