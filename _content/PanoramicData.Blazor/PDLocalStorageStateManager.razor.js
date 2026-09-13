export function getItem(key) {
	if (localStorage) {
		return localStorage.getItem(key);
	} else {
		throw "LocalStorage is not available";
	}
}

export function removeItem(key) {
	if (localStorage) {
		localStorage.removeItem(key);
	} else {
		throw "LocalStorage is not available";
	}
}

export function setItem(key, data) {
	if (localStorage) {
		localStorage.setItem(key, data);
	} else {
		throw "LocalStorage is not available";
	}
}