var timelines = {};

class Timeline {

	el = null;
	options = {
		bar: {
			padding: 2,
			width: 20
		},
		colours: {
			background: "White",
			border: "Silver"
		},
		series: []
	};
	ref = null;
	debouncedResizeHandler = null;
	plotElement = null;
	shiftKeyDown = false;
	lastMouseX = 0;
	resizeObserver = null;

	constructor(id, options, ref) {
		var el = document.getElementById(id);
		if (el) {
			this.el = el;
			this.ref = ref;
			this.options = options || this.options;
			this.plotElement = el.querySelector('.tl-plot-area');
			this.debouncedResizeHandler = this.debounce(() => this.onResize(), 500);
			this.log("init timeline: ", arguments);
			el.addEventListener('wheel', this.onWheel, { passive: false });
			window.addEventListener("resize", this.debouncedResizeHandler, { passive: false });
			
			// Add key event listeners for cursor management
			window.addEventListener('keydown', this.onKeyDown.bind(this));
			window.addEventListener('keyup', this.onKeyUp.bind(this));
			
			// Add mouse move listener on plot element to track position
			if (this.plotElement) {
				this.plotElement.addEventListener('mousemove', this.onMouseMove.bind(this));
				this.plotElement.addEventListener('mouseleave', this.onMouseLeave.bind(this));
			}
			
			// Add ResizeObserver to detect container size changes (e.g., when splitter is adjusted)
			if (typeof ResizeObserver !== 'undefined') {
				this.resizeObserver = new ResizeObserver(this.debouncedResizeHandler);
				this.resizeObserver.observe(el);
			}
		}
	}

	onKeyDown(ev) {
		if (ev.key === 'Shift' && !this.shiftKeyDown) {
			this.shiftKeyDown = true;
			this.updateCursor(this.lastMouseX);
		}
	}

	onKeyUp(ev) {
		if (ev.key === 'Shift' && this.shiftKeyDown) {
			this.shiftKeyDown = false;
			this.updateCursor(this.lastMouseX);
		}
	}

	onMouseMove(ev) {
		this.lastMouseX = ev.clientX;
		this.updateCursor(ev.clientX);
	}

	onMouseLeave(ev) {
		// Remove cursor class when mouse leaves the plot area
		if (this.plotElement) {
			this.plotElement.classList.remove('shift-move-cursor');
		}
	}

	async updateCursor(clientX) {
		if (!this.plotElement || !this.ref) {
			return;
		}

		// Only show move cursor if Shift is pressed AND mouse is over selection
		if (this.shiftKeyDown && clientX > 0) {
			try {
				const isInSelection = await this.ref.invokeMethodAsync(
					"PanoramicData.Blazor.PDTimeline.IsPointInSelection",
					clientX
				);
				
				if (isInSelection) {
					this.plotElement.classList.add('shift-move-cursor');
				} else {
					this.plotElement.classList.remove('shift-move-cursor');
				}
			} catch (e) {
				// Handle case where component might be disposed
				this.plotElement.classList.remove('shift-move-cursor');
			}
		} else {
			this.plotElement.classList.remove('shift-move-cursor');
		}
	}

	debounce(func, wait) {
		let timeout;
		return function executedFunction(...args) {
			const later = () => {
				timeout = null;
				func(...args);
			};
			clearTimeout(timeout);
			timeout = setTimeout(later, wait);
		};
	}

	log(data) {
		//console.log(...arguments);
	}

	onWheel(ev) {
		if (ev.ctrlKey) {
			ev.preventDefault();
		}
	}

	onResize() {
		if (this.ref) {
			this.ref.invokeMethodAsync("PanoramicData.Blazor.PDTimeline.OnResize");
		}
	}

	term() {
		if (this.el) {
			this.el.removeEventListener("wheel", this.onWheel);
			window.removeEventListener("resize", this.debouncedResizeHandler);
			window.removeEventListener('keydown', this.onKeyDown);
			window.removeEventListener('keyup', this.onKeyUp);
			if (this.plotElement) {
				this.plotElement.removeEventListener('mousemove', this.onMouseMove);
				this.plotElement.removeEventListener('mouseleave', this.onMouseLeave);
			}
			if (this.resizeObserver) {
				this.resizeObserver.disconnect();
				this.resizeObserver = null;
			}
			this.log("term timeline: ", this.canvasId);
		}
	}
}

//export { Timeline };

function debounce(func, wait) {
	let timeout;
	return function executedFunction(...args) {
		const later = () => {
			timeout = null;
			func(...args);
		};
		clearTimeout(timeout);
		timeout = setTimeout(later, wait);
	};
}

export function dispose(id) {
	var tl = timelines[id];
	if (tl) {
		tl.term();
	}
}

export function initialize(id, options, data, ref) {
	timelines[id] = new Timeline(id, options, data, ref);
}

export function setData(id, data) {
	var tl = timelines[id];
	if (tl) {
		tl.setData(data);
	}
}