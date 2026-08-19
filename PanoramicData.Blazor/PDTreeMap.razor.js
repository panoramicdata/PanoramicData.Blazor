// PDTreeMap - container size observation only.
//
// The squarified layout runs in C#; the one thing it cannot do from there is find out how large the
// container actually is. This module reports that, and nothing else.

const instances = new Map();

/**
 * Begins observing the given element and reports its content size to .NET.
 * @param {string} id Unique instance identifier.
 * @param {HTMLElement} element The element to observe.
 * @param {any} dotNetRef Reference used to call back into the component.
 */
export function init(id, element, dotNetRef) {
	if (!id || !element || !dotNetRef) {
		return;
	}

	dispose(id);

	const report = (width, height) => {
		try {
			dotNetRef.invokeMethodAsync("OnContainerResized", width, height);
		} catch {
			// The circuit may have gone away between the observation and the callback.
		}
	};

	const observer = new ResizeObserver(entries => {
		for (const entry of entries) {
			const box = entry.contentRect;
			report(box.width, box.height);
		}
	});

	observer.observe(element);
	instances.set(id, observer);

	// Report the initial size immediately; ResizeObserver fires on observe in most browsers, but
	// relying on that would leave the map blank where it does not.
	const rect = element.getBoundingClientRect();
	report(rect.width, rect.height);
}

/**
 * Stops observing and releases the instance.
 * @param {string} id Unique instance identifier.
 */
export function dispose(id) {
	const observer = instances.get(id);

	if (observer) {
		observer.disconnect();
		instances.delete(id);
	}
}
