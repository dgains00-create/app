/*
    P2-T03 (A5) shared picker adapter (contract §4.5).

    Thin DOM adapter over the normative C# ToolPickerInteraction model: it reads the rendered
    data-dmo-* hooks, applies the frozen transitions and dispatches the frozen generic presentation
    events. The consumer owns everything else — it runs the search, decides whether the create
    affordance exists, performs creation, keeps its own origin state and closes the surface itself.

    It contains no search algorithm, no matching, no ranking, no compatibility rule and no domain
    vocabulary. It performs no request, reads or writes no document address, opens no window and
    posts no form. It installs no width listener, so viewport width never changes behaviour, and it
    never swallows Tab, so it cannot trap focus. Loading it twice installs no second listener.

    Frozen rules honoured here: the search activation and Enter raise only the search request and
    never select; only a candidate's explicit select control selects; Escape requests cancel and
    discards nothing; cancel and return restore the invoking control's focus through the accepted
    shared focus helper.
*/
(function () {
    'use strict';

    if (window.dmoToolPicker) {
        return;
    }

    var EVENT_NAME = 'dmo:tool-picker';
    var DEFAULT_CANCEL_KEY = 'cancel';

    function closest(node, selector) {
        if (!node || typeof node.closest !== 'function') {
            return null;
        }

        return node.closest(selector);
    }

    function pickerRoot(node) {
        return closest(node, '[data-dmo-picker]');
    }

    function tokenOf(element) {
        return element ? element.getAttribute('data-dmo-origin-token') : null;
    }

    function emit(root, detail) {
        if (!root) {
            return;
        }

        root.dispatchEvent(new CustomEvent(EVENT_NAME, { bubbles: true, detail: detail }));
    }

    function queryValue(root) {
        var input = root ? root.querySelector('[data-dmo-search-input]') : null;

        return input && typeof input.value === 'string' ? input.value : '';
    }

    function restoreInvokingControl() {
        if (window.dmoFocus && typeof window.dmoFocus.restore === 'function') {
            window.dmoFocus.restore();
        }
    }

    document.addEventListener('click', function (event) {
        var selectControl = closest(event.target, '[data-dmo-select-candidate]');

        if (selectControl) {
            var selectRoot = pickerRoot(selectControl);
            emit(selectRoot, {
                kind: 'candidate-selected',
                originToken: tokenOf(selectRoot),
                candidateKey: selectControl.getAttribute('data-dmo-select-candidate')
            });

            return;
        }

        var createControl = closest(event.target, '[data-dmo-create]');

        if (createControl) {
            var createRoot = pickerRoot(createControl);
            emit(createRoot, {
                kind: 'create-requested',
                originToken: tokenOf(createRoot),
                actionKey: createControl.getAttribute('data-dmo-create')
            });

            return;
        }

        var cancelControl = closest(event.target, '[data-dmo-cancel]');

        if (cancelControl) {
            var cancelRoot = pickerRoot(cancelControl);
            emit(cancelRoot, {
                kind: 'cancel-requested',
                originToken: tokenOf(cancelRoot),
                actionKey: cancelControl.getAttribute('data-dmo-cancel') || DEFAULT_CANCEL_KEY
            });
            restoreInvokingControl();

            return;
        }

        var searchControl = closest(event.target, '[data-dmo-search-request]');

        if (searchControl) {
            var searchRoot = pickerRoot(searchControl);
            emit(searchRoot, {
                kind: 'search-requested',
                originToken: tokenOf(searchRoot),
                query: queryValue(searchRoot)
            });
        }
    }, false);

    document.addEventListener('keydown', function (event) {
        var root = pickerRoot(event.target);

        if (!root) {
            return;
        }

        var input = event.target;

        if (event.key === 'Enter' && input && typeof input.hasAttribute === 'function' &&
            input.hasAttribute('data-dmo-search-input')) {
            /* Enter requests a search. It never selects a candidate, not even the only one. */
            event.preventDefault();
            emit(root, {
                kind: 'search-requested',
                originToken: tokenOf(root),
                query: queryValue(root)
            });

            return;
        }

        if (event.key === 'Escape' || event.key === 'Esc') {
            var cancelControl = root.querySelector('[data-dmo-cancel]');

            if (cancelControl && cancelControl.disabled) {
                return;
            }

            emit(root, {
                kind: 'cancel-requested',
                originToken: tokenOf(root),
                actionKey: cancelControl
                    ? cancelControl.getAttribute('data-dmo-cancel') || DEFAULT_CANCEL_KEY
                    : DEFAULT_CANCEL_KEY
            });
            restoreInvokingControl();
        }
    }, false);

    window.dmoToolPicker = {
        eventName: EVENT_NAME,
        defaultCancelActionKey: DEFAULT_CANCEL_KEY,
        rootHook: '[data-dmo-picker]',
        searchInputHook: '[data-dmo-search-input]',
        selectHook: '[data-dmo-select-candidate]',
        createHook: '[data-dmo-create]',
        cancelHook: '[data-dmo-cancel]',
        query: queryValue
    };
})();
