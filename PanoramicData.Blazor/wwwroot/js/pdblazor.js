window.panoramicData = {

	shortcutKeys: [],
	splits: {},
	unloadListener: false,
	unloadListenerIds: {},

	confirm: function (msg) {
		return window.confirm(msg);
	},

	isTouchDevice: function () {
		return (('ontouchstart' in window) ||
				(navigator.maxTouchPoints > 0) ||
				(navigator.msMaxTouchPoints > 0));
	},

	hasSplitJs: function () {
		return typeof Split !== 'undefined';
	},

	initializeSplitter: function (id, ids, options) {
		this.splits[id] = Split(ids, options);
	},

	splitterGetSizes: function (id) {
		if (this.splits[id]) {
			return this.splits[id].getSizes();
		}
	},

	splitterSetSizes: function (id, sizes) {
		if (this.splits[id]) {
			this.splits[id].setSizes(sizes);
		}
	},

	destroySplitter: function (id) {
		if (this.splits[id]) {
			delete this.splits[id];
		}
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

	click: function (id) {
		var el = document.getElementById(id);
		if (el && el.click) {
			el.click();
		}
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

	getElementAtPoint: function (x, y) {
		var el = document.elementFromPoint(x, y);
		if (el) {
			var baseInfo = this.getElementInfo(el);
			var info = baseInfo;
			el = el.parentElement;
			while (el) {
				info.parent = this.getElementInfo(el);
				info = info.parent;
				el = el.parentElement;
			}
			return baseInfo;
		}
		return null;
	},

	getElementInfo: function(el) {
		var info = {
			id: el.id || '',
			tag: el.tagName,
			classList: []
		}
		for (var i = 0; i < el.classList.length; i++) {
			info.classList.push(el.classList[i]);
		}
		return info;
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

	setValue: function (id, value) {
		var node = document.getElementById(id);
		if (node)
			node.value = value;
	},

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

	scrollIntoView: function (id, alignTop) {
		var el = document.getElementById(id);
		if (el)
			el.scrollIntoView(alignTop);
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

	initializeDropDown: function (id, ref) {
		var el = $(`#${id}`);
		if (el) {
			el.on("shown.bs.dropdown", function () {
				if (ref) {
					ref.invokeMethodAsync("OnDropDownShown");
				}
			});
			el.on("hidden.bs.dropdown", function () {
				if (ref) {
					ref.invokeMethodAsync("OnDropDownHidden");
				}
			});
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

	showBsDialog: function(id, backdrop, keyboard) {
		$(id).modal({
			show: true,
			backdrop: backdrop ? true : 'static',
			keyboard: keyboard || false
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

	initGlobalListener: function (ref) {
		this.globalListenerReference = ref;
		window.addEventListener("keydown", this.onKeyDown);
		window.addEventListener("keyup", this.onKeyUp);
	},

	destroyGlobalListener: function () {
		window.removeEventListener("keydown", this.onKeyDown);
		window.removeEventListener("keyup", this.onKeyUp);
		delete this.globalListenerReference;
	},

	registerShortcutKeys: function (shortcuts) {
		this.shortcutKeys = shortcuts || [];
	},

	isShortcutKeyMatch: function (keyInfo) {
		var match = this.shortcutKeys.find((v) => v.altKey == keyInfo.altKey &&
			v.ctrlKey == keyInfo.ctrlKey &&
			v.shiftKey == keyInfo.shiftKey &&
			(v.key.toLowerCase() == keyInfo.key.toLowerCase()) || (v.code.toLowerCase() == keyInfo.code.toLowerCase()));
		return match ? true : false;
	},

	onKeyDown: function (e) {
		if (window.panoramicData.globalListenerReference) {
			var keyInfo = panoramicData.getKeyArgs(e);
			if (window.panoramicData.isShortcutKeyMatch(keyInfo)) {
				e.stopPropagation();
				e.preventDefault();
			}
			window.panoramicData.globalListenerReference.invokeMethodAsync("OnKeyDown", keyInfo);
		}
	},

	onKeyUp: function (e) {
		if (window.panoramicData.globalListenerReference) {
			var keyInfo = panoramicData.getKeyArgs(e);
			if (window.panoramicData.isShortcutKeyMatch(keyInfo)) {
				e.stopPropagation();
				e.preventDefault();
			}
			window.panoramicData.globalListenerReference.invokeMethodAsync("OnKeyUp", keyInfo);
		}
	},

	getKeyArgs: function (e) {
		var obj = {};
		obj.key = e.key;
		obj.code = e.code;
		obj.keyCode = e.keyCode;
		obj.altKey = e.altKey;
		obj.ctrlKey = e.ctrlKey;
		obj.shiftKey = e.shiftKey;
		return obj;
	},

	clearFiles: function () {
		this.files = [];
	},

	initDropzone: function (idSelector, opt, sessionId, dnRef) {
		const getPath = function (file) {
			var path = file.targetRootDir || "/";
			if (file.fullPath && file.fullPath.indexOf("/") > -1) {
				var idx = file.fullPath.lastIndexOf("/");
				if (!path.endsWith('/')) path = path + "/";
				path = path + file.fullPath.slice(0, idx);
			}
			return path;
		};
		var options = Object.assign({
			url: "/files/upload",
			timeout: 30000,
			maxFilesize: 512,
			init: function () {
				// initialize batch variables
				this.fileCount = 0;
				this.adding = false;
				// add event listeners
				this.on('addedfile', function (file) {
					this.adding = true;
					this.fileCount++;
					var fullPath = getPath(file);
					if (!fullPath.endsWith('/')) fullPath = fullPath + "/";
					fullPath = fullPath + (file.targetName || file.name);
					file.previewElement.querySelector(".pdfe-dz-name").innerHTML = fullPath;
				});
				this.on("sending", function (file, xhr) {
					// sending event occurs after all files have been added - signal batch start
					if (this.adding) {
						this.adding = false;
						dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnAllUploadsStarted', this.fileCount);
					}
					dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadBegin', { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId });
				});
				this.on("uploadprogress", function (file, pct, bytes) {
					if (options.autoScroll) {
						file.previewElement.scrollIntoView();
					}
					dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadProgress', { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId, Progress: pct });
				});
				this.on("success", function (file) {
					dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadEnd', { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId, Success: true });
				});
				this.on("error", function (file, msg, xhr) {
					dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadEnd', { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId, Success: false, Reason: msg });
				});
				this.on("queuecomplete", function () {
					dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnAllUploadsComplete');
					if (this.fileCount)
						this.fileCount = 0;
				});
				this.on("totaluploadprogress", function (uploadProgress, totalBytes, totalBytesSent) {
					dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnAllUploadsProgress', uploadProgress, totalBytes, totalBytesSent);
				});
			},
			accept: function (file, done) {
				dnRef.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnDrop', [{ Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId }])
					.then(data => {
						if (data.cancel || data.reason) {
							done(data.reason || "Upload canceled");
						} else {
							// file skipped?
							if (data.files[0].skip) {
								done("Upload skipped");
							}
							// file renamed?
							if (data.files[0].newName) {
								file.targetName = data.files[0].newName;
							}
							file.targetRootDir = data.rootDir;
							done(); // accept file
						}
					});
			},
			params: function (files, xhr) {
				return {
					"SessionId": sessionId,
					"Key": files[0].upload.uuid,
					"Path": getPath(files[0]),
					"Name": files[0].targetName || files[0].name
				};
			}
		}, opt);
		if (opt.previewItemTemplate) {
			var el = document.querySelector(opt.previewItemTemplate);
			if (el) {
				options.previewTemplate = el.innerHTML;
			}
		}
		// store drop zone object
		var dzEl = document.querySelector(idSelector);
		if (dzEl) {
			dzEl.dropzone = new Dropzone(idSelector, options);
		}
	},

	dropzoneClick: function (el) {
		if (el && el.parentElement) {
			el.parentElement.click();
		}
	},

	clearDropzone: function (idSelector) {
		var el = document.querySelector(idSelector);
		if (el && el.dropzone) {
			el.dropzone.removeAllFiles();
		}
	},

	cancelDropzone: function (idSelector) {
		var el = document.querySelector(idSelector);
		if (el && el.dropzone) {
			el.dropzone.removeAllFiles(true);
		}
	},

	beforeUnloadListener: function (event) {
		event.preventDefault();
		return event.returnValue = "Exit and lose changes?";
	},

	removeUnloadListener: function () {
		if(this.unloadListener) {
			removeEventListener("beforeunload", panoramicData.beforeUnloadListener, { capture: true });
			this.unloadListener = false;
		}
		this.unloadListenerIds = {};
	},

	setUnloadListener: function (id, changesMade) {

		// update dictionary
		if (changesMade) {
			this.unloadListenerIds[id] = true;
		} else {
			delete this.unloadListenerIds[id];
		}

		// add or remove unload listener
		listenerCount = Object.keys(this.unloadListenerIds).length;
		if (listenerCount > 0 && !this.unloadListener) {
			addEventListener("beforeunload", panoramicData.beforeUnloadListener, { capture: true });
			this.unloadListener = true;
		}
		else if (listenerCount == 0 && this.unloadListener) {
			removeEventListener("beforeunload", panoramicData.beforeUnloadListener, { capture: true });
			this.unloadListener = false;
		}
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