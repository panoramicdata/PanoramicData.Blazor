using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTilesDocumentation
{
	// Basic logos for examples
	private readonly List<string> _basicLogos =
	[
		"images/tiles/AlertMagic Logo.svg",
		"images/tiles/DataMagic Logo.svg",
		"images/tiles/CaseMagic Logo.svg",
		"images/tiles/MonitorMagic Logo.svg",
		"images/tiles/ReportMagic Logo.svg",
		"images/tiles/SchemaMagic Logo.svg"
	];

	// Example 1: Minimal
	private readonly TileGridOptions _example1Options = new()
	{
		Columns = 3,
		Rows = 3,
		ShowBackground = false
	};

	private const string _example1Code = """
		<PDTiles Options="@_options"
		         Logos="@_logos"
		         Height="250px" />
		
		@code {
		    private TileGridOptions _options = new()
		    {
		        Columns = 3,
		        Rows = 3
		    };
		
		    private List<string> _logos = new()
		    {
		        "images/logo1.svg",
		        "images/logo2.svg",
		        // ...
		    };
		}
		""";

	// Example 2: Grid Configuration
	private readonly TileGridOptions _example2Options = new()
	{
		Columns = 4,
		Rows = 3,
		Depth = 20,
		Gap = 75,
		TileColor = "#1B4965",
		BackgroundColor = "#0d1b2a",
		ShowBackground = false
	};

	private const string _example2Code = """
		<PDTiles Options="@_options"
		         Logos="@_logos"
		         Height="300px" />
		
		@code {
		    private TileGridOptions _options = new()
		    {
		        Columns = 4,
		        Rows = 3,
		        Depth = 20,        // Thicker tiles
		        Gap = 75,          // Closer together
		        TileColor = "#1B4965",
		        BackgroundColor = "#0d1b2a"
		    };
		}
		""";

	// Example 3: Straight Connectors
	private readonly TileGridOptions _example3Options = new()
	{
		Columns = 3,
		Rows = 3,
		Depth = 15,
		ShowBackground = false
	};

	private readonly TileConnectorOptions _example3ConnectorOptions = new()
	{
		ConnectionMode = ConnectionMode.StraightLine,
		FillPattern = ConnectorFillPattern.Bars,
		Direction = ConnectorDirection.Orthogonal,
		AnimationSpeed = 35
	};

	private readonly List<TileConnector> _example3Connectors =
	[
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 2, Row = 1 },
			Direction = "right",
			Color = "#00FFFF"
		},
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 1, Row = 2 },
			Direction = "down",
			Color = "#FF6B6B"
		},
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 0, Row = 1 },
			Direction = "left",
			Color = "#4ECDC4"
		}
	];

	private const string _example3Code = """
		<PDTiles Options="@_options"
		         ConnectorOptions="@_connectorOptions"
		         Connectors="@_connectors"
		         Height="300px" />
		
		@code {
		    private TileConnectorOptions _connectorOptions = new()
		    {
		        ConnectionMode = ConnectionMode.StraightLine,
		        FillPattern = ConnectorFillPattern.Bars,
		        Direction = ConnectorDirection.Orthogonal,
		        AnimationSpeed = 35
		    };
		
		    private List<TileConnector> _connectors = new()
		    {
		        new()
		        {
		            StartTile = new() { Column = 1, Row = 1 },
		            EndTile = new() { Column = 2, Row = 1 },
		            Direction = "right",
		            Color = "#00FFFF"
		        },
		        // Add more connectors...
		    };
		}
		""";

	// Example 4: Row Curves
	private readonly TileGridOptions _example4Options = new()
	{
		Columns = 4,
		Rows = 3,
		Depth = 15,
		ShowBackground = false
	};

	private readonly TileConnectorOptions _example4ConnectorOptions = new()
	{
		ConnectionMode = ConnectionMode.RowCurves,
		FillPattern = ConnectorFillPattern.Solid,
		CurveTension = 60,
		AnimationSpeed = 0
	};

	private readonly List<TileConnector> _example4Connectors =
	[
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 0, Row = 0 },
			Direction = "curve-up",
			Color = "#FF6B6B"
		},
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 2, Row = 0 },
			Direction = "curve-up",
			Color = "#4ECDC4"
		},
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 3, Row = 0 },
			Direction = "curve-up",
			Color = "#FFE66D"
		},
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 0, Row = 2 },
			Direction = "curve-down",
			Color = "#95E1D3"
		},
		new()
		{
			StartTile = new() { Column = 1, Row = 1 },
			EndTile = new() { Column = 2, Row = 2 },
			Direction = "curve-down",
			Color = "#F38181"
		}
	];

	private const string _example4Code = """
		<PDTiles Options="@_options"
		         ConnectorOptions="@_connectorOptions"
		         Connectors="@_connectors"
		         Height="300px" />
		
		@code {
		    private TileConnectorOptions _connectorOptions = new()
		    {
		        ConnectionMode = ConnectionMode.RowCurves,
		        FillPattern = ConnectorFillPattern.Solid,
		        CurveTension = 60
		    };
		
		    private List<TileConnector> _connectors = new()
		    {
		        new()
		        {
		            StartTile = new() { Column = 1, Row = 1 },
		            EndTile = new() { Column = 0, Row = 0 },
		            Direction = "curve-up",
		            Color = "#FF6B6B"
		        },
		        new()
		        {
		            StartTile = new() { Column = 1, Row = 1 },
		            EndTile = new() { Column = 3, Row = 0 },
		            Direction = "curve-up",
		            Color = "#FFE66D"
		        },
		        // More curves to different columns...
		    };
		}
		""";

	// Example 5: Visual Effects
	private readonly TileGridOptions _example5Options = new()
	{
		Columns = 3,
		Rows = 3,
		Depth = 15,
		TileColor = "#2D3047",
		BackgroundColor = "#0a0a14",
		Glow = 50,
		GlowFalloff = 150,
		Reflection = 50,
		ReflectionDepth = 100,
		LineOpacity = 10,
		LineColor = "#333355",
		ShowBackground = false
	};

	private const string _example5Code = """
		<PDTiles Options="@_options"
		         Logos="@_logos"
		         Height="300px" />
		
		@code {
		    private TileGridOptions _options = new()
		    {
		        Columns = 3,
		        Rows = 3,
		        Glow = 50,           // Center glow intensity
		        GlowFalloff = 150,   // How far glow extends
		        Reflection = 50,     // Mirror reflection below tiles
		        ReflectionDepth = 100,
		        LineOpacity = 10,    // Subtle grid lines
		        LineColor = "#333355"
		    };
		}
		""";

	// Example 6: Child Content
	private readonly TileGridOptions _example6Options = new()
	{
		Columns = 3,
		Rows = 3,
		Depth = 15,
		Alignment = GridAlignment.MiddleRight,
		Scale = 80,
		ShowBackground = false
	};

	private const string _example6Code = """
		<PDTiles Options="@_options"
		         Logos="@_logos"
		         Height="350px">
		    <div class="content-overlay">
		        <h3>Product Suite</h3>
		        <p>Our integrated platform connects all your 
		           business processes.</p>
		    </div>
		</PDTiles>
		
		@code {
		    private TileGridOptions _options = new()
		    {
		        Columns = 3,
		        Rows = 3,
		        Alignment = GridAlignment.MiddleRight,
		        Scale = 80  // Make room for content
		    };
		}
		
		<style>
		    .content-overlay {
		        position: absolute;
		        left: 0; top: 0;
		        width: 50%; height: 100%;
		        padding: 1rem;
		        color: white;
		    }
		</style>
		""";
}
