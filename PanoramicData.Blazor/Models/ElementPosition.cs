namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents the position of an element on the page.
/// </summary>
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

	public override int GetHashCode()
			=> (Top, Left).GetHashCode();
}