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

    // ── Node factories ─────────────────────────────────────────────
    private static PDStatusCascadeNode Node(StatusType status, string title, string summary)
        => new() { Status = status, Title = title, Summary = summary };

    private static PDStatusCascadeNode Node(StatusType status, string title, string summary, string detail)
        => new() { Status = status, Title = title, Summary = summary, Detail = detail };

    private static PDStatusCascadeNode Group(PDStatusCascadeNode node, params PDStatusCascadeNode[] children)
    {
        node.Children.AddRange(children);
        return node;
    }

    private static PDStatusCascadeNode WithExpandable(PDStatusCascadeNode node, bool expandable)
    {
        node.Expandable = expandable;
        return node;
    }

    // ── Built-in leaf nodes ────────────────────────────────────────
    private readonly PDStatusCascadeNode _greenLeaf = Node(StatusType.Green, "Service A", "All checks passed.");
    private readonly PDStatusCascadeNode _amberLeaf = Node(StatusType.Amber, "Service B", "Response time elevated — monitoring.");
    private readonly PDStatusCascadeNode _redLeaf   = Node(StatusType.Red,   "Service C", "Connection refused.", "ECONNREFUSED 10.0.0.42:8080");
    private readonly PDStatusCascadeNode _grayLeaf  = Node(StatusType.Gray,  "Service D", "Status unknown — agent unreachable.");

    // ── Custom-status leaf nodes ───────────────────────────────────
    private readonly PDStatusCascadeNode _runningNode   = Node(Running,   "Export Job",  "Row 4,820 of 12,000 — 40 % complete.");
    private readonly PDStatusCascadeNode _pendingNode   = Node(Pending,   "Sync Job",    "Queued — waiting for prior job to complete.");
    private readonly PDStatusCascadeNode _pausedNode    = Node(Paused,    "Archive Job", "Paused by operator at 14:32.");
    private readonly PDStatusCascadeNode _cancelledNode = Node(Cancelled, "Import Job",  "Cancelled by user.", "Cancelled at 2025-07-16T09:14:05Z");
    private readonly PDStatusCascadeNode _deferredNode  = Node(Deferred,  "Report Job",  "Deferred until off-peak window.");
    private readonly PDStatusCascadeNode _stoppedNode   = Node(Stopped,   "Cleanup Job", "Stopped — maximum runtime exceeded.", "Limit: 120 s · Actual: 183 s");

    // ── Mixed hierarchy ────────────────────────────────────────────
    private readonly PDStatusCascadeNode _jobCluster = Group(
        Node(StatusType.Amber, "Nightly Jobs", "3 of 6 jobs completed; 1 running, 1 failed, 1 cancelled."),
        Node(StatusType.Green, "DB Backup",       "Completed in 4 m 12 s."),
        Node(StatusType.Green, "Log Archive",     "Completed in 1 m 08 s."),
        Node(StatusType.Green, "Cache Warm",      "Completed in 22 s."),
        Node(Running,          "Data Export",     "Row 8,102 of 12,000."),
        Node(StatusType.Red,   "Report Generate", "Unhandled exception.", "System.OutOfMemoryException at ReportEngine.cs:214"),
        Node(Cancelled,        "Sync to S3",      "Cancelled — dependency failed."));

    // ── Lazy-loaded example ────────────────────────────────────────
    private readonly PDStatusCascadeNode _lazyJobRoot = WithExpandable(Node(StatusType.Gray, "Report Jobs", "Click to load current job statuses."), true);

    public async Task<PDStatusCascadeNode?> OnLazyJobExpandAsync(PDStatusCascadeNode node)
    {
        // Simulate an API call
        await Task.Delay(900);

        return Group(
            Node(StatusType.Amber, "Report Jobs", "4 jobs tracked; 1 running, 1 pending, 1 failed."),
            Node(StatusType.Green, "Daily Summary",    "Completed 06:00 — 2.3 MB output."),
            Node(Running,          "Weekly Rollup",    "Processing 14 of 52 data sets."),
            Node(Pending,          "Monthly Forecast", "Queued — starts after Weekly Rollup."),
            Node(StatusType.Red,   "Audit Export",     "Failed — source table locked.", "SqlException: object 'AuditLog' is locked by process 2841"));
    }

    // ── Lazy deep — 3 levels fetched independently ────────────────
    // Root → Pipelines; Level 1 → Jobs in a pipeline; Level 2 → Steps in a job.
    // Custom statuses (Running, Pending, Stopped) appear at every level.

    private readonly PDStatusCascadeNode _lazyDeepRoot = WithExpandable(Node(StatusType.Gray, "Pipelines", "Click to load pipeline statuses."), true);

    // Jobs returned when each pipeline is expanded (indexed by pipeline name)
    private static readonly Dictionary<string, PDStatusCascadeNode[]> _pipelineJobs = new()
    {
        ["ETL Pipeline"] =
        [
            WithExpandable(Node(StatusType.Green, "Extract",   "12,400 rows read in 4 s."), true),
            WithExpandable(Node(StatusType.Green, "Transform", "All rules passed."), true),
            WithExpandable(Node(StatusType.Red,   "Load",      "Target table locked — job failed."), true),
        ],
        ["Report Pipeline"] =
        [
            WithExpandable(Node(StatusType.Green, "Aggregate",  "Completed in 2 m 14 s."), true),
            WithExpandable(Node(Running,          "Render",     "Page 8 of 24 — 33 % complete."), true),
            WithExpandable(Node(Pending,          "Distribute", "Waiting for Render to complete."), true),
        ],
        ["Sync Pipeline"] =
        [
            WithExpandable(Node(Stopped,         "Fetch",  "Stopped — max runtime exceeded.", "Limit: 60 s · Actual: 94 s"), true),
            WithExpandable(Node(StatusType.Gray, "Merge",  "Did not start."), false),
            WithExpandable(Node(StatusType.Gray, "Commit", "Did not start."), false),
        ]
    };

    // Steps returned when each job is expanded (keyed by job title)
    private static readonly Dictionary<string, PDStatusCascadeNode[]> _jobSteps = new()
    {
        ["Extract"]   = [Node(StatusType.Green, "Open connection",  "Connected in 12 ms."),       Node(StatusType.Green, "Read rows",        "12,400 rows in 3.8 s."),        Node(StatusType.Green, "Close connection", "Closed cleanly.")],
        ["Transform"] = [Node(StatusType.Green, "Validate schema",  "All 18 columns matched."),   Node(StatusType.Green, "Apply rules",      "0 rule violations."),           Node(StatusType.Green, "Map output",       "12,400 rows mapped.")],
        ["Load"]      = [Node(StatusType.Green, "Open transaction", "Transaction started."),      Node(StatusType.Red,   "Acquire lock",     "Table locked by process 841.", "LOCK_TIMEOUT after 30 s"), Node(StatusType.Gray, "Commit", "Not reached.")],
        ["Aggregate"] = [Node(StatusType.Green, "Group data",       "48 groups computed."),       Node(StatusType.Green, "Calculate totals", "All aggregates valid.")],
        ["Render"]    = [Node(StatusType.Green, "Load template",    "Template loaded in 80 ms."), Node(Running,          "Render pages",     "Page 8 of 24 in progress."),    Node(Pending,          "Write output",     "Waiting for render.")],
        ["Distribute"]= [Node(Pending,          "Send email",       "Waiting for upstream job."), Node(Pending,          "Upload to S3",     "Waiting for upstream job.")],
        ["Fetch"]     = [Node(StatusType.Green, "DNS resolve",      "Resolved in 4 ms."),         Node(Stopped,          "Download data",    "Stopped after 94 s.", "RuntimeLimitExceededException")],
    };

    // Worst-first: the first rule matched by any step decides the parent status (Green when none match)
    private static readonly (StatusType[] Matches, StatusType Result)[] _worstStatusRules =
    [
        ([StatusType.Red], StatusType.Red),
        ([StatusType.Amber, Stopped], StatusType.Amber),
        ([Running, Pending], Running),
    ];

    private async Task<PDStatusCascadeNode?> OnLazyDeepExpandAsync(PDStatusCascadeNode node)
    {
        // Level 0 — root opened: return pipeline summaries (600 ms)
        if (node == _lazyDeepRoot)
        {
            await Task.Delay(600).ConfigureAwait(true);
            return CreatePipelinesNode();
        }

        // Level 1 — a pipeline opened: return its jobs (500 ms)
        if (_pipelineJobs.TryGetValue(node.Title, out var jobs))
        {
            await Task.Delay(500).ConfigureAwait(true);
            return Group(Node(node.Status, node.Title, "Jobs fetched at " + DateTime.Now.ToString("HH:mm:ss")), jobs);
        }

        // Level 2 — a job opened: return its steps (400 ms)
        if (_jobSteps.TryGetValue(node.Title, out var steps))
        {
            await Task.Delay(400).ConfigureAwait(true);
            return Group(Node(GetWorstStatus(steps), node.Title, "Steps fetched at " + DateTime.Now.ToString("HH:mm:ss")), steps);
        }

        // Leaf steps — nothing further to load
        return null;
    }

    private static PDStatusCascadeNode CreatePipelinesNode()
        => Group(
            Node(StatusType.Red, "Pipelines", "1 pipeline failed, 1 running — fetched at " + DateTime.Now.ToString("HH:mm:ss")),
            WithExpandable(Node(StatusType.Red, "ETL Pipeline",    "Load stage failed — click to drill in."), true),
            WithExpandable(Node(Running,        "Report Pipeline", "Render stage running — click to drill in."), true),
            WithExpandable(Node(Stopped,        "Sync Pipeline",   "Fetch stage stopped — click to drill in."), true));

    private static StatusType GetWorstStatus(PDStatusCascadeNode[] steps)
        => _worstStatusRules.FirstOrDefault(rule => steps.Any(step => rule.Matches.Contains(step.Status))).Result ?? StatusType.Green;
}
