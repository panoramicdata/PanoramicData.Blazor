using PanoramicData.Blazor.Arguments;
using System;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Interfaces
{
	public interface INavigationCancelService
	{
		/// <summary>
		/// Event raised before a navigation occurs.
		/// </summary>
		event EventHandler<BeforeNavigateEventArgs> BeforeNavigate;

		///// <summary>
		///// Raises the BeforeNavigate event and returns whether the navigation request
		///// should be canceled.
		///// </summary>
		///// <param name="target">The destination relative or absolute target address.</param>
		///// <returns>true if the navigation should proceed otherwise false.</returns>
		//bool OnBeforeNavigate(string target);

		///// <summary>
		///// Raises the BeforeNavigate event and returns whether the navigation request
		///// should be canceled.
		///// </summary>
		///// <param name="target">The destination relative or absolute target address.</param>
		///// <returns>true if the navigation should proceed otherwise false.</returns>
		//Task<bool> OnBeforeNavigateAsync(string target = "");

		/// <summary>
		/// Determines whether the intended operation should proceed or be canceled.
		/// </summary>
		/// <param name="target">Optional data for intended operation. May be a target URL or operation name etc.</param>
		/// <returns>true if the operation should proceed otherwise false.</returns>
		Task<bool> ProceedAsync(string target = "");
	}
}
