function init(container, handle, cornerClass) {
	if (!container || !handle) return;

	let resizing = false;
	let startX, startY, startWidth, startHeight;

	const onPointerDown = (e) => {
		resizing = true;
		startX = e.clientX;
		startY = e.clientY;
		const style = getComputedStyle(container);
		startWidth = parseInt(style.width, 10);
		startHeight = parseInt(style.height, 10);
		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
		e.preventDefault();
	};

	const onPointerMove = (e) => {
		if (!resizing) return;

		let dx = e.clientX - startX;
		let dy = e.clientY - startY;

		switch (cornerClass) {
			case 'handle-tl':
				container.style.width = `${startWidth - dx}px`;
				container.style.height = `${startHeight - dy}px`;
				container.style.left = `${container.offsetLeft + dx}px`;
				container.style.top = `${container.offsetTop + dy}px`;
				break;

			case 'handle-tr':
				container.style.width = `${startWidth + dx}px`;
				container.style.height = `${startHeight - dy}px`;
				container.style.top = `${container.offsetTop + dy}px`;
				break;

			case 'handle-bl':
				container.style.width = `${startWidth - dx}px`;
				container.style.height = `${startHeight + dy}px`;
				container.style.left = `${container.offsetLeft + dx}px`;
				break;

			case 'handle-br':
				container.style.width = `${startWidth + dx}px`;
				container.style.height = `${startHeight + dy}px`;
				break;
		}
	};

	const onPointerUp = (e) => {
		resizing = false;
		document.removeEventListener('pointermove', onPointerMove);
		document.removeEventListener('pointerup', onPointerUp);
	};

	handle.style.touchAction = 'none'; // prevent scrolling on touch devices
	handle.addEventListener('pointerdown', onPointerDown);
}
