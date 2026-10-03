/**
 * Selection styling for the PDGraph component (applied without regenerating the layout).
 */

const SHAPE_SELECTOR = "circle, rect, ellipse, polygon";
const SELECTION_STROKE = "#ffff00";
const DEFAULT_STROKE = "#333333";

function clearShapeSelection(shape) {
	shape.removeAttribute("filter");
	const strokeWidth = parseFloat(shape.getAttribute("stroke-width")) || 2;
	if (strokeWidth > 4) {
		// It was previously selected
		shape.setAttribute("stroke-width", strokeWidth - 2);
	}
	if (shape.getAttribute("stroke") === SELECTION_STROKE) {
		shape.setAttribute(
			"stroke",
			shape.getAttribute("data-original-stroke") || DEFAULT_STROKE,
		);
	}
}

function clearSelection(nodesGroup, edgesGroup) {
	if (nodesGroup) {
		nodesGroup.querySelectorAll(".graph-node").forEach((node) => {
			node.classList.remove("selected");
			node.querySelectorAll(SHAPE_SELECTOR).forEach(clearShapeSelection);
		});
	}

	if (edgesGroup) {
		edgesGroup.querySelectorAll(".graph-edge").forEach((edge) => {
			edge.classList.remove("selected");
		});
	}
}

function selectShape(shape) {
	// Store original stroke color
	const originalStroke = shape.getAttribute("stroke") || DEFAULT_STROKE;
	shape.setAttribute("data-original-stroke", originalStroke);

	// Yellow selection border with a glow effect
	shape.setAttribute("stroke", SELECTION_STROKE);
	const currentWidth = parseFloat(shape.getAttribute("stroke-width")) || 2;
	shape.setAttribute("stroke-width", currentWidth + 2);
	shape.setAttribute("filter", "drop-shadow(0 0 8px rgba(255, 255, 0, 0.6))");
}

function selectNode(nodesGroup, selectedId) {
	const selectedNode = nodesGroup?.querySelector(
		`[data-node-id="${selectedId}"]`,
	);
	if (selectedNode) {
		selectedNode.classList.add("selected");
		selectedNode.querySelectorAll(SHAPE_SELECTOR).forEach(selectShape);
	}
}

function selectEdge(edgesGroup, selectedId) {
	const selectedEdge = edgesGroup?.querySelector(
		`[data-edge-id="${selectedId}"]`,
	);
	if (selectedEdge) {
		selectedEdge.classList.add("selected");
		selectedEdge.setAttribute("stroke", SELECTION_STROKE);
		selectedEdge.setAttribute("stroke-width", "3");
	}
}

export function applySelection(svg, selectedId, selectionType) {
	const nodesGroup = svg.querySelector(".nodes-group");
	const edgesGroup = svg.querySelector(".edges-group");

	clearSelection(nodesGroup, edgesGroup);

	if (selectionType === "node") {
		selectNode(nodesGroup, selectedId);
	} else if (selectionType === "edge") {
		selectEdge(edgesGroup, selectedId);
	}
}
