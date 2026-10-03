// Calls a .NET method on the component reference, ignoring a disconnected circuit
function invokeDotNet(ref, method, ...args) {
	try {
		ref.invokeMethodAsync(method, ...args);
	} catch {
		// BC-85: Circuit may be disconnected
	}
}

// Whether the pointer has left for an element outside the drop down with the given id
function isLeavingDropDown(ev, id) {
	return ev.relatedTarget?.parentElement?.id != id;
}

export function initialize(id, toggleId, dropdownId, ref, opt) {
	var el = document.getElementById(toggleId);
	if (ref && el) {
		el.parentElement.addEventListener("keypress", function (ev) {
			if (ev.keyCode === 13) {
				invokeDotNet(ref, "OnKeyPressed", 13);
			}
		});

		el.addEventListener("shown.bs.dropdown", function () {
			invokeDotNet(ref, "OnDropDownShown");
		});

		el.addEventListener("hidden.bs.dropdown", function () {
			invokeDotNet(ref, "OnDropDownHidden");
		});

		const onMouseLeave = function (ev) {
			if (isLeavingDropDown(ev, id)) {
				invokeDotNet(ref, "OnMouseLeave");
			}
		};
		el.addEventListener("mouseleave", onMouseLeave);
		document
			.getElementById(dropdownId)
			?.addEventListener("mouseleave", onMouseLeave);

		// Use 'fixed' strategy so Popper.js positions relative to the viewport.
		// Without this, dropdowns inside overflow:hidden/auto ancestors (e.g. fixed-height
		// table wrappers) are clipped and disappear. 'fixed' escapes any overflow context.
		const popperConfig = (defaultConfig) => ({
			...defaultConfig,
			strategy: "fixed",
		});
		return new window.bootstrap.Dropdown(el, { ...opt, popperConfig });
	}
}
