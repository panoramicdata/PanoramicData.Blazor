/**
 * PDGraph.razor.js - Advanced force-directed graph layout with multi-dimensional positioning
 * Supports physics-based animations and dimensional projection
 */
import {
	calculateKineticEnergy,
	createInitialNodes,
	updateForces,
	updatePositions,
} from "./js/pdgraph/pdgraph-physics.js";
import { renderEdges, renderNodes } from "./js/pdgraph/pdgraph-render.js";
import { applySelection } from "./js/pdgraph/pdgraph-selection.js";
import { scheduleFrame } from "./js/pdgraph/pdgraph-utils.js";
import {
	animateCenterOnNode,
	fitToView as fitGraphToView,
	focusOnNode,
} from "./js/pdgraph/pdgraph-view.js";

class GraphRenderer {
	constructor(elementId, clusteringConfig) {
		this.clusteringConfig = clusteringConfig;
		this.elementId = elementId;
		this.svg = null;
		this.nodes = [];
		this.edges = [];
		this.dimensions = [];
		this.simulation = null;
		this.transform = { x: 0, y: 0, k: 1 };
		this.isDragging = false;
		this.isRunning = false;
		this.isAnimating = false;
		this.focusNode = null;

		// Force simulation parameters
		this.params = {
			repulsionStrength: 800, // Much stronger repulsion to spread nodes
			attractionStrength: 0.05, // Weaker attraction to allow spreading
			damping: 0.95, // More damping for faster settling
			velocityDecay: 0.05, // More decay to maintain movement
			minDistance: 80, // Larger minimum distance between nodes
			maxDistance: 500, // Larger max distance for forces
			iterations: 600, // More iterations for longer physics
			convergenceThreshold: 0.02, // Allow physics to continue longer
			centerForce: 0.005, // Weaker center force to allow spreading
			focusForce: 0.03, // Weaker focus force
		};

		// Animation parameters
		this.animationParams = {
			duration: 2000, // 2 seconds
			easing: "easeInOutCubic",
		};
	}

	initialize() {
		const container = document.getElementById(this.elementId);
		if (!container) return;

		this.svg = container.querySelector(".graph-svg");
		if (!this.svg) return;

		this.setupEventListeners();
		console.log(`PDGraph ${this.elementId} initialized with advanced physics`);
	}

	setupEventListeners() {
		if (!this.svg) return;

		// Mouse/touch interactions for pan and zoom
		this.svg.addEventListener("mousedown", (e) => this.onMouseDown(e));
		this.svg.addEventListener("mousemove", (e) => this.onMouseMove(e));
		this.svg.addEventListener("mouseup", () => this.onMouseUp());
		this.svg.addEventListener("wheel", (e) => this.onWheel(e));

		// Touch events for mobile
		this.svg.addEventListener("touchstart", (e) => this.onTouchStart(e));
		this.svg.addEventListener("touchmove", (e) => this.onTouchMove(e));
		this.svg.addEventListener("touchend", () => this.onTouchEnd());

		// Prevent context menu
		this.svg.addEventListener("contextmenu", (e) => e.preventDefault());
	}

	startForceSimulation(graphData) {
		if (!graphData || !graphData.nodes) return;

		// Check if this is initial load or just an update
		const isInitialLoad = this.nodes.length === 0;
		const needsRepositioning =
			this.nodes.length > 0 && this.nodes.every((n) => n.x === 0 && n.y === 0);

		if (isInitialLoad || needsRepositioning) {
			this.createInitialLayout(graphData);
		} else {
			// Just update existing nodes with new data, preserve positions
			this.refreshData(graphData);
			console.log(`Updated existing node data, preserved positions`);
		}

		this.runSimulation();
	}

	createInitialLayout(graphData) {
		// Get viewport dimensions for proper centering
		const rect = this.svg.getBoundingClientRect();
		this.nodes = createInitialNodes(graphData, rect);

		this.edges = (graphData.edges || []).map((edge) => ({
			id: edge.id,
			source: edge.fromNodeId,
			target: edge.toNodeId,
			strength: edge.strength || 1.0,
			dimensions: edge.dimensions || {},
			originalEdge: edge,
		}));

		// Extract unique dimensions for multi-dimensional calculations
		this.extractDimensions();

		const radiusX = rect.width * 0.4;
		const radiusY = rect.height * 0.4;
		console.log(
			`Generated initial positions for ${this.nodes.length} nodes with spread: ${radiusX.toFixed(0)}x${radiusY.toFixed(0)}`,
		);
	}

	/**
	 * Updates node and edge data from the given graph data while preserving positions and physics state.
	 */
	refreshData(graphData) {
		this.nodes.forEach((node) => {
			const updatedNode = graphData.nodes.find((n) => n.id === node.id);
			if (updatedNode) {
				node.dimensions = updatedNode.dimensions || {};
				node.originalNode = updatedNode;
				node.label = updatedNode.label;
			}
		});

		this.edges.forEach((edge) => {
			const updatedEdge = graphData.edges.find((e) => e.id === edge.id);
			if (updatedEdge) {
				edge.dimensions = updatedEdge.dimensions || {};
				edge.originalEdge = updatedEdge;
				edge.strength = updatedEdge.strength || 1.0;
			}
		});
	}

	extractDimensions() {
		const dimensionSet = new Set();

		this.nodes.forEach((node) => {
			Object.keys(node.dimensions).forEach((dim) => dimensionSet.add(dim));
		});

		this.edges.forEach((edge) => {
			Object.keys(edge.dimensions).forEach((dim) => dimensionSet.add(dim));
		});

		this.dimensions = Array.from(dimensionSet);
		console.log("Extracted dimensions:", this.dimensions);
	}

	runSimulation() {
		if (this.isRunning) return;

		this.isRunning = true;
		let iteration = 0;
		const maxIterations = this.params.iterations;

		const tick = () => {
			if (iteration >= maxIterations || !this.isRunning) {
				this.isRunning = false;
				console.log(`Simulation completed after ${iteration} iterations`);
				return;
			}

			updateForces(this);
			updatePositions(this);
			this.updateDOM();

			// Check convergence
			const totalKineticEnergy = calculateKineticEnergy(this);
			if (iteration < 10 || iteration % 50 === 0) {
				console.log(
					`Iteration ${iteration}: Kinetic Energy = ${totalKineticEnergy.toFixed(3)}`,
				);
			}

			if (totalKineticEnergy < this.params.convergenceThreshold) {
				this.isRunning = false;
				console.log(
					`Simulation converged after ${iteration} iterations (energy: ${totalKineticEnergy.toFixed(3)})`,
				);
				return;
			}

			iteration++;
			scheduleFrame(tick);
		};

		scheduleFrame(tick);
	}

	updateDOM() {
		renderNodes(this);
		renderEdges(this);
	}

	// Node click handler with Blazor interop
	handleNodeClick(node) {
		console.log(`Node clicked: ${node.label || node.id}`);

		// Notify Blazor component about node selection
		try {
			const component = document.getElementById(this.elementId);
			if (component && component.blazorComponent) {
				component.blazorComponent
					.invokeMethodAsync("OnNodeClickedFromJS", node.originalNode)
					.catch((error) => console.error("Error invoking node click:", error));
			}
		} catch (error) {
			console.error("Error handling node click:", error);
		}
	}

	onMouseDown(event) {
		if (
			event.target.closest(".graph-node") ||
			event.target.closest(".graph-edge")
		) {
			return; // Don't pan when clicking on nodes/edges
		}

		this.isDragging = true;
		this.lastMouseX = event.clientX;
		this.lastMouseY = event.clientY;
		this.svg.style.cursor = "grabbing";
		event.preventDefault();
	}

	onMouseMove(event) {
		if (this.isDragging && !this.isAnimating) {
			const deltaX = event.clientX - this.lastMouseX;
			const deltaY = event.clientY - this.lastMouseY;

			this.transform.x += deltaX;
			this.transform.y += deltaY;

			this.updateTransform();

			this.lastMouseX = event.clientX;
			this.lastMouseY = event.clientY;
		}
	}

	onMouseUp() {
		this.isDragging = false;
		this.svg.style.cursor = "grab";
	}

	onWheel(event) {
		if (this.isAnimating) return;
		event.preventDefault();

		const rect = this.svg.getBoundingClientRect();
		const mouseX = event.clientX - rect.left;
		const mouseY = event.clientY - rect.top;

		const scaleFactor = event.deltaY > 0 ? 0.9 : 1.1;
		const newScale = Math.max(0.1, Math.min(5, this.transform.k * scaleFactor));

		// Zoom towards mouse position
		this.transform.x =
			mouseX - (mouseX - this.transform.x) * (newScale / this.transform.k);
		this.transform.y =
			mouseY - (mouseY - this.transform.y) * (newScale / this.transform.k);
		this.transform.k = newScale;

		this.updateTransform();
	}

	onTouchStart(event) {
		if (event.touches.length === 1) {
			const touch = event.touches[0];
			this.onMouseDown({
				clientX: touch.clientX,
				clientY: touch.clientY,
				target: event.target,
				preventDefault: () => event.preventDefault(),
			});
		}
	}

	onTouchMove(event) {
		if (event.touches.length === 1 && this.isDragging) {
			const touch = event.touches[0];
			this.onMouseMove({
				clientX: touch.clientX,
				clientY: touch.clientY,
			});
		}
		event.preventDefault();
	}

	onTouchEnd() {
		this.onMouseUp();
	}

	updateTransform() {
		const transformString = `translate(${this.transform.x},${this.transform.y}) scale(${this.transform.k})`;

		// Update transform on node and edge groups
		const nodesGroup = this.svg.querySelector(".nodes-group");
		const edgesGroup = this.svg.querySelector(".edges-group");

		if (nodesGroup) nodesGroup.setAttribute("transform", transformString);
		if (edgesGroup) edgesGroup.setAttribute("transform", transformString);

		this.notifyTransformChange(transformString);
	}

	notifyTransformChange(transformString) {
		const component = document.getElementById(this.elementId);
		if (component && component.blazorComponent) {
			component.blazorComponent
				.invokeMethodAsync("UpdateTransform", transformString)
				.catch(() => {
					// Ignore errors when updating transform
				});
		}
	}

	setFocusNode(nodeId) {
		const node = this.nodes.find((n) => n.id === nodeId);
		if (!node) return;

		console.log(`Setting focus to node: ${node.label || nodeId}`);
		focusOnNode(this, node);
	}

	centerOnNode(x, y) {
		animateCenterOnNode(this, x, y);
	}

	fitToView() {
		fitGraphToView(this);
	}

	stopSimulation() {
		this.isRunning = false;
	}

	destroy() {
		this.stopSimulation();
		this.isAnimating = false;
	}
}

/**
 * Global graph instance management and Blazor interop
 */
const graphInstances = new Map();

export function initialize(elementId, dotNetRef, clusteringConfig) {
	console.log(`Initializing PDGraph for element: ${elementId}`);
	const renderer = new GraphRenderer(elementId, clusteringConfig);
	renderer.initialize();

	// Store the Blazor component reference
	const container = document.getElementById(elementId);
	if (container && dotNetRef) {
		container.blazorComponent = dotNetRef;
	}

	graphInstances.set(elementId, renderer);
	console.log(`PDGraph initialized successfully for ${elementId}`);
}

export function startForceSimulation(elementId, graphData) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.startForceSimulation(graphData);
	}
}

const DEFAULT_CONVERGENCE_THRESHOLD = 0.02;

export function regenerateLayout(elementId, graphData, threshold, config) {
	const convergenceThreshold =
		threshold === undefined ? DEFAULT_CONVERGENCE_THRESHOLD : threshold;
	console.log("regenerateLayout called for", elementId, graphData);
	const renderer = graphInstances.get(elementId);
	if (!renderer) {
		console.error(`No renderer found for elementId: ${elementId}`);
		return;
	}

	// Don't restart if simulation is already running
	if (renderer.isRunning) {
		console.log(
			`Simulation already running for ${elementId}, skipping regenerate`,
		);
		return;
	}

	// Update convergence threshold
	renderer.params.convergenceThreshold = convergenceThreshold;
	renderer.clusteringConfig = config;

	console.log(
		`Found renderer for ${elementId}, clearing nodes and starting simulation with convergence: ${convergenceThreshold}`,
	);
	// Force regeneration by clearing existing nodes
	renderer.nodes = [];
	renderer.edges = [];
	renderer.focusNode = null;
	renderer.startForceSimulation(graphData);
}

export function setFocusNode(elementId, nodeId) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.setFocusNode(nodeId);
	}
}

export function centerOnNode(elementId, x, y) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.centerOnNode(x, y);
	}
}

export function fitToView(elementId) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.fitToView();
	}
}

export function destroy(elementId) {
	const renderer = graphInstances.get(elementId);
	if (renderer) {
		renderer.destroy();
		graphInstances.delete(elementId);
	}
}

export function updatePhysicsParameters(elementId, threshold, damping) {
	const renderer = graphInstances.get(elementId);
	if (!renderer) return;

	const oldThreshold = renderer.params.convergenceThreshold;
	renderer.params.convergenceThreshold = threshold;
	renderer.params.damping = damping;
	console.log(
		`Updated physics parameters for ${elementId}: Threshold ${oldThreshold} -> ${threshold}, Damping ${renderer.params.damping} -> ${damping}`,
	);

	// Don't restart simulation unless it's already stopped and threshold changed significantly
	if (
		!renderer.isRunning &&
		(Math.abs(oldThreshold - threshold) > 0.01 ||
			Math.abs(renderer.params.damping - damping) > 0.01)
	) {
		console.log(`Restarting simulation with new physics parameters`);
		renderer.runSimulation();
	}
}

export function updateConfiguration(elementId, graphData, clusteringConfig) {
	console.log(`updateConfiguration called for ${elementId}`);
	const renderer = graphInstances.get(elementId);
	if (!renderer) {
		console.error(`No renderer found for elementId: ${elementId}`);
		return;
	}

	renderer.clusteringConfig = clusteringConfig; // Assign the new clustering config

	if (!graphData || !graphData.nodes) {
		console.warn(`No graph data provided for updateConfiguration`);
		return;
	}

	console.log(
		`Updating configuration with ${graphData.nodes.length} nodes and ${graphData.edges?.length || 0} edges`,
	);

	// Update node and edge data only; positions and physics state are preserved
	renderer.refreshData(graphData);

	// Extract dimensions again in case they changed
	renderer.extractDimensions();

	// Re-render with new styling but preserve positions
	renderer.updateDOM();

	console.log(
		`Configuration updated for ${elementId}, ${renderer.nodes.length} nodes, positions preserved`,
	);
}

// Updates selection styling without regenerating the layout
export function updateSelection(elementId, selectedId, selectionType) {
	const renderer = graphInstances.get(elementId);
	if (!renderer) return;

	console.log(`Updating selection: ${selectionType} ${selectedId}`);
	applySelection(renderer.svg, selectedId, selectionType);
}
