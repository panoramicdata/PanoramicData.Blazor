namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDStatusCascadePage
{
    // ── Custom StatusType definitions ──────────────────────────────
    // Defined once here; in a real app these would live in a shared constants class.
    private static readonly StatusType Running   = StatusType.Custom("running",   "fas fa-spinner fa-spin", "text-info");
    private static readonly StatusType Pending   = StatusType.Custom("pending",   "fas fa-clock",           "text-primary");
    private static readonly StatusType Paused    = StatusType.Custom("paused",    "fas fa-pause-circle",    "text-warning");
    private static readonly StatusType Cancelled = StatusType.Custom("cancelled", "fas fa-ban",             "text-secondary");
    private static readonly StatusType Deferred  = StatusType.Custom("deferred",  "fas fa-forward",         "text-muted");
    private static readonly StatusType Stopped   = StatusType.Custom("stopped",   "fas fa-stop-circle",     "text-danger");

    // ── Built-in leaf nodes ────────────────────────────────────────
    private readonly PDStatusCascadeNode _greenLeaf = new()
    {
        Status  = StatusType.Green,
        Title   = "Service A",
        Summary = "All checks passed."
    };

    private readonly PDStatusCascadeNode _amberLeaf = new()
    {
        Status  = StatusType.Amber,
        Title   = "Service B",
        Summary = "Response time elevated — monitoring."
    };

    private readonly PDStatusCascadeNode _redLeaf = new()
    {
        Status  = StatusType.Red,
        Title   = "Service C",
        Summary = "Connection refused.",
        Detail  = "ECONNREFUSED 10.0.0.42:8080"
    };

    private readonly PDStatusCascadeNode _grayLeaf = new()
    {
        Status  = StatusType.Gray,
        Title   = "Service D",
        Summary = "Status unknown — agent unreachable."
    };

    // ── Custom-status leaf nodes ───────────────────────────────────
    private readonly PDStatusCascadeNode _runningNode = new()
    {
        Status  = Running,
        Title   = "Export Job",
        Summary = "Row 4,820 of 12,000 — 40 % complete."
    };

    private readonly PDStatusCascadeNode _pendingNode = new()
    {
        Status  = Pending,
        Title   = "Sync Job",
        Summary = "Queued — waiting for prior job to complete."
    };

    private readonly PDStatusCascadeNode _pausedNode = new()
    {
        Status  = Paused,
        Title   = "Archive Job",
        Summary = "Paused by operator at 14:32."
    };

    private readonly PDStatusCascadeNode _cancelledNode = new()
    {
        Status  = Cancelled,
        Title   = "Import Job",
        Summary = "Cancelled by user.",
        Detail  = "Cancelled at 2025-07-16T09:14:05Z"
    };

    private readonly PDStatusCascadeNode _deferredNode = new()
    {
        Status  = Deferred,
        Title   = "Report Job",
        Summary = "Deferred until off-peak window."
    };

    private readonly PDStatusCascadeNode _stoppedNode = new()
    {
        Status  = Stopped,
        Title   = "Cleanup Job",
        Summary = "Stopped — maximum runtime exceeded.",
        Detail  = "Limit: 120 s · Actual: 183 s"
    };

    // ── Mixed hierarchy ────────────────────────────────────────────
    private readonly PDStatusCascadeNode _jobCluster = new()
    {
        Status  = StatusType.Amber,
        Title   = "Nightly Jobs",
        Summary = "3 of 6 jobs completed; 1 running, 1 failed, 1 cancelled.",
        Children =
        [
            new() { Status = StatusType.Green, Title = "DB Backup",      Summary = "Completed in 4 m 12 s." },
            new() { Status = StatusType.Green, Title = "Log Archive",     Summary = "Completed in 1 m 08 s." },
            new() { Status = StatusType.Green, Title = "Cache Warm",      Summary = "Completed in 22 s." },
            new() { Status = Running,          Title = "Data Export",     Summary = "Row 8,102 of 12,000." },
            new() { Status = StatusType.Red,   Title = "Report Generate", Summary = "Unhandled exception.", Detail = "System.OutOfMemoryException at ReportEngine.cs:214" },
            new() { Status = Cancelled,        Title = "Sync to S3",      Summary = "Cancelled — dependency failed." }
        ]
    };

    // ── Lazy-loaded example ────────────────────────────────────────
    private readonly PDStatusCascadeNode _lazyJobRoot = new()
    {
        Status     = StatusType.Gray,
        Title      = "Report Jobs",
        Summary    = "Click to load current job statuses.",
        Expandable = true
    };

    public async Task<PDStatusCascadeNode?> OnLazyJobExpandAsync(PDStatusCascadeNode node)
    {
        // Simulate an API call
        await Task.Delay(900);

        return new PDStatusCascadeNode
        {
            Status  = StatusType.Amber,
            Title   = "Report Jobs",
            Summary = "4 jobs tracked; 1 running, 1 pending, 1 failed.",
            Children =
            [
                new() { Status = StatusType.Green, Title = "Daily Summary",    Summary = "Completed 06:00 — 2.3 MB output." },
                new() { Status = Running,          Title = "Weekly Rollup",    Summary = "Processing 14 of 52 data sets." },
                new() { Status = Pending,          Title = "Monthly Forecast", Summary = "Queued — starts after Weekly Rollup." },
                new() { Status = StatusType.Red,   Title = "Audit Export",     Summary = "Failed — source table locked.", Detail = "SqlException: object 'AuditLog' is locked by process 2841" }
            ]
        };
    }

    // ── Lazy deep — 3 levels fetched independently ────────────────
    // Root → Pipelines; Level 1 → Jobs in a pipeline; Level 2 → Steps in a job.
    // Custom statuses (Running, Pending, Stopped) appear at every level.

    private readonly PDStatusCascadeNode _lazyDeepRoot = new()
    {
        Status     = StatusType.Gray,
        Title      = "Pipelines",
        Summary    = "Click to load pipeline statuses.",
        Expandable = true
    };

    // Jobs returned when each pipeline is expanded (indexed by pipeline name)
    private static readonly Dictionary<string, PDStatusCascadeNode[]> _pipelineJobs = new()
    {
        ["ETL Pipeline"] =
        [
            new() { Status = StatusType.Green, Title = "Extract",   Summary = "12,400 rows read in 4 s.",          Expandable = true },
            new() { Status = StatusType.Green, Title = "Transform", Summary = "All rules passed.",                  Expandable = true },
            new() { Status = StatusType.Red,   Title = "Load",      Summary = "Target table locked — job failed.",  Expandable = true },
        ],
        ["Report Pipeline"] =
        [
            new() { Status = StatusType.Green, Title = "Aggregate", Summary = "Completed in 2 m 14 s.",            Expandable = true },
            new() { Status = Running,          Title = "Render",    Summary = "Page 8 of 24 — 33 % complete.",      Expandable = true },
            new() { Status = Pending,          Title = "Distribute", Summary = "Waiting for Render to complete.",   Expandable = true },
        ],
        ["Sync Pipeline"] =
        [
            new() { Status = Stopped, Title = "Fetch",  Summary = "Stopped — max runtime exceeded.", Detail = "Limit: 60 s · Actual: 94 s", Expandable = true },
            new() { Status = StatusType.Gray, Title = "Merge",  Summary = "Did not start.",          Expandable = false },
            new() { Status = StatusType.Gray, Title = "Commit", Summary = "Did not start.",          Expandable = false },
        ]
    };

    // Steps returned when each job is expanded (keyed by job title)
    private static readonly Dictionary<string, PDStatusCascadeNode[]> _jobSteps = new()
    {
        ["Extract"]   = [ new() { Status = StatusType.Green, Title = "Open connection",  Summary = "Connected in 12 ms." },  new() { Status = StatusType.Green, Title = "Read rows",        Summary = "12,400 rows in 3.8 s." },    new() { Status = StatusType.Green, Title = "Close connection", Summary = "Closed cleanly." } ],
        ["Transform"] = [ new() { Status = StatusType.Green, Title = "Validate schema",  Summary = "All 18 columns matched." }, new() { Status = StatusType.Green, Title = "Apply rules",      Summary = "0 rule violations." },       new() { Status = StatusType.Green, Title = "Map output",      Summary = "12,400 rows mapped." } ],
        ["Load"]      = [ new() { Status = StatusType.Green, Title = "Open transaction", Summary = "Transaction started." },   new() { Status = StatusType.Red,   Title = "Acquire lock",    Summary = "Table locked by process 841.", Detail = "LOCK_TIMEOUT after 30 s" }, new() { Status = StatusType.Gray, Title = "Commit", Summary = "Not reached." } ],
        ["Aggregate"] = [ new() { Status = StatusType.Green, Title = "Group data",       Summary = "48 groups computed." },    new() { Status = StatusType.Green, Title = "Calculate totals", Summary = "All aggregates valid." } ],
        ["Render"]    = [ new() { Status = StatusType.Green, Title = "Load template",    Summary = "Template loaded in 80 ms." }, new() { Status = Running,        Title = "Render pages",    Summary = "Page 8 of 24 in progress." },  new() { Status = Pending, Title = "Write output", Summary = "Waiting for render." } ],
        ["Distribute"]= [ new() { Status = Pending, Title = "Send email",   Summary = "Waiting for upstream job." },  new() { Status = Pending, Title = "Upload to S3", Summary = "Waiting for upstream job." } ],
        ["Fetch"]     = [ new() { Status = StatusType.Green, Title = "DNS resolve",      Summary = "Resolved in 4 ms." },       new() { Status = Stopped,          Title = "Download data",   Summary = "Stopped after 94 s.",          Detail = "RuntimeLimitExceededException" } ],
    };

    private async Task<PDStatusCascadeNode?> OnLazyDeepExpandAsync(PDStatusCascadeNode node)
    {
        // Level 0 — root opened: return pipeline summaries (600 ms)
        if (node == _lazyDeepRoot)
        {
            await Task.Delay(600).ConfigureAwait(true);
            return new PDStatusCascadeNode
            {
                Status  = StatusType.Red,
                Title   = "Pipelines",
                Summary = "1 pipeline failed, 1 running — fetched at " + DateTime.Now.ToString("HH:mm:ss"),
                Children =
                [
                    new() { Status = StatusType.Red,   Title = "ETL Pipeline",    Summary = "Load stage failed — click to drill in.",    Expandable = true },
                    new() { Status = Running,          Title = "Report Pipeline", Summary = "Render stage running — click to drill in.", Expandable = true },
                    new() { Status = Stopped,          Title = "Sync Pipeline",   Summary = "Fetch stage stopped — click to drill in.",  Expandable = true },
                ]
            };
        }

        // Level 1 — a pipeline opened: return its jobs (500 ms)
        if (_pipelineJobs.TryGetValue(node.Title, out var jobs))
        {
            await Task.Delay(500).ConfigureAwait(true);
            return new PDStatusCascadeNode
            {
                Status   = node.Status,
                Title    = node.Title,
                Summary  = "Jobs fetched at " + DateTime.Now.ToString("HH:mm:ss"),
                Children = [.. jobs]
            };
        }

        // Level 2 — a job opened: return its steps (400 ms)
        if (_jobSteps.TryGetValue(node.Title, out var steps))
        {
            await Task.Delay(400).ConfigureAwait(true);
            var worst = steps.Any(s => s.Status == StatusType.Red) ? StatusType.Red
                      : steps.Any(s => s.Status == StatusType.Amber || s.Status == Stopped) ? StatusType.Amber
                      : steps.Any(s => s.Status == Running || s.Status == Pending) ? Running
                      : StatusType.Green;
            return new PDStatusCascadeNode
            {
                Status   = worst,
                Title    = node.Title,
                Summary  = "Steps fetched at " + DateTime.Now.ToString("HH:mm:ss"),
                Children = [.. steps]
            };
        }

        // Leaf steps — nothing further to load
        return null;
    }
}
