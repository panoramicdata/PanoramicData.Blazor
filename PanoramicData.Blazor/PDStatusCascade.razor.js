// =================================================================
// PDStatusCascade.razor.js — Blazor JS module
// Cascade pop-over engine for PDStatusCascade.
// StatusType is serialised as { name, iconClass, colorClass } so
// the JS never needs a separate icon map — all styling comes from
// the node data itself.
// =================================================================

const MAX_DEPTH = 2;

// Per-trigger state: triggerId → { node, el, abortCtrl, dotNetRef? }
const _triggers = new Map();

// Shared DOM infrastructure (created once on first init)
let _infra = null;

// The trigger ID whose popup stack is currently open
let _activeId = null;

// ── Public API ────────────────────────────────────────────────────

export function init(triggerId, nodeJson, dotNetRef) {
    _ensureInfra();

    const el = document.getElementById(triggerId);
    if (!el) return;

    const node = typeof nodeJson === 'string' ? JSON.parse(nodeJson) : nodeJson;
    const abortCtrl = new AbortController();
    const opts = { signal: abortCtrl.signal };

    _triggers.set(triggerId, { node, el, abortCtrl, dotNetRef: dotNetRef ?? null });

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
        entry.abortCtrl.abort();
        _triggers.delete(triggerId);
    }
    if (_activeId === triggerId) _closeAll();
}

// ── Infrastructure ────────────────────────────────────────────────

function _ensureInfra() {
    if (_infra) return;

    if (!document.getElementById('pdsc-global-styles')) {
        const style = document.createElement('style');
        style.id = 'pdsc-global-styles';
        style.textContent = _globalStyles();
        document.head.appendChild(style);
    }

    const overlay = document.createElement('div');
    overlay.id = 'pdsc-overlay';
    overlay.className = 'pdsc-overlay';
    overlay.addEventListener('click', () => _closeAll());
    overlay.addEventListener('keydown', e => { if (e.key === 'Escape') _closeAll(); });
    document.body.appendChild(overlay);

    const popups = [];
    for (let i = 0; i <= MAX_DEPTH; i++) {
        const popup = document.createElement('div');
        popup.className = 'pdsc-popup';
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
    _infra.overlay.classList.add('pdsc-overlay--active');

    const node = await _maybeExpand(entry, entry.node, '', entry.el);
    _renderPopup(0, node, entry.el, entry, '');
}

function _closeAll() {
    if (_activeId) {
        const entry = _triggers.get(_activeId);
        if (entry) entry.el.setAttribute('aria-expanded', 'false');
        _activeId = null;
    }
    if (_infra) {
        _infra.overlay.classList.remove('pdsc-overlay--active');
        _infra.popups.forEach(p => {
            p.classList.remove('pdsc-popup--open');
            p.innerHTML = '';
        });
    }
}

function _closeFrom(depth) {
    if (!_infra) return;
    for (let i = depth; i <= MAX_DEPTH; i++) {
        _infra.popups[i].classList.remove('pdsc-popup--open');
        _infra.popups[i].innerHTML = '';
    }
}

// ── Lazy expansion helper ─────────────────────────────────────────

async function _maybeExpand(entry, node, nodePath, anchorEl) {
    if (!entry.dotNetRef) return node;

    const depth = nodePath === '' ? 0 : nodePath.split('.').length;
    const popup = _infra.popups[Math.min(depth, MAX_DEPTH)];

    popup.innerHTML = `<div class="pdsc-spinner"><i class="fas fa-spinner fa-spin"></i></div>`;
    _positionPopup(popup, anchorEl, depth);
    popup.classList.add('pdsc-popup--open');

    try {
        const json = await entry.dotNetRef.invokeMethodAsync('ExpandNodeAsync', nodePath);
        if (json) {
            return JSON.parse(json);
        }
    } catch {
        // Fall through to render with existing node
    }

    return node;
}

// ── Popup Rendering ───────────────────────────────────────────────

function _renderPopup(depth, node, anchorEl, entry, nodePath) {
    if (depth > MAX_DEPTH) return;

    const popup = _infra.popups[depth];
    const status = node.status || {};
    const iconCls = status.iconClass || 'fas fa-question-circle';
    const colorCls = status.colorClass || 'text-secondary';

    // ── Header ──
    let html = `<div class="pdsc-popup-head">
        <div class="pdsc-popup-title-row">
            <i class="${_esc(iconCls)} ${_esc(colorCls)}" aria-hidden="true"></i>
            <span class="pdsc-popup-title">${_esc(node.title)}</span>
        </div>`;

    if (node.summary) {
        html += `<p class="pdsc-popup-summary">${_esc(node.summary)}</p>`;
    }
    if (node.detail) {
        html += `<p class="pdsc-popup-detail">${_esc(node.detail)}</p>`;
    }
    html += `</div>`;

    // ── Children ──
    if (node.children && node.children.length > 0) {
        html += `<div class="pdsc-popup-body">`;
        node.children.forEach((child, idx) => {
            const hasChildren = child.children && child.children.length > 0;
            const cStatus = child.status || {};
            const cIcon = cStatus.iconClass || 'fas fa-question-circle';
            const cColor = cStatus.colorClass || 'text-secondary';
            const drillable = depth < MAX_DEPTH && (
                child.expandable === true ||
                (child.expandable !== false && hasChildren)
            );

            html += `<div class="pdsc-item${drillable ? ' pdsc-item--drillable' : ''}" data-idx="${idx}" role="${drillable ? 'button' : 'listitem'}"${drillable ? ' tabindex="0"' : ''}>
                <i class="${_esc(cIcon)} ${_esc(cColor)}" aria-hidden="true"></i>
                <div class="pdsc-item-body">
                    <span class="pdsc-item-label">${_esc(child.title)}</span>
                    ${child.summary ? `<span class="pdsc-item-summary">${_esc(child.summary)}</span>` : ''}
                </div>`;

            if (child.detail) {
                html += `<i class="fa-solid fa-circle-info pdsc-item-aside" title="${_esc(child.detail)}" aria-label="Has detail"></i>`;
            }
            if (drillable) {
                html += `<i class="fas fa-chevron-right pdsc-item-chevron" aria-hidden="true"></i>`;
            }

            html += `</div>`;
        });
        html += `</div>`;
    }

    popup.innerHTML = html;

    // ── Drill-down handlers ──
    if (node.children) {
        popup.querySelectorAll('.pdsc-item--drillable').forEach(item => {
            const idx = parseInt(item.dataset.idx, 10);
            const childPath = nodePath === '' ? `${idx}` : `${nodePath}.${idx}`;

            const drill = async e => {
                e.stopPropagation();
                _closeFrom(depth + 1);
                popup.querySelectorAll('.pdsc-item').forEach(i => i.classList.remove('pdsc-item--active'));
                item.classList.add('pdsc-item--active');
                const childNode = await _maybeExpand(entry, node.children[idx], childPath, item);
                node.children[idx] = childNode;
                _updateItemIcon(item, childNode.status);
                _renderPopup(depth + 1, childNode, item, entry, childPath);
            };

            item.addEventListener('click', drill);
            item.addEventListener('keydown', e => {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); drill(e); }
            });
        });
    }

    _positionPopup(popup, anchorEl, depth);
    popup.classList.add('pdsc-popup--open');
}

// ── Helpers ───────────────────────────────────────────────────────

function _updateItemIcon(itemEl, status) {
    const icon = itemEl.querySelector('i:first-child');
    if (!icon) return;
    const s = status || {};
    icon.className = `${s.iconClass || 'fas fa-question-circle'} ${s.colorClass || 'text-secondary'}`;
}

function _positionPopup(popup, anchor, depth) {
    popup.style.visibility = 'hidden';
    popup.style.display = 'block';

    const rect = anchor.getBoundingClientRect();
    const pw = popup.offsetWidth || 280;
    const ph = popup.offsetHeight || 200;
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    let left, top;

    if (depth === 0) {
        left = rect.left;
        top = rect.bottom + 4;
        if (left + pw > vw - 8) left = Math.max(8, rect.right - pw);
    } else {
        left = rect.right + 4;
        top = rect.top;
        if (left + pw > vw - 8) left = Math.max(8, rect.left - pw - 4);
    }

    if (top + ph > vh - 8) top = Math.max(8, vh - ph - 8);

    popup.style.left = `${left}px`;
    popup.style.top = `${top}px`;
    popup.style.visibility = '';
    popup.style.display = '';
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
/* PDStatusCascade — popup styles */
.pdsc-overlay {
    display: none;
    position: fixed;
    inset: 0;
    z-index: 1040;
}
.pdsc-overlay--active {
    display: block;
}
.pdsc-popup {
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
.pdsc-popup--open {
    display: block;
}
.pdsc-popup-head {
    padding: .6rem .75rem .5rem;
    border-bottom: 1px solid var(--bs-border-color, rgba(0,0,0,.08));
    background: var(--bs-tertiary-bg, rgba(0,0,0,.025));
    user-select: none;
}
.pdsc-popup-title-row {
    display: flex;
    align-items: center;
    gap: .45rem;
    font-weight: 600;
}
.pdsc-popup-title-row i {
    line-height: 1;
    font-size: .85rem;
    flex-shrink: 0;
}
.pdsc-popup-title {
    font-size: .875rem;
}
.pdsc-popup-summary {
    margin: .35rem 0 0;
    font-size: .8rem;
    color: var(--bs-secondary-color, #6c757d);
    line-height: 1.4;
}
.pdsc-popup-detail {
    margin: .25rem 0 0;
    font-size: .75rem;
    color: var(--bs-secondary-color, #868e96);
    font-family: monospace;
    word-break: break-all;
    line-height: 1.4;
}
.pdsc-item {
    display: flex;
    align-items: flex-start;
    gap: .5rem;
    padding: .45rem .75rem;
    border-bottom: 1px solid var(--bs-border-color, rgba(0,0,0,.06));
    cursor: default;
    user-select: none;
}
.pdsc-item:last-child {
    border-bottom: none;
}
.pdsc-item > i:first-child {
    flex-shrink: 0;
    margin-top: .15rem;
    font-size: .8rem;
}
.pdsc-item-body {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: .1rem;
}
.pdsc-item-label {
    font-size: .8rem;
    font-weight: 500;
    line-height: 1.3;
    word-break: break-word;
}
.pdsc-item-summary {
    font-size: .75rem;
    color: var(--bs-secondary-color, #6c757d);
    line-height: 1.3;
}
.pdsc-item--drillable {
    cursor: pointer;
}
.pdsc-item--drillable:hover,
.pdsc-item--active {
    background: var(--bs-tertiary-bg, rgba(0,0,0,.04));
}
.pdsc-item-chevron {
    flex-shrink: 0;
    font-size: .65rem;
    color: var(--bs-secondary-color, #adb5bd);
    align-self: center;
}
.pdsc-item-aside {
    flex-shrink: 0;
    font-size: .75rem;
    color: var(--bs-secondary-color, #adb5bd);
    align-self: center;
    cursor: help;
}
.pdsc-spinner {
    padding: 1rem;
    text-align: center;
    color: var(--bs-secondary-color, #6c757d);
}
/* Amber — Bootstrap text-warning is too pale for icons.
   Override --pdsc-color-amber on any ancestor element if needed. */
.pdsc-icon-amber { color: var(--pdsc-color-amber, #fd7e14); }
`;
}
