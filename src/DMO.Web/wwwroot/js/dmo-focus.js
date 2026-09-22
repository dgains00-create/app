/*
    P2-T02 (A4) shared generic focus helper (contract §8.2, §14.4).

    Generic, domain-neutral and presentation-only. It captures the control that invoked a
    consumer-owned surface and restores focus to it when that surface closes, so the shared
    components only have to supply the `data-dmo-focus-return` hook.

    It contains no fetch, no request object, no navigation, no form submission and no endpoint
    knowledge, it never reads or writes the document address, and it installs NO key handler:
    `Tab` is never swallowed, so no focus trap is possible.

    It is idempotent: loading it twice does not install a second listener.
*/
(function () {
    'use strict';

    if (window.dmoFocus) {
        return;
    }

    var HOOK = '[data-dmo-focus-return]';
    var lastInvoker = null;

    function closest(node, selector) {
        if (!node || typeof node.closest !== 'function') {
            return null;
        }

        return node.closest(selector);
    }

    /* Captures the invoking control so a closing consumer surface can restore it. */
    function capture(element) {
        lastInvoker = element || null;

        return lastInvoker;
    }

    function isFocusable(element) {
        return !!element && element.isConnected === true && typeof element.focus === 'function';
    }

    /*
        Restores focus to the captured invoking control, or to an explicitly supplied token.
        Returns whether focus was restored; when the invoker is gone it returns false instead of
        throwing, so the consumer keeps control of its own recovery.
    */
    function restore(token) {
        var target = token || lastInvoker;

        if (!isFocusable(target)) {
            return false;
        }

        target.focus();

        return true;
    }

    /* Documented fallback: the first hooked control inside the supplied container. */
    function restoreFirst(container) {
        var target = closest(container, HOOK) || (container ? container.querySelector(HOOK) : null);

        return restore(target);
    }

    document.addEventListener('click', function (event) {
        var invoker = closest(event.target, HOOK);

        if (invoker) {
            capture(invoker);
        }
    }, true);

    window.dmoFocus = {
        capture: capture,
        restore: restore,
        restoreFirst: restoreFirst,
        hook: HOOK
    };
})();
