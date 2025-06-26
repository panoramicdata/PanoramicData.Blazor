namespace PanoramicData.Blazor.Models
{
	internal class ElementPosition
	{
		public double Top { get; set; }

		public double Left { get; set; }

		public override bool Equals(object? obj)
		{
			if (obj is ElementPosition other)
			{
				return Top.Equals(other.Top) && Left.Equals(other.Left);
			}
			return false;
		}
	}
}