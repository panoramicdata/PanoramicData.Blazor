export function scrollToBottom(element) {
	if (element) {
		element.scrollTo({
			top: element.scrollHeight,
			behavior: 'smooth'
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
		textarea.removeEventListener('keydown', textarea._pdEnterHandler);
	}
	textarea._pdEnterHandler = async (e) => {
		if (e.key === 'Enter' && !e.shiftKey && !e.altKey && !e.ctrlKey) {
			e.preventDefault();
			await dotNetRef.invokeMethodAsync('OnEnterPressed');
		}
	};
	textarea.addEventListener('keydown', textarea._pdEnterHandler);
}

export function detachEnterHandler(textarea) {
	if (textarea && textarea._pdEnterHandler) {
		textarea.removeEventListener('keydown', textarea._pdEnterHandler);
		delete textarea._pdEnterHandler;
	}
}
