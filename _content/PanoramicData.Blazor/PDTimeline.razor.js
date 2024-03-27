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

	constructor(id, options, ref) {
		var el = document.getElementById(id);
		if (el) {
			this.el = el;
			this.ref = ref;
			this.options = options || this.options;
			this.log("init timeline: ", arguments);
			el.addEventListener('wheel', this.onWheel, { passive: false });
			window.addEventListener("resize", this.onResize.bind(this), { passive: false });
		}
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
			window.removeEventListener("resize", this.onResize);
			this.log("term timeline: ", this.canvasId);
		}
	}
}

//export { Timeline };

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