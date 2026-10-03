const FALLBACK_FRAME_INTERVAL_MS = 16;

// Runs the callback on the next animation frame (or after a short delay where unsupported)
function onNextFrame(callback) {
	if (typeof window.requestAnimationFrame === "function") {
		window.requestAnimationFrame(callback);
	} else {
		window.setTimeout(callback, FALLBACK_FRAME_INTERVAL_MS);
	}
}

export function getPosition(id) {
	const el = document.getElementById(id);

	if (!el) {
		return null;
	}

	const rect = el.getBoundingClientRect();
	const position = { top: rect.top, left: rect.left };
	return position;
}

export function animate(id, previous, current, duration, timingFunction) {
	const el = document.getElementById(id);

	// If any of these are null, cancel the animation
	if (!el || !previous || !current) {
		return;
	}

	// Calculate the deltas between previous and current positions
	const deltaLeft = previous.left - current.left;
	const deltaTop = previous.top - current.top;

	// Instantly move the element back to its previous position using transform
	el.style.transition = "none";
	el.style.transform = `translate(${deltaLeft}px, ${deltaTop}px)`;

	// Force reflow to apply the initial transform
	el.getBoundingClientRect();

	// Now animate it to its current position (translate back to 0,0)
	el.style.transition = `transform ${duration}s ${timingFunction}`;
	onNextFrame(() => {
		el.style.transform = "translate(0px, 0px)";
	});
}

export function cancelAnimation(id) {
	const el = document.getElementById(id);

	// If any of these are null, cannot continue
	if (!el) {
		return;
	}

	el.style.transform = "translate(0px, 0px)";
	el.style.transition = "none"; // Cancel the transition
}
