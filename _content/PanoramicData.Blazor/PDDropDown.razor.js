export function initialize(id, toggleId, dropdownId, ref, opt) {
	var el = document.getElementById(toggleId);
	if (ref && el) {

		el.parentElement.addEventListener("keypress", function (ev) {
			if (ev.keyCode === 13) {
				try {
					ref.invokeMethodAsync("OnKeyPressed", 13);
				} catch {
					// BC-85: Circuit may be disconnected
				}
			}
		});

		el.addEventListener("shown.bs.dropdown", function () {
			try {
				ref.invokeMethodAsync("OnDropDownShown");
			} catch {
				// BC-85: Circuit may be disconnected
			}
		});

		el.addEventListener("hidden.bs.dropdown", function () {
			try {
				ref.invokeMethodAsync("OnDropDownHidden");
			} catch {
				// BC-85: Circuit may be disconnected
			}
		});

		el.addEventListener("mouseleave", function (ev) {
			if (!ev.relatedTarget || !ev.relatedTarget.parentElement || ev.relatedTarget.parentElement.id != id) {
				try {
					ref.invokeMethodAsync("OnMouseLeave");
				} catch {
					// BC-85: Circuit may be disconnected
				}
			}
		});

		var dropdownEl = document.getElementById(dropdownId);
		if (dropdownEl) {
			dropdownEl.addEventListener("mouseleave", function (ev) {
				if (!ev.relatedTarget || !ev.relatedTarget.parentElement || ev.relatedTarget.parentElement.id != id) {
					try {
						ref.invokeMethodAsync("OnMouseLeave");
					} catch {
						// BC-85: Circuit may be disconnected
					}
				}
			});
		}

		// Use 'fixed' strategy so Popper.js positions relative to the viewport.
		// Without this, dropdowns inside overflow:hidden/auto ancestors (e.g. fixed-height
		// table wrappers) are clipped and disappear. 'fixed' escapes any overflow context.
		const popperConfig = (defaultConfig) => ({
			...defaultConfig,
			strategy: 'fixed'
		});
		return new bootstrap.Dropdown(el, { ...opt, popperConfig });
	}
}