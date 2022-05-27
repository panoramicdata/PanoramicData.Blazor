window.panoramicData = {

	// -----------------------------------------------------------------------------------
	//
	// MOVED To common.js
	//
	// -----------------------------------------------------------------------------------

	addClass: function (id, cls) {
		var el = document.getElementById(id);
		if (el)
			el.classList.add(cls);
	},

	removeClass: function (id, cls) {
		var el = document.getElementById(id);
		if (el)
			el.classList.remove(cls);
	},

	focus: function (id) {
		var node = document.getElementById(id);
		if (node && node.focus) {
			node.focus();
			return true;
		}
		return false;
	},

	debounce: function (func, wait) {
		let timeout;
		return function executedFunction(...args) {
			const later = () => {
				timeout = null;
				func(...args);
			};
			clearTimeout(timeout);
			timeout = setTimeout(later, wait);
		};
	},

	click: function (id) {
		var el = document.getElementById(id);
		if (el && el.click) {
			el.click();
		}
	},

	isTouchDevice: function () {
		return (('ontouchstart' in window) ||
			(navigator.maxTouchPoints > 0) ||
			(navigator.msMaxTouchPoints > 0));
	},

	scrollIntoView: function (id, alignTop) {
		var el = document.getElementById(id);
		if (el)
			el.scrollIntoView(alignTop);
	},

	selectText: function (id, start, end) {
		var node = document.getElementById(id);
		if (!node) return;
		if (!start) start = 0;
		if (!end) end = node.value.length;
		if (node.createTextRange) {
			var selRange = node.createTextRange();
			selRange.collapse(true);
			selRange.moveStart('character', start);
			selRange.moveEnd('character', end);
			selRange.select();
			node.focus();
		} else if (node.setSelectionRange) {
			node.focus();
			node.setSelectionRange(start, end);
		} else if (typeof node.selectionStart != 'undefined') {
			node.selectionStart = start;
			node.selectionEnd = end;
			node.focus();
		}
	},

	// -----------------------------------------------------------------------------------


	confirm: function (msg) {
		return window.confirm(msg);
	},

	getValue: function(id) {
		var node = document.getElementById(id);
		if(node)
			return node.value;
		return null;
	},

	setValue: function (id, value) {
		var node = document.getElementById(id);
		if (node)
			node.value = value;
	},

	// 04/08/20 - bytesBase64 limited to 125MB by System.Text.Json writer
	downloadfromBase: function(filename, bytesBase64) {
		var link = document.createElement('a');
		link.download = filename;
		link.href = "data:application/octet-stream;base64," + bytesBase64;
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
	},

	downloadFromUrl: function (url, fileName) {
		var xhr = new XMLHttpRequest();
		xhr.open("GET", url, true);
		xhr.responseType = "blob";
		xhr.onload = function () {
			var urlCreator = window.URL || window.webkitURL;
			var imageUrl = urlCreator.createObjectURL(this.response);
			var tag = document.createElement('a');
			tag.href = imageUrl;
			tag.download = fileName;
			document.body.appendChild(tag);
			tag.click();
			document.body.removeChild(tag);
		}
		xhr.send();
	},

	initializeDropZone: function(id, uploadUrl, dotnetHelper) {
		var zone = document.getElementById(id);
		if (zone) {
			zone.dotnetHelper = dotnetHelper;
			zone.uploadUrl = uploadUrl;
			zone.addEventListener('dragenter', panoramicData.onDropZoneDragEnterOver, false);
			zone.addEventListener('dragover', panoramicData.onDropZoneDragEnterOver, false);
			zone.addEventListener('dragleave', panoramicData.onDropZoneDragLeave, false);
			zone.addEventListener('drop', panoramicData.onDropZoneDrop, false);
		}
	},

	onDropZoneDragEnterOver: function (e) {
		if (e.dataTransfer && e.dataTransfer.types && e.dataTransfer.types.indexOf("Files") > -1) {
			var zone = panoramicData.findAncestor(e.target, 'pddropzone');
			if (zone) {
				e.preventDefault();
				e.stopPropagation();
				zone.classList.add('highlight');
			}
		}
	},

	onDropZoneDrop: function(e) {
		if (e.dataTransfer && e.dataTransfer.types && e.dataTransfer.types.indexOf("Files") > -1) {
			var zone = panoramicData.findAncestor(e.target, 'pddropzone');
			if (zone) {
				e.preventDefault();
				e.stopPropagation();
				zone.classList.remove('highlight');
				let files = e.dataTransfer.files
				if (zone.dotnetHelper) {
					var dto = [];
					for (var i = 0; i < files.length; i++)
						dto.push({ Name: files[i].name, Size: files[i].size, Skip: false });
						zone.dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnDrop', dto)
							.then(result => {
								if (!result.cancel) {
									for (var i = 0; i < files.length; i++) {
										var skip = result.files.reduce(function (pv, cv) {
											return pv || (cv.name == files[i].name && cv.skip);
										}, false);
										if (!skip) {
											panoramicData.uploadFile(files[i], zone.uploadUrl, result.state, zone);
										}
									}
								}
							});
				}
			}
		}
	},

	onDropZoneDragLeave: function(e) {
		if (e.dataTransfer && e.dataTransfer.types && e.dataTransfer.types.indexOf("Files") > -1) {
			var zone = panoramicData.findAncestor(e.target, 'pddropzone');
			if (zone) {
				e.preventDefault();
				e.stopPropagation();
				zone.classList.remove('highlight');
			}
		}
	},

	findAncestor: function(el, cls) {
		while ((el = el.parentElement) && !el.classList.contains(cls));
		return el;
	},

	uploadFile: function (file, url, path, zone) {
		var pct = 0;
		var xhr = new XMLHttpRequest();
		var formData = new FormData();
		xhr.open('POST', url, true);
		xhr.upload.addEventListener("progress", function (e) {
			var progress = Math.round(e.loaded * 100 / e.total || 100);
			if (progress > pct) {
				pct = progress;
				if (zone.dotnetHelper) {
					zone.dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadProgress', { Path: path, Name: file.name, Size: file.size, Progress: progress });
				}
			}
		})
		xhr.addEventListener('readystatechange', function (e) {
			if (xhr.readyState == 4 && xhr.status == 200) {
				// done - send upload complete
				if (zone.dotnetHelper)
					zone.dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadEnd', { Path: path, Name: file.name, Size: file.size, Success: true });
			}
			else if (xhr.readyState == 4 && xhr.status != 200) {
				// error - send error
				if (zone.dotnetHelper)
					zone.dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadEnd', { Path: path, Name: file.name, Size: file.size, Success: false, StatusCode: xhr.status });
			}
		});
		formData.append('path', path);
		formData.append('file', file);
		if (zone.dotnetHelper) {
			zone.dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadBegin', { Path: path, Name: file.name, Size: file.size })
				.then(data => {
					if (data.length) {
						for (var i = 0; i < data.length; i++) {
							var kvp = data[i].split('=');
							if (kvp.length && kvp.length == 2) {
								formData.append(kvp[0], kvp[1]);
							}
						}
					}
					xhr.send(formData);
				});
		}
		else {
			xhr.send(formData);
		}
		//return xhr;
	},

	initPopover: function(el) {
		$(el).popover({
			container: 'body'
		});
	},

	disposePopover: function(el) {
		$(el).popover('dispose');
	},

	openUrl: function(url, target) {
		window.open(url, target);
	},

	debounceInput: function(id, wait, objRef) {
		var el = document.getElementById(id);
		if (el) {
			var debouncedFunction = panoramicData.debounce(function (ev) {
				objRef.invokeMethodAsync('OnDebouncedInput', ev.srcElement.value)
			}, wait);
			el.addEventListener('input', debouncedFunction);
		}
	},

	alert: function (msg) {
		alert(msg);
	},

	initializeFileSelect: function (id, dropZoneId) {
		const inputEl = document.getElementById(id);
		const dropzoneEl = document.getElementById(dropZoneId);
		if (inputEl && dropzoneEl) {
			inputEl.zoneEl = dropzoneEl;
			inputEl.addEventListener("change", panoramicData.handleSelectedFiles, false);
		}
	},

	handleSelectedFiles: function() {
		var me = this;
		var files = this.files;
		var zone = this.zoneEl;
		if (zone.dotnetHelper) {
			var dto = [];
			for (var i = 0; i < files.length; i++)
				dto.push({ Name: files[i].name, Size: files[i].size, Skip: false });
			zone.dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnDrop', dto)
				.then(result => {
					if (result.cancel) {
						me.files = [];
					} else {
						for (var i = 0; i < files.length; i++) {
							var skip = result.files.reduce(function (pv, cv) {
								return pv || (cv.name == files[i].name && cv.skip);
							}, false);
							if (!skip) {
								panoramicData.uploadFile(files[i], zone.uploadUrl, result.state, zone);
							}
						}
					}
					me.value = '';
				});
		}
	},

	disposeFileSelect: function (id) {
		var el = document.getElementById(id);
		if (el) {
			delete el.zoneEl;
			el.removeEventListener('change', panoramicData.handleSelectedFiles, false);
		}
	},

	clearFiles: function () {
		this.files = [];
	},

	getAddress: function () {
		return window.location.href;
	},

	updateAddress: function (url) {
		window.history.replaceState(null, document.title, url);
	},

	getHeight: function (el) {
		var rect = el.getBoundingClientRect();
		return rect.height || 0;
	},

	getWidth: function (el) {
		var rect = el.getBoundingClientRect();
		return rect.width || 0;
	},

	getX: function (el) {
		var rect = el.getBoundingClientRect();
		return rect.left || 0;
	},

	getY: function (el) {
		var rect = el.getBoundingClientRect();
		return rect.top || 0;
	},

	setPointerCapture: function (id, el) {
		el.setPointerCapture(id);
	},

	zoombar: {

		zoombars: {
		},

		init: function (id, value, options, ref) {
			import('./zoombar.js')
				.then((module) => {
					this.zoombars[id] = new module.Zoombar(id, value, options, ref);
				});
		},

		setValue: function (id, v) {
			var zb = this.zoombars[id];
			if (zb) {
				return zb.setValue(v);
			}
			return 0;
		},

		term: function (id) {
			var zb = this.zoombars[id];
			if (zb) {
				zb.term();
			}
		}
	},

	timeline: {

		timelines: {
		},

		init: function (id, options, data, ref) {
			import('./timeline.js')
				.then((module) => {
					this.timelines[id] = new module.Timeline(id, options, data, ref);
				});
		},

		setData: function (id, data) {
			var tl = this.timelines[id];
			if (tl) {
				tl.setData(data);
			}
		},

		term: function (id) {
			var tl = this.timelines[id];
			if (tl) {
				tl.term();
			}
		}
	},

	canvas: {

		setFillStyle: function (id, fillStyle) {
			var canvas = document.getElementById(id);
			if (canvas) {
				var ctx = canvas.getContext("2d");
				ctx.fillStyle = fillStyle;
			}
		},

		drawRect: function (id, x, y, w, h) {
			var canvas = document.getElementById(id);
			if (canvas) {
				var ctx = canvas.getContext("2d");
				ctx.fillRect(x, y, w, h);
			}
		}
	}
}