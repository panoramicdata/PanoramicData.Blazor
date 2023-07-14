namespace PanoramicData.Blazor.Models;

public class TableState
{
	public IDictionary<string, ColumnState> Columns { get; set; } = new Dictionary<string, ColumnState>();
}
