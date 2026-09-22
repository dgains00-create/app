/*
    P2-T02 (A4) shared generic DenseDataTable DOM adapter (contract §5.6, §5.7, §14.4).

    The C# `DenseTableInteraction` model is the NORMATIVE statement of the frozen
    selection/open arbitration; this file is a thin DOM adapter over exactly the same rules:

      - single click / Space  -> selects the row, never opens it;
      - double click          -> selects the row and requests open once per gesture;
      - Enter                 -> requests open once per activation burst;
      - explicit open control -> behaves exactly as Enter;
      - two consecutive open activations for the same row collapse into one request;
        a selection input or an interaction with a different row reopens the window;
      - open is consumer-gated by `data-dmo-open-enabled`;
      - selection is consumer-gated by `data-dmo-selection-enabled`.

    It is presentation-only and domain-neutral: it raises generic `dmo:*` events carrying only
    opaque keys, it constructs no consumer address or route, it holds no domain vocabulary, it
    never fetches, never persists and never mutates domain state. It also installs no key
    handler that could swallow `Tab`.

    It is idempotent: loading it twice does not install a second set of listeners.
*/
(function () {
    'use strict';

    if (window.dmoDenseTable) {
        return;
    }

    var REGION = '[data-dmo-dense-table]';
    var ROW = '[data-dmo-row]';
    var ROW_ACTION = '[data-dmo-action][data-dmo-action-scope="row"]';
    var CONTROL = 'button, a, input, select, textarea';
    var regions = new WeakMap();

    function closest(node, selector) {
        if (!node || typeof node.closest !== 'function') {
            return null;
        }

        return node.closest(selector);
    }

    function attribute(node, name) {
        return node && typeof node.getAttribute === 'function' ? node.getAttribute(name) : null;
    }

    function stateOf(region) {
        var state = regions.get(region);

        if (!state) {
            var selected = region.querySelector(ROW + '[aria-selected="true"]');
            state = {
                selectedKey: attribute(selected, 'data-dmo-row-key'),
                openWindowRowKey: null
            };
            regions.set(region, state);
        }

        return state;
    }

    function selectionEnabled(region) {
        return attribute(region, 'data-dmo-selection-enabled') === 'true';
    }

    function openEnabled(region) {
        return attribute(region, 'data-dmo-open-enabled') === 'true';
    }

    function rowKeyOf(row) {
        return attribute(row, 'data-dmo-row-key');
    }

    function emit(region, name, detail) {
        region.dispatchEvent(new CustomEvent('dmo:' + name, { bubbles: true, detail: detail }));
    }

    function isInactive(control) {
        return !!control &&
            (control.hasAttribute('disabled') || attribute(control, 'aria-disabled') === 'true');
    }

    /* Keeps the visible, non-colour-only selection indicator in step with aria-selected. */
    function syncMarker(row, isSelected) {
        var cell = row.querySelector('td');

        if (!cell) {
            return;
        }

        var existing = cell.querySelector('[data-dmo-row-marker]');

        if (!isSelected) {
            if (existing && existing.parentNode) {
                existing.parentNode.removeChild(existing);
            }

            return;
        }

        if (existing) {
            return;
        }

        var slots = cell.querySelector('.dmo-table__slots');

        if (!slots) {
            slots = document.createElement('span');
            slots.className = 'dmo-table__slots';
            cell.insertBefore(slots, cell.firstChild);
        }

        var marker = document.createElement('span');
        marker.className = 'dmo-table__marker';
        marker.setAttribute('data-dmo-row-marker', 'true');
        marker.setAttribute('aria-hidden', 'true');
        marker.textContent = '\u25B8';
        slots.insertBefore(marker, slots.firstChild);
    }

    function applySelection(region, key) {
        var rows = region.querySelectorAll(ROW);

        Array.prototype.forEach.call(rows, function (row) {
            var isSelected = key !== null && rowKeyOf(row) === key;

            if (row.hasAttribute('aria-selected')) {
                row.setAttribute('aria-selected', isSelected ? 'true' : 'false');
            }

            if (isSelected) {
                row.setAttribute('data-dmo-row-selected', 'true');
            } else {
                row.removeAttribute('data-dmo-row-selected');
            }

            syncMarker(row, isSelected);
        });
    }

    /* Mirrors DenseTableInteraction.Click / .Space: selects, never opens. */
    function select(region, key) {
        if (!selectionEnabled(region)) {
            return false;
        }

        stateOf(region).selectedKey = key;
        applySelection(region, key);
        emit(region, 'row-selected', { rowKey: key });

        return true;
    }

    /* Mirrors DenseTableInteraction.RequestOpen: consumer-gated, duplicate-collapsed. */
    function requestOpen(region, key) {
        var state = stateOf(region);

        if (!openEnabled(region) || state.openWindowRowKey === key) {
            return false;
        }

        state.openWindowRowKey = key;
        emit(region, 'open-requested', { rowKey: key });

        return true;
    }

    function click(region, key) {
        stateOf(region).openWindowRowKey = null;

        return select(region, key);
    }

    function space(region, key) {
        stateOf(region).openWindowRowKey = null;

        return select(region, key);
    }

    /* Mirrors DenseTableInteraction.DoubleClick: select (idempotent) then open once. */
    function doubleClick(region, key) {
        stateOf(region).openWindowRowKey = null;
        select(region, key);

        return requestOpen(region, key);
    }

    /* Mirrors DenseTableInteraction.Enter / .ActivateOpen: selection unchanged. */
    function enter(region, key) {
        return requestOpen(region, key);
    }

    function onOpenControl(event) {
        var control = closest(event.target, '[data-dmo-open]');

        if (!control || isInactive(control)) {
            return false;
        }

        var region = closest(control, REGION);

        if (!region) {
            return false;
        }

        enter(region, attribute(control, 'data-dmo-open'));

        return true;
    }

    function onRowAction(event) {
        var control = closest(event.target, ROW_ACTION);

        if (!control || isInactive(control)) {
            return false;
        }

        var region = closest(control, REGION);

        if (!region) {
            return false;
        }

        emit(region, 'action-invoked', {
            rowKey: attribute(control, 'data-dmo-row-key'),
            actionKey: attribute(control, 'data-dmo-action')
        });

        return true;
    }

    function onPagingControl(event) {
        var control = closest(event.target, '[data-dmo-page]');

        if (!control || isInactive(control)) {
            return false;
        }

        var region = closest(control, REGION);

        if (!region) {
            return false;
        }

        emit(region, 'page-changed', { pageCarrier: attribute(control, 'data-dmo-page') });

        return true;
    }

    /* Presentation-only affordance: the current programmatic direction is read from the
       server-rendered supplied state, and the consumer still owns the actual sort. */
    function onSortControl(event) {
        var control = closest(event.target, '[data-dmo-sort]');

        if (!control || isInactive(control)) {
            return false;
        }

        var region = closest(control, REGION);

        if (!region) {
            return false;
        }

        var direction = attribute(closest(control, 'th'), 'aria-sort') === 'ascending'
            ? 'descending'
            : 'ascending';

        emit(region, 'sort-requested', {
            sortColumnKey: attribute(control, 'data-dmo-sort'),
            sortDirection: direction
        });

        return true;
    }

    function onDetailControl(event) {
        var control = closest(event.target, '[data-dmo-detail]');

        if (!control || isInactive(control)) {
            return false;
        }

        var region = closest(control, '[data-dmo-audit]') || closest(control, REGION);

        if (!region) {
            return false;
        }

        emit(region, 'detail-requested', {
            entryKey: attribute(control, 'data-dmo-detail'),
            actionKey: attribute(control, 'data-dmo-action')
        });

        return true;
    }

    function onClick(event) {
        if (isInactive(closest(event.target, '[data-dmo-action], [data-dmo-page], [data-dmo-open], [data-dmo-sort], [data-dmo-detail]'))) {
            return;
        }

        if (onOpenControl(event) ||
            onPagingControl(event) ||
            onSortControl(event) ||
            onRowAction(event) ||
            onDetailControl(event)) {
            return;
        }

        /* A control inside the row keeps its own behaviour; the row is not silently selected. */
        if (closest(event.target, CONTROL)) {
            return;
        }

        var row = closest(event.target, ROW);
        var region = row ? closest(row, REGION) : null;

        if (row && region) {
            click(region, rowKeyOf(row));
        }
    }

    function onDoubleClick(event) {
        if (closest(event.target, CONTROL)) {
            return;
        }

        var row = closest(event.target, ROW);
        var region = row ? closest(row, REGION) : null;

        if (row && region) {
            doubleClick(region, rowKeyOf(row));
        }
    }

    function onKeyDown(event) {
        if (closest(event.target, CONTROL)) {
            return;
        }

        var row = closest(event.target, ROW);
        var region = row ? closest(row, REGION) : null;

        if (!row || !region) {
            return;
        }

        var key = rowKeyOf(row);

        if (event.key === ' ' || event.key === 'Spacebar') {
            event.preventDefault();
            space(region, key);

            return;
        }

        if (event.key === 'Enter') {
            event.preventDefault();
            enter(region, key);
        }
    }

    function onChange(event) {
        var control = closest(event.target, '[data-dmo-filter]');

        if (!control) {
            return;
        }

        var region = closest(control, REGION);

        if (!region) {
            return;
        }

        emit(region, 'filter-changed', {
            filterKey: attribute(control, 'data-dmo-filter'),
            filterValue: control.value === undefined ? null : String(control.value)
        });
    }

    document.addEventListener('click', onClick, false);
    document.addEventListener('dblclick', onDoubleClick, false);
    document.addEventListener('keydown', onKeyDown, false);
    document.addEventListener('change', onChange, false);

    window.dmoDenseTable = {
        click: click,
        space: space,
        doubleClick: doubleClick,
        enter: enter,
        requestOpen: requestOpen,
        stateOf: stateOf,
        regionSelector: REGION,
        rowSelector: ROW
    };
})();
