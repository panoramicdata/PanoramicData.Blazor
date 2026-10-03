/**
 * SVG rendering of nodes and edges for the PDGraph component.
 */
import { hasValidPosition } from "./pdgraph-utils.js";

const SVG_NS = "http://www.w3.org/2000/svg";
const NODE_SHAPES = [
	"circle",
	"oval",
	"diamond",
	"octagon",
	"square",
	"rectangle",
];

function createSvgElement(tagName, attributes) {
	const element = document.createElementNS(SVG_NS, tagName);
	Object.entries(attributes).forEach(([name, value]) =>
		element.setAttribute(name, value),
	);
	return element;
}

function filledShapeAttributes(style) {
	return {
		fill: style.fillColor,
		stroke: style.strokeColor,
		"stroke-width": style.strokeThickness,
	};
}

function createOctagon(style) {
	const octSize = style.size;
	const octInner = octSize * 0.7;
	const points = `${-octInner},${-octSize} ${octInner},${-octSize} ${octSize},${-octInner} ${octSize},${octInner} ${octInner},${octSize} ${-octInner},${octSize} ${-octSize},${octInner} ${-octSize},${-octInner}`;
	return createSvgElement("polygon", {
		points,
		...filledShapeAttributes(style),
	});
}

const SHAPE_FACTORIES = new Map([
	[
		"circle",
		(style) =>
			createSvgElement("circle", {
				r: style.size,
				"fill-opacity": style.fillAlpha,
				"stroke-opacity": style.strokeAlpha,
				...filledShapeAttributes(style),
			}),
	],
	[
		"oval",
		(style) =>
			createSvgElement("ellipse", {
				rx: style.size * 1.4,
				ry: style.size * 0.8,
				"fill-opacity": style.fillAlpha,
				...filledShapeAttributes(style),
			}),
	],
	[
		"diamond",
		(style) =>
			createSvgElement("polygon", {
				points: `0,${-style.size} ${style.size},0 0,${style.size} ${-style.size},0`,
				...filledShapeAttributes(style),
			}),
	],
	[
		"square",
		(style) =>
			createSvgElement("rect", {
				x: -style.size,
				y: -style.size,
				width: style.size * 2,
				height: style.size * 2,
				...filledShapeAttributes(style),
			}),
	],
	[
		"rectangle",
		(style) =>
			createSvgElement("rect", {
				x: -style.size * 1.5,
				y: -style.size,
				width: style.size * 3,
				height: style.size * 2,
				...filledShapeAttributes(style),
			}),
	],
	["octagon", createOctagon],
]);

export function createNodeShape(style) {
	// Unknown shapes fall back to a circle
	const factory =
		SHAPE_FACTORIES.get(style.shape) || SHAPE_FACTORIES.get("circle");
	return factory(style);
}

export function calculateNodeStyle(node) {
	// Default styling
	const style = {
		size: 20,
		fillColor: "#4a90e2",
		fillAlpha: 0.8,
		strokeColor: "#333333",
		strokeThickness: 2,
		strokeAlpha: 1.0,
		shape: "circle", // circle, oval, diamond, octagon, square, rectangle
	};

	// Apply dimensional styling if available
	if (node.dimensions) {
		// Size based on 'Influence' dimension
		const influence = node.dimensions["Influence"] || 0.5;
		style.size = 15 + influence * 20; // 15-35px range

		// Color based on 'Era' dimension (hue)
		const era = node.dimensions["Era"] || 0.5;
		const fame = node.dimensions["Fame"] || 0.5;
		const creativity = node.dimensions["Creativity"] || 0.5;

		const hue = era * 360; // 0-360 degrees
		const saturation = fame * 100; // 0-100%
		const luminance = 30 + creativity * 40; // 30-70% range for good contrast

		style.fillColor = `hsl(${hue.toFixed(0)}, ${saturation.toFixed(0)}%, ${luminance.toFixed(0)}%)`;

		// Shape based on 'Category' dimension
		const category = node.dimensions["Category"] || 0.5;
		const shapeIndex = Math.floor(category * 6); // 0-5 range
		style.shape = NODE_SHAPES[Math.min(shapeIndex, NODE_SHAPES.length - 1)];
	}

	return style;
}

export function getContrastingTextColor(backgroundColor) {
	// For HSL colors, parse the luminance value
	if (backgroundColor.startsWith("hsl(")) {
		const values = backgroundColor
			.replace("hsl(", "")
			.replace(")", "")
			.split(",");
		if (values.length >= 3) {
			const luminance = parseFloat(values[2].replaceAll("%", "").trim());
			// If luminance > 50%, use dark text, otherwise use light text
			return luminance > 50 ? "#212529" : "#ffffff";
		}
	}
	// Default to white text
	return "#ffffff";
}

export function getTruncatedLabel(label, nodeSize) {
	const baseLength = Math.max(3, Math.floor(nodeSize / 3.5)); // More generous length calculation

	// Handle different node sizes more intelligently
	let maxLength;
	if (nodeSize < 20) {
		maxLength = Math.min(4, baseLength); // Very small nodes: max 4 chars
	} else if (nodeSize < 30) {
		maxLength = Math.min(8, baseLength); // Medium nodes: max 8 chars
	} else {
		maxLength = Math.min(12, baseLength); // Large nodes: max 12 chars
	}

	if (label.length <= maxLength) {
		return label;
	}

	// Keep the beginning, which is usually the most meaningful part
	return label.substring(0, maxLength - 1) + "…";
}

function createNodeLabel(label, nodeStyle) {
	const text = createSvgElement("text", {
		"text-anchor": "middle",
		dy: ".35em",
		"font-size": Math.max(8, Math.min(12, nodeStyle.size * 0.4)),
		fill: getContrastingTextColor(nodeStyle.fillColor),
		"pointer-events": "none",
		"user-select": "none",
		"font-family": "Arial, sans-serif",
		"font-weight": "500",
	});
	text.textContent = getTruncatedLabel(label, nodeStyle.size);
	return text;
}

function createNodeElement(graph, node) {
	const nodeGroup = createSvgElement("g", {
		class: "graph-node",
		transform: `translate(${node.x},${node.y})`,
		"data-node-id": node.id,
	});

	// Multi-dimensional styling
	const nodeStyle = calculateNodeStyle(node);

	// Create shape based on dimensional data, with a click handler for node selection
	const shape = createNodeShape(nodeStyle);
	shape.setAttribute("style", "cursor: pointer;");
	shape.addEventListener("click", (e) => {
		e.stopPropagation();
		graph.handleNodeClick(node);
	});
	nodeGroup.appendChild(shape);

	// Create label with contrasting color
	if (node.label) {
		nodeGroup.appendChild(createNodeLabel(node.label, nodeStyle));
	}

	return nodeGroup;
}

export function renderNodes(graph) {
	if (!graph.svg) return;

	const nodesGroup = graph.svg.querySelector(".nodes-group");
	if (!nodesGroup) return;

	// Clear existing nodes
	nodesGroup.textContent = "";

	// Filter out invalid nodes before rendering
	const validNodes = graph.nodes.filter((node) => {
		const isValid = hasValidPosition(node);
		if (!isValid) {
			console.warn("Skipping invalid node:", node.id, {
				x: node.x,
				y: node.y,
			});
		}
		return isValid;
	});

	validNodes.forEach((node) =>
		nodesGroup.appendChild(createNodeElement(graph, node)),
	);
}

function restingEdgeOpacity(edge) {
	return Math.max(0.4, edge.strength * 0.6);
}

function createEdgeElement(edge, sourceNode, targetNode) {
	const line = createSvgElement("line", {
		class: "graph-edge",
		x1: sourceNode.x,
		y1: sourceNode.y,
		x2: targetNode.x,
		y2: targetNode.y,
		stroke: "#888", // Lighter color for better visibility
		"stroke-width": Math.max(1, edge.strength * 2), // Variable width based on strength
		"stroke-opacity": restingEdgeOpacity(edge), // Variable opacity
		"data-edge-id": edge.id,
		style: "cursor: pointer; transition: stroke-opacity 0.2s ease;",
	});

	// Hover effects
	line.addEventListener("mouseenter", () => {
		const currentOpacity = parseFloat(line.getAttribute("stroke-opacity"));
		line.setAttribute(
			"stroke-opacity",
			Math.min(1.0, (currentOpacity || 0.4) + 0.3),
		);
	});
	line.addEventListener("mouseleave", () => {
		line.setAttribute("stroke-opacity", restingEdgeOpacity(edge));
	});

	return line;
}

function describeEndpoint(node) {
	return node ? { x: node.x, y: node.y } : "missing";
}

export function renderEdges(graph) {
	if (!graph.svg) return;

	const edgesGroup = graph.svg.querySelector(".edges-group");
	if (!edgesGroup) return;

	// Clear existing edges
	edgesGroup.textContent = "";

	graph.edges.forEach((edge) => {
		const sourceNode = graph.nodes.find((n) => n.id === edge.source);
		const targetNode = graph.nodes.find((n) => n.id === edge.target);

		// Validate both nodes exist and have valid positions
		const isValid =
			sourceNode &&
			targetNode &&
			hasValidPosition(sourceNode) &&
			hasValidPosition(targetNode);

		if (isValid) {
			edgesGroup.appendChild(createEdgeElement(edge, sourceNode, targetNode));
		} else {
			console.warn("Skipping invalid edge:", edge.id, {
				source: describeEndpoint(sourceNode),
				target: describeEndpoint(targetNode),
			});
		}
	});
}
