using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreeMapPage
{
	private readonly TreeMapColourMode[] _colourModes =
	[
		TreeMapColourMode.Category,
		TreeMapColourMode.Depth,
		TreeMapColourMode.Heat,
		TreeMapColourMode.Custom
	];

	private int _maxDepth = 3;
	private DiskNode? _zoom;
	private DiskNode? _lastClicked;
	private string _zoomMessage = string.Empty;

	/// <summary>
	/// A folder or file. Folders report zero bytes of their own, exactly as a directory listing does,
	/// which is what the default Aggregate size mode exists to handle.
	/// </summary>
	public sealed class DiskNode(string name, long bytes, string category = "other", params DiskNode[] children)
	{
		public string Name { get; } = name;

		public long Bytes { get; } = bytes;

		public string Category { get; } = category;

		public List<DiskNode> Children { get; } = [.. children];

		public long TotalBytes => Bytes + Children.Sum(c => c.TotalBytes);

		public double AgeDays { get; init; }

		public string CustomColour { get; init; } = "#4c78a8";
	}

	private static string FormatBytes(double bytes)
	{
		string[] units = ["B", "KB", "MB", "GB", "TB"];
		var value = bytes;
		var unit = 0;

		while (value >= 1024 && unit < units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		return $"{value:0.#} {units[unit]}";
	}

	private static string GetTooltip(DiskNode node) => $"{node.Name} — {FormatBytes(node.TotalBytes)}";

	private static Func<DiskNode, string>? GetColourSelector(TreeMapColourMode mode)
		=> mode == TreeMapColourMode.Custom ? n => n.CustomColour : null;

	private void OnNodeClicked(DiskNode node) => _lastClicked = node;

	private void OnBeforeZoom(TreeMapBeforeZoomEventArgs<DiskNode> args)
	{
		if (args.To?.Name.StartsWith("Locked", StringComparison.OrdinalIgnoreCase) == true)
		{
			args.Cancel = true;
			_zoomMessage = $"Zoom into '{args.To.Name}' was cancelled by BeforeZoomChange.";
			return;
		}

		_zoomMessage = string.Empty;
	}

	private static DiskNode File(string name, long bytes, string category, double ageDays = 0)
		=> new(name, bytes, category) { AgeDays = ageDays, CustomColour = ColourFor(category) };

	private static string ColourFor(string category) => category switch
	{
		"video" => "#e45756",
		"image" => "#f58518",
		"document" => "#4c78a8",
		"code" => "#54a24b",
		"archive" => "#b279a2",
		_ => "#9d9d9d"
	};

	private static readonly DiskNode _disk = new("C:\\", 0, "other",
		new DiskNode("Users", 0, "other",
			new DiskNode("david", 0, "other",
				File("holiday.mp4", 4_800_000_000, "video", 420),
				File("presentation.pptx", 42_000_000, "document", 12),
				new DiskNode("Pictures", 0, "image",
					File("dsc_0001.raw", 38_000_000, "image", 200),
					File("dsc_0002.raw", 41_000_000, "image", 198),
					File("dsc_0003.raw", 39_500_000, "image", 195),
					File("thumbnails.db", 2_400_000, "other", 5)),
				new DiskNode("Documents", 0, "document",
					File("accounts-2025.xlsx", 8_400_000, "document", 60),
					File("contract.pdf", 1_200_000, "document", 340),
					File("notes.md", 48_000, "document", 2))),
			new DiskNode("shared", 0, "other",
				File("archive.zip", 900_000_000, "archive", 730),
				File("backup.tar.gz", 1_400_000_000, "archive", 90))),
		new DiskNode("Windows", 0, "other",
			File("winsxs.dat", 12_000_000_000, "other", 900),
			File("pagefile.sys", 8_000_000_000, "other", 1),
			new DiskNode("System32", 0, "code",
				File("ntoskrnl.exe", 11_000_000, "code", 400),
				File("kernel32.dll", 760_000, "code", 400),
				File("drivers.cab", 320_000_000, "archive", 400))),
		new DiskNode("Projects", 0, "code",
			new DiskNode("magic-suite", 0, "code",
				File("bin", 2_100_000_000, "code", 1),
				File("obj", 1_800_000_000, "code", 1),
				File("src.zip", 240_000_000, "archive", 30)),
			new DiskNode("blazor", 0, "code",
				File("node_modules", 1_100_000_000, "code", 20),
				File("dist", 180_000_000, "code", 3))));

	// A source that already reports subtree totals, as pg_total_relation_size would.
	private static readonly DiskNode _database = new("DataMagic", 96_000_000_000, "other",
		new DiskNode("Tenant A", 52_000_000_000, "other",
			new DiskNode("measurements", 44_000_000_000, "other"),
			new DiskNode("devices", 5_000_000_000, "other"),
			new DiskNode("indexes", 3_000_000_000, "other")),
		new DiskNode("Tenant B", 30_000_000_000, "other",
			new DiskNode("measurements", 26_000_000_000, "other"),
			new DiskNode("devices", 4_000_000_000, "other")),
		new DiskNode("Tenant C", 14_000_000_000, "other",
			new DiskNode("measurements", 12_500_000_000, "other"),
			new DiskNode("devices", 1_500_000_000, "other")));

	private static readonly DiskNode _locked = new("root", 0, "other",
		new DiskNode("Open folder", 0, "code",
			File("a.txt", 500, "document"),
			File("b.txt", 900, "document")),
		new DiskNode("Locked folder", 0, "archive",
			File("secret.bin", 2_000, "archive"),
			File("secret2.bin", 1_000, "archive")));

	private static readonly DiskNode _empty = new("empty", 0);

	private static readonly DiskNode _large = BuildLarge();

	private static DiskNode BuildLarge()
	{
		// Deterministic so the demo looks the same on every visit.
		var random = new Random(1701);
		string[] categories = ["video", "image", "document", "code", "archive", "other"];

		var branches = Enumerable.Range(0, 100)
			.Select(i => new DiskNode(
				$"branch-{i:000}",
				0,
				categories[i % categories.Length],
				[.. Enumerable.Range(0, 100).Select(j => File($"leaf-{i:000}-{j:000}", random.Next(1_000, 10_000_000), categories[j % categories.Length]))]))
			.ToArray();

		return new DiskNode("10,000 leaves", 0, "other", branches);
	}
}
