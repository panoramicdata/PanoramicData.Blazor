/**
 * View animations (focus, centering, fit-to-view) for the PDGraph component.
 * Every function takes the GraphRenderer instance ("graph") it operates on.
 */
import {
	calculateDimensionalAngle,
	calculateDimensionalSimilarity,
} from "./pdgraph-physics.js";
import { calculateNodeStyle } from "./pdgraph-render.js";
import { easeInOutCubic, scheduleFrame } from "./pdgraph-utils.js";

const FIT_PADDING = 60;
const MAX_FIT_SCALE = 1.5;

/**
 * Runs a time-based animation, calling onProgress with the eased progress (0..1) every frame.
 */
function runAnimation(duration, onProgress, onComplete) {
	const startTime = Date.now();

	const step = () => {
		const elapsed = Date.now() - startTime;
		const progress = Math.min(elapsed / duration, 1);

		onProgress(easeInOutCubic(progress));

		if (progress < 1) {
			scheduleFrame(step);
		} else if (onComplete) {
			onComplete();
		}
	};

	scheduleFrame(step);
}

function interpolate(from, to, eased) {
	return from + (to - from) * eased;
}

function currentTransform(graph) {
	return { x: graph.transform.x, y: graph.transform.y, k: graph.transform.k };
}

export function animateTransform(graph, from, to) {
	if (graph.isAnimating) return;

	graph.isAnimating = true;
	runAnimation(
		graph.animationParams.duration,
		(eased) => {
			graph.transform.x = interpolate(from.x, to.x, eased);
			graph.transform.y = interpolate(from.y, to.y, eased);
			graph.transform.k = interpolate(from.k, to.k, eased);
			graph.updateTransform();
		},
		() => {
			graph.isAnimating = false;
		},
	);
}

export function animateCenterOnNode(graph, x, y) {
	const rect = graph.svg.getBoundingClientRect();

	// Zoom in slightly and move the point to the viewport centre
	const targetScale = Math.min(1.5, graph.transform.k * 1.2);
	const target = {
		x: rect.width / 2 - x * targetScale,
		y: rect.height / 2 - y * targetScale,
		k: targetScale,
	};

	animateTransform(graph, currentTransform(graph), target);
}

function animateNodesToPositions(graph, fromPositions, toPositions, duration) {
	runAnimation(duration, (eased) => {
		graph.nodes.forEach((node) => {
			const fromPos = fromPositions[node.id];
			const toPos = toPositions[node.id];

			if (fromPos && toPos) {
				node.x = interpolate(fromPos.x, toPos.x, eased);
				node.y = interpolate(fromPos.y, toPos.y, eased);

				// Reset velocities during animation
				node.vx = 0;
				node.vy = 0;
			}
		});

		// Update DOM with new positions
		graph.updateDOM();
	});
}

function calculateFocusPositions(graph, centerX, centerY) {
	const focusNode = graph.focusNode;
	const targetPositions = {};
	graph.nodes.forEach((n) => {
		if (n.id === focusNode.id) {
			// Focus node goes to center
			targetPositions[n.id] = { x: centerX, y: centerY };
			return;
		}

		// Other nodes positioned based on dimensional similarity
		const proximity = calculateDimensionalSimilarity(graph, n, focusNode);
		const angle = calculateDimensionalAngle(graph, n, focusNode);
		const idealDistance = 100 + (1 - proximity) * 180; // Closer in dimensional space = physically closer

		targetPositions[n.id] = {
			x: centerX + Math.cos(angle) * idealDistance,
			y: centerY + Math.sin(angle) * idealDistance,
		};
	});
	return targetPositions;
}

export function focusOnNode(graph, node) {
	// Clear any previous fixed positions and store current positions for animation
	const currentPositions = {};
	graph.nodes.forEach((n) => {
		n.fx = null;
		n.fy = null;
		currentPositions[n.id] = { x: n.x, y: n.y };
	});

	graph.focusNode = node;

	// Calculate new target positions based on dimensional relationships
	const rect = graph.svg.getBoundingClientRect();
	const centerX = rect.width / 2;
	const centerY = rect.height / 2;

	// Fix the selected node to the center
	node.fx = centerX;
	node.fy = centerY;
	node.vx = 0;
	node.vy = 0;

	const targetPositions = calculateFocusPositions(graph, centerX, centerY);

	// Animate all nodes to their new positions (1.5 second animation)
	animateNodesToPositions(graph, currentPositions, targetPositions, 1500);

	// Restart simulation with focus forces after the animation completes
	setTimeout(() => {
		graph.runSimulation();
	}, 1600);

	// Smooth zoom and center on the focus node
	animateCenterOnNode(graph, centerX, centerY);
}

function calculateGraphBounds(nodes) {
	const bounds = {
		minX: Infinity,
		maxX: -Infinity,
		minY: Infinity,
		maxY: -Infinity,
	};

	// Include node radius in bounds calculation
	nodes.forEach((node) => {
		const nodeRadius = calculateNodeStyle(node).size;
		bounds.minX = Math.min(bounds.minX, node.x - nodeRadius);
		bounds.maxX = Math.max(bounds.maxX, node.x + nodeRadius);
		bounds.minY = Math.min(bounds.minY, node.y - nodeRadius);
		bounds.maxY = Math.max(bounds.maxY, node.y + nodeRadius);
	});

	return bounds;
}

export function fitToView(graph) {
	if (graph.nodes.length === 0) return;

	const rect = graph.svg.getBoundingClientRect();
	const bounds = calculateGraphBounds(graph.nodes);
	const graphWidth = bounds.maxX - bounds.minX;
	const graphHeight = bounds.maxY - bounds.minY;

	if (graphWidth === 0 || graphHeight === 0) return;

	// Scaling calculation that uses as much of the viewport as possible
	const scaleX = (rect.width - FIT_PADDING * 2) / graphWidth;
	const scaleY = (rect.height - FIT_PADDING * 2) / graphHeight;
	const targetScale = Math.min(scaleX, scaleY, MAX_FIT_SCALE);

	// Calculate transform to center the graph
	const graphCenterX = (bounds.minX + bounds.maxX) / 2;
	const graphCenterY = (bounds.minY + bounds.maxY) / 2;
	const targetX = rect.width / 2 - graphCenterX * targetScale;
	const targetY = rect.height / 2 - graphCenterY * targetScale;

	console.log(
		`Fit to view: scale=${targetScale.toFixed(2)}, center=(${graphCenterX.toFixed(1)}, ${graphCenterY.toFixed(1)}), transform=(${targetX.toFixed(1)}, ${targetY.toFixed(1)})`,
	);

	// Clear focus node when fitting to view
	graph.focusNode = null;

	animateTransform(graph, currentTransform(graph), {
		x: targetX,
		y: targetY,
		k: targetScale,
	});
}
