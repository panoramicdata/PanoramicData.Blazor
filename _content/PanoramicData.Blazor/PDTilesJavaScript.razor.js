/**
 * PDTilesJavaScript - JavaScript interop for the tile grid component.
 * This wraps the IsometricTileGrid library.
 */

const grids = new Map();

/**
 * Get the grid created for a container, if any.
 * @param {string} id - Container element ID
 */
function getGrid(id) {
	const entry = grids.get(id);
	return entry?.grid;
}

/**
 * Initialize a new tile grid.
 * @param {string} id - Container element ID
 * @param {object} config - Grid configuration
 * @param {object} dotNetRef - .NET object reference for callbacks
 */
export function initialize(id, config, dotNetRef) {
	const container = document.getElementById(id);
	if (!container) {
		console.error(`PDTilesJavaScript: Container '${id}' not found`);
		return;
	}

	// Check if IsometricTileGrid is available
	if (typeof window.IsometricTileGrid === "undefined") {
		console.error(
			"PDTilesJavaScript: IsometricTileGrid library not loaded. Please include tile-grid.js",
		);
		return;
	}

	// Extend config with click handlers
	const extendedConfig = {
		...config,
		onTileClick: function (detail) {
			if (dotNetRef) {
				dotNetRef.invokeMethodAsync(
					"OnTileClick",
					detail.tileId,
					detail.tileName,
					detail.col,
					detail.row,
				);
			}
		},
		onConnectorClick: function (detail) {
			if (dotNetRef) {
				dotNetRef.invokeMethodAsync(
					"OnConnectorClick",
					detail.connectorName,
					detail.startTile.col,
					detail.startTile.row,
					detail.endTile.col,
					detail.endTile.row,
				);
			}
		},
	};

	// Generate initial random connectors if animation is enabled
	if (config.connAnimation && config.connAnimSpeed > 0) {
		extendedConfig.connectors =
			window.IsometricTileGrid.generateRandomConnectors(extendedConfig);
	}

	// Create the grid
	const grid = new window.IsometricTileGrid(container, extendedConfig);
	grids.set(id, { grid, dotNetRef });
}

/**
 * Update grid configuration.
 * @param {string} id - Container element ID
 * @param {object} config - New configuration
 */
export function update(id, config) {
	const grid = getGrid(id);
	grid?.update(config);
}

/**
 * Shuffle tile logos.
 * @param {string} id - Container element ID
 */
export function shuffle(id) {
	const grid = getGrid(id);
	grid?.shuffle();
}

/**
 * Generate and apply random connectors.
 * @param {string} id - Container element ID
 */
export function randomizeConnectors(id) {
	const grid = getGrid(id);
	if (grid) {
		const cfg = grid.getConfig();
		const connectors = window.IsometricTileGrid.generateRandomConnectors(cfg);
		grid.setConnectors(connectors);
	}
}

/**
 * Clear all connectors.
 * @param {string} id - Container element ID
 */
export function clearConnectors(id) {
	const grid = getGrid(id);
	grid?.setConnectors([]);
}

/**
 * Dispose of the grid.
 * @param {string} id - Container element ID
 */
export function dispose(id) {
	// Note: IsometricTileGrid may not have a dispose method
	// Clean up our reference
	grids.delete(id);
}
