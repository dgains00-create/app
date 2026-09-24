// P2-T07 Boquilhas — page-owned interaction adapter (OWNER CLARIFICATION, production movement
// register, §34 pré-JobOn association). Loaded on /boquilhas (index), /boquilhas/novo (novo),
// /boquilhas/historico and /boquilhas/definicoes (definicoes).
//
// Surface rules (the accepted arbitration everywhere):
//  - tables are SELECTION surfaces: single click selects, double click opens (via the page-owned
//    opaque-key → route map; no per-row action buttons);
//  - all mutating actions live OUTSIDE the tables: "Registar movimento" and "Guardar edição" on
//    the index surface, "Associar BQ"/"Criar registo"/"Criar registo pré-JobOn" on the novo
//    surface, "Adicionar"/"Renomear"/"Guardar"/"Limpar" on the definicoes surface;
//  - the movement selector exposes EXACTLY the three types (saida | entrada | entrada_sem_reparacao);
//    no Início, no Irreparável, no lifecycle action exists anywhere;
//  - §34 association is ALWAYS human-confirmed (an explicit per-row confirmation dialog): the
//    candidates are never auto-selected and the association is never silent; the same canonical
//    tool_id UUID is the only identity proof (never reference/lote/máquina/texto);
//  - a refused/stale response is presented ONCE (no auto-retry) and a stale movement version
//    enters the accepted conflict presentation (D2) with the explicit "Recarregar estado atual"
//    recovery — reloading the page state, never an automatic retry;
//  - the shared Tool picker is consumed AS-IS (dmo:tool-picker events; never auto-selects; the
//    contextual create posts the P2-T04 ferramentas route and returns to THIS origin).
(function () {
    "use strict";

    var SURFACE = document.querySelector("[data-dmo-boquilhas-surface]");
    if (!SURFACE) {
        return;
    }

    var MODE = SURFACE.getAttribute("data-dmo-boquilhas-surface");
    var STATE = SURFACE.querySelector("[data-dmo-boquilhas-state]");

    // ------------------------------------------------------------------ forms / escapes

    function value(selector, root) {
        var node = (root || SURFACE).querySelector(selector);
        return node ? node.value : "";
    }

    function esc(text) {
        return String(text == null ? "" : text)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function today() {
        var now = new Date();
        var month = String(now.getMonth() + 1).padStart(2, "0");
        var day = String(now.getDate()).padStart(2, "0");
        return now.getFullYear() + "-" + month + "-" + day;
    }

    // ------------------------------------------------------------------ state surface (D2)

    function renderState(kind, html) {
        if (!STATE) {
            return;
        }

        STATE.hidden = false;
        STATE.className = "dmo-boquilhas__state dmo-boquilhas__state--" + esc(kind);

        // Build DOM nodes (never innerHTML strings): the same discipline as the accepted P2-T04
        // conflict presentation, and directly testable by the behavioral harness.
        STATE.textContent = "";
        while (STATE.firstChild) {
            STATE.removeChild(STATE.firstChild);
        }

        var paragraph = document.createElement("p");
        paragraph.textContent = html;
        STATE.appendChild(paragraph);
    }

    function renderError(message) {
        renderState("error", message);
    }

    function renderConflict(message) {
        renderState("conflict", message);

        var recovery = document.createElement("button");
        recovery.className = "dmo-boquilhas__button";
        recovery.type = "button";
        recovery.setAttribute("data-dmo-conflict-reload", "true");
        recovery.textContent = "Recarregar estado atual";
        STATE.appendChild(recovery);

        recovery.addEventListener("click", function () {
            window.location.reload();
        });
    }

    // One refusal presented at a time; never an automatic retry.
    function handleFailure(state) {
        if (!state || state.status !== 409) {
            renderError("A operação falhou (erro inesperado).");
            return;
        }

        state.json().then(function (payload) {
            var reason = payload && payload.reason ? payload.reason : "unknown";
            var message = payload && payload.message ? payload.message : "A operação foi recusada.";

            if (reason === "stale-version") {
                renderConflict(message);
            } else {
                renderError(message);
            }
        }, function () {
            renderError("A operação foi recusada pelo servidor.");
        });
    }

    function send(route, options) {
        options = options || {};
        options.headers = options.headers || { "Content-Type": "application/json" };
        return fetch(route, options);
    }

    // ------------------------------------------------------------------ shared table arbitration

    function wireOpenRoutes(surface) {
        var map = surface.querySelector("[data-dmo-open-route-map]");
        if (!map) {
            return;
        }

        var routes = {};
        map.querySelectorAll("[data-dmo-open-route]").forEach(function (entry) {
            routes[entry.getAttribute("data-dmo-open-route")] = entry.getAttribute("data-dmo-open-href");
        });

        // Double click opens the exact route (single click only selects — the DV row selection
        // state is left untouched here). No per-row action buttons exist anywhere.
        surface.querySelectorAll("[data-dmo-row]").forEach(function (row) {
            var key = row.getAttribute("data-dmo-row");
            var openHref = routes[key];

            if (openHref) {
                row.addEventListener("dblclick", function (event) {
                    event.preventDefault();
                    window.location.href = openHref;
                });
                row.addEventListener("keydown", function (event) {
                    if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                        event.preventDefault();
                        window.location.href = openHref;
                    }
                });
            }
        });

        // Table interactions never leak outside the table container.
        var tableRoot = surface.querySelector(".dmo-data-table, [data-dmo-table]") || surface;
        tableRoot.addEventListener("click", function (event) {
            event.stopPropagation();
        });
        tableRoot.addEventListener("dblclick", function (event) {
            event.stopPropagation();
        });
    }

    wireOpenRoutes(SURFACE);

    // ------------------------------------------------------------------ index surface

    if (MODE === "index") {
        var register = SURFACE.querySelector("[data-dmo-boquilhas-region='register']");
        if (register) {
            wireIndexRegister(register);
        }
    }

    // ---- §34 association (index surface): the candidates of a PENDING register ---------------
    // The candidates are the REAL bq_contexts rows whose tool_id equals the pending anchor
    // (/boquilhas/registers/{id}/association-candidates). Every association is HUMAN-CONFIRMED:
    // the operator confirms the explicit production before the POST; nothing is ever silent.
    function wireAssociationPanel(panel, boquilhasId) {
        var candidatesRegion = panel.querySelector("[data-dmo-association-candidates]");
        var version = parseInt(panel.getAttribute("data-dmo-boquilhas-version"), 10) || 1;

        if (!candidatesRegion) {
            return;
        }

        send("/boquilhas/registers/" + encodeURIComponent(boquilhasId) + "/association-candidates")
            .then(function (response) {
                if (!response.ok) {
                    handleFailure(response);
                    return Promise.resolve(null);
                }

                return response.json();
            })
            .then(function (payload) {
                if (!payload) {
                    return;
                }

                var candidates = payload.candidates || [];

                candidatesRegion.textContent = "";
                while (candidatesRegion.firstChild) {
                    candidatesRegion.removeChild(candidatesRegion.firstChild);
                }

                if (candidates.length === 0) {
                    var empty = document.createElement("p");
                    empty.className = "dmo-boquilhas__empty";
                    empty.textContent = "Sem produções candidatas: a associação é apresentada quando existir um contexto BQ com a mesma ferramenta (o mesmo tool_id).";
                    candidatesRegion.appendChild(empty);
                    return;
                }

                candidates.forEach(function (candidate) {
                    var row = document.createElement("div");
                    row.className = "dmo-boquilhas__candidate";

                    var facts = document.createElement("span");
                    facts.className = "dmo-boquilhas__candidate-facts";
                    facts.textContent = candidate.reference + " — " + candidate.productionNumber +
                        " (" + candidate.machine + ")" +
                        (candidate.productionDate ? " — " + candidate.productionDate : "");
                    row.appendChild(facts);

                    var associate = document.createElement("button");
                    associate.className = "dmo-boquilhas__button";
                    associate.type = "button";
                    associate.setAttribute("data-dmo-associate-register", candidate.bqId);
                    associate.textContent = "Associar a esta produção";
                    row.appendChild(associate);

                    associate.addEventListener("click", function () {
                        // The explicit human confirmation of the §34 association.
                        var confirmed = window.confirm(
                            "Associar o registo pré-JobOn à produção " +
                            candidate.reference + " — " + candidate.productionNumber +
                            " (" + candidate.machine + ")? O registo mantém o mesmo número e todo o histórico.");

                        if (!confirmed) {
                            return;
                        }

                        send("/boquilhas/registers/" + encodeURIComponent(boquilhasId) + "/associate", {
                            method: "POST",
                            body: JSON.stringify({
                                bqId: candidate.bqId,
                                expectedVersion: version
                            })
                        }).then(function (response) {
                            if (response.ok) {
                                window.location.reload();
                                return;
                            }

                            handleFailure(response);
                        }, function () {
                            renderError("Não foi possível contactar o servidor.");
                        });
                    });
                });
            }, function () {
                renderError("Não foi possível contactar o servidor.");
            });
    }

    function wireIndexRegister(register) {
        var boquilhasId = register.getAttribute("data-dmo-boquilhas-id");
        var movementTable = register.querySelector("[data-dmo-movement-table]");
        var entry = register.querySelector("[data-dmo-movement-entry]");
        var editRegion = SURFACE.querySelector("[data-dmo-boquilhas-region='detail']");
        var editForm = editRegion ? editRegion.querySelector("[data-dmo-edit-form]") : null;

        // §34: the human-confirmed association panel of a pending register.
        var associationPanel = register.querySelector("[data-dmo-association-panel]");
        if (associationPanel) {
            wireAssociationPanel(associationPanel, boquilhasId);
        }

        // The movement-entry resolutions (consumed reads; never administered here).
        var resolutions = {};
        register.querySelectorAll("[data-dmo-assignment]").forEach(function (span) {
            resolutions[span.getAttribute("data-dmo-assignment")] = {
                repairerId: span.getAttribute("data-dmo-assignment-repairer") || "",
                unavailable: span.getAttribute("data-dmo-assignment-unavailable") === "true"
            };
        });

        // ---- ledger selection/open arbitration -----------------------------------------
        var selectedRow = null;

        if (movementTable) {
            movementTable.addEventListener("click", function (event) {
                var row = event.target.closest("[data-dmo-movement-row]");
                if (!row) {
                    return;
                }

                if (selectedRow) {
                    selectedRow.classList.remove("dmo-boquilhas__selected");
                }

                selectedRow = row;
                row.classList.add("dmo-boquilhas__selected");
            });

            movementTable.addEventListener("dblclick", function (event) {
                var row = event.target.closest("[data-dmo-movement-row]");
                if (!row) {
                    return;
                }

                var movementId = row.getAttribute("data-dmo-movement-row");
                window.location.href = "/boquilhas?boquilhasId=" + encodeURIComponent(boquilhasId) +
                    "&movementId=" + encodeURIComponent(movementId);
            });
        }

        // ---- movement entry (EXACTLY the three closed types) ---------------------------
        var typeField = entry.querySelector("[data-dmo-movement-field='type']");
        var machineField = entry.querySelector("[data-dmo-movement-field='machine']");
        var repairerField = entry.querySelector("[data-dmo-movement-field='repairer']");

        function refreshEntryRequirements() {
            var isSaida = typeField && typeField.value === "saida";

            if (!isSaida) {
                return;
            }

            var machine = machineField ? machineField.value : "";
            var resolution = resolutions[machine];

            // machine → current assignment → suggested repairer (auto-resolve ONLY from the
            // authoritative consumed read; explicit assignmentUnavailable stays blank).
            if (resolution && !resolution.unavailable && resolution.repairerId && repairerField) {
                repairerField.value = resolution.repairerId;
            }
        }

        if (typeField) {
            typeField.addEventListener("change", refreshEntryRequirements);
        }

        if (machineField) {
            machineField.addEventListener("change", refreshEntryRequirements);
        }

        var submit = entry.querySelector("[data-dmo-movement-submit]");
        if (submit) {
            submit.addEventListener("click", function () {
                var payload = {
                    movementType: typeField ? typeField.value : "",
                    quantity: parseInt(value("[data-dmo-movement-field='quantity']", entry), 10) || 0,
                    businessDate: value("[data-dmo-movement-field='businessDate']", entry) || today(),
                    machine: machineField ? machineField.value || null : null,
                    repairerId: repairerField ? repairerField.value || null : null,
                    observations: value("[data-dmo-movement-field='observations']", entry) || null
                };

                send("/boquilhas/registers/" + encodeURIComponent(boquilhasId) + "/movements", {
                    method: "POST",
                    body: JSON.stringify(payload)
                }).then(function (response) {
                    if (response.ok) {
                        window.location.reload();
                        return;
                    }

                    handleFailure(response);
                }, function () {
                    renderError("Não foi possível contactar o servidor.");
                });
            });
        }

        // ---- movement edit (SAME row; audit rows only; version-guarded) -------------------
        if (editForm && editRegion) {
            var editMovementId = editRegion.getAttribute("data-dmo-edit-movement-id");
            var editVersion = parseInt(editRegion.getAttribute("data-dmo-edit-movement-version"), 10) || 0;

            var editSubmit = editRegion.querySelector("[data-dmo-edit-submit]");
            if (editSubmit) {
                editSubmit.addEventListener("click", function () {
                    var payload = {
                        expectedMovementVersion: editVersion,
                        quantity: parseInt(value("[data-dmo-edit-field='quantity']", editForm), 10) || 0,
                        businessDate: value("[data-dmo-edit-field='businessDate']", editForm) || today(),
                        machine: value("[data-dmo-edit-field='machine']", editForm) || null,
                        repairerId: value("[data-dmo-edit-field='repairer']", editForm) || null,
                        observations: value("[data-dmo-edit-field='observations']", editForm) || null
                    };

                    send("/boquilhas/registers/" + encodeURIComponent(boquilhasId) + "/movements/" +
                        encodeURIComponent(editMovementId), {
                        method: "PUT",
                        body: JSON.stringify(payload)
                    }).then(function (response) {
                        if (response.ok) {
                            window.location.reload();
                            return;
                        }

                        handleFailure(response);
                    }, function () {
                        renderError("Não foi possível contactar o servidor.");
                    });
                });
            }
        }
    }

    // ------------------------------------------------------------------ novo surface

    if (MODE === "novo") {
        wireNovo();
    }

    function wireNovo() {
        var productions = SURFACE.querySelector("[data-dmo-productions]");
        var jobsPanel = SURFACE.querySelector("[data-dmo-jobon-facts]");
        var bqPresent = SURFACE.querySelector("[data-dmo-bq-present]");
        var bqMissing = SURFACE.querySelector("[data-dmo-bq-missing]");
        var associateBq = SURFACE.querySelector("[data-dmo-associate-bq]");
        var selectedBq = SURFACE.querySelector("[data-dmo-selected-bq]");
        var selectedTool = SURFACE.querySelector("[data-dmo-selected-tool]");
        var selectedJobOn = SURFACE.querySelector("[data-dmo-selected-jobon]");
        var selectedJobOnVersion = SURFACE.querySelector("[data-dmo-selected-jobon-version]");

        // §34.1 rule 2 (Job-On-incoming): the pending registers of the production's canonical Tool.
        var pendingPanel = SURFACE.querySelector("[data-dmo-pending-registers]");
        var pendingRows = SURFACE.querySelector("[data-dmo-pending-register-rows]");
        var pendingEmpty = SURFACE.querySelector("[data-dmo-pending-registers-empty]");

        var jobOnId = null;
        var jobOnVersion = 0;

        // ---- §34.1 rule 2: the pending registers of THE SAME tool_id (never a global scan) ----
        // The light packet is fetched only when the production context (bq_id) is present; each
        // candidate requires the explicit human confirmation before the association POST.
        function loadPendingRegisters(bqId) {
            if (!pendingPanel || !pendingRows || !pendingEmpty) {
                return;
            }

            send("/boquilhas/pending-registers?bqId=" + encodeURIComponent(bqId))
                .then(function (response) {
                    if (!response.ok) {
                        handleFailure(response);
                        return Promise.resolve(null);
                    }

                    return response.json();
                })
                .then(function (payload) {
                    if (!payload) {
                        return;
                    }

                    var registers = payload.registers || [];

                    pendingPanel.hidden = false;
                    pendingEmpty.hidden = registers.length > 0;
                    pendingRows.textContent = "";
                    while (pendingRows.firstChild) {
                        pendingRows.removeChild(pendingRows.firstChild);
                    }

                    registers.forEach(function (register) {
                        var row = document.createElement("div");
                        row.className = "dmo-boquilhas__candidate";

                        var facts = document.createElement("span");
                        facts.className = "dmo-boquilhas__candidate-facts";
                        facts.textContent = register.toolReference + " — lote " + register.toolLot +
                            " · registado em " + register.createdAt + " UTC · " +
                            register.movementCount + " movimento(s) · pendente " + register.outstanding;
                        row.appendChild(facts);

                        var associate = document.createElement("button");
                        associate.className = "dmo-boquilhas__button";
                        associate.type = "button";
                        associate.setAttribute("data-dmo-associate-pending", register.boquilhasId);
                        associate.textContent = "Associar este registo";
                        row.appendChild(associate);

                        associate.addEventListener("click", function () {
                            // The explicit human confirmation of the §34 association: the same
                            // tool_id proves the Tool; only the operator decides the production.
                            var confirmed = window.confirm(
                                "Associar o registo pré-JobOn " + register.boquilhasId +
                                " (" + register.toolReference + " — lote " + register.toolLot + ") " +
                                "a esta produção? O registo mantém o mesmo número e todo o histórico.");

                            if (!confirmed) {
                                return;
                            }

                            send("/boquilhas/registers/" +
                                encodeURIComponent(register.boquilhasId) + "/associate", {
                                method: "POST",
                                body: JSON.stringify({
                                    bqId: bqId,
                                    expectedVersion: register.version
                                })
                            }).then(function (response) {
                                if (response.ok) {
                                    window.location.href = "/boquilhas?boquilhasId=" +
                                        encodeURIComponent(register.boquilhasId);
                                    return;
                                }

                                handleFailure(response);
                            }, function () {
                                renderError("Não foi possível contactar o servidor.");
                            });
                        });
                    });
                }, function () {
                    renderError("Não foi possível contactar o servidor.");
                });
        }

        // ---- reference → productions (explicit selection) ----------------------------------
        var searchSubmit = SURFACE.querySelector("[data-dmo-production-search-submit]");
        var referenceInput = SURFACE.querySelector("[data-dmo-production-reference]");
        var productionsEmpty = SURFACE.querySelector("[data-dmo-productions-empty]");

        if (searchSubmit) {
            searchSubmit.addEventListener("click", function () {
                var reference = referenceInput ? referenceInput.value.trim() : "";
                if (!reference) {
                    return;
                }

                send("/boquilhas/productions?reference=" + encodeURIComponent(reference))
                    .then(function (response) {
                        if (!response.ok) {
                            handleFailure(response);
                            return Promise.resolve(null);
                        }

                        return response.json();
                    })
                    .then(function (payload) {
                        if (!payload) {
                            return;
                        }

                        productions.hidden = false;
                        productionsEmpty.hidden = payload.productions.length > 0;
                        productions.innerHTML = payload.productions.map(function (production) {
                            return "<button type=\"button\" class=\"dmo-boquilhas__production\" data-dmo-production=\"" +
                                esc(production.jobonId) + "\">" +
                                esc(production.reference) + " — " + esc(production.productionNumber) +
                                " (" + esc(production.machine) + ")</button>";
                        }).join("");

                        productions.querySelectorAll("[data-dmo-production]").forEach(function (button) {
                            button.addEventListener("click", function () {
                                openJobOn(button.getAttribute("data-dmo-production"));
                            });
                        });
                    }, function () {
                        renderError("Não foi possível contactar o servidor.");
                    });
            });
        }

        function openJobOn(id) {
            jobOnId = id;
            send("/boquilhas/jobons/" + encodeURIComponent(id))
                .then(function (response) {
                    if (!response.ok) {
                        handleFailure(response);
                        return Promise.resolve(null);
                    }

                    return response.json();
                })
                .then(function (payload) {
                    if (!payload || !payload.ficha) {
                        return;
                    }

                    var ficha = payload.ficha;
                    jobOnVersion = ficha.version || 0;

                    if (selectedJobOn) {
                        selectedJobOn.value = jobOnId;
                    }

                    if (selectedJobOnVersion) {
                        selectedJobOnVersion.value = String(jobOnVersion);
                    }

                    var bq = ficha.contexts ? ficha.contexts.find(function (context) {
                        return context.toolType === "bq";
                    }) : null;

                    jobsPanel.hidden = false;
                    bqPresent.hidden = !bq;
                    bqMissing.hidden = !!bq;
                    associateBq.hidden = !!bq;
                    associateBq.disabled = !!bq;

                    if (bq) {
                        if (selectedBq) {
                            selectedBq.value = bq.contextId;
                        }

                        // §34.1 rule 2: the pending registers of the SAME tool_id are fetched
                        // ONLY when the production context (bq_id) is present.
                        loadPendingRegisters(bq.contextId);
                    } else {
                        if (selectedBq) {
                            selectedBq.value = "";
                        }

                        if (pendingPanel) {
                            pendingPanel.hidden = true;
                        }
                    }

                    // Successful association returns the REAL bq_id.
                    if (selectedTool) {
                        selectedTool.value = "";
                    }
                }, function () {
                    renderError("Não foi possível contactar o servidor.");
                });
        }

        // ---- shared Tool picker consumption (dmo:tool-picker events) ------------------------
        var pickerRoot = SURFACE.querySelector("#dmo-boquilhas-picker");
        var candidatesMap = SURFACE.querySelector("[data-dmo-boquilhas-candidate-map]");
        var toolCreatePanel = SURFACE.querySelector("[data-dmo-boquilhas-tool-create-panel]");

        if (pickerRoot) {
            document.addEventListener("dmo:tool-picker:query", function (event) {
                var detail = event.detail || {};
                if (!detail.originToken || detail.originToken !== "boquilhas-novo") {
                    return;
                }

                var query = detail.query || "";
                send("/ferramentas/tools?query=" + encodeURIComponent(query) + "&expectedType=bq")
                    .then(function (response) {
                        if (!response.ok) {
                            return response.json().then(function (payload) {
                                throw payload;
                            });
                        }

                        return response.json();
                    })
                    .then(function (payload) {
                        var candidates = payload && payload.candidates ? payload.candidates : [];
                        var target = document.querySelector("[data-dmo-picker-candidates]");
                        if (!target) {
                            return;
                        }

                        candidatesMap.querySelectorAll("[data-dmo-candidate]").forEach(function (node) {
                            node.remove();
                        });

                        candidates.forEach(function (candidate) {
                            var span = document.createElement("span");
                            span.setAttribute("data-dmo-candidate", candidate.toolId);
                            span.setAttribute("data-dmo-candidate-key", candidate.toolId);
                            candidatesMap.appendChild(span);
                        });

                        target.innerHTML = candidates.map(function (candidate) {
                            return "<button type=\"button\" class=\"dmo-picker__candidate\" data-dmo-picker-candidate=\"" +
                                esc(candidate.toolId) + "\" title=\"" + esc(candidate.reference) + " — lote " +
                                esc(candidate.lot) + "\">" + esc(candidate.reference) + " — lote " +
                                esc(candidate.lot) + "</button>";
                        }).join("") || "<p class=\"dmo-picker__empty\">Sem ferramentas BQ para esta pesquisa.</p>";

                        target.querySelectorAll("[data-dmo-picker-candidate]").forEach(function (button) {
                            button.addEventListener("click", function () {
                                document.dispatchEvent(new CustomEvent("dmo:tool-picker:selected", {
                                    detail: {
                                        originToken: "boquilhas-novo",
                                        candidateKey: button.getAttribute("data-dmo-picker-candidate"),
                                        candidateReference: button.title,
                                        createActionKey: null
                                    }
                                }));
                            });
                        });
                    }, function () {
                        renderError("A pesquisa de ferramentas falhou.");
                    });
            });

            // The shared picker announces every selection; the adapter keeps the canonical id.
            document.addEventListener("dmo:tool-picker:selected", function (event) {
                var detail = event.detail || {};
                if (!detail.originToken || detail.originToken !== "boquilhas-novo") {
                    return;
                }

                var key = detail.candidateKey;
                var canonical = candidatesMap.querySelector("[data-dmo-candidate='" + key + "']");
                if (canonical && selectedTool) {
                    selectedTool.value = canonical.getAttribute("data-dmo-candidate");
                }
            });

            // The contextual create opens the page-owned create panel (posts the P2-T04 route;
            // returns to THIS origin).
            document.addEventListener("dmo:tool-picker:create", function (event) {
                var detail = event.detail || {};
                if (!detail.originToken || detail.originToken !== "boquilhas-novo") {
                    return;
                }

                if (toolCreatePanel) {
                    toolCreatePanel.hidden = false;
                }
            });

            document.addEventListener("dmo:tool-picker:cancel", function (event) {
                var detail = event.detail || {};
                if (!detail.originToken || detail.originToken !== "boquilhas-novo") {
                    return;
                }

                if (toolCreatePanel) {
                    toolCreatePanel.hidden = true;
                }
            });
        }

        // ---- contextual Tool create (posts POST /ferramentas/tools; returns to origin) ------
        var toolCreateSubmit = SURFACE.querySelector("[data-dmo-boquilhas-tool-create-submit]");
        var toolCreateCancel = SURFACE.querySelector("[data-dmo-boquilhas-tool-create-cancel]");
        var toolCreateError = SURFACE.querySelector("[data-dmo-boquilhas-tool-create-error]");

        if (toolCreateSubmit) {
            toolCreateSubmit.addEventListener("click", function () {
                var machines = [];
                SURFACE.querySelectorAll("[data-dmo-boquilhas-tool-machine]:checked").forEach(function (checkbox) {
                    machines.push(checkbox.value);
                });

                var payload = {
                    reference: value("[data-dmo-boquilhas-tool-field='reference']", toolCreatePanel),
                    lot: value("[data-dmo-boquilhas-tool-field='lot']", toolCreatePanel),
                    internalProcess: value("[data-dmo-boquilhas-tool-field='processo']", toolCreatePanel) || null,
                    quantity: parseInt(value("[data-dmo-boquilhas-tool-field='quantity']", toolCreatePanel), 10) || 0,
                    machines: machines,
                    expectedType: "bq",
                    originToken: "boquilhas-novo"
                };

                send("/ferramentas/tools", {
                    method: "POST",
                    body: JSON.stringify(payload)
                }).then(function (response) {
                    if (!response.ok) {
                        toolCreateError.hidden = false;
                        toolCreateError.textContent = "A criação da ferramenta falhou (recuse ou validação).";
                        return;
                    }

                    return response.json();
                }).then(function (payload) {
                    if (!payload || !payload.toolId) {
                        return;
                    }

                    if (selectedTool) {
                        selectedTool.value = payload.toolId;
                    }

                    if (toolCreatePanel) {
                        toolCreatePanel.hidden = true;
                    }
                }, function () {
                    toolCreateError.hidden = false;
                    toolCreateError.textContent = "Não foi possível contactar o servidor.";
                });
            });
        }

        if (toolCreateCancel) {
            toolCreateCancel.addEventListener("click", function () {
                if (toolCreatePanel) {
                    toolCreatePanel.hidden = true;
                }
            });
        }

        // ---- associate BQ through IJobOnService (Keep + Set BQ slot; route 9) -----------------
        if (associateBq) {
            associateBq.addEventListener("click", function () {
                var toolId = selectedTool ? selectedTool.value : "";

                if (!toolId) {
                    renderError("Selecione primeiro a ferramenta BQ na lista de ferramentas.");
                    return;
                }

                send("/boquilhas/jobons/" + encodeURIComponent(jobOnId) + "/bq-association", {
                    method: "POST",
                    body: JSON.stringify({
                        toolId: toolId,
                        expectedJobOnVersion: jobOnVersion
                    })
                }).then(function (response) {
                    if (!response.ok) {
                        handleFailure(response);
                        return Promise.resolve(null);
                    }

                    return response.json();
                }).then(function (payload) {
                    if (!payload) {
                        return;
                    }

                    // The REAL bq_id created/updated by Job On's own code.
                    if (selectedBq) {
                        selectedBq.value = payload.bqId;
                    }

                    jobOnVersion = payload.version || jobOnVersion;

                    if (selectedJobOnVersion) {
                        selectedJobOnVersion.value = String(jobOnVersion);
                    }

                    bqPresent.hidden = false;
                    bqMissing.hidden = true;
                    associateBq.hidden = true;

                    // §34.1 rule 2: the BQ context now exists — the pending registers of the
                    // same tool_id become presentable.
                    loadPendingRegisters(payload.bqId);
                }, function () {
                    renderError("A associação ao contexto BQ falhou.");
                });
            });
        }

        // ---- create the register IDENTITY (route 4; NO quantity movement) --------------------
        var createSubmit = SURFACE.querySelector("[data-dmo-create-submit]");
        if (createSubmit) {
            createSubmit.addEventListener("click", function () {
                var bqId = selectedBq ? selectedBq.value : "";

                if (!jobOnId || !bqId) {
                    renderError("Associe primeiro a produção ao contexto BQ.");
                    return;
                }

                send("/boquilhas/registers", {
                    method: "POST",
                    body: JSON.stringify({ bqId: bqId })
                }).then(function (response) {
                    if (!response.ok) {
                        handleFailure(response);
                        return Promise.resolve(null);
                    }

                    return response.json();
                }).then(function (payload) {
                    if (!payload) {
                        return;
                    }

                    // Success: open the created register (identity only; zero movements).
                    window.location.href = "/boquilhas?boquilhasId=" + encodeURIComponent(payload.boquilhasId);
                }, function () {
                    renderError("A criação do registo falhou.");
                });
            });
        }

        // ---- §34.1 rule 1: the TRANSITIONAL pré-JobOn register (provisional tool_id anchor) --
        var createPendingSubmit = SURFACE.querySelector("[data-dmo-create-pending-submit]");
        if (createPendingSubmit) {
            createPendingSubmit.addEventListener("click", function () {
                var toolId = selectedTool ? selectedTool.value : "";

                if (!toolId) {
                    renderError("Selecione primeiro a ferramenta BQ na lista de ferramentas.");
                    return;
                }

                send("/boquilhas/registers", {
                    method: "POST",
                    body: JSON.stringify({ pendingToolId: toolId })
                }).then(function (response) {
                    if (!response.ok) {
                        handleFailure(response);
                        return Promise.resolve(null);
                    }

                    return response.json();
                }).then(function (payload) {
                    if (!payload) {
                        return;
                    }

                    // Success: open the pending register (identity only; zero movements).
                    window.location.href = "/boquilhas?boquilhasId=" + encodeURIComponent(payload.boquilhasId);
                }, function () {
                    renderError("A criação do registo pré-JobOn falhou.");
                });
            });
        }
    }

    // ------------------------------------------------------------------ definicoes surface

    if (MODE === "definicoes") {
        wireDefinicoes();
    }

    // The Boquilhas > Definições adapter: the repairer register + the independent machine
    // assignments (Owner clarification P2-T07 §34.3). Every write posts the
    // /boquilhas/definicoes routes; success reloads the page state (D2 recovery stays the same).
    function wireDefinicoes() {
        // ---- repairers: add (POST) / rename (PUT, version-guarded) -------------------------
        var addButton = SURFACE.querySelector("[data-dmo-repairer-add]");
        if (addButton) {
            addButton.addEventListener("click", function () {
                var name = value("[data-dmo-repairer-new-name]");

                send("/boquilhas/definicoes/repairers", {
                    method: "POST",
                    body: JSON.stringify({ name: name })
                }).then(function (response) {
                    if (response.ok) {
                        window.location.reload();
                        return;
                    }

                    handleFailure(response);
                }, function () {
                    renderError("Não foi possível contactar o servidor.");
                });
            });
        }

        SURFACE.querySelectorAll("[data-dmo-repairer-rename-save]").forEach(function (button) {
            var repairerId = button.getAttribute("data-dmo-repairer-rename-save");
            var input = SURFACE.querySelector("[data-dmo-repairer-rename='" + repairerId + "']");
            var version = input
                ? parseInt(input.getAttribute("data-dmo-repairer-version"), 10) || 1
                : 1;

            button.addEventListener("click", function () {
                var name = input ? input.value : "";

                send("/boquilhas/definicoes/repairers/" + encodeURIComponent(repairerId), {
                    method: "PUT",
                    body: JSON.stringify({ expectedVersion: version, name: name })
                }).then(function (response) {
                    if (response.ok) {
                        window.location.reload();
                        return;
                    }

                    handleFailure(response);
                }, function () {
                    renderError("Não foi possível contactar o servidor.");
                });
            });
        });

        // ---- machine assignments: set (PUT with repairerId) / clear (PUT with null) ----------
        function submitAssignment(machine, repairerId) {
            var select = SURFACE.querySelector("[data-dmo-assignment-repairer='" + machine + "']");
            var expectedVersion = select
                ? parseInt(select.getAttribute("data-dmo-assignment-version"), 10) || 1
                : 1;

            send("/boquilhas/definicoes/machine-assignments/" + encodeURIComponent(machine), {
                method: "PUT",
                body: JSON.stringify({
                    repairerId: repairerId,
                    expectedVersion: expectedVersion
                })
            }).then(function (response) {
                if (response.ok) {
                    window.location.reload();
                    return;
                }

                handleFailure(response);
            }, function () {
                renderError("Não foi possível contactar o servidor.");
            });
        }

        SURFACE.querySelectorAll("[data-dmo-assignment-set]").forEach(function (button) {
            var machine = button.getAttribute("data-dmo-assignment-set");
            var select = SURFACE.querySelector("[data-dmo-assignment-repairer='" + machine + "']");

            button.addEventListener("click", function () {
                var repairerId = select ? select.value : "";
                submitAssignment(machine, repairerId ? repairerId : null);
            });
        });

        SURFACE.querySelectorAll("[data-dmo-assignment-clear]").forEach(function (button) {
            var machine = button.getAttribute("data-dmo-assignment-clear");

            button.addEventListener("click", function () {
                var confirmed = window.confirm(
                    "Limpar a atribuição de reparador da máquina " + machine + "?");

                if (!confirmed) {
                    return;
                }

                submitAssignment(machine, null);
            });
        });
    }
})();