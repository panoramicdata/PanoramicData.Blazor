export function playSound(soundUrl) {
	if (soundUrl) {
		const audio = new Audio(soundUrl);
		audio.play().catch(error => console.error('Error playing sound:', error));
	}
}