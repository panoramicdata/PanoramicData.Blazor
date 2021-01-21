window.panoramicData = {

	hasSplitJs: function () {
		return typeof Split !== 'undefined';
	},

	initializeSplitter: function (ids, options) {
		Split(ids, options);
	},

	hasPopperJs: function() {
		return typeof Popper !== 'undefined';
	},

	showMenu: function(menuId, x, y) {
		var menu = window.panoramicData.contextMenuEl = document.getElementById(menuId);
		var reference = {
			getBoundingClientRect() {
				return {
					width: 0,
					height: 0,
					top: y,
					bottom: y,
					left: x,
					right: x
				};
			}
		};
		var options = {
			placement: 'bottom-start',
			positionFixed: true
		};
		menu.classList.add("show");
		//var popper = Popper.createPopper(reference, menu, options); // this is popper v2.4.4 syntax
		window.panoramicData.menuPopper = new Popper(reference, menu, options); // this is popper v1.16.1 syntax
		document.addEventListener("mousedown", window.panoramicData.menuMouseDown);
	},

	menuMouseDown: function (event) {
		var menu = window.panoramicData.contextMenuEl;
		if (menu && window.panoramicData.menuPopper) {
			let isClickInside = menu.contains(event.target);
			if (!isClickInside) {
				window.panoramicData.hideMenu();
			}
		}
	},

	hideMenu: function (menuId) {
		var menu = window.panoramicData.contextMenuEl;
		if (menu) {
			menu.classList.remove("show");
			document.removeEventListener("mousedown", window.panoramicData.menuMouseDown);
			window.panoramicData.contextMenuEl = null;
			if (window.panoramicData.menuPopper) {
				window.panoramicData.menuPopper.destroy();
				window.panoramicData.menuPopper = null;
			}
		}
	},

	focus: function(id) {
		var node = document.getElementById(id);
		if (node && node.focus) {
			node.focus();
			return true;
		}
		return false;
	},

	selectText: function(id, start, end) {
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

	getFocusedElementId: function() {
		return document.activeElement.id;
	},

	getValue: function(id) {
		var node = document.getElementById(id);
		if(node)
			return node.value;
		return null;
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

	disposeDropZone: function(id) {
		var zone = document.getElementById(id);
		if (zone) {
			zone.removeEventListener('dragenter', panoramicData.onDropZoneDragEnter, false);
			zone.removeEventListener('dragover', panoramicData.onDropZoneDragEnter, false);
			zone.removeEventListener('dragleave', panoramicData.onDropZoneDragLeave, false);
			zone.removeEventListener('drop', panoramicData.onDropZoneDrop, false);
			// zone.dotnetHelper is disposed of by runtime
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

	showBsDialog: function(id, backdrop) {
		$(id).modal({
			show: true,
			backdrop: backdrop ? true : 'static'
		})
	},

	hideBsDialog: function(id) {
		$(id).modal('hide');
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

	debounce: function(func, wait) {
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
}