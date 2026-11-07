/**
 * Force-directed layout physics for the PDGraph component.
 * Every function takes the GraphRenderer instance ("graph") it operates on.
 */
import { calculateNodeStyle } from "./pdgraph-render.js";
import { isFreeNode, secureRandom } from "./pdgraph-utils.js";

const GOLDEN_RATIO = (1 + Math.sqrt(5)) / 2;

/**
 * Generates diverse initial positions using a golden ratio spiral with proper spacing.
 */
export function createInitialNodes(graphData, rect) {
	const centerX = rect.width / 2;
	const centerY = rect.height / 2;

	// Much larger spread considering viewport aspect ratio
	const radiusX = rect.width * 0.4; // 40% of width for better spread
	const radiusY = rect.height * 0.4; // 40% of height for better spread
	const averageRadius = (radiusX + radiusY) / 2;

	return graphData.nodes.map((node, index) => {
		const angle = (index * 2 * Math.PI) / GOLDEN_RATIO; // True golden angle

		// Better distance progression with aspect ratio consideration
		const distance = Math.sqrt(index + 1) * 60;

		// Apply aspect ratio to X and Y positions differently
		const normalizedDistance = Math.min(distance, averageRadius);
		const xDistance = normalizedDistance * (radiusX / averageRadius);
		const yDistance = normalizedDistance * (radiusY / averageRadius);

		// Add some randomness for more natural look but keep structure
		const randomAngleOffset = (secureRandom() - 0.5) * 0.3; // Smaller angle variation
		const finalAngle = angle + randomAngleOffset;
		const randomDistanceOffset = (secureRandom() - 0.5) * 30; // Distance variation

		return {
			id: node.id,
			label: node.label,
			x: centerX + Math.cos(finalAngle) * (xDistance + randomDistanceOffset),
			y: centerY + Math.sin(finalAngle) * (yDistance + randomDistanceOffset),
			vx: (secureRandom() - 0.5) * 6, // Larger initial velocity for more movement
			vy: (secureRandom() - 0.5) * 6,
			fx: node.isFixed ? node.x : null,
			fy: node.isFixed ? node.y : null,
			dimensions: node.dimensions || {},
			originalNode: node,
		};
	});
}

export function calculateDimensionalSimilarity(graph, nodeA, nodeB) {
	if (graph.dimensions.length === 0) return 0;

	let similarity = 0;
	let count = 0;

	graph.dimensions.forEach((dim) => {
		const valueA = nodeA.dimensions[dim] || 0;
		const valueB = nodeB.dimensions[dim] || 0;
		similarity += 1 - Math.abs(valueA - valueB); // Higher similarity for closer values
		count++;
	});

	return count > 0 ? similarity / count : 0;
}

function calculateEdgeDimensionalWeight(graph, edge) {
	if (graph.dimensions.length === 0) return 1;

	let weight = 1;
	graph.dimensions.forEach((dim) => {
		const value = edge.dimensions[dim] || 0.5;
		weight += value * 0.5; // Edge dimensions boost connection strength
	});

	return weight;
}

export function calculateDimensionalAngle(graph, node, focusNode) {
	if (graph.dimensions.length === 0) return secureRandom() * Math.PI * 2;

	// Create a unique angle based on dimensional differences
	let angle = 0;
	graph.dimensions.forEach((dim, index) => {
		const nodeValue = node.dimensions[dim] || 0;
		const focusValue = focusNode.dimensions[dim] || 0;
		const difference = nodeValue - focusValue;
		angle += (difference * (index + 1) * Math.PI) / 2;
	});

	return angle;
}

/**
 * Applies a force pair: nodeA is moved by -force, nodeB by +force (fixed nodes are not moved).
 */
function applyForcePair(nodeA, nodeB, fx, fy) {
	if (isFreeNode(nodeA)) {
		nodeA.vx -= fx;
		nodeA.vy -= fy;
	}
	if (isFreeNode(nodeB)) {
		nodeB.vx += fx;
		nodeB.vy += fy;
	}
}

function forEachNodePair(nodes, action) {
	for (let i = 0; i < nodes.length; i++) {
		for (let j = i + 1; j < nodes.length; j++) {
			action(nodes[i], nodes[j]);
		}
	}
}

function applyRepulsion(graph, nodeA, nodeB) {
	const { repulsionStrength, minDistance, maxDistance } = graph.params;
	const dx = nodeB.x - nodeA.x;
	const dy = nodeB.y - nodeA.y;
	const distance = Math.sqrt(dx * dx + dy * dy);

	if (isNaN(distance) || distance === 0) return; // Skip if distance is invalid or zero

	// Stronger repulsion at very close distances (like a strong nuclear force)
	const effectiveRepulsionStrength =
		repulsionStrength * (distance < minDistance ? 5 : 1); // 5x stronger if overlapping

	if (distance > 0 && distance < maxDistance) {
		// Closer in dimensional space = stronger repulsion
		const dimensionalSimilarity = calculateDimensionalSimilarity(
			graph,
			nodeA,
			nodeB,
		);
		const adjustedRepulsion =
			effectiveRepulsionStrength * (1 + dimensionalSimilarity * 0.5);

		const force = adjustedRepulsion / (distance * distance + 10); // Add small constant to prevent singularities
		applyForcePair(
			nodeA,
			nodeB,
			(dx / distance) * force,
			(dy / distance) * force,
		);
	}
}

function applyAttraction(graph, edge) {
	const source = graph.nodes.find((n) => n.id === edge.source);
	const target = graph.nodes.find((n) => n.id === edge.target);

	if (!source || !target) return;

	const dx = target.x - source.x;
	const dy = target.y - source.y;
	const distance = Math.sqrt(dx * dx + dy * dy);

	// Spring-like behavior with natural length
	const edgeDimensionalWeight = calculateEdgeDimensionalWeight(graph, edge);
	const adjustedAttraction =
		graph.params.attractionStrength * edge.strength * edgeDimensionalWeight;

	// Natural length based on edge strength and viewport size
	const rect = graph.svg.getBoundingClientRect();
	const baseLength = Math.min(rect.width, rect.height) * 0.15; // 15% of smaller viewport dimension
	const naturalLength = baseLength * (1 + edge.strength * 0.5); // Stronger edges can be slightly longer

	// Spring force: F = k * (current_length - natural_length)
	const displacement = distance - naturalLength;
	const springForce = adjustedAttraction * displacement;

	if (distance > 0) {
		// The source moves towards the target and vice versa
		applyForcePair(
			target,
			source,
			(dx / distance) * springForce,
			(dy / distance) * springForce,
		);
	}
}

function applyCollision(nodeA, nodeB) {
	const dx = nodeB.x - nodeA.x;
	const dy = nodeB.y - nodeA.y;
	const distance = Math.sqrt(dx * dx + dy * dy);
	const minDistance =
		(calculateNodeStyle(nodeA).size + calculateNodeStyle(nodeB).size) * 1.2; // 20% buffer

	if (distance < minDistance) {
		const overlap = minDistance - distance;
		const force = overlap * 0.5; // Stronger push
		applyForcePair(
			nodeA,
			nodeB,
			(dx / distance) * force,
			(dy / distance) * force,
		);
	}
}

/**
 * Pulls nodes towards the centre of the cluster sharing their value of the given dimension.
 */
function applyClusteringForce(nodes, clusterDimension) {
	const clusterPoints = new Map();

	// Find the center of each cluster
	nodes.forEach((node) => {
		const clusterValue = node.dimensions[clusterDimension];
		if (clusterValue !== undefined) {
			if (!clusterPoints.has(clusterValue)) {
				clusterPoints.set(clusterValue, { x: 0, y: 0, count: 0 });
			}
			const clusterPoint = clusterPoints.get(clusterValue);
			clusterPoint.x += node.x;
			clusterPoint.y += node.y;
			clusterPoint.count++;
		}
	});

	clusterPoints.forEach((cp) => {
		cp.x /= cp.count;
		cp.y /= cp.count;
	});

	// Pull nodes towards their cluster center
	nodes.forEach((node) => {
		const clusterValue = node.dimensions[clusterDimension];
		if (clusterValue !== undefined) {
			const clusterPoint = clusterPoints.get(clusterValue);
			node.vx += (clusterPoint.x - node.x) * 0.01; // Gentle pull
			node.vy += (clusterPoint.y - node.y) * 0.01;
		}
	});
}

function applyClusteringForces(graph) {
	const config = graph.clusteringConfig;
	if (!config || !config.clusterByDimension) return;

	applyClusteringForce(graph.nodes, config.clusterByDimension);

	// When clustering is explicitly enabled the pull is applied a second time
	if (config.isEnabled) {
		applyClusteringForce(graph.nodes, config.clusterByDimension);
	}
}

function applyCenterForce(graph, node, center, forceStrength) {
	if (!isFreeNode(node)) return;

	const focusNode = graph.focusNode;
	if (focusNode && node.id === focusNode.id) {
		// Strong force to center the focus node
		node.vx += (center.x - node.x) * forceStrength * 3;
		node.vy += (center.y - node.y) * forceStrength * 3;
	} else if (focusNode) {
		// Position other nodes based on dimensional proximity to focus node
		const proximity = calculateDimensionalSimilarity(graph, node, focusNode);
		const angle = calculateDimensionalAngle(graph, node, focusNode);
		const idealDistance = 100 + (1 - proximity) * 180; // Closer in dimensional space = physically closer

		const idealX = center.x + Math.cos(angle) * idealDistance;
		const idealY = center.y + Math.sin(angle) * idealDistance;

		node.vx += (idealX - node.x) * forceStrength * 1.5;
		node.vy += (idealY - node.y) * forceStrength * 1.5;
	} else {
		// Regular center force - center on actual viewport center
		node.vx += (center.x - node.x) * forceStrength;
		node.vy += (center.y - node.y) * forceStrength;
	}
}

function applyCenterForces(graph) {
	// Center force or focus force - ALWAYS use actual viewport center
	const rect = graph.svg.getBoundingClientRect();
	const center = { x: rect.width / 2, y: rect.height / 2 };
	const forceStrength = graph.focusNode
		? graph.params.focusForce
		: graph.params.centerForce;

	graph.nodes.forEach((node) =>
		applyCenterForce(graph, node, center, forceStrength),
	);
}

export function updateForces(graph) {
	// Multi-dimensional repulsion forces
	forEachNodePair(graph.nodes, (nodeA, nodeB) =>
		applyRepulsion(graph, nodeA, nodeB),
	);

	// Attraction forces based on edge strength and dimensional similarity
	graph.edges.forEach((edge) => applyAttraction(graph, edge));

	// Collision force to prevent node overlap
	forEachNodePair(graph.nodes, applyCollision);

	applyClusteringForces(graph);
	applyCenterForces(graph);
}

function resetToCenter(node, rect) {
	node.x = rect.width / 2;
	node.y = rect.height / 2;
}

function repairInvalidState(node, rect) {
	if (isNaN(node.x) || isNaN(node.y)) {
		console.warn(`Fixing NaN position for node ${node.id}`);
		resetToCenter(node, rect);
	}
	if (isNaN(node.vx) || isNaN(node.vy)) {
		console.warn(`Fixing NaN velocity for node ${node.id}`);
		node.vx = 0;
		node.vy = 0;
	}
}

function moveFreeNode(node, params) {
	// Apply velocity
	node.x += node.vx;
	node.y += node.vy;

	// Apply damping
	node.vx *= params.damping;
	node.vy *= params.damping;

	// Velocity decay to prevent oscillation
	const speed = Math.sqrt(node.vx * node.vx + node.vy * node.vy);
	if (speed > 0.01) {
		const decay = Math.min(params.velocityDecay, speed);
		const factor = Math.max(0, (speed - decay) / speed);
		node.vx *= factor;
		node.vy *= factor;
	}
}

function updateNodePosition(node, params, rect) {
	repairInvalidState(node, rect);

	if (node.fx !== null && node.fy !== null) {
		// Fixed nodes
		node.x = node.fx;
		node.y = node.fy;
		node.vx = 0;
		node.vy = 0;
	} else {
		moveFreeNode(node, params);
	}

	// Final validation after position updates
	if (isNaN(node.x) || isNaN(node.y)) {
		console.error(
			`Node ${node.id} still has NaN position after update, resetting`,
		);
		resetToCenter(node, rect);
		node.vx = 0;
		node.vy = 0;
	}
}

export function updatePositions(graph) {
	const rect = graph.svg.getBoundingClientRect();
	graph.nodes.forEach((node) => updateNodePosition(node, graph.params, rect));
}

export function calculateKineticEnergy(graph) {
	return graph.nodes.reduce(
		(total, node) => total + (node.vx * node.vx + node.vy * node.vy),
		0,
	);
}
