export function scrollToBottom(element) {
	if (element) {
		// Use smooth scrolling for better UX
		element.scrollTo({
			top: element.scrollHeight,
			behavior: 'smooth'
		});
		
		// Fallback for browsers that don't support smooth scrolling
		if (element.scrollTop < element.scrollHeight - element.clientHeight - 10) {
			element.scrollTop = element.scrollHeight;
		}
	}
}