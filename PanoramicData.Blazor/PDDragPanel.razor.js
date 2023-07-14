export function init(id) {
	var el = document.getElementById(id);
	if (el) {
		el.addEventListener("dragstart", function (e) {
			var crt = e.target.el;
			e.dataTransfer.setDragImage(crt, 0, 0);
		}, false);
	}
}