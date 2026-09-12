// =================================================================
// PDStatusRollUp.razor.js — Blazor JS module
// Cascade pop-over engine.  Three exported functions:
//   init(triggerId, nodeJson, iconMap, dotNetRef?)
//   dispose(triggerId)
// =================================================================

const MAX_DEPTH = 2;

// Per-trigger state: triggerId → { node, iconMap, el, abortCtrl, dotNetRef? }
const _triggers = new Map();

// Shared DOM infrastructure (created once on first init)
let _infra = null;  // { overlay, popups[] }

// The trigger ID whose popup stack is currently open
let _activeId = null;

// ── Public API ────────────────────────────────────────────────────

export function init(triggerId, nodeJson, iconMap, dotNetRef) {
	_ensureInfra();

	const el = document.getElementById(triggerId);
	if (!el) return;

	const node = typeof nodeJson === 'string' ? JSON.parse(nodeJson) : nodeJson;
	const abortCtrl = new AbortController();
	const opts = { signal: abortCtrl.signal };

	_triggers.set(triggerId, { node, iconMap, el, abortCtrl, dotNetRef: dotNetRef ?? null });

	el.addEventListener('click', async e => {
		e.stopPropagation();
		if (_activeId === triggerId) {
			_closeAll();
		} else {
			_closeAll();
			await _open(triggerId);
		}
	}, opts);

	el.addEventListener('keydown', e => {
		if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); el.click(); }
		if (e.key === 'Escape') _closeAll();
	}, opts);
}

export function dispose(triggerId) {
	const entry = _triggers.get(triggerId);
	if (entry) {
		entry.abortCtrl.abort(); // removes all listeners attached with { signal }
		_triggers.delete(triggerId);
	}
	if (_activeId === triggerId) _closeAll();
}

// ── Infrastructure ────────────────────────────────────────────────

function _ensureInfra() {
	if (_infra) return;

	// Inject popup styles once
	if (!document.getElementById('pdsr-global-styles')) {
		const style = document.createElement('style');
		style.id = 'pdsr-global-styles';
		style.textContent = _globalStyles();
		document.head.appendChild(style);
	}

	// Transparent overlay — click dismisses the popup stack
	const overlay = document.createElement('div');
	overlay.id = 'pdsr-overlay';
	overlay.className = 'pdsr-overlay';
	overlay.addEventListener('click', () => _closeAll());
	overlay.addEventListener('keydown', e => { if (e.key === 'Escape') _closeAll(); });
	document.body.appendChild(overlay);

	// Three popup layers (depth 0, 1, 2)
	const popups = [];
	for (let i = 0; i <= MAX_DEPTH; i++) {
		const popup = document.createElement('div');
		popup.className = 'pdsr-popup';
		popup.dataset.depth = i;
		popup.setAttribute('role', i === 0 ? 'dialog' : 'region');
		popup.addEventListener('click', e => e.stopPropagation());
		document.body.appendChild(popup);
		popups.push(popup);
	}

	_infra = { overlay, popups };
}

// ── Open / Close ──────────────────────────────────────────────────

async function _open(triggerId) {
	const entry = _triggers.get(triggerId);
	if (!entry) return;

	_activeId = triggerId;
	entry.el.setAttribute('aria-expanded', 'true');
	_infra.overlay.classList.add('pdsr-overlay--active');

	const node = await _maybeExpand(entry, entry.node, '', entry.el);
	_renderPopup(0, node, entry.el, entry.iconMap, entry, '');
}

function _closeAll() {
	if (_activeId) {
		const entry = _triggers.get(_activeId);
		if (entry) entry.el.setAttribute('aria-expanded', 'false');
		_activeId = null;
	}
	if (_infra) {
		_infra.overlay.classList.remove('pdsr-overlay--active');
		_infra.popups.forEach(p => {
			p.classList.remove('pdsr-popup--open');
			p.innerHTML = '';
		});
	}
}

function _closeFrom(depth) {
	if (!_infra) return;
	for (let i = depth; i <= MAX_DEPTH; i++) {
		_infra.popups[i].classList.remove('pdsr-popup--open');
		_infra.popups[i].innerHTML = '';
	}
}

// ── Lazy expansion helper ─────────────────────────────────────────

// Shows a spinner in the popup at `depth` while the .NET callback runs.
// Returns the (possibly updated) node.
async function _maybeExpand(entry, node, nodePath, anchorEl) {
	if (!entry.dotNetRef) return node;

	const depth = nodePath === '' ? 0 : nodePath.split('.').length;
	const popup = _infra.popups[Math.min(depth, MAX_DEPTH)];

	// Show spinner while awaiting, positioned next to the element that was clicked
	popup.innerHTML = `<div class="pdsr-spinner"><i class="fas fa-spinner fa-spin"></i></div>`;
	_positionPopup(popup, anchorEl, depth);
	popup.classList.add('pdsr-popup--open');

	try {
		const json = await entry.dotNetRef.invokeMethodAsync('ExpandNodeAsync', nodePath);
		if (json) {
			return JSON.parse(json);
		}
	} catch {
		// Ignore — fall through to render with existing node
	}

	return node;
}

// ── Popup Rendering ───────────────────────────────────────────────

function _renderPopup(depth, node, anchorEl, iconMap, entry, nodePath) {
	if (depth > MAX_DEPTH) return;

	const popup = _infra.popups[depth];

	// ── Header ──
	const iconCls = _iconClass(node.status, iconMap);
	const colorCls = _colorClass(node.status);

	let html = `<div class="pdsr-popup-head">
		<div class="pdsr-popup-title-row">
			<i class="${iconCls} ${colorCls}" aria-hidden="true"></i>
			<span class="pdsr-popup-title">${_esc(node.title)}</span>
		</div>`;

	if (node.summary) {
		html += `<p class="pdsr-popup-summary">${_esc(node.summary)}</p>`;
	}
	if (node.detail) {
		html += `<p class="pdsr-popup-detail">${_esc(node.detail)}</p>`;
	}

	html += `</div>`;

	// ── Children ──
	if (node.children && node.children.length > 0) {
		html += `<div class="pdsr-popup-body">`;
		node.children.forEach((child, idx) => {
			const hasChildren = child.children && child.children.length > 0;
			const cIconCls = _iconClass(child.status, iconMap);
			const cColorCls = _colorClass(child.status);
			// Expandable tri-state: true=force on, false=force off, null/undefined=auto (has children)
			const drillable = depth < MAX_DEPTH && (
				child.expandable === true ||
				(child.expandable !== false && hasChildren)
			);

			html += `<div class="pdsr-item${drillable ? ' pdsr-item--drillable' : ''}" data-idx="${idx}" role="${drillable ? 'button' : 'listitem'}"${drillable ? ' tabindex="0"' : ''}>
				<i class="${cIconCls} ${cColorCls}" aria-hidden="true"></i>
				<div class="pdsr-item-body">
					<span class="pdsr-item-label">${_esc(child.title)}</span>
					${child.summary ? `<span class="pdsr-item-summary">${_esc(child.summary)}</span>` : ''}
				</div>`;

			if (child.detail) {
				html += `<i class="fa-solid fa-circle-info pdsr-item-aside" title="${_esc(child.detail)}" aria-label="Has detail"></i>`;
			}
			if (drillable) {
				html += `<i class="fas fa-chevron-right pdsr-item-chevron" aria-hidden="true"></i>`;
			}

			html += `</div>`;
		});
		html += `</div>`;
	}

	popup.innerHTML = html;

	// ── Drill-down handlers ──
	if (node.children) {
		popup.querySelectorAll('.pdsr-item--drillable').forEach(item => {
			const idx = parseInt(item.dataset.idx, 10);
			const childPath = nodePath === '' ? `${idx}` : `${nodePath}.${idx}`;

			const drill = async e => {
					e.stopPropagation();
					_closeFrom(depth + 1);
					popup.querySelectorAll('.pdsr-item').forEach(i => i.classList.remove('pdsr-item--active'));
					item.classList.add('pdsr-item--active');
					const childNode = await _maybeExpand(entry, node.children[idx], childPath, item);
					// Update the in-memory child so subsequent opens reflect the fetched status
					node.children[idx] = childNode;
					// Patch the row icon/colour in the parent popup to reflect the resolved status
					_updateItemIcon(item, childNode.status, iconMap);
					_renderPopup(depth + 1, childNode, item, iconMap, entry, childPath);
			};

			item.addEventListener('click', drill);
			item.addEventListener('keydown', e => {
				if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); drill(e); }
			});
		});
	}

	// ── Position and show ──
	_positionPopup(popup, anchorEl, depth);
	popup.classList.add('pdsr-popup--open');
}

function _positionPopupNear(popup, anchor) {
	_positionPopup(popup, anchor, 0);
}

function _positionPopup(popup, anchor, depth) {
	// Measure before making visible
	popup.style.visibility = 'hidden';
	popup.style.display = 'block';

	const rect = anchor.getBoundingClientRect();
	const pw = popup.offsetWidth || 280;
	const ph = popup.offsetHeight || 200;
	const vw = window.innerWidth;
	const vh = window.innerHeight;

	let left, top;

	if (depth === 0) {
		// Below the trigger, aligned to its left edge
		left = rect.left;
		top = rect.bottom + 4;
		// Flip right-to-left if it would overflow the right edge
		if (left + pw > vw - 8) left = Math.max(8, rect.right - pw);
	} else {
		// To the right of the parent item
		left = rect.right + 4;
		top = rect.top;
		// Flip to the left if it would overflow the right edge
		if (left + pw > vw - 8) left = Math.max(8, rect.left - pw - 4);
	}

	// Clamp within the vertical viewport
	if (top + ph > vh - 8) top = Math.max(8, vh - ph - 8);

	popup.style.left = `${left}px`;
	popup.style.top = `${top}px`;
	popup.style.visibility = '';
	popup.style.display = '';
}

// ── Helpers ───────────────────────────────────────────────────────

// Patches the icon element inside a .pdsr-item row to reflect a newly resolved status.
// Called after _maybeExpand returns so the parent popup row updates without a full re-render.
function _updateItemIcon(itemEl, status, iconMap) {
	const icon = itemEl.querySelector('i:first-child');
	if (!icon) return;
	// Strip existing colour and icon classes, then apply the resolved ones
	icon.className = `${_iconClass(status, iconMap)} ${_colorClass(status)}`;
}

function _iconClass(status, iconMap) {
	switch ((status || '').toLowerCase()) {
		case 'red':   return (iconMap && iconMap.red)   || 'fa-solid fa-circle-xmark';
		case 'amber': return (iconMap && iconMap.amber) || 'fa-solid fa-triangle-exclamation';
		case 'green': return (iconMap && iconMap.green) || 'fa-solid fa-circle-check';
		default:      return (iconMap && iconMap.gray)  || 'fa-solid fa-circle-question';
	}
}

function _colorClass(status) {
	switch ((status || '').toLowerCase()) {
		case 'red':   return 'text-danger';
		case 'amber': return 'pdsr-icon-amber';
		case 'green': return 'text-success';
		default:      return 'text-secondary';
	}
}

function _esc(text) {
	if (!text) return '';
	return text
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;')
		.replace(/"/g, '&quot;');
}

// ── Global popup styles (injected once) ───────────────────────────

function _globalStyles() {
	return `
/* PDStatusRollUp — popup styles */
.pdsr-overlay {
	display: none;
	position: fixed;
	inset: 0;
	z-index: 1040;
}
.pdsr-overlay--active {
	display: block;
}
.pdsr-popup {
	display: none;
	position: fixed;
	z-index: 1050;
	width: 280px;
	background: var(--bs-body-bg, #fff);
	color: var(--bs-body-color, #212529);
	border: 1px solid var(--bs-border-color, rgba(0,0,0,.15));
	border-radius: .375rem;
	box-shadow: 0 .5rem 1rem var(--bs-border-color-translucent, rgba(0,0,0,.15));
	font-size: .875rem;
	overflow: hidden;
}
.pdsr-popup--open {
	display: block;
}
.pdsr-popup-head {
	padding: .6rem .75rem .5rem;
	border-bottom: 1px solid var(--bs-border-color, rgba(0,0,0,.08));
	background: var(--bs-tertiary-bg, rgba(0,0,0,.025));
	user-select: none;
}
.pdsr-popup-title-row {
	display: flex;
	align-items: center;
	gap: .45rem;
	font-weight: 600;
}
.pdsr-popup-title-row i {
	line-height: 1;
	font-size: .85rem;
	flex-shrink: 0;
}
.pdsr-popup-title {
	font-size: .875rem;
}
.pdsr-popup-summary {
	margin: .35rem 0 0;
	font-size: .8rem;
	color: var(--bs-secondary-color, #6c757d);
	line-height: 1.4;
}
.pdsr-popup-detail {
	margin: .25rem 0 0;
	font-size: .75rem;
	color: var(--bs-secondary-color, #868e96);
	font-family: monospace;
	word-break: break-all;
	line-height: 1.4;
}
.pdsr-popup-body {}
.pdsr-item {
	display: flex;
	align-items: flex-start;
	gap: .5rem;
	padding: .45rem .75rem;
	border-bottom: 1px solid var(--bs-border-color, rgba(0,0,0,.06));
	cursor: default;
	user-select: none;
}
.pdsr-item:last-child {
	border-bottom: none;
}
.pdsr-item > i:first-child {
	line-height: 1;
	margin-top: .15rem;
	flex-shrink: 0;
	font-size: .8rem;
}
.pdsr-item--drillable {
	cursor: pointer;
	transition: background .12s;
}
.pdsr-item--drillable:hover {
	background: var(--bs-tertiary-bg, rgba(0,0,0,.04));
}
.pdsr-item--active {
	background: var(--bs-secondary-bg, rgba(0,0,0,.06));
}
.pdsr-item-body {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: .1rem;
}
.pdsr-item-label {
	font-weight: 500;
	font-size: .8rem;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.pdsr-item-summary {
	font-size: .75rem;
	color: var(--bs-secondary-color, #6c757d);
	line-height: 1.35;
}
.pdsr-item-aside {
	color: var(--bs-secondary-color, #adb5bd);
	font-size: .7rem;
	margin-top: .2rem;
	flex-shrink: 0;
}
.pdsr-item-chevron {
	color: var(--bs-secondary-color, #adb5bd);
	font-size: .7rem;
	margin-top: .2rem;
	flex-shrink: 0;
}
/* Amber is the only custom status colour — Bootstrap text-warning is too pale for icons.
   Override with --pdsr-color-amber on any ancestor element if needed. */
.pdsr-icon-amber { color: var(--pdsr-color-amber, #fd7e14); }
/* Loading spinner shown during lazy expansion */
.pdsr-spinner {
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 1rem;
	color: var(--bs-secondary-color, #6c757d);
}
`;
}
