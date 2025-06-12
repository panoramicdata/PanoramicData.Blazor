namespace PanoramicData.Blazor.Interfaces;

public interface ISelectable
{
	bool IsSelected { set; get; }

	bool IsEnabled { get; }
}
