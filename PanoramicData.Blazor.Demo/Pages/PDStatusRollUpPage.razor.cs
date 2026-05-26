using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDStatusRollUpPage
{
	// ── Simple leaf nodes ──────────────────────────────────────────
	private readonly PDStatusRollUpNode _greenLeaf = new()
	{
		Status = RollUpStatus.Green,
		Title = "Service A",
		Summary = "All checks passed."
	};

	private readonly PDStatusRollUpNode _amberLeaf = new()
	{
		Status = RollUpStatus.Amber,
		Title = "Service B",
		Summary = "Response time elevated — monitoring."
	};

	private readonly PDStatusRollUpNode _redLeaf = new()
	{
		Status = RollUpStatus.Red,
		Title = "Service C",
		Summary = "Connection refused.",
		Detail = "ECONNREFUSED 10.0.0.42:8080"
	};

	private readonly PDStatusRollUpNode _grayLeaf = new()
	{
		Status = RollUpStatus.Gray,
		Title = "Service D",
		Summary = "Status unknown — agent unreachable."
	};

	// ── Server nodes with children ─────────────────────────────────
	private readonly PDStatusRollUpNode _serverOk = new()
	{
		Status = RollUpStatus.Green,
		Title = "srv-prod-01",
		Summary = "All checks healthy.",
		Children =
		[
			new() { Status = RollUpStatus.Green, Title = "Connectivity",  Summary = "HTTP 200 in 42 ms" },
			new() { Status = RollUpStatus.Green, Title = "Disk",          Summary = "87 GB free (74 %)" },
			new() { Status = RollUpStatus.Green, Title = "Version",       Summary = "v4.2.1104" },
			new() { Status = RollUpStatus.Green, Title = "VM node",       Summary = "pdl-kvm-01" }
		]
	};

	private readonly PDStatusRollUpNode _serverWarn = new()
	{
		Status = RollUpStatus.Amber,
		Title = "srv-prod-02",
		Summary = "Disk space is low.",
		Children =
		[
			new() { Status = RollUpStatus.Green, Title = "Connectivity",  Summary = "HTTP 200 in 38 ms" },
			new() { Status = RollUpStatus.Amber, Title = "Disk",          Summary = "12 GB free (8 %)", Detail = "Threshold: 15 GB" },
			new() { Status = RollUpStatus.Green, Title = "Version",       Summary = "v4.2.1104" },
			new() { Status = RollUpStatus.Green, Title = "VM node",       Summary = "pdl-kvm-02" }
		]
	};

	private readonly PDStatusRollUpNode _serverError = new()
	{
		Status = RollUpStatus.Red,
		Title = "srv-prod-03",
		Summary = "Connection refused on port 8080.",
		Detail = "ECONNREFUSED 10.0.0.43:8080",
		Children =
		[
			new() { Status = RollUpStatus.Red,  Title = "Connectivity",  Summary = "Connection refused", Detail = "ECONNREFUSED 10.0.0.43:8080" },
			new() { Status = RollUpStatus.Gray, Title = "Disk",          Summary = "Unknown — agent not responding" },
			new() { Status = RollUpStatus.Gray, Title = "Version",       Summary = "Unknown — agent not responding" },
			new() { Status = RollUpStatus.Green, Title = "VM node",      Summary = "pdl-kvm-03" }
		]
	};

	// ── Deep three-level tree ──────────────────────────────────────
	private readonly PDStatusRollUpNode _deepTree = new()
	{
		Status = RollUpStatus.Amber,
		Title = "Infrastructure",
		Summary = "1 warning across 2 clusters.",
		Children =
		[
			new()
			{
				Status = RollUpStatus.Green,
				Title = "Prod cluster",
				Summary = "All 6 nodes healthy.",
				Children =
				[
					new() { Status = RollUpStatus.Green, Title = "pdl-kvm-01", Summary = "32 GB RAM · 8 vCPU" },
					new() { Status = RollUpStatus.Green, Title = "pdl-kvm-02", Summary = "32 GB RAM · 8 vCPU" }
				]
			},
			new()
			{
				Status = RollUpStatus.Amber,
				Title = "Test cluster",
				Summary = "1 of 3 nodes has a warning.",
				Children =
				[
					new() { Status = RollUpStatus.Green, Title = "pdl-kvm-test-01", Summary = "16 GB RAM · 4 vCPU" },
					new() { Status = RollUpStatus.Amber, Title = "pdl-kvm-test-02", Summary = "Disk 92 % full",       Detail = "Only 4 GB free" },
					new() { Status = RollUpStatus.Green, Title = "pdl-kvm-test-03", Summary = "16 GB RAM · 4 vCPU" }
				]
			}
		]
	};

	// ── Lazy-loaded example ────────────────────────────────────────
	// Root node has no children on load; OnBeforeExpand populates them.
	private readonly PDStatusRollUpNode _lazyRoot = new()
	{
		Status = RollUpStatus.Amber,
		Title = "Live Services",
		Summary = "Click to load current status…"
	};

	private async Task<PDStatusRollUpNode?> OnLazyExpandAsync(PDStatusRollUpNode node)
	{
		// Simulate a 1-second API round-trip
		await Task.Delay(1000).ConfigureAwait(true);

		// Only populate the root node; leaf nodes (no further children) return null = unchanged
		if (node != _lazyRoot)
		{
			return null;
		}

		return new PDStatusRollUpNode
		{
			Status = RollUpStatus.Amber,
			Title = node.Title,
			Summary = "1 service degraded — fetched at " + DateTime.Now.ToString("HH:mm:ss"),
			Children =
			[
				new() { Status = RollUpStatus.Green, Title = "Auth API",      Summary = "Responding — 38 ms avg" },
				new() { Status = RollUpStatus.Green, Title = "Reporting API", Summary = "Responding — 92 ms avg" },
				new() { Status = RollUpStatus.Amber, Title = "Export Worker", Summary = "Queue depth elevated (142)", Detail = "Threshold: 50" },
				new() { Status = RollUpStatus.Green, Title = "Database",      Summary = "All replicas in sync" },
			]
		};
	}

	// ── Lazy deep — 3 levels fetched independently ────────────────
	// Each level has no children until OnBeforeExpand populates them.
	private readonly PDStatusRollUpNode _lazyDeepRoot = new()
	{
		Status = RollUpStatus.Gray,
		Title = "Data Centres",
		Summary = "Click to load current status…"
	};

	// Server nodes returned when a cluster (London/Amsterdam) is expanded.
	// Status is already known from monitoring; individual checks load lazily on the next click.
	private static readonly PDStatusRollUpNode[][] _lazyClusters =
	[
		[
			new() { Status = RollUpStatus.Green, Title = "web-01", Summary = "All checks passing — click to drill in",  Expandable = true },
			new() { Status = RollUpStatus.Amber, Title = "web-02", Summary = "HTTP slow — click to drill in",           Expandable = true },
			new() { Status = RollUpStatus.Red,   Title = "db-01",  Summary = "Disk critical — click to drill in",       Expandable = true },
		],
		[
			new() { Status = RollUpStatus.Green, Title = "web-03", Summary = "All checks passing — click to drill in",  Expandable = true },
			new() { Status = RollUpStatus.Green, Title = "db-02",  Summary = "All checks passing — click to drill in",  Expandable = true },
		]
	];

	private static readonly PDStatusRollUpNode[][] _lazyChecks =
	[
		// web-01
		[
			new() { Status = RollUpStatus.Green, Title = "HTTP",        Summary = "200 OK in 38 ms",           Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "Disk",        Summary = "210 GB free (71 %)",         Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "CPU",         Summary = "12 % avg over 5 min",        Expandable = false },
		],
		// web-02
		[
			new() { Status = RollUpStatus.Amber, Title = "HTTP",        Summary = "200 OK in 940 ms", Detail = "Threshold: 500 ms", Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "Disk",        Summary = "198 GB free (67 %)",         Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "CPU",         Summary = "18 % avg over 5 min",        Expandable = false },
		],
		// db-01
		[
			new() { Status = RollUpStatus.Green, Title = "Replication", Summary = "Replica lag < 1 s",          Expandable = false },
			new() { Status = RollUpStatus.Red,   Title = "Disk",        Summary = "4 GB free (3 %)", Detail = "Critical: < 5 GB", Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "Connections", Summary = "42 / 200 in use",            Expandable = false },
		],
		// web-03
		[
			new() { Status = RollUpStatus.Green, Title = "HTTP",        Summary = "200 OK in 51 ms",            Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "Disk",        Summary = "175 GB free (59 %)",         Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "CPU",         Summary = "9 % avg over 5 min",         Expandable = false },
		],
		// db-02
		[
			new() { Status = RollUpStatus.Green, Title = "Replication", Summary = "Replica lag < 1 s",          Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "Disk",        Summary = "88 GB free (30 %)",          Expandable = false },
			new() { Status = RollUpStatus.Green, Title = "Connections", Summary = "17 / 200 in use",            Expandable = false },
		],
	];

	// Maps a cluster node back to its check data by matching title.
	private static readonly (string ClusterName, string NodeTitle, int CheckIndex)[] _nodeCheckMap =
	[
		("London", "web-01", 0),
		("London", "web-02", 1),
		("London", "db-01",  2),
		("Amsterdam", "web-03", 3),
		("Amsterdam", "db-02",  4),
	];

	private async Task<PDStatusRollUpNode?> OnLazyDeepExpandAsync(PDStatusRollUpNode node)
	{
		// Level 0 — root opened: return cluster nodes only, no server children yet (600 ms delay)
		if (node == _lazyDeepRoot)
		{
			await Task.Delay(600).ConfigureAwait(true);
			return new PDStatusRollUpNode
			{
				Status = RollUpStatus.Red,
				Title = "Data Centres",
				Summary = "1 critical issue — fetched at " + DateTime.Now.ToString("HH:mm:ss"),
				Children =
				[
					new() { Status = RollUpStatus.Red,   Title = "London",    Summary = "1 node critical — click to drill in",  Expandable = true },
					new() { Status = RollUpStatus.Green, Title = "Amsterdam", Summary = "All nodes healthy — click to drill in", Expandable = true },
				]
			};
		}

		// Level 1 — a cluster node opened: return its server members (500 ms delay)
		if (node.Title is "London" or "Amsterdam")
		{
			await Task.Delay(500).ConfigureAwait(true);
			var clusterIndex = node.Title == "London" ? 0 : 1;
			return new PDStatusRollUpNode
			{
				Status = node.Status,
				Title = node.Title,
				Summary = "Servers fetched at " + DateTime.Now.ToString("HH:mm:ss"),
				Children = [.. _lazyClusters[clusterIndex]]
			};
		}

		// Level 2 — a server node opened: return its individual checks (800 ms delay)
		var map = _nodeCheckMap.FirstOrDefault(m => m.NodeTitle == node.Title);
		if (map != default)
		{
			await Task.Delay(800).ConfigureAwait(true);
			var checks = _lazyChecks[map.CheckIndex];
			var worstStatus = RollUpStatus.Green;
			if (checks.Any(c => c.Status == RollUpStatus.Red))
			{
				worstStatus = RollUpStatus.Red;
			}
			else if (checks.Any(c => c.Status == RollUpStatus.Amber))
			{
				worstStatus = RollUpStatus.Amber;
			}

			return new PDStatusRollUpNode
			{
				Status = worstStatus,
				Title = node.Title,
				Summary = "Checks fetched at " + DateTime.Now.ToString("HH:mm:ss"),
				Children = [.. checks]
			};
		}

		// Leaf nodes (individual checks) — nothing further to load
		return null;
	}

	// ── Status bar ─────────────────────────────────────────────────
	private readonly PDStatusRollUpNode[] _statusBar =
	[
		new() { Status = RollUpStatus.Green, Title = "API",       Summary = "Healthy" },
		new() { Status = RollUpStatus.Green, Title = "Database",  Summary = "Healthy" },
		new() { Status = RollUpStatus.Amber, Title = "Cache",     Summary = "Redis degraded — replica lag 4 s" },
		new() { Status = RollUpStatus.Green, Title = "Storage",   Summary = "Healthy" },
		new() { Status = RollUpStatus.Red,   Title = "Email",     Summary = "SMTP relay unreachable",            Detail = "smtp.example.com:587 — ECONNREFUSED" },
		new() { Status = RollUpStatus.Gray,  Title = "Analytics", Summary = "Monitoring agent not responding" }
	];
}
