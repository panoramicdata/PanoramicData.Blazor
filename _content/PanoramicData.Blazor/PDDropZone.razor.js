const CALLBACK_PREFIX = "PanoramicData.Blazor.PDDropZone.";

export function cancel(id) {
	var el = document.querySelector(id);
	if (el && el.dropzone) {
		el.dropzone.removeAllFiles(true);
	}
}

export function clear(id) {
	var el = document.querySelector(id);
	if (el && el.dropzone) {
		el.dropzone.removeAllFiles();
	}
}

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
	var zone = document.getElementById(id);
	if (zone?.dropzone) {
		zone.dropzone.destroy();
		// zone.dotnetHelper is disposed of by runtime
	}
}

function getPath(file) {
	var path = file.targetRootDir || "/";
	if (file.fullPath && file.fullPath.indexOf("/") > -1) {
		var idx = file.fullPath.lastIndexOf("/");
		if (!path.endsWith("/")) path = path + "/";
		path = path + file.fullPath.slice(0, idx);
	}
	return path;
}

function getFileName(file) {
	return file.targetName || file.name;
}

function describeFile(file, sessionId, extra) {
	return Object.assign(
		{
			Path: getPath(file),
			Name: getFileName(file),
			Size: file.size,
			Key: file.upload.uuid,
			SessionId: sessionId,
		},
		extra,
	);
}

function onFileAdded(dropzone, file, filesAddedFunction) {
	dropzone.fileCount++;
	var fullPath = getPath(file);
	if (!fullPath.endsWith("/")) fullPath = fullPath + "/";
	fullPath = fullPath + getFileName(file);
	file.previewElement.querySelector(".pdfe-dz-name").textContent = fullPath;
	filesAddedFunction(dropzone);
}

function onUploadEnd(dropzone, dnRef, fileInfo) {
	dnRef.invokeMethodAsync(CALLBACK_PREFIX + "OnUploadEnd", fileInfo);
	if (dropzone.getQueuedFiles().length > 0) {
		dropzone.processQueue();
	}
}

function registerEventHandlers(dropzone, context) {
	const dnRef = context.dnRef;
	const sessionId = context.sessionId;

	dropzone.on("drop", function () {
		this.fileCount = 0;
	});
	dropzone.on("addedfile", function (file) {
		onFileAdded(this, file, context.filesAddedFunction);
	});
	dropzone.on("sending", function (file) {
		dnRef.invokeMethodAsync(
			CALLBACK_PREFIX + "OnUploadBegin",
			describeFile(file, sessionId),
		);
	});
	dropzone.on("uploadprogress", function (file, pct) {
		if (context.options.autoScroll) {
			file.previewElement.scrollIntoView();
		}
		dnRef.invokeMethodAsync(
			CALLBACK_PREFIX + "OnUploadProgress",
			describeFile(file, sessionId, { Progress: pct }),
		);
	});
	dropzone.on("success", function (file) {
		onUploadEnd(this, dnRef, describeFile(file, sessionId, { Success: true }));
	});
	dropzone.on("error", function (file, msg) {
		const extra = { Success: false, Reason: msg };
		onUploadEnd(this, dnRef, describeFile(file, sessionId, extra));
	});
	dropzone.on("queuecomplete", function () {
		dnRef.invokeMethodAsync(CALLBACK_PREFIX + "OnAllUploadsComplete");
		if (this.fileCount) this.fileCount = 0;
		this.removeAllFiles(true);
	});
	dropzone.on("totaluploadprogress", function (progress, total, sent) {
		dnRef.invokeMethodAsync(
			CALLBACK_PREFIX + "OnAllUploadsProgress",
			progress,
			total,
			sent,
		);
	});
}

function applyDropResult(file, data, done) {
	if (data.cancel || data.reason) {
		done(data.reason || "Upload canceled");
		return;
	}

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

export function initialize(id, opt, sessionId, dnRef) {
	// create a debounced function to call when all files to upload determined
	var filesAddedFunction = debounce((dz) => {
		var files = dz.files.map((file) => describeFile(file, sessionId));
		dnRef.invokeMethodAsync(CALLBACK_PREFIX + "OnAllUploadsReady", files);
	}, 500);
	var options = Object.assign(
		{
			url: "/files/upload",
			timeout: 30000,
			maxFilesize: 512,
			init: function () {
				// initialize batch variables
				this.fileCount = 0;
				// add event listeners
				registerEventHandlers(this, {
					dnRef,
					sessionId,
					options,
					filesAddedFunction,
				});
			},
			accept: function (file, done) {
				dnRef
					.invokeMethodAsync(CALLBACK_PREFIX + "OnDrop", [
						describeFile(file, sessionId),
					])
					.then((data) => applyDropResult(file, data, done));
			},
			params: function (files) {
				return {
					SessionId: sessionId,
					Key: files[0].upload.uuid,
					Path: getPath(files[0]),
					Name: getFileName(files[0]),
					Overwrite: files[0].overwrite || false,
				};
			},
		},
		opt,
	);
	if (opt.previewItemTemplate) {
		var el = document.querySelector(opt.previewItemTemplate);
		if (el) {
			options.previewTemplate = el.innerHTML;
		}
	}
	// store drop zone object (Dropzone is loaded by the host page as a global script)
	var dzEl = document.querySelector(id);
	if (dzEl) {
		dzEl.dropzone = new window.Dropzone(id, options);
	}
}

export function process(id, overwriteAll) {
	var el = document.querySelector(id);
	if (el && el.dropzone) {
		if (overwriteAll) {
			for (var i = 0; i < el.dropzone.files.length; i++) {
				el.dropzone.files[i].overwrite = true;
			}
		}
		el.dropzone.processQueue();
	}
}

export function removeFile(id, fileId) {
	var el = document.querySelector(id);
	if (el && el.dropzone) {
		var file = el.dropzone.files.find((x) => x.upload.uuid == fileId);
		if (file) {
			el.dropzone.removeFile(file);
		}
	}
}
