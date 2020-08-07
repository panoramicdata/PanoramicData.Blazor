function hasSplitJs() {
	return typeof Split !== 'undefined';
}

function initializeSplitter(ids, options) {
	Split(ids, options);
}

function hasPopperJs() {
	return typeof Popper !== 'undefined';
}

function showMenu(menuId, x, y) {
	var menu = document.getElementById(menuId);
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
		placement: 'bottom-start'
	};
	menu.classList.add("show");
	var popper = Popper.createPopper(reference, menu, options);
	document.addEventListener("mousedown", function (event) {
		let isClickInside = menu.contains(event.target);
		if (!isClickInside) {
			menu.classList.remove("show");
			popper.destroy();
		}
	});
}

function hideMenu(menuId) {
	var menu = document.getElementById(menuId);
	menu.classList.remove("show");
}

function focus(id) {
	var node = document.getElementById(id);
	if (node && node.focus) {
		node.focus();
	}
}

function selectText(id, start, end) {
	var node = document.getElementById(id);
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
}

function getFocusedElementId() {
	return document.activeElement.id;
}

function getValue(id) {
	var node = document.getElementById(id);
	if(node)
		return node.value;
	return null;
}

// 04/08/20 - bytesBase64 limited to 125MB by System.Text.Json writer
function downloadFile(filename, bytesBase64) {
	var link = document.createElement('a');
	link.download = filename;
	link.href = "data:application/octet-stream;base64," + bytesBase64;
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
}

function initializeDropZone(id, uploadUrl, dotnetHelper) {
	var zone = document.getElementById(id);
	if (zone) {
		zone.dotnetHelper = dotnetHelper;
		zone.uploadUrl = uploadUrl;
		zone.addEventListener('dragenter', onDropZoneDragEnter, false);
		zone.addEventListener('dragover', onDropZoneDragEnter, false);
		zone.addEventListener('dragleave', onDropZoneDragLeave, false);
		zone.addEventListener('drop', onDropZoneDrop, false);
	}
}

function onDropZoneDragEnter(e) {
	var zone = findAncestor(e.target, 'pddropzone');
	if (zone) {
		e.preventDefault();
		e.stopPropagation();
		zone.classList.add('highlight');
	}
}

function onDropZoneDrop(e) {
	var zone = findAncestor(e.target, 'pddropzone');
	if (zone) {
		e.preventDefault();
		e.stopPropagation();
		zone.classList.remove('highlight');
		let files = e.dataTransfer.files
		if (zone.dotnetHelper) {
			var dto = [];
			for (var i = 0; i < files.length; i++)
				dto.push({ Name: files[i].name, Size: files[i].size });
			zone.dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnDrop', dto)
				.then(result => {
					if (result.cancel) {
						console.warn(result.reason);
					} else {
						for (var i = 0; i < files.length; i++)
							uploadFile(files[i], zone.uploadUrl, result.state, zone.dotnetHelper);
					}
				});
		}
	}
}

function onDropZoneDragLeave(e) {
	var zone = findAncestor(e.target, 'pddropzone');
	if (zone) {
		e.preventDefault();
		e.stopPropagation();
		zone.classList.remove('highlight');
	}
}

function disposeDropZone(id) {
	var zone = document.getElementById(id);
	if (zone) {
		zone.removeEventListener('dragenter', onDropZoneDragEnter, false);
		zone.removeEventListener('dragover', onDropZoneDragEnter, false);
		zone.removeEventListener('dragleave', onDropZoneDragLeave, false);
		zone.removeEventListener('drop', onDropZoneDrop, false);
		if (zone.dotnetHelper) {
			zone.dotnetHelper.dispose();
		}
	}
}

function findAncestor(el, cls) {
	while ((el = el.parentElement) && !el.classList.contains(cls));
	return el;
}

function uploadFile(file, url, path, dotnetHelper) {
	var xhr = new XMLHttpRequest();
	var formData = new FormData()
	xhr.open('POST', url, true);
	xhr.addEventListener('readystatechange', function (e) {
		if (xhr.readyState == 4 && xhr.status == 200) {
			// done - send upload complete
			if (dotnetHelper)
				dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadEnd', { Name: file.name, Size: file.size, Success: true });
		}
		else if (xhr.readyState == 4 && xhr.status != 200) {
			// error - send error
			dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadEnd', { Name: file.name, Size: file.size, Success: false, StatusCode: xhr.status });
		}
	});
	formData.append('path', path);
	formData.append('file', file);
	if (dotnetHelper)
		dotnetHelper.invokeMethodAsync('PanoramicData.Blazor.PDDropZone.OnUploadBegin', { Name: file.name, Size: file.size });
	xhr.send(formData);
}