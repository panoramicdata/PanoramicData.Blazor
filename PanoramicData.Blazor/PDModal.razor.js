export function initialize(id, opt, ref) {
	var el = document.getElementById(id);
	if (el) {

		const allowBg = !!(opt && opt.AllowBackgroundInteraction);

		el.addEventListener("shown.bs.modal", function () {
			if (ref) {
				ref.invokeMethodAsync("OnModalShown");
			}
			if (allowBg) {
				// Ensure body is not locked and no backdrops remain
				document.body.classList.remove('modal-open');
				document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());

				// Disable Bootstrap's internal focus trap so background inputs can receive focus
				try {
					const instance = bootstrap.Modal.getInstance(el);
					if (instance) {
						instance._config.focus = false;
						if (instance._focustrap && typeof instance._focustrap.deactivate === 'function') {
							instance._focustrap.deactivate();
						}
					}
				} catch { }

				// Fallback: intercept focusin during capture so Bootstrap can't redirect focus back into the modal
				const focusInHandler = function (ev) {
					if (!el.contains(ev.target)) {
						if (typeof ev.stopImmediatePropagation === 'function') ev.stopImmediatePropagation();
					}
				};
				el._pd_focusInHandler = focusInHandler;
				document.addEventListener('focusin', focusInHandler, true);
			}
		});

		el.addEventListener("hidden.bs.modal", function () {
			if (ref) {
				ref.invokeMethodAsync("OnModalHidden");
			}
			// cleanup focus handler if we added it
			if (el._pd_focusInHandler) {
				document.removeEventListener('focusin', el._pd_focusInHandler, true);
				el._pd_focusInHandler = null;
			}
		});

		// Map PascalCase option names coming from .NET to the lower-case keys Bootstrap expects
		const options = {};
		if (opt) {
			if (Object.prototype.hasOwnProperty.call(opt, 'Backdrop')) options.backdrop = opt.Backdrop;
			if (Object.prototype.hasOwnProperty.call(opt, 'Keyboard')) options.keyboard = opt.Keyboard;
			if (Object.prototype.hasOwnProperty.call(opt, 'Focus')) options.focus = opt.Focus;
		}

		const instance = new bootstrap.Modal(el, options);

		// If background interaction is allowed, proactively disable focus trap pre-show too
		if (allowBg) {
			try {
				if (instance._focustrap && typeof instance._focustrap.deactivate === 'function') {
					instance._focustrap.deactivate();
				}
			} catch { }
		}

		return instance;
	}
}