// PDToolbarColorPicker JavaScript module
const colorPickers = new Map();

export function initialize(id, dotNetRef) {
    if (colorPickers.has(id)) {
        dispose(id);
    }

    const handler = (e) => {
        const picker = document.getElementById(id);
        if (picker && !picker.contains(e.target)) {
            dotNetRef.invokeMethodAsync('OnOutsideClick');
        }
    };

    // Use mousedown instead of click to catch the event before it bubbles
    document.addEventListener('mousedown', handler, true);
    colorPickers.set(id, { handler, dotNetRef });
}

export function dispose(id) {
    const data = colorPickers.get(id);
    if (data) {
        document.removeEventListener('mousedown', data.handler, true);
        colorPickers.delete(id);
    }
}

export function getElementBounds(element) {
    if (!element) return null;
    const rect = element.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        left: rect.left,
        top: rect.top
    };
}
