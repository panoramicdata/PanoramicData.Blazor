class Timeline {

	canvas = null;
	data = null;
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

	constructor(id, options, data, ref) {
		var canvas = document.getElementById(id);
		if (canvas) {
			this.canvas = canvas;
			this.canvasId = id;
			this.data = data || this.data;
			this.ref = ref;
			this.options = options || this.options;
			this.redraw();
			this.log("init timeline: ", arguments);
		}
	}

	log(data) {
		console.log(...arguments);
	}

	redraw() {
		if (this.canvas) {
			var ctx = this.canvas.getContext("2d");

			// draw background
			ctx.fillStyle = this.options.colours.background;
			ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

			// draw bars
			if (this.data.length > 0 && this.options.series.length > 0)
			{
				// initialize
				var barWidth = this.canvas.width / this.data.length;
				var maxSum = 0;
				for (var i = 0; i < this.data.length; i++) {
					var sum = 0;
					for (var s = 0; s < this.options.series.length; s++) {
						sum += this.data[i].seriesValues[s];
					}
					if (sum > maxSum) {
						maxSum = sum;
					}
				}
				var valueRatio = this.canvas.height / maxSum;
				// draw bars
				var x = 0;
				for (var i = 0; i < this.data.length; i++) {
					// draw series
					var y = this.canvas.height;
					for (var s = 0; s < this.options.series.length; s++) {
						ctx.fillStyle = this.options.series[s].colour;
						if (this.data[i].seriesValues[s] > 0) {
							var h = this.data[i].seriesValues[s] * valueRatio;
							ctx.fillRect(x, y - h, barWidth, h);
						}
						y -= h;
					}
					x += barWidth;
				}
			}

			// draw border
			ctx.strokeStyle = this.options.colours.border;
			ctx.strokeRect(0, 0, this.canvas.width, this.canvas.height);
		}
	}

	setData(data) {
		this.data = data;
		this.redraw();
	}

	term() {
		if (this.canvas) {
			//this.canvas.removeEventListener("mousedown", this.onMouseDown);
			//this.canvas.removeEventListener("mouseup", this.onMouseUp);
			//this.canvas.addEventListener("mousemove", this.onMouseMove);
			this.log("term timeline: ", this.canvasId);
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

export { Timeline };