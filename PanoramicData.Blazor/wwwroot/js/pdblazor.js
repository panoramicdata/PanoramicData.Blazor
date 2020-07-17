function hasSplitJs() {
	return typeof Split !== 'undefined';
}


function initializeSplitter(ids, options) {
	Split(ids, options);
}
