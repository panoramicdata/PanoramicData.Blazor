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

export function initialize(id, opt, sessionId, dnRef) {
	const getPath = function (file) {
		var path = file.targetRootDir || "/";
		if (file.fullPath && file.fullPath.indexOf("/") > -1) {
			var idx = file.fullPath.lastIndexOf("/");
			if (!path.endsWith("/")) path = path + "/";
			path = path + file.fullPath.slice(0, idx);
		}
		return path;
	};
	var me = this;
	// create a debounced function to call when all files to upload determined
	var filesAddedFunction = debounce((dz) => {
		//console.log("addedfile - completed", dz.files);
		var files = dz.files.map(file => { return { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId } });
		dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnAllUploadsReady", files);
	}, 500);
	var options = Object.assign({
		url: "/files/upload",
		timeout: 30000,
		maxFilesize: 512,
		init: function () {
			// initialize batch variables
			this.fileCount = 0;
			// add event listeners
			this.on("drop", function () {
				this.fileCount = 0;
			});
			this.on("addedfile", function (file) {
				this.fileCount++;
				var fullPath = getPath(file);
				if (!fullPath.endsWith("/")) fullPath = fullPath + "/";
				fullPath = fullPath + (file.targetName || file.name);
				file.previewElement.querySelector(".pdfe-dz-name").innerHTML = fullPath;
				filesAddedFunction(this);
			});
			this.on("sending", function (file, xhr) {
				dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnUploadBegin", { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId });
			});
			this.on("uploadprogress", function (file, pct, bytes) {
				if (options.autoScroll) {
					file.previewElement.scrollIntoView();
				}
				dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnUploadProgress", { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId, Progress: pct });
			});
			this.on("success", function (file) {
				//console.log("success", file);
				dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnUploadEnd", { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId, Success: true });
				if (this.getQueuedFiles().length > 0) {
					this.processQueue();
				}
			});
			this.on("error", function (file, msg, xhr) {
				//console.log("error", file);
				dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnUploadEnd", { Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId, Success: false, Reason: msg });
				if (this.getQueuedFiles().length > 0) {
					this.processQueue();
				}
			});
			this.on("queuecomplete", function () {
				//console.log("queuecomplete");
				dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnAllUploadsComplete");
				if (this.fileCount)
					this.fileCount = 0;
				this.removeAllFiles(true);
			});
			this.on("totaluploadprogress", function (uploadProgress, totalBytes, totalBytesSent) {
				//console.log("totaluploadprogress", uploadProgress, totalBytes, totalBytesSent);
				dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnAllUploadsProgress", uploadProgress, totalBytes, totalBytesSent);
			});
		},
		accept: function (file, done) {
			dnRef.invokeMethodAsync("PanoramicData.Blazor.PDDropZone.OnDrop", [{ Path: getPath(file), Name: file.targetName || file.name, Size: file.size, Key: file.upload.uuid, SessionId: sessionId }])
				.then(data => {
					//console.log("accept", data);
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
				"Name": files[0].targetName || files[0].name,
				"Overwrite": files[0].overwrite || false
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
	var dzEl = document.querySelector(id);
	if (dzEl) {
		dzEl.dropzone = new Dropzone(id, options);
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
		var file = el.dropzone.files.find(x => x.upload.uuid == fileId);
		if (file) {
			el.dropzone.removeFile(file);
		}
	}
}