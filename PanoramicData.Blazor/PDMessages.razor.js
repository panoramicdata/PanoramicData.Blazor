export function scrollToBottom(element) {
	if (element) {
		element.scrollTo({
			top: element.scrollHeight,
			behavior: "smooth",
		});

		if (element.scrollTop < element.scrollHeight - element.clientHeight - 10) {
			element.scrollTop = element.scrollHeight;
		}
	}
}

// Attach a native keydown listener to the textarea that prevents the default
// Enter newline BEFORE Blazor's oninput fires, then calls back into .NET to send.
export function attachEnterHandler(textarea, dotNetRef) {
	if (!textarea) return;
	// Remove any existing handler before attaching to avoid duplicates
	if (textarea._pdEnterHandler) {
		textarea.removeEventListener("keydown", textarea._pdEnterHandler);
	}
	textarea._pdEnterHandler = async (e) => {
		if (e.key === "Enter" && !e.shiftKey && !e.altKey && !e.ctrlKey) {
			e.preventDefault();
			await dotNetRef.invokeMethodAsync("OnEnterPressed");
		}
	};
	textarea.addEventListener("keydown", textarea._pdEnterHandler);
}

export function detachEnterHandler(textarea) {
	if (textarea && textarea._pdEnterHandler) {
		textarea.removeEventListener("keydown", textarea._pdEnterHandler);
		delete textarea._pdEnterHandler;
	}
}


// Images: pasted, dropped, picked or captured images are shrunk in the browser, then handed to .NET one at a time.
const maxImageEdge = 1600;
const keepAsIsBytes = 1.5 * 1024 * 1024;

function blobToBase64(blob) {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = () => resolve(String(reader.result).split(",")[1] ?? "");
		reader.onerror = () => reject(reader.error);
		reader.readAsDataURL(blob);
	});
}

async function shrink(blob) {
	const bitmap = await createImageBitmap(blob);
	const scale = Math.min(1, maxImageEdge / Math.max(bitmap.width, bitmap.height));
	if (scale === 1 && blob.size <= keepAsIsBytes) {
		bitmap.close();
		return blob;
	}

	const canvas = document.createElement("canvas");
	canvas.width = Math.max(1, Math.round(bitmap.width * scale));
	canvas.height = Math.max(1, Math.round(bitmap.height * scale));
	canvas.getContext("2d").drawImage(bitmap, 0, 0, canvas.width, canvas.height);
	bitmap.close();
	return await new Promise((resolve, reject) =>
		canvas.toBlob((b) => (b ? resolve(b) : reject(new Error("encode failed"))), "image/jpeg", 0.85));
}

async function addImage(blob, name, dotNetRef) {
	try {
		const small = await shrink(blob);
		await dotNetRef.invokeMethodAsync("OnImageAdded", small.type || "image/png", await blobToBase64(small), name ?? null);
	} catch {
		await dotNetRef.invokeMethodAsync("OnImageError", "That image could not be read.");
	}
}

function imageFiles(files) {
	return Array.from(files ?? []).filter((f) => f.type.startsWith("image/"));
}

export function attachImageHandlers(textarea, dotNetRef) {
	if (!textarea) return;
	detachImageHandlers(textarea);
	const zone = textarea.closest(".chat-input-container") ?? textarea;
	const onPaste = async (e) => {
		const images = imageFiles(Array.from(e.clipboardData?.items ?? []).filter((i) => i.kind === "file").map((i) => i.getAsFile()));
		if (images.length === 0) return;
		e.preventDefault();
		for (const file of images) await addImage(file, null, dotNetRef);
	};
	const onDragOver = (e) => {
		if (Array.from(e.dataTransfer?.types ?? []).includes("Files")) e.preventDefault();
	};
	const onDrop = async (e) => {
		const images = imageFiles(e.dataTransfer?.files);
		if (images.length === 0) return;
		e.preventDefault();
		for (const file of images) await addImage(file, file.name, dotNetRef);
	};
	textarea.addEventListener("paste", onPaste);
	zone.addEventListener("dragover", onDragOver);
	zone.addEventListener("drop", onDrop);
	textarea._pdImageHandlers = { zone, onPaste, onDragOver, onDrop };
}

export function detachImageHandlers(textarea) {
	const h = textarea?._pdImageHandlers;
	if (!h) return;
	textarea.removeEventListener("paste", h.onPaste);
	h.zone.removeEventListener("dragover", h.onDragOver);
	h.zone.removeEventListener("drop", h.onDrop);
	delete textarea._pdImageHandlers;
}

export function openFilePicker(input) {
	if (input) {
		input.value = "";
		input.click();
	}
}

export async function readPickedFiles(input, dotNetRef, room) {
	const files = imageFiles(input?.files);
	if (input) input.value = "";
	if (files.length > Math.max(room, 0)) {
		await dotNetRef.invokeMethodAsync("OnImageError", "A message can carry at most 5 images.");
	}

	for (const file of files.slice(0, Math.max(room, 0))) await addImage(file, file.name, dotNetRef);
}

// The current tab only: Chrome and Edge skip the picker with preferCurrentTab, and still ask once for permission.
export async function captureTab(dotNetRef) {
	if (!navigator.mediaDevices?.getDisplayMedia) {
		await dotNetRef.invokeMethodAsync("OnImageError", "This browser cannot capture the screen.");
		return;
	}

	let stream;
	try {
		stream = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: false, preferCurrentTab: true, selfBrowserSurface: "include" });
	} catch {
		await dotNetRef.invokeMethodAsync("OnImageError", "The screenshot was not allowed.");
		return;
	}

	try {
		const video = document.createElement("video");
		video.muted = true;
		video.srcObject = stream;
		await video.play();
		await new Promise((resolve) => requestAnimationFrame(() => resolve()));
		const canvas = document.createElement("canvas");
		canvas.width = video.videoWidth;
		canvas.height = video.videoHeight;
		canvas.getContext("2d").drawImage(video, 0, 0);
		const blob = await new Promise((resolve) => canvas.toBlob(resolve, "image/png"));
		if (blob) await addImage(blob, "Screenshot", dotNetRef);
	} catch {
		await dotNetRef.invokeMethodAsync("OnImageError", "The screenshot could not be taken.");
	} finally {
		stream.getTracks().forEach((t) => t.stop());
	}
}
