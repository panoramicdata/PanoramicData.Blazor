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

async function blobToBase64(blob) {
	const bytes = new Uint8Array(await blob.arrayBuffer());
	let binary = "";
	const chunk = 0x8000;
	for (let i = 0; i < bytes.length; i += chunk) {
		binary += String.fromCharCode(...bytes.subarray(i, i + chunk));
	}

	return btoa(binary);
}

// A Blazor Server hub refuses a message from the browser over 32 KB by default, so an image goes in pieces.
const chunkSize = 16000;
let imageCounter = 0;

async function sendImage(dotNetRef, type, base64, name) {
	const key = `${Date.now()}-${imageCounter++}`;
	for (let i = 0; i < base64.length; i += chunkSize) {
		await dotNetRef.invokeMethodAsync(
			"OnImageChunk",
			key,
			base64.slice(i, i + chunkSize),
			i + chunkSize >= base64.length,
			type,
			name,
		);
	}
}

// The image as [type, base64], re-encoded as JPEG only when it is too big to send as it is.
async function shrink(source, width, height, original) {
	const scale = Math.min(1, maxImageEdge / Math.max(width, height));
	if (original && scale === 1 && original.size <= keepAsIsBytes) {
		return [original.type, await blobToBase64(original)];
	}

	const canvas = document.createElement("canvas");
	canvas.width = Math.max(1, Math.round(width * scale));
	canvas.height = Math.max(1, Math.round(height * scale));
	canvas.getContext("2d").drawImage(source, 0, 0, canvas.width, canvas.height);
	const url = canvas.toDataURL("image/jpeg", 0.85);
	return ["image/jpeg", url.split(",")[1]];
}

async function readImage(blob) {
	const bitmap = await createImageBitmap(blob);
	const image = await shrink(bitmap, bitmap.width, bitmap.height, blob);
	bitmap.close();
	return image;
}

async function addImage(blob, name, dotNetRef) {
	const image = await readImage(blob).catch(() => null);
	if (!image) {
		await dotNetRef.invokeMethodAsync(
			"OnImageError",
			"That image could not be read.",
		);
		return;
	}

	await sendImage(dotNetRef, image[0] || "image/png", image[1], name ?? null);
}

function imageFiles(files) {
	return Array.from(files ?? []).filter((f) => f.type.startsWith("image/"));
}

function clipboardImages(clipboardData) {
	const files = Array.from(clipboardData?.items ?? [])
		.filter((item) => item.kind === "file")
		.map((item) => item.getAsFile());
	return imageFiles(files);
}

export function attachImageHandlers(textarea, dotNetRef) {
	if (!textarea) return;
	detachImageHandlers(textarea);
	const zone = textarea.closest(".chat-input-container") ?? textarea;
	const onPaste = async (e) => {
		const images = clipboardImages(e.clipboardData);
		if (images.length === 0) return;
		e.preventDefault();
		for (const file of images) {
			await addImage(file, null, dotNetRef);
		}
	};
	const onDragOver = (e) => {
		if (Array.from(e.dataTransfer?.types ?? []).includes("Files")) {
			e.preventDefault();
		}
	};
	const onDrop = async (e) => {
		const images = imageFiles(e.dataTransfer?.files);
		if (images.length === 0) return;
		e.preventDefault();
		for (const file of images) {
			await addImage(file, file.name, dotNetRef);
		}
	};
	textarea.addEventListener("paste", onPaste);
	zone.addEventListener("dragover", onDragOver);
	zone.addEventListener("drop", onDrop);
	textarea._pdImageHandlers = { zone, onPaste, onDragOver, onDrop };
	textarea.dataset.imagesReady = "true";
}

export function detachImageHandlers(textarea) {
	const h = textarea?._pdImageHandlers;
	if (!h) return;
	textarea.removeEventListener("paste", h.onPaste);
	h.zone.removeEventListener("dragover", h.onDragOver);
	h.zone.removeEventListener("drop", h.onDrop);
	delete textarea._pdImageHandlers;
	delete textarea.dataset.imagesReady;
}

// The file input sits in the same chat panel as the text box.
function filePicker(textarea) {
	return textarea
		?.closest(".pdchat-panel-flex")
		?.querySelector(".pdchat-image-file-input");
}

export function openFilePicker(textarea) {
	const input = filePicker(textarea);
	if (input) {
		input.value = "";
		input.click();
	}
}

export async function readPickedFiles(textarea, dotNetRef, room) {
	const input = filePicker(textarea);
	const files = imageFiles(input?.files);
	const allowed = Math.max(room, 0);
	if (input) input.value = "";
	if (files.length > allowed) {
		await dotNetRef.invokeMethodAsync(
			"OnImageError",
			"A message can carry at most 5 images.",
		);
	}

	for (const file of files.slice(0, allowed)) {
		await addImage(file, file.name, dotNetRef);
	}
}

// The current tab only: Chrome and Edge skip the picker with preferCurrentTab, and still ask once for permission.
export async function captureTab(dotNetRef) {
	if (!navigator.mediaDevices?.getDisplayMedia) {
		await dotNetRef.invokeMethodAsync(
			"OnImageError",
			"This browser cannot capture the screen.",
		);
		return;
	}

	let stream;
	try {
		stream = await navigator.mediaDevices.getDisplayMedia({
			video: true,
			audio: false,
			preferCurrentTab: true,
			selfBrowserSurface: "include",
		});
	} catch {
		await dotNetRef.invokeMethodAsync(
			"OnImageError",
			"The screenshot was not allowed.",
		);
		return;
	}

	const stop = () => stream.getTracks().forEach((t) => t.stop());
	const video = document.createElement("video");
	video.muted = true;
	video.srcObject = stream;

	// The first frame is not there when play() returns, so the capture waits for it.
	video.addEventListener(
		"loadeddata",
		async () => {
			try {
				const frame = await createImageBitmap(video);
				const [type, base64] = await shrink(
					frame,
					frame.width,
					frame.height,
					null,
				);
				frame.close();
				await sendImage(dotNetRef, type, base64, "Screenshot");
			} catch {
				await dotNetRef.invokeMethodAsync(
					"OnImageError",
					"The screenshot could not be taken.",
				);
			} finally {
				stop();
			}
		},
		{ once: true },
	);

	const isPlaying = await video.play().then(
		() => true,
		() => false,
	);
	if (!isPlaying) {
		stop();
		await dotNetRef.invokeMethodAsync(
			"OnImageError",
			"The screenshot could not be taken.",
		);
	}
}
