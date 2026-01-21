// Helper to safely invoke .NET methods, handling disconnected SignalR state
async function safeInvoke(ref, methodName, ...args) {
	try {
		await ref.invokeMethodAsync(methodName, ...args);
	} catch (e) {
		// Connection likely disconnected - ignore silently
	}
}

export function initialize(id, toggleId, dropdownId, ref, opt) {
	var el = document.getElementById(toggleId);
	if (ref && el) {

		el.parentElement.addEventListener("keypress", function (ev) {
			if (ev.keyCode === 13) {
				safeInvoke(ref, "OnKeyPressed", 13);
			}
		});

		el.addEventListener("shown.bs.dropdown", function () {
			safeInvoke(ref, "OnDropDownShown");
		});

		el.addEventListener("hidden.bs.dropdown", function () {
			safeInvoke(ref, "OnDropDownHidden");
		});

		el.addEventListener("mouseleave", function (ev) {
			if (!ev.relatedTarget || !ev.relatedTarget.parentElement || ev.relatedTarget.parentElement.id != id) {
				safeInvoke(ref, "OnMouseLeave");
			}
		});

		var dropdownEl = document.getElementById(dropdownId);
		if (dropdownEl) {
			dropdownEl.addEventListener("mouseleave", function (ev) {
				if (!ev.relatedTarget || !ev.relatedTarget.parentElement || ev.relatedTarget.parentElement.id != id) {
					safeInvoke(ref, "OnMouseLeave");
				}
			});
		}

		return new bootstrap.Dropdown(el, opt);
	}
}
