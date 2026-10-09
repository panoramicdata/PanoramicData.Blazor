/**
 * Shared helpers for the PDGraph component modules.
 */

const UINT32_RANGE = Math.pow(2, 32);
const FALLBACK_FRAME_INTERVAL_MS = 16;

/**
 * Returns a uniformly distributed number in the range [0, 1) using the Web Crypto API.
 */
export function secureRandom() {
	const buffer = new Uint32Array(1);
	window.crypto.getRandomValues(buffer);
	return buffer[0] / UINT32_RANGE;
}

/**
 * Schedules the callback for the next animation frame, falling back to a timer
 * on browsers without requestAnimationFrame.
 */
export function scheduleFrame(callback) {
	if (typeof window.requestAnimationFrame === "function") {
		window.requestAnimationFrame(callback);
	} else {
		window.setTimeout(callback, FALLBACK_FRAME_INTERVAL_MS);
	}
}

/**
 * Cubic ease-in-out for a progress value in the range [0, 1].
 */
export function easeInOutCubic(progress) {
	return progress < 0.5
		? 4 * progress * progress * progress
		: 1 - Math.pow(-2 * progress + 2, 3) / 2;
}

/**
 * True when the node has a finite x/y position.
 */
export function hasValidPosition(node) {
	return (
		!isNaN(node.x) &&
		!isNaN(node.y) &&
		node.x !== undefined &&
		node.y !== undefined &&
		isFinite(node.x) &&
		isFinite(node.y)
	);
}

/**
 * True when the node is not pinned to a fixed position.
 */
export function isFreeNode(node) {
	return node.fx === null && node.fy === null;
}
