export function initialize(id, toggleId, dropdownId, ref, opt) {
	var el = document.getElementById(toggleId);
	if (ref && el) {

		el.parentElement.addEventListener("keypress", function (ev) {
			if (ev.keyCode === 13) {
				ref.invokeMethodAsync("OnKeyPressed", 13);
			}
		});

		el.addEventListener("shown.bs.dropdown", function () {
			ref.invokeMethodAsync("OnDropDownShown");
		});

		el.addEventListener("hidden.bs.dropdown", function () {
			ref.invokeMethodAsync("OnDropDownHidden");
		});

		el.addEventListener("mouseleave", function (ev) {
			if (!ev.relatedTarget || !ev.relatedTarget.parentElement || ev.relatedTarget.parentElement.id != id) {
				ref.invokeMethodAsync("OnMouseLeave");
			}
		});

		var dropdownEl = document.getElementById(dropdownId);
		if (dropdownEl) {
			dropdownEl.addEventListener("mouseleave", function (ev) {
				if (!ev.relatedTarget || !ev.relatedTarget.parentElement || ev.relatedTarget.parentElement.id != id) {
					ref.invokeMethodAsync("OnMouseLeave");
				}
			});
		}

		return new bootstrap.Dropdown(el, opt);
	}
}