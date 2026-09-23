/*
    P2-T04 Job On surface adapter (contract §18.2.6, §9.3.5 and §11).

    This is a NEW P2-T04-owned file: no frozen P2-T01/P2-T02/P2-T03 asset is modified. It is the
    domain orchestration around the accepted shared primitives, not a second primitive:

      - it consumes the frozen `dmo:tool-picker` presentation events and never re-implements the
        picker's own arbitration;
      - it consumes the frozen `dmo:open-requested` presentation event of the dense table and resolves
        the opaque row key through the P2-T04-owned route map (the row key itself is never parsed);
      - it restores focus through the accepted `window.dmoFocus` helper instead of implementing a
        second focus helper;
      - it posts the origin surface's command to the contracted endpoints, so the inline Tool create
        subflow never navigates away and the origin state is never at risk;
      - it performs no domain decision: it binds the surface's own form state to the contracted
        request shapes and lets the server decide.

    It is idempotent: loading it twice installs no second listener.
*/
(function () {
    'use strict';

    if (window.dmoJobOn) {
        return;
    }

    var TOOL_PICKER_EVENT = 'dmo:tool-picker';
    var OPEN_REQUESTED_EVENT = 'dmo:open-requested';
    var TOOL_CREATE_ENDPOINT = '/ferramentas/tools';
    var DEFAULT_CANCEL_HREF = '/jobon';
    var CREATE_SUCCESS = 201;

    function closest(node, selector) {
        if (!node || typeof node.closest !== 'function') {
            return null;
        }

        return node.closest(selector);
    }

    function surfaceOf(node) {
        return closest(node, '[data-dmo-jobon-surface]');
    }

    function slotOf(node) {
        return closest(node, '[data-dmo-jobon-slot]');
    }

    function attribute(node, name) {
        return node && typeof node.getAttribute === 'function' ? node.getAttribute(name) : null;
    }

    function field(root, name) {
        return root ? root.querySelector('[data-dmo-jobon-field="' + name + '"]') : null;
    }

    function fieldValue(root, name) {
        var element = field(root, name);

        return element && typeof element.value === 'string' ? element.value : '';
    }

    function associatedInput(slot) {
        return slot ? slot.querySelector('[data-dmo-jobon-associated-tool]') : null;
    }

    function originalInput(slot) {
        return slot ? slot.querySelector('[data-dmo-jobon-original-tool]') : null;
    }

    function slotToken(slot) {
        return attribute(slot, 'data-dmo-jobon-slot');
    }

    function candidates(slot) {
        return slot ? slot.querySelectorAll('[data-dmo-candidate]') : [];
    }

    function canonicalToolId(slot, candidateKey) {
        if (!slot || !candidateKey) {
            return null;
        }

        var entries = slot.querySelectorAll('[data-dmo-jobon-candidate]');
        for (var index = 0; index < entries.length; index++) {
            if (attribute(entries[index], 'data-dmo-jobon-candidate') === candidateKey) {
                return attribute(entries[index], 'data-dmo-jobon-candidate-tool');
            }
        }

        return null;
    }

    function markSelected(slot, candidateKey) {
        var list = candidates(slot);

        for (var index = 0; index < list.length; index++) {
            var candidate = list[index];
            var selected = attribute(candidate, 'data-dmo-candidate-key') === candidateKey;

            candidate.setAttribute('aria-selected', selected ? 'true' : 'false');
            candidate.classList.toggle('dmo-picker__candidate--selected', selected);

            var marker = candidate.querySelector('[data-dmo-candidate-marker]');
            if (selected && !marker) {
                marker = document.createElement('span');
                marker.className = 'dmo-picker__marker';
                marker.setAttribute('data-dmo-candidate-marker', 'true');
                marker.setAttribute('aria-hidden', 'true');
                marker.textContent = '\u25B8';
                candidate.insertBefore(marker, candidate.firstChild);
            } else if (!selected && marker) {
                marker.parentNode.removeChild(marker);
            }
        }
    }

    function showSubmitError(surface, message) {
        var target = surface ? surface.querySelector('[data-dmo-jobon-submit-error]') : null;

        if (!target) {
            return;
        }

        target.textContent = message;
        target.hidden = false;
    }

    function clearSubmitError(surface) {
        var target = surface ? surface.querySelector('[data-dmo-jobon-submit-error]') : null;

        if (target) {
            target.textContent = '';
            target.hidden = true;
        }
    }

    /* ---- candidate selection: exactly one explicit human action ---------------------------- */

    function onCandidateSelected(pickerRoot, candidateKey) {
        var slot = slotOf(pickerRoot);
        var hidden = associatedInput(slot);
        var toolId = canonicalToolId(slot, candidateKey);

        if (!hidden || !toolId) {
            return;
        }

        hidden.value = toolId;
        markSelected(slot, candidateKey);
    }

    /* ---- search: one round trip to this same route, carrying the origin form state ---------- */

    function onSearchRequested(pickerRoot, query) {
        var form = closest(pickerRoot, '[data-dmo-jobon-origin-form]');
        var slot = slotOf(pickerRoot);
        var token = slotToken(slot);

        if (!form || !token) {
            return;
        }

        var action = attribute(form, 'action') || window.location.pathname;
        var url = new URL(action, window.location.origin);
        var data = new FormData(form);

        data.forEach(function (value, name) {
            if (name) {
                url.searchParams.set(name, value);
            }
        });

        url.searchParams.set('slot', token);
        url.searchParams.set('slotQuery', query || '');

        window.location.assign(url.pathname + url.search);
    }

    /* ---- inline Tool create subflow: no navigation, no draft store -------------------------- */

    function createPanel(surface) {
        return surface ? surface.querySelector('[data-dmo-jobon-tool-create-panel]') : null;
    }

    function onToolCreateRequested(pickerRoot) {
        var surface = surfaceOf(pickerRoot);
        var panel = createPanel(surface);
        var slot = slotOf(pickerRoot);

        if (!panel || !surface) {
            return;
        }

        surface.setAttribute('data-dmo-jobon-active-slot', slotToken(slot) || 'CM');
        panel.hidden = false;

        var typeSelect = panel.querySelector('[data-dmo-jobon-tool-field="type"]');
        if (typeSelect) {
            typeSelect.value = slotToken(slot) || 'CM';
        }

        if (typeSelect && typeof typeSelect.focus === 'function') {
            typeSelect.focus();
        }
    }

    function onToolCreateCancel(pickerRoot) {
        var surface = surfaceOf(pickerRoot);
        var panel = createPanel(surface);

        if (panel && !panel.hidden) {
            panel.hidden = true;

            /* Focus returns through the accepted shared helper: no second focus helper exists. */
            if (window.dmoFocus && typeof window.dmoFocus.restoreFirst === 'function') {
                window.dmoFocus.restoreFirst(pickerRoot);
            }
        }
    }

    function toolCreatePayload(panel) {
        var machines = panel.querySelectorAll('[data-dmo-jobon-tool-machine]');
        var selectedMachines = [];
        var quantityField = panel.querySelector('[data-dmo-jobon-tool-field="quantity"]');
        var quantity = quantityField && quantityField.value !== '' ? parseInt(quantityField.value, 10) : null;

        for (var index = 0; index < machines.length; index++) {
            if (machines[index].checked) {
                selectedMachines.push(machines[index].value);
            }
        }

        return {
            type: fieldValue(panel, 'type') || null,
            reference: fieldValue(panel, 'reference') || null,
            lot: fieldValue(panel, 'lot') || null,
            processo: fieldValue(panel, 'processo') || null,
            quantity: Number.isNaN(quantity) ? null : quantity,
            machines: selectedMachines
        };
    }

    function submitToolCreate(pickerRoot) {
        var surface = surfaceOf(pickerRoot);
        var panel = createPanel(surface);
        var errorTarget = panel ? panel.querySelector('[data-dmo-jobon-tool-create-error]') : null;

        if (!panel || !surface) {
            return;
        }

        if (errorTarget) {
            errorTarget.textContent = '';
            errorTarget.hidden = true;
        }

        fetch(TOOL_CREATE_ENDPOINT, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(toolCreatePayload(panel))
        }).then(function (response) {
            return response.json().then(function (body) {
                return { status: response.status, body: body };
            });
        }).then(function (result) {
            if (result.status === CREATE_SUCCESS && result.body && result.body.toolId) {
                var slot = surface.querySelector(
                    '[data-dmo-jobon-slot="' + attribute(surface, 'data-dmo-jobon-active-slot') + '"]');
                var hidden = associatedInput(slot);

                if (hidden) {
                    hidden.value = result.body.toolId;
                }

                panel.hidden = true;

                if (window.dmoFocus && typeof window.dmoFocus.restoreFirst === 'function') {
                    window.dmoFocus.restoreFirst(pickerRoot);
                }

                return;
            }

            if (errorTarget) {
                errorTarget.textContent = result.body && result.body.message
                    ? result.body.message
                    : 'Não foi possível criar a ferramenta.';
                errorTarget.hidden = false;
            }
        }).catch(function () {
            if (errorTarget) {
                errorTarget.textContent = 'Não foi possível criar a ferramenta.';
                errorTarget.hidden = false;
            }
        });
    }

    /* ---- origin command submission --------------------------------------------------------- */

    function readDate(root, name) {
        var value = fieldValue(root, name);

        return value === '' ? null : value;
    }

    function associationsOf(form) {
        var slots = form.querySelectorAll('[data-dmo-jobon-slot]');
        var changes = [];

        for (var index = 0; index < slots.length; index++) {
            var slot = slots[index];
            var current = associatedInput(slot) ? associatedInput(slot).value : '';
            var original = originalInput(slot) ? originalInput(slot).value : '';

            if (!current && !original) {
                continue;
            }

            if (current && current !== original) {
                changes.push({
                    contextType: slotToken(slot),
                    action: 'Set',
                    toolId: current
                });
            } else if (!current && original) {
                changes.push({
                    contextType: slotToken(slot),
                    action: 'Remove',
                    toolId: null
                });
            }
        }

        return changes;
    }

    function slotValue(form, token) {
        var slot = form.querySelector('[data-dmo-jobon-slot="' + token + '"]');
        var hidden = associatedInput(slot);

        return hidden && hidden.value ? hidden.value : null;
    }

    function sendJson(form, method, endpoint, payload, onSuccess) {
        var surface = surfaceOf(form);

        clearSubmitError(surface);

        fetch(endpoint, {
            method: method,
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        }).then(function (response) {
            return response.text().then(function (text) {
                return { status: response.status, text: text };
            });
        }).then(function (result) {
            var body = null;

            if (result.text) {
                try {
                    body = JSON.parse(result.text);
                } catch (error) {
                    body = null;
                }
            }

            if (result.status >= 200 && result.status < 300) {
                onSuccess(body);
                return;
            }

            showSubmitError(surface, describeFailure(result.status, body));
        }).catch(function () {
            showSubmitError(surface, 'Não foi possível concluir a operação. Tente novamente.');
        });
    }

    function describeFailure(status, body) {
        if (body && body.reason === 'validation-failed' && body.errors) {
            return 'Dados inválidos: ' + body.errors.join(', ') + '.';
        }

        if (body && body.reason === 'duplicate-production') {
            return body.message || 'Já existe um Job On com esta referência e número de produção.';
        }

        if (body && body.reason === 'stale-version') {
            return body.message || 'O registo foi alterado entretanto; recarregue a página.';
        }

        if (body && body.reason === 'dependency-exists') {
            return body.message || 'Existem factos operacionais dependentes; nada foi eliminado.';
        }

        if (body && body.reason === 'date-threshold-confirmation-required') {
            return body.message || 'É necessário confirmar explicitamente esta produção.';
        }

        return 'A operação foi recusada (HTTP ' + status + ').';
    }

    function cancelHref(form) {
        var endpoint = attribute(form, 'data-dmo-jobon-update-endpoint') ||
            attribute(form, 'data-dmo-jobon-duplicate-endpoint') ||
            attribute(form, 'data-dmo-jobon-delete-endpoint');

        if (!endpoint) {
            return DEFAULT_CANCEL_HREF;
        }

        var segments = endpoint.split('/').filter(function (segment) { return segment !== ''; });

        return segments.length >= 2 ? '/' + segments[0] + '/' + segments[1] : DEFAULT_CANCEL_HREF;
    }

    function onDecisionAction(control) {
        var form = closest(control, '[data-dmo-jobon-origin-form]');

        if (!form) {
            return;
        }

        var action = attribute(control, 'data-dmo-action');
        var surface = surfaceOf(form);

        if (action === 'cancel') {
            window.location.assign(cancelHref(form));
            return;
        }

        if (action === 'delete') {
            var expectedVersion = fieldValue(form, 'expectedVersion');
            var acknowledge = form.querySelector('[data-dmo-jobon-acknowledge]');
            var url = new URL(attribute(form, 'data-dmo-jobon-delete-endpoint'), window.location.origin);

            url.searchParams.set('expectedVersion', expectedVersion);
            url.searchParams.set('deleteConfirmed', 'true');
            url.searchParams.set('dateThresholdAcknowledged', acknowledge && acknowledge.checked ? 'true' : 'false');

            fetch(url.pathname + url.search, { method: 'DELETE', credentials: 'same-origin' })
                .then(function (response) {
                    if (response.status === 204) {
                        window.location.assign(DEFAULT_CANCEL_HREF);
                        return;
                    }

                    return response.text().then(function (text) {
                        var body = null;

                        try {
                            body = text ? JSON.parse(text) : null;
                        } catch (error) {
                            body = null;
                        }

                        showSubmitError(surface, describeFailure(response.status, body));
                    });
                })
                .catch(function () {
                    showSubmitError(surface, 'Não foi possível eliminar o Job On. Tente novamente.');
                });

            return;
        }

        if (action === 'duplicate') {
            sendJson(
                form,
                'POST',
                attribute(form, 'data-dmo-jobon-duplicate-endpoint'),
                {
                    expectedSourceVersion: parseInt(fieldValue(form, 'expectedSourceVersion'), 10),
                    productionNumber: fieldValue(form, 'productionNumber'),
                    machine: fieldValue(form, 'machine'),
                    productionDate: readDate(form, 'productionDate')
                },
                function (body) {
                    window.location.assign('/jobon/' + body.jobonId);
                });

            return;
        }

        if (action === 'save') {
            var createEndpoint = attribute(form, 'data-dmo-jobon-create-endpoint');
            var acknowledgeField = form.querySelector('[data-dmo-jobon-acknowledge]');

            if (createEndpoint) {
                sendJson(
                    form,
                    'POST',
                    createEndpoint,
                    {
                        reference: fieldValue(form, 'reference'),
                        productionNumber: fieldValue(form, 'productionNumber'),
                        machine: fieldValue(form, 'machine'),
                        productionDate: readDate(form, 'productionDate'),
                        cmToolId: slotValue(form, 'CM'),
                        mfToolId: slotValue(form, 'MF'),
                        bqToolId: slotValue(form, 'BQ')
                    },
                    function (body) {
                        window.location.assign('/jobon/' + body.jobonId);
                    });

                return;
            }

            sendJson(
                form,
                'PUT',
                attribute(form, 'data-dmo-jobon-update-endpoint'),
                {
                    expectedVersion: parseInt(fieldValue(form, 'expectedVersion'), 10),
                    reference: fieldValue(form, 'reference'),
                    productionNumber: fieldValue(form, 'productionNumber'),
                    machine: fieldValue(form, 'machine'),
                    productionDate: readDate(form, 'productionDate'),
                    associations: associationsOf(form),
                    dateThresholdWarningAcknowledged: !!(acknowledgeField && acknowledgeField.checked)
                },
                function () {
                    window.location.assign(cancelHref(form));
                });
        }
    }

    /* ---- event wiring ---------------------------------------------------------------------- */

    document.addEventListener(TOOL_PICKER_EVENT, function (event) {
        var detail = event.detail || {};
        var pickerRoot = closest(event.target, '[data-dmo-picker]');

        if (!pickerRoot) {
            return;
        }

        if (detail.kind === 'candidate-selected') {
            onCandidateSelected(pickerRoot, detail.candidateKey);
        } else if (detail.kind === 'search-requested') {
            onSearchRequested(pickerRoot, detail.query);
        } else if (detail.kind === 'create-requested') {
            onToolCreateRequested(pickerRoot);
        } else if (detail.kind === 'cancel-requested') {
            onToolCreateCancel(pickerRoot);
        }
    }, false);

    document.addEventListener(OPEN_REQUESTED_EVENT, function (event) {
        var detail = event.detail || {};
        var surface = surfaceOf(event.target);
        var entry = surface && detail.rowKey
            ? surface.querySelector('[data-dmo-jobon-route="' + detail.rowKey + '"]')
            : null;

        if (entry) {
            window.location.assign(attribute(entry, 'data-dmo-jobon-href'));
        }
    }, false);

    document.addEventListener('click', function (event) {
        var decision = closest(event.target, '[data-dmo-decision-bar] [data-dmo-action]');

        if (decision) {
            event.preventDefault();
            onDecisionAction(decision);
            return;
        }

        var createSubmit = closest(event.target, '[data-dmo-jobon-tool-create-submit]');
        if (createSubmit) {
            event.preventDefault();
            submitToolCreate(closest(createSubmit, '[data-dmo-jobon-surface]'));
            return;
        }

        var createCancel = closest(event.target, '[data-dmo-jobon-tool-create-cancel]');
        if (createCancel) {
            event.preventDefault();
            onToolCreateCancel(closest(createCancel, '[data-dmo-jobon-surface]'));
        }
    }, false);

    /* The origin form's default action is the search round trip; a real command never submits it
       natively, so an accidental native submit cannot lose the operator's state. */
    document.addEventListener('submit', function (event) {
        var form = closest(event.target, '[data-dmo-jobon-origin-form]');

        if (form && event.target.hasAttribute('data-dmo-jobon-command-form')) {
            event.preventDefault();
        }
    }, false);

    window.dmoJobOn = {
        selectCandidate: onCandidateSelected,
        searchRequested: onSearchRequested,
        createRequested: onToolCreateRequested,
        cancelRequested: onToolCreateCancel,
        associations: associationsOf,
        failure: describeFailure
    };
})();
