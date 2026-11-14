var splits = {};

export function hasSplitJs() {
	return typeof window.Split !== "undefined";
}

export function initialize(id, ids, options, dotNetRef) {
	if (dotNetRef) {
		options.onDragEnd = function (sizes) {
			// The circuit may be gone by the time a drag ends, and then there is no one left to tell
			dotNetRef.invokeMethodAsync("OnDragEnd", sizes).catch(function () { });
		};
	}
	splits[id] = window.Split(ids, options);
}

export function getSizes(id) {
	if (splits[id]) {
		return splits[id].getSizes();
	}
}

export function setSizes(id, sizes) {
	if (splits[id]) {
		splits[id].setSizes(sizes);
	}
}

export function destroy(id) {
	if (splits[id]) {
		delete splits[id];
	}
}
