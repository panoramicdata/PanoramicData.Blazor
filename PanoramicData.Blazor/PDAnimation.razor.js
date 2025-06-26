export function getPosition(id) {
	const el = document.getElementById(id);
	if (!el) return null;
	const rect = el.getBoundingClientRect();
	return {
		top: rect.top,
		left: rect.left
	};
}

export function animate(id, prevPosition, animationDuration) {
	const el = document.getElementById(id);
	if (!el || !prevPosition) return;

	const rect = el.getBoundingClientRect();
	const dx = prevPosition.left - rect.left;
	const dy = prevPosition.top - rect.top;

	if (dx === 0 && dy === 0) return;

	el.style.transition = 'none';
	el.style.transform = `translate(${dx}px, ${dy}px)`;
	el.getBoundingClientRect(); // force reflow

	el.style.transition = `transform ${animationDuration}s ease`;
	el.style.transform = '';
}