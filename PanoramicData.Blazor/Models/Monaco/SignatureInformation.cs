namespace PanoramicData.Blazor.Models.Monaco;

public class SignatureInformation
{
	public int? ActiveParameter { get; set; }

	public string Documentation { get; set; } = string.Empty;

	public string Label { get; set; } = string.Empty;

	public ParameterInformation[] Parameters { get; set; } = [];
}


