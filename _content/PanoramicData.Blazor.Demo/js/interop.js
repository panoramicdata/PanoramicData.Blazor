// Scroll to element by ID (used for documentation anchor links)
window.scrollToElement = function (elementId) {
	var element = document.getElementById(elementId);
	if (element) {
		element.scrollIntoView({ behavior: 'smooth', block: 'start' });
	}
};

window.panoramicDataDemo = {

	downloadFiles: function (args) {
		for (var i = 0; i < args.items.length; i++) {
			var url = "/files/download?path=" + args.items[i].path;
			panoramicDataDemo.downloadFromUrl(url, args.items[i].name);
		}
	},

	downloadFromUrl: function (url, fileName) {
		var xhr = new XMLHttpRequest();
		xhr.open("GET", url, true);
		xhr.responseType = "blob";
		xhr.onload = function () {
			var urlCreator = window.URL || window.webkitURL;
			var imageUrl = urlCreator.createObjectURL(this.response);
			var tag = document.createElement('a');
			tag.href = imageUrl;
			tag.download = fileName;
			document.body.appendChild(tag);
			tag.click();
			document.body.removeChild(tag);
		}
		xhr.send();
	}

}