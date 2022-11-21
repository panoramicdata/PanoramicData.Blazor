var splits = {};

export function hasSplitJs() {
	return typeof Split !== "undefined";
}

export function initialize (id, ids, options) {
	splits[id] = Split(ids, options);
}

export function getSizes (id) {
	if (splits[id]) {
		return splits[id].getSizes();
	}
}

export function setSizes (id, sizes) {
	if (splits[id]) {
		splits[id].setSizes(sizes);
	}
}

export function destroy (id) {
	if (splits[id]) {
		delete splits[id];
	}
}