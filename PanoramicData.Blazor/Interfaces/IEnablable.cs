namespace PanoramicData.Blazor.Interfaces;
internal interface IEnablable
{
	bool IsEnabled { get; set; }

	void Enable();

	void Disable();

	void SetEnabled(bool isEnabled);
}
