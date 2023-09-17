namespace PanoramicData.Blazor.Interfaces;

public interface IDragItem
{
	bool CanDrag { get; }

	string Id { get; }
}
