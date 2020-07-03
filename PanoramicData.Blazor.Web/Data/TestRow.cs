using System;

namespace PanoramicData.Blazor.Web.Data
{
	public class TestRow
	{
		public int IntField { get; set; }
		public DateTimeOffset DateField { get; set; }
		public string NameField { get; set; } = "";
		public bool BooleanField { get; set; }
		public string StringField { get; set; } = "";
	}
}
