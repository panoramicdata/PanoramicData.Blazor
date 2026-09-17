var zoombars = {};

class Zoombar {

	data = [];
	options = {
		zoomSteps: [25, 50, 75],
		colours: {
			background: "White",
			border: "Silver",
			handleBackground: "Green",
			handleForeground: "White"
		}
	};
	value = {
		zoom: 100,
		pan: 0
	};
	canvas = null;
	dragOriginX = 0;
	dragOriginY = 0;
	dragOffset = 0;
	dragging = false;
	handleBounds = new Rect();
	ref = null;

	constructor(id, value, options, ref) {
		var canvas = document.getElementById(id);
		if (canvas) {
			this.canvas = canvas;
			this.canvasId = id;
			this.value = value || this.value;
			this.options = options || this.options;
			this.canvas.addEventListener("mousedown", this.onMouseDown.bind(this), false);
			this.canvas.addEventListener("mouseup", this.onMouseUp.bind(this), false);
			this.canvas.addEventListener("mousemove", this.onMouseMove.bind(this), false);
			this.canvas.addEventListener("mouseout", this.onMouseOut.bind(this), false);
			this.canvas.addEventListener("wheel", this.onMouseWheel.bind(this), false);
			this.handleBounds.w = (this.canvas.width / 100) * this.value.zoom;
			this.handleBounds.x = (this.canvas.width / 100) * this.value.pan;
			this.handleBounds.h = this.canvas.height;
			this.ref = ref;
			this.redraw();
			this.log("init zoombar: ", arguments);
		}
	}

	log(data) {
		//console.log(...arguments);
	}

	onMouseDown(ev) {
		if (!this.dragging) {
			// mouse down on handle?
			if (this.value.zoom < 100 && this.handleBounds.contains(ev.offsetX, ev.offsetY)) {
				this.dragging = true;
				this.dragOriginX = ev.offsetX;
				this.dragOriginY = ev.offsetY;
				this.dragOffset = ev.offsetX - this.handleBounds.x;
				this.log("drag started");
			}
		}
	}

	onMouseMove(ev) {
		if (this.dragging) {
			var newX = ev.offsetX - this.dragOffset;
			if (newX < 0) {
				newX = 0;
			} else if ((newX + this.handleBounds.w) > this.canvas.width) {
				newX = this.canvas.width - this.handleBounds.w;
			}
			if (this.handleBounds.x !== newX) {
				this.handleBounds.x = newX;
				this.redraw();
			}
		}
	}

	onMouseOut(ev) {
		this.onMouseUp(ev);
	}

	onMouseUp(ev) {
		if (this.dragging) {
			this.dragging = false;
			var dx = ev.offsetX - this.dragOriginX;
			var dy = ev.offsetY - this.dragOriginY;
			this.updatePan();
			this.log("drag ended", dx, dy);
		}
	}

	onMouseWheel(ev) {
		// re-position handle
		if (this.value.zoom < 100) {
			var newX = this.handleBounds.x + ((this.canvas.width / 10) * (ev.deltaY * 0.01));
			if (newX < 0) {
				newX = 0;
			} else if ((newX + this.handleBounds.w) > this.canvas.width) {
				newX = this.canvas.width - this.handleBounds.w;
			}
			if (this.handleBounds.x != newX) {
				this.handleBounds.x = newX;
				this.redraw();
				this.updatePan();
			}
		}
		ev.stopPropagation();
		ev.preventDefault();
	}

	onValueChanged() {
		this.value.pan = (this.handleBounds.x / this.canvas.width) * 100;
		if (this.ref) {
			this.ref.invokeMethodAsync("OnValueChanged", this.value);
		}
	}

	updatePan() {
		this.value.pan = (this.handleBounds.x / this.canvas.width) * 100;
		this.onValueChanged();
	}

	redraw() {
		if (this.canvas) {
			var ctx = this.canvas.getContext("2d");

			// draw background
			ctx.fillStyle = this.options.colours.background;
			ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

			// draw handle
			ctx.fillStyle = this.options.colours.handleBackground;
			ctx.fillRect(this.handleBounds.x, this.handleBounds.y, this.handleBounds.w, this.handleBounds.h);

			// show handle
			var text = `${this.value.zoom}%`;
			let textInfo = ctx.measureText(text);
			ctx.fillStyle = this.options.colours.handleForeground;
			ctx.fillText(text, this.handleBounds.xm() - (textInfo.width / 2), this.canvas.height - 6);

			// draw border
			ctx.strokeStyle = this.options.colours.border;
			ctx.strokeRect(0, 0, this.canvas.width, this.canvas.height);
		}
	}

	setValue(v) {
		if (v.zoom > 0 && v.zoom <= 100 && v.pan >= 0 && v.pan <= 100) {
			this.value = v;
			// re-size handle
			this.handleBounds.w = (this.canvas.width / 100) * this.value.zoom;
			// re-position handle?
			this.handleBounds.x = (this.canvas.width / 100) * this.value.pan;
			if (this.handleBounds.x2() > this.canvas.width) {
				this.handleBounds.x = this.canvas.width - this.handleBounds.w;
			}
			this.redraw();
		}
	}

	term() {
		if (this.canvas) {
			this.canvas.removeEventListener("mousedown", this.onMouseDown);
			this.canvas.removeEventListener("mouseup", this.onMouseUp);
			this.canvas.removeEventListener("mousemove", this.onMouseMove);
			this.canvas.removeEventListener("mouseout", this.onMouseOut);
			this.log("term zoombar: ", this.canvasId);
		}
	}
}

class Rect {
	x = 0;
	y = 0;
	w = 0;
	h = 0;
	xm() {
		return this.x + (this.w / 2);
	}
	x2() {
		return this.x + this.w;
	}
	contains(x, y) {
		return x >= this.x && x <= (this.x + this.w) && y >= this.y && y <= (this.y + this.h);
	}
}

export function initialize (id, value, options, ref) {
	zoombars[id] = new Zoombar(id, value, options, ref);
}

export function setValue (id, v) {
	var zb = zoombars[id];
	if (zb) {
		return zb.setValue(v);
	}
	return 0;
}

export function dispose (id) {
	var zb = zoombars[id];
	if (zb) {
		zb.term();
	}
}