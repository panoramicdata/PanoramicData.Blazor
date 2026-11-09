// PDStudio's left panel edge: dragging it, or the arrow keys once it has focus, set the panel's width in rem. The width
// changes in the page as the pointer or key moves it, and is reported to .NET once, when the drag or key press ends, so a
// host saving it does so once rather than for every step.

const keyStepRem = 1;
const initializedHandles = new WeakSet();

export function initializeLeftPanelHandle(handle, dotNetRef) {
	if (!handle || initializedHandles.has(handle)) {
		return;
	}

	initializedHandles.add(handle);
	handle.addEventListener("pointerdown", event => startDrag(event, handle, dotNetRef));

	// A held arrow key repeats, so the width changes on each key down and is reported once, when the key is let go
	const keys = { pending: null };
	handle.addEventListener("keydown", event => onKeyDown(event, handle, keys));
	handle.addEventListener("keyup", () => reportPending(dotNetRef, keys));
	handle.addEventListener("blur", () => reportPending(dotNetRef, keys));
}

function reportPending(dotNetRef, keys) {
	if (keys.pending !== null) {
		report(dotNetRef, keys.pending);
		keys.pending = null;
	}
}

function panelOf(handle) {
	return handle.previousElementSibling;
}

function remPixels() {
	return parseFloat(getComputedStyle(document.documentElement).fontSize) || 16;
}

// The width the stylesheet gives the panel, in rem; scale is how much wider it shows, as a host may zoom the panel
function measure(panel) {
	const shown = panel.getBoundingClientRect().width;
	const styled = parseFloat(getComputedStyle(panel).width) || shown;
	return { width: styled / remPixels(), scale: styled > 0 && shown > 0 ? shown / styled : 1 };
}

function limits(handle) {
	const min = parseFloat(handle.getAttribute("aria-valuemin"));
	const max = parseFloat(handle.getAttribute("aria-valuemax"));
	return { min: Number.isFinite(min) ? min : 0, max: Number.isFinite(max) ? max : Number.POSITIVE_INFINITY };
}

function clamp(width, bounds) {
	return Math.min(Math.max(width, bounds.min), bounds.max);
}

// Returns the width set, or null when there is no panel or no real width to set
function apply(handle, width) {
	const panel = panelOf(handle);
	if (!panel || !Number.isFinite(width)) {
		return null;
	}

	const rounded = Math.round(width * 100) / 100;
	panel.style.setProperty("--pd-studio-left-panel-width", rounded + "rem");
	handle.setAttribute("aria-valuenow", String(rounded));
	return rounded;
}

function report(dotNetRef, width) {
	if (!Number.isFinite(width)) {
		return;
	}

	// The circuit may be gone by the time a drag ends, and then there is no one left to tell
	try {
		dotNetRef.invokeMethodAsync("OnLeftPanelResized", width).catch(() => { });
	} catch {
		// As above, when the call cannot even be sent
	}
}

// Captured, the pointer reaches the edge wherever it goes; failing that, the window still hears it
function capture(handle, pointerId) {
	try {
		handle.setPointerCapture(pointerId);
		return handle;
	} catch {
		return window;
	}
}

function startDrag(event, handle, dotNetRef) {
	const panel = panelOf(handle);
	if (event.button !== 0 || !panel) {
		return;
	}

	// No text selection while dragging; a click or double-click still arrives
	event.preventDefault();
	const studio = handle.closest(".pd-studio");
	const startX = event.clientX;
	const start = measure(panel);
	const bounds = limits(handle);
	const pixels = remPixels();
	let width = null;
	let finished = false;

	const move = moveEvent => {
		width = apply(handle, clamp(start.width + (moveEvent.clientX - startX) / start.scale / pixels, bounds)) ?? width;
	};

	// Also run when capture is lost, such as when the host removes the edge mid-drag, so only the first call counts
	const end = endEvent => {
		if (finished) {
			return;
		}

		finished = true;
		target.removeEventListener("pointermove", move);
		target.removeEventListener("pointerup", end);
		target.removeEventListener("pointercancel", end);
		target.removeEventListener("lostpointercapture", end);
		if (target === handle && handle.hasPointerCapture(endEvent.pointerId)) {
			handle.releasePointerCapture(endEvent.pointerId);
		}

		studio?.classList.remove("pd-studio-resizing");

		// A click that did not move leaves the width alone, so a double-click is not reported as two resizes
		if (width !== null) {
			report(dotNetRef, width);
		}
	};

	const target = capture(handle, event.pointerId);
	studio?.classList.add("pd-studio-resizing");
	target.addEventListener("pointermove", move);
	target.addEventListener("pointerup", end);
	target.addEventListener("pointercancel", end);
	target.addEventListener("lostpointercapture", end);
}

function onKeyDown(event, handle, keys) {
	const panel = panelOf(handle);
	if (!panel) {
		return;
	}

	const bounds = limits(handle);
	const current = measure(panel).width;
	let next;
	switch (event.key) {
		case "ArrowLeft":
			next = current - keyStepRem;
			break;
		case "ArrowRight":
			next = current + keyStepRem;
			break;
		case "Home":
			next = bounds.min;
			break;
		case "End":
			next = bounds.max;
			break;
		default:
			return;
	}

	event.preventDefault();
	keys.pending = apply(handle, clamp(next, bounds)) ?? keys.pending;
}
