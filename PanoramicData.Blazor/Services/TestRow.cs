using System;

namespace PanoramicData.Blazor.Services
{
	public class TestRow
	{
		public int IntField { get; set; }
		public DateTimeOffset DateField { get; set; }
		public bool BooleanField { get; set; }
		public string StringField { get; set; } = "";
	}
}
