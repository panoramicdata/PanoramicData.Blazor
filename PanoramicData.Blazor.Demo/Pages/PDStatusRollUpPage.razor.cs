using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDStatusRollUpPage
{
	// ── Node factories ─────────────────────────────────────────────
	private static PDStatusRollUpNode Node(RollUpStatus status, string title, string summary)
		=> new() { Status = status, Title = title, Summary = summary };

	private static PDStatusRollUpNode Node(RollUpStatus status, string title, string summary, string detail)
		=> new() { Status = status, Title = title, Summary = summary, Detail = detail };

	private static PDStatusRollUpNode Group(PDStatusRollUpNode node, params PDStatusRollUpNode[] children)
	{
		node.Children.AddRange(children);
		return node;
	}

	private static PDStatusRollUpNode[] WithExpandable(bool expandable, params PDStatusRollUpNode[] nodes)
	{
		foreach (var node in nodes)
		{
			node.Expandable = expandable;
		}

		return nodes;
	}

	// ── Simple leaf nodes ──────────────────────────────────────────
	private readonly PDStatusRollUpNode _greenLeaf = Node(RollUpStatus.Green, "Service A", "All checks passed.");
	private readonly PDStatusRollUpNode _amberLeaf = Node(RollUpStatus.Amber, "Service B", "Response time elevated — monitoring.");
	private readonly PDStatusRollUpNode _redLeaf = Node(RollUpStatus.Red, "Service C", "Connection refused.", "ECONNREFUSED 10.0.0.42:8080");
	private readonly PDStatusRollUpNode _grayLeaf = Node(RollUpStatus.Gray, "Service D", "Status unknown — agent unreachable.");

	// ── Server nodes with children ─────────────────────────────────
	private readonly PDStatusRollUpNode _serverOk = Group(
		Node(RollUpStatus.Green, "srv-prod-01", "All checks healthy."),
		Node(RollUpStatus.Green, "Connectivity", "HTTP 200 in 42 ms"),
		Node(RollUpStatus.Green, "Disk", "87 GB free (74 %)"),
		Node(RollUpStatus.Green, "Version", "v4.2.1104"),
		Node(RollUpStatus.Green, "VM node", "pdl-kvm-01"));

	private readonly PDStatusRollUpNode _serverWarn = Group(
		Node(RollUpStatus.Amber, "srv-prod-02", "Disk space is low."),
		Node(RollUpStatus.Green, "Connectivity", "HTTP 200 in 38 ms"),
		Node(RollUpStatus.Amber, "Disk", "12 GB free (8 %)", "Threshold: 15 GB"),
		Node(RollUpStatus.Green, "Version", "v4.2.1104"),
		Node(RollUpStatus.Green, "VM node", "pdl-kvm-02"));

	private readonly PDStatusRollUpNode _serverError = Group(
		Node(RollUpStatus.Red, "srv-prod-03", "Connection refused on port 8080.", "ECONNREFUSED 10.0.0.43:8080"),
		Node(RollUpStatus.Red, "Connectivity", "Connection refused", "ECONNREFUSED 10.0.0.43:8080"),
		Node(RollUpStatus.Gray, "Disk", "Unknown — agent not responding"),
		Node(RollUpStatus.Gray, "Version", "Unknown — agent not responding"),
		Node(RollUpStatus.Green, "VM node", "pdl-kvm-03"));

	// ── Deep three-level tree ──────────────────────────────────────
	private readonly PDStatusRollUpNode _deepTree = Group(
		Node(RollUpStatus.Amber, "Infrastructure", "1 warning across 2 clusters."),
		Group(
			Node(RollUpStatus.Green, "Prod cluster", "All 6 nodes healthy."),
			Node(RollUpStatus.Green, "pdl-kvm-01", "32 GB RAM · 8 vCPU"),
			Node(RollUpStatus.Green, "pdl-kvm-02", "32 GB RAM · 8 vCPU")),
		Group(
			Node(RollUpStatus.Amber, "Test cluster", "1 of 3 nodes has a warning."),
			Node(RollUpStatus.Green, "pdl-kvm-test-01", "16 GB RAM · 4 vCPU"),
			Node(RollUpStatus.Amber, "pdl-kvm-test-02", "Disk 92 % full", "Only 4 GB free"),
			Node(RollUpStatus.Green, "pdl-kvm-test-03", "16 GB RAM · 4 vCPU")));

	// ── Lazy-loaded example ────────────────────────────────────────
	// Root node has no children on load; OnBeforeExpand populates them.
	private readonly PDStatusRollUpNode _lazyRoot = Node(RollUpStatus.Amber, "Live Services", "Click to load current status…");

	private async Task<PDStatusRollUpNode?> OnLazyExpandAsync(PDStatusRollUpNode node)
	{
		// Simulate a 1-second API round-trip
		await Task.Delay(1000).ConfigureAwait(true);

		// Only populate the root node; leaf nodes (no further children) return null = unchanged
		if (node != _lazyRoot)
		{
			return null;
		}

		return Group(
			Node(RollUpStatus.Amber, node.Title, "1 service degraded — fetched at " + DateTime.Now.ToString("HH:mm:ss")),
			Node(RollUpStatus.Green, "Auth API", "Responding — 38 ms avg"),
			Node(RollUpStatus.Green, "Reporting API", "Responding — 92 ms avg"),
			Node(RollUpStatus.Amber, "Export Worker", "Queue depth elevated (142)", "Threshold: 50"),
			Node(RollUpStatus.Green, "Database", "All replicas in sync"));
	}

	// ── Lazy deep — 3 levels fetched independently ────────────────
	// Each level has no children until OnBeforeExpand populates them.
	private readonly PDStatusRollUpNode _lazyDeepRoot = Node(RollUpStatus.Gray, "Data Centres", "Click to load current status…");

	// Server nodes returned when a cluster (London/Amsterdam) is expanded.
	// Status is already known from monitoring; individual checks load lazily on the next click.
	private static readonly PDStatusRollUpNode[][] _lazyClusters =
	[
		WithExpandable(true,
			Node(RollUpStatus.Green, "web-01", "All checks passing — click to drill in"),
			Node(RollUpStatus.Amber, "web-02", "HTTP slow — click to drill in"),
			Node(RollUpStatus.Red, "db-01", "Disk critical — click to drill in")),
		WithExpandable(true,
			Node(RollUpStatus.Green, "web-03", "All checks passing — click to drill in"),
			Node(RollUpStatus.Green, "db-02", "All checks passing — click to drill in")),
	];

	private static readonly PDStatusRollUpNode[][] _lazyChecks =
	[
		// web-01
		WithExpandable(false,
			Node(RollUpStatus.Green, "HTTP", "200 OK in 38 ms"),
			Node(RollUpStatus.Green, "Disk", "210 GB free (71 %)"),
			Node(RollUpStatus.Green, "CPU", "12 % avg over 5 min")),
		// web-02
		WithExpandable(false,
			Node(RollUpStatus.Amber, "HTTP", "200 OK in 940 ms", "Threshold: 500 ms"),
			Node(RollUpStatus.Green, "Disk", "198 GB free (67 %)"),
			Node(RollUpStatus.Green, "CPU", "18 % avg over 5 min")),
		// db-01
		WithExpandable(false,
			Node(RollUpStatus.Green, "Replication", "Replica lag < 1 s"),
			Node(RollUpStatus.Red, "Disk", "4 GB free (3 %)", "Critical: < 5 GB"),
			Node(RollUpStatus.Green, "Connections", "42 / 200 in use")),
		// web-03
		WithExpandable(false,
			Node(RollUpStatus.Green, "HTTP", "200 OK in 51 ms"),
			Node(RollUpStatus.Green, "Disk", "175 GB free (59 %)"),
			Node(RollUpStatus.Green, "CPU", "9 % avg over 5 min")),
		// db-02
		WithExpandable(false,
			Node(RollUpStatus.Green, "Replication", "Replica lag < 1 s"),
			Node(RollUpStatus.Green, "Disk", "88 GB free (30 %)"),
			Node(RollUpStatus.Green, "Connections", "17 / 200 in use")),
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
			return CreateDataCentresNode();
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
			return new PDStatusRollUpNode
			{
				Status = GetWorstStatus(checks),
				Title = node.Title,
				Summary = "Checks fetched at " + DateTime.Now.ToString("HH:mm:ss"),
				Children = [.. checks]
			};
		}

		// Leaf nodes (individual checks) — nothing further to load
		return null;
	}

	private static PDStatusRollUpNode CreateDataCentresNode()
		=> Group(
			Node(RollUpStatus.Red, "Data Centres", "1 critical issue — fetched at " + DateTime.Now.ToString("HH:mm:ss")),
			WithExpandable(true,
				Node(RollUpStatus.Red, "London", "1 node critical — click to drill in"),
				Node(RollUpStatus.Green, "Amsterdam", "All nodes healthy — click to drill in")));

	private static RollUpStatus GetWorstStatus(IEnumerable<PDStatusRollUpNode> checks)
	{
		if (checks.Any(c => c.Status == RollUpStatus.Red))
		{
			return RollUpStatus.Red;
		}

		return checks.Any(c => c.Status == RollUpStatus.Amber) ? RollUpStatus.Amber : RollUpStatus.Green;
	}

	// ── Status bar ─────────────────────────────────────────────────
	private readonly PDStatusRollUpNode[] _statusBar =
	[
		Node(RollUpStatus.Green, "API", "Healthy"),
		Node(RollUpStatus.Green, "Database", "Healthy"),
		Node(RollUpStatus.Amber, "Cache", "Redis degraded — replica lag 4 s"),
		Node(RollUpStatus.Green, "Storage", "Healthy"),
		Node(RollUpStatus.Red, "Email", "SMTP relay unreachable", "smtp.example.com:587 — ECONNREFUSED"),
		Node(RollUpStatus.Gray, "Analytics", "Monitoring agent not responding")
	];
}
