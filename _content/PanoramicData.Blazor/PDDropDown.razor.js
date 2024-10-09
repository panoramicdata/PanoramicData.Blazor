export function initialize(id, ref, opt) {
	var el = document.getElementById(id);
	if (el) {

		el.parentElement.addEventListener("keypress", function (ev) {
			if (ev.keyCode === 13 && ref) {
				ref.invokeMethodAsync("OnKeyPressed", 13);
			}
		});

		el.addEventListener("shown.bs.dropdown", function () {
			if (ref) {
				ref.invokeMethodAsync("OnDropDownShown");
			}
		});

		el.addEventListener("hidden.bs.dropdown", function () {
			if (ref) {
				ref.invokeMethodAsync("OnDropDownHidden");
			}
		});

		return new bootstrap.Dropdown(el, opt);
	}
}