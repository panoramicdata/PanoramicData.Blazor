export function initialize(id, opt, ref) {
	var el = document.getElementById(id);
	if (el) {

		el.addEventListener("shown.bs.modal", function () {
			if (ref) {
				ref.invokeMethodAsync("OnModalShown");
			}
		});

		el.addEventListener("hidden.bs.modal", function () {
			if (ref) {
				ref.invokeMethodAsync("OnModalHidden");
			}
		});

		return new bootstrap.Modal(el, opt);
	}
}