/**
 * Time-based animation of the PDGraph component's view transform (pan and zoom).
 * Every exported animation takes the GraphRenderer instance ("graph") it operates on.
 */
import { easeInOutCubic, scheduleFrame } from "./pdgraph-utils.js";

/**
 * Runs a time-based animation, calling onProgress with the eased progress (0..1) every frame.
 */
export function runAnimation(duration, onProgress, onComplete) {
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

export function interpolate(from, to, eased) {
	return from + (to - from) * eased;
}

export function currentTransform(graph) {
	const transform = graph.transform;
	const copy = { x: transform.x, y: transform.y, k: transform.k };
	return copy;
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
