export function init() {

	var dragTarget = document.createElement("img");
	dragTarget.src = "https://www.bluefishsoftware.co.uk/favicon.ico";
	document.body.insertBefore(dragTarget, document.body.firstChild);

	dragTarget.addEventListener("dragstart", function (event) {
		event.dataTransfer.setData("DownloadURL", "https://www.bluefishsoftware.co.uk/favicon.ico");
	}, false);
}