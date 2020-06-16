using System;

namespace PanoramicData.Blazor.Services
{
	/// <summary>
	/// This services provides showing a full-screen overlay which is normally used
	/// when awaiting a network response, during which the UI should be frozen.
	/// </summary>
	public interface IBlockOverlayService
	{
		/// <summary>
		/// This is the action to execute when Hide is called
		/// </summary>
		event Action? OnHide;

		/// <summary>
		/// This is the action to execute when Show is called
		/// </summary>
		event Action<string?>? OnShow;

		/// <summary>
		/// Hide the BlockOverlay
		/// </summary>
		void Hide();

		/// <summary>
		/// Display the BlockOverlay
		/// </summary>
		/// <param name="html">The html to include</param>
		void Show(string? html = null);
	}
}