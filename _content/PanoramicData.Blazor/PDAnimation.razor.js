export function getPosition(id) {
	const el = document.getElementById(id);

	if (!el) {
		return null;
	}

	const rect = el.getBoundingClientRect();

	return { top: rect.top, left: rect.left };

}


export function animate(id, prevPosition, currentPosition, animationDuration, animationTimingFunction) {
	const el = document.getElementById(id);

	// If any of these are null, cancel the animation
	if (!el || !prevPosition || !currentPosition) {
		return;
	}

	// Calculate the deltas between previous and current positions
	const deltaLeft = prevPosition.left - currentPosition.left;
	const deltaTop = prevPosition.top - currentPosition.top;

	// Instantly move the element back to its previous position using transform
	el.style.transition = "none";
	el.style.transform = `translate(${deltaLeft}px, ${deltaTop}px)`;

	// Force reflow to apply the initial transform
	el.getBoundingClientRect();

	// Now animate it to its current position (translate back to 0,0)
	el.style.transition = `transform ${animationDuration}s ${animationTimingFunction}`;
	requestAnimationFrame(() => {
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
