using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

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
