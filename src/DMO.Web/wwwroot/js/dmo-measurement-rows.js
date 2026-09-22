/*
    P2-T03 (A6) shared repeated-row adapter (contract §4.5).

    Thin DOM adapter over the normative C# MeasurementRowsInteraction model: it reads the rendered
    data-dmo-* hooks, allocates a frontend-only key for a requested new row, applies the frozen
    focus targeting and dispatches the frozen generic presentation events. The consumer owns the
    row contents, the row values and the decision to re-render.

    It contains no field list, no formula, no measurement meaning, no unit and no validation rule:
    it never computes a verdict, never converts a supplied value and never rejects one. It performs
    no request, reads or writes no document address, opens no window and posts no form. It installs
    no width listener, so viewport width never changes behaviour, and it installs no key handler at
    all, so it cannot swallow Tab or trap focus. Loading it twice installs no second listener.

    Focus is deterministic: after an add the first editable control of the new row is targeted, and
    after a removal the first editable control of the nearest surviving row is targeted, with the
    add control as the documented fallback. When the consumer has not re-rendered yet the intent
    stays pending and `focusPending` re-applies it.
*/
(function () {
    'use strict';

    if (window.dmoMeasurementRows) {
        return;
    }

    var EVENT_NAME = 'dmo:measurement-rows';
    var KEY_PREFIX = 'dmo-row-';
    var counter = 0;
    var pendingFocus = null;

    function closest(node, selector) {
        if (!node || typeof node.closest !== 'function') {
            return null;
        }

        return node.closest(selector);
    }

    function rowsRoot(node) {
        return closest(node, '[data-dmo-measurement-rows]');
    }

    function emit(root, detail) {
        if (!root) {
            return;
        }

        root.dispatchEvent(new CustomEvent(EVENT_NAME, { bubbles: true, detail: detail }));
    }

    function rowElements(root) {
        return root ? Array.prototype.slice.call(root.querySelectorAll('[data-dmo-row]')) : [];
    }

    function rowKeys(root) {
        return rowElements(root).map(function (element) {
            return element.getAttribute('data-dmo-row-key');
        });
    }

    function findRow(root, rowKey) {
        var rows = rowElements(root);

        for (var index = 0; index < rows.length; index++) {
            if (rows[index].getAttribute('data-dmo-row-key') === rowKey) {
                return rows[index];
            }
        }

        return null;
    }

    function allocateKey(root) {
        var keys = rowKeys(root);
        var candidate;

        do {
            counter++;
            candidate = KEY_PREFIX + counter;
        } while (keys.indexOf(candidate) !== -1);

        return candidate;
    }

    function firstEditableControl(rowElement) {
        if (!rowElement) {
            return null;
        }

        var controls = Array.prototype.slice.call(rowElement.querySelectorAll('[data-dmo-field]'));

        for (var index = 0; index < controls.length; index++) {
            var control = controls[index];

            if (control.disabled !== true && control.readOnly !== true) {
                return control;
            }
        }

        return null;
    }

    function focusIfPossible(element) {
        if (!element || typeof element.focus !== 'function' || element.disabled === true) {
            return false;
        }

        element.focus();

        return true;
    }

    function focusPending() {
        if (!pendingFocus) {
            return false;
        }

        var roots = Array.prototype.slice.call(
            document.querySelectorAll('[data-dmo-measurement-rows]'));

        for (var index = 0; index < roots.length; index++) {
            var root = roots[index];

            if (pendingFocus.rowKey === null) {
                if (focusIfPossible(root.querySelector('[data-dmo-add]'))) {
                    pendingFocus = null;

                    return true;
                }

                continue;
            }

            var row = findRow(root, pendingFocus.rowKey);

            if (!row) {
                continue;
            }

            if (focusIfPossible(firstEditableControl(row))) {
                pendingFocus = null;

                return true;
            }

            if (focusIfPossible(root.querySelector('[data-dmo-add]'))) {
                pendingFocus = null;

                return true;
            }
        }

        return false;
    }

    document.addEventListener('click', function (event) {
        var addControl = closest(event.target, '[data-dmo-add]');

        if (addControl) {
            var addRoot = rowsRoot(addControl);

            if (!addRoot || addControl.disabled === true) {
                return;
            }

            var allocatedKey = allocateKey(addRoot);
            var appendIndex = rowElements(addRoot).length;

            pendingFocus = { rowKey: allocatedKey };
            emit(addRoot, {
                kind: 'add-requested',
                rowKey: allocatedKey,
                index: appendIndex
            });
            focusPending();

            return;
        }

        var removeControl = closest(event.target, '[data-dmo-remove]');

        if (removeControl) {
            var removeRoot = rowsRoot(removeControl);

            if (!removeRoot || removeControl.disabled === true) {
                return;
            }

            var removedKey = removeControl.getAttribute('data-dmo-remove');
            var keys = rowKeys(removeRoot);
            var removedIndex = keys.indexOf(removedKey);
            var surviving = keys.slice();

            if (removedIndex >= 0) {
                surviving.splice(removedIndex, 1);
            }

            var nearestKey = surviving.length === 0
                ? null
                : survivedNearest(surviving, removedIndex);

            pendingFocus = { rowKey: nearestKey };
            emit(removeRoot, { kind: 'remove-requested', rowKey: removedKey });
            focusPending();
        }
    }, false);

    /* The row that now occupies the removed index, else the preceding row. */
    function survivedNearest(survivingKeys, removedIndex) {
        if (removedIndex >= 0 && removedIndex < survivingKeys.length) {
            return survivingKeys[removedIndex];
        }

        return survivingKeys[survivingKeys.length - 1];
    }

    document.addEventListener('change', function (event) {
        var fieldControl = closest(event.target, '[data-dmo-field]');

        if (!fieldControl) {
            return;
        }

        var fieldRoot = rowsRoot(fieldControl);

        if (!fieldRoot) {
            return;
        }

        emit(fieldRoot, {
            kind: 'value-changed',
            rowKey: fieldControl.getAttribute('data-dmo-row-key'),
            fieldKey: fieldControl.getAttribute('data-dmo-field'),
            value: typeof fieldControl.value === 'string' ? fieldControl.value : ''
        });
    }, false);

    window.dmoMeasurementRows = {
        eventName: EVENT_NAME,
        keyPrefix: KEY_PREFIX,
        rootHook: '[data-dmo-measurement-rows]',
        addHook: '[data-dmo-add]',
        removeHook: '[data-dmo-remove]',
        fieldHook: '[data-dmo-field]',
        focusPending: focusPending,
        rowKeys: rowKeys
    };
})();
