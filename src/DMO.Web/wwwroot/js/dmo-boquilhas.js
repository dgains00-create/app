/* P2-T07 Boquilhas page-owned adapter (contract §13.2, §16, §22.4, §24.3, §25).
 *
 * Three surfaces:
 *   index     — Registo: lot grid + aggregate register + movement entry/edit + close/reopen;
 *   novo      — Novo: production-linked | standalone opening with the SHARED Tool orchestration;
 *   historico — Histórico (local): filters + history table + detail/reopen.
 *
 * The adapter performs NO domain decision: backend validation is authoritative and every
 * persisted-state refusal surfaces its typed reason. Selection/open arbitration for the shared
 * DenseDataTable is delegated to the shared adapters (single click selects, double click opens);
 * the page adapter only resolves the consumer route map. A typed `stale-version` refusal enters
 * the accepted conflict state with the explicit reload recovery (the D2 pattern) — no automatic
 * retry, no auto-merge, no silent overwrite, and the observed version is refreshed only on
 * success. The Tool picker is the SHARED orchestration: candidates are BQ Tools only, a single
 * candidate is never auto-selected, and the contextual create subflow posts the P2-T04
 * ferramentas route and returns to THIS origin state (opening facts preserved).
 *
 * No width listener exists anywhere (AC-L1): the page renders its fixed-desktop composition and
 * scrolling is handled by the layout, not by script. It reuses the frozen window.dmoFocus helper
 * only when genuinely required and never declares a second focus helper.
 */
if (!window.dmoBoquilhas) {
  window.dmoBoquilhas = (function () {
    "use strict";

    var basePath = "/boquilhas";
    var TOOL_PICKER_EVENT = "dmo:tool-picker";
    var OPEN_REQUESTED_EVENT = "dmo:open-requested";
    var TOOL_CREATE_ENDPOINT = "/ferramentas/tools";

    function closest(node, selector) {
      if (!node || typeof node.closest !== "function") {
        return null;
      }

      return node.closest(selector);
    }

    function attribute(node, name) {
      return node && typeof node.getAttribute === "function" ? node.getAttribute(name) : null;
    }

    function surfaceOf(node) {
      return closest(node, "[data-dmo-boquilhas-surface]");
    }

    function api(path, method, body) {
      var options = { method: method, headers: {}, credentials: "same-origin" };
      if (body !== undefined && body !== null) {
        options.headers["Content-Type"] = "application/json";
        options.body = JSON.stringify(body);
      }
      return fetch(path, options).then(function (response) {
        return response.json().catch(function () { return {}; }).then(function (payload) {
          return { ok: response.ok, status: response.status, body: payload };
        });
      });
    }

    /**
     * Binds an event listener ONLY when the target element exists: absence of an element is a
     * VALID state (e.g. no ficha opened, closed aggregate), so the adapter must initialize
     * normally and never throw on a missing control (the accepted D1 pattern).
     */
    function on(root, selector, event, handler) {
      var element = root ? root.querySelector(selector) : null;
      if (element) { element.addEventListener(event, handler); }
    }

    function onAny(root, selector, event, handler) {
      var elements = root ? root.querySelectorAll(selector) : [];
      elements.forEach(function (element) { element.addEventListener(event, handler); });
    }

    /**
     * The page-owned failure presentation of every mutation (D2 pattern): a typed
     * `stale-version` refusal enters the accepted conflict state with an explicit recovery
     * action (reload the authoritative current server state) — never an automatic retry, never a
     * silent merge, never an overwrite of the newer server version. Every other typed failure
     * keeps the errors presentation exactly.
     */
    function renderMutationFailure(root, response) {
      var payload = response && response.body ? response.body : null;
      if (payload && payload.reason === "stale-version") {
        renderConflict(root, payload.message);
      } else {
        renderErrors(root, payload);
      }
    }

    function renderErrors(root, payload) {
      var region = root.querySelector("[data-dmo-boquilhas-state]");
      if (!region) { return; }
      var messages = [];
      if (payload && payload.errors && Array.isArray(payload.errors)) {
        messages = payload.errors;
      } else if (payload && payload.reason) {
        messages = [payload.reason + (payload.message ? " — " + payload.message : "")];
      } else {
        messages = ["A operação não foi concluída."];
      }
      region.hidden = false;
      region.setAttribute("role", "alert");
      region.removeAttribute("data-dmo-conflict");
      region.textContent = "";
      var list = document.createElement("ul");
      messages.forEach(function (message) {
        var item = document.createElement("li");
        item.textContent = message;
        list.appendChild(item);
      });
      region.appendChild(list);
    }

    /**
     * The conflict presentation (freeze §4 `conflict`): a clear message plus supplied recovery
     * choices — one explicit reload action that fetches the authoritative current server state.
     * No automatic retry and no auto-merge ever happens (AC-K5).
     */
    function renderConflict(root, message) {
      var region = root.querySelector("[data-dmo-boquilhas-state]");
      if (!region) { return; }
      region.hidden = false;
      region.setAttribute("role", "alert");
      region.setAttribute("data-dmo-conflict", "true");
      region.textContent = "";
      var heading = document.createElement("strong");
      heading.textContent = "Conflito — os dados foram alterados por outra ação; nada foi guardado.";
      region.appendChild(heading);
      var detail = document.createElement("p");
      detail.textContent = message || "Recarregue para obter o estado atual do servidor.";
      region.appendChild(detail);
      var recover = document.createElement("button");
      recover.type = "button";
      recover.textContent = "Recarregar estado atual";
      recover.setAttribute("data-dmo-conflict-reload", "true");
      recover.addEventListener("click", function () { window.location.reload(); });
      region.appendChild(recover);
    }

    function withPending(root, key) {
      var buttons = root.querySelectorAll("[data-dmo-action='" + key + "']");
      var originals = [];
      buttons.forEach(function (button) {
        originals.push({ button: button, text: button.textContent, disabled: button.disabled });
        var pending = attribute(button, "data-dmo-action-pending");
        if (pending) { button.textContent = pending; }
        button.disabled = true;
      });
      return function () {
        originals.forEach(function (entry) {
          entry.button.textContent = entry.text;
          entry.button.disabled = entry.disabled;
        });
      };
    }

    function observedState(root) {
      var state = root.querySelector("[data-dmo-boquilhas-id]");
      return {
        boquilhasId: state ? attribute(state, "data-dmo-boquilhas-id") || "" : "",
        version: state ? parseInt(attribute(state, "data-dmo-version") || "1", 10) : 1
      };
    }

    function fieldValue(root, hook, dataHook) {
      var element = root.querySelector(hook);
      if (!element) { return null; }
      var selector = dataHook ? "[data-dmo-movement-field='" + dataHook + "']" : hook;
      element = root.querySelector(selector) || element;
      return typeof element.value === "string" ? element.value : "";
    }

    function movementField(root, name) {
      var element = root.querySelector("[data-dmo-movement-field='" + name + "']");
      return element && typeof element.value === "string" ? element.value : "";
    }

    function editField(root, name) {
      var element = root.querySelector("[data-dmo-edit-field='" + name + "']");
      return element && typeof element.value === "string" ? element.value : "";
    }

    function checkedMachines(root, hook) {
      var boxes = root.querySelectorAll(hook);
      var selected = [];
      boxes.forEach(function (box) {
        if (box.checked) { selected.push(box.value); }
      });
      return selected;
    }

    /* =======================================================================================
     * Registo (index)
     * ====================================================================================== */

    function wireOpenRoutes(root) {
      root.querySelectorAll("[data-dmo-dense-table]").forEach(function (table) {
        table.addEventListener(OPEN_REQUESTED_EVENT, function (event) {
          var key = event.detail && event.detail.rowKey;
          if (!key) { return; }
          var entry = root.querySelector("[data-dmo-open-route='" + key + "']");
          if (entry) {
            window.location.assign(attribute(entry, "data-dmo-open-href"));
          }
        });
      });
    }

    /**
     * The movement ledger follows the accepted selection/open arbitration (AC-H3): single click
     * selects a row, double click opens its edit/detail (R5) — actions live outside the table.
     */
    function wireMovementLedger(root) {
      var ledger = root.querySelector("[data-dmo-movement-table]");
      if (!ledger) { return; }

      ledger.addEventListener("click", function (event) {
        var row = closest(event.target, "[data-dmo-movement-row]");
        if (!row) { return; }
        var rows = ledger.querySelectorAll("[data-dmo-movement-row]");
        rows.forEach(function (candidate) {
          candidate.classList.toggle("dmo-boquilhas__ledger-row--selected", candidate === row);
        });
      });

      ledger.addEventListener("dblclick", function (event) {
        var row = closest(event.target, "[data-dmo-movement-row]");
        if (!row) { return; }
        var state = observedState(root);
        if (state.boquilhasId && attribute(row, "data-dmo-movement-row")) {
          window.location.assign(
            basePath + "?boquilhasId=" + state.boquilhasId +
            "&movementId=" + attribute(row, "data-dmo-movement-row"));
        }
      });
    }

    /* Machine → current assignment → suggested repairer (route 16 composition): the suggestion
       pre-fills the movement repairer select; the FINAL stored value is the human-confirmed one. */
    function wireRepairerResolution(root) {
      var machineSelect = root.querySelector("[data-dmo-movement-field='machine']");
      var repairerSelect = root.querySelector("[data-dmo-movement-field='repairer']");
      var resolutions = root.querySelector("[data-dmo-resolutions]");

      if (!machineSelect || !repairerSelect || !resolutions) { return; }

      machineSelect.addEventListener("change", function () {
        var machine = machineSelect.value;
        var entry = resolutions.querySelector("[data-dmo-assignment='" + machine + "']");
        if (!entry || attribute(entry, "data-dmo-assignment-unavailable") === "true") {
          repairerSelect.value = "";
          return;
        }
        var suggested = attribute(entry, "data-dmo-assignment-repairer") || "";
        repairerSelect.value = suggested;
      });
    }

    function appendMovement(root, control) {
      var state = observedState(root);
      if (!state.boquilhasId) { return; }

      var release = withPending(root, "movement");
      api(basePath + "/aggregates/" + state.boquilhasId + "/movements", "POST", {
        expectedAggregateVersion: state.version,
        movementType: movementField(root, "type"),
        quantity: parseInt(movementField(root, "quantity"), 10) || 0,
        businessDate: movementField(root, "businessDate"),
        machine: movementField(root, "machine") || null,
        repairerId: movementField(root, "repairer") || null,
        observations: movementField(root, "observations") || null
      }).then(function (response) {
        release();
        if (response.status === 201) {
          window.location.reload();
          return;
        }
        renderMutationFailure(root, response);
      });
    }

    function editMovement(root, control) {
      var state = observedState(root);
      var detail = root.querySelector("[data-dmo-edit-movement-id]");
      if (!state.boquilhasId || !detail) { return; }

      var movementId = attribute(detail, "data-dmo-edit-movement-id");
      var release = withPending(root, "edit");
      api(basePath + "/aggregates/" + state.boquilhasId + "/movements/" + movementId, "PUT", {
        expectedAggregateVersion: state.version,
        expectedMovementVersion: parseInt(attribute(detail, "data-dmo-edit-movement-version"), 10),
        quantity: parseInt(editField(root, "quantity"), 10) || 0,
        businessDate: editField(root, "businessDate"),
        machine: editField(root, "machine") || null,
        repairerId: editField(root, "repairer") || null,
        observations: editField(root, "observations") || null
      }).then(function (response) {
        release();
        if (response.ok) {
          window.location.reload();
          return;
        }
        renderMutationFailure(root, response);
      });
    }

    function closeAggregate(root, control) {
      var state = observedState(root);
      if (!state.boquilhasId) { return; }

      var release = withPending(root, "close");
      api(basePath + "/aggregates/" + state.boquilhasId + "/close", "POST", {
        expectedVersion: state.version
      }).then(function (response) {
        release();
        if (response.ok) {
          window.location.reload();
          return;
        }
        renderMutationFailure(root, response);
      });
    }

    function reasonInput(root) {
      var input = root.querySelector("[data-dmo-reopen-reason-input]");
      return input ? input.value.trim() : "";
    }

    function reopenAggregate(root, control) {
      var state = observedState(root);
      if (!state.boquilhasId) { return; }

      var region = root.querySelector("[data-dmo-reason-region]");
      if (region && region.hidden) {
        region.hidden = false;
        var input = region.querySelector("[data-dmo-reopen-reason-input]");
        if (input && typeof input.focus === "function") { input.focus(); }
        return;
      }

      var release = withPending(root, "reopen");
      api(basePath + "/aggregates/" + state.boquilhasId + "/reopen", "POST", {
        expectedVersion: state.version,
        reason: reasonInput(root)
      }).then(function (response) {
        release();
        if (response.ok) {
          window.location.reload();
          return;
        }
        renderMutationFailure(root, response);
      });
    }

    function updateOpeningFacts(root, control) {
      var state = observedState(root);
      if (!state.boquilhasId) { return; }

      var dateInput = root.querySelector("[data-dmo-opening-fact-date]");
      var utilInput = root.querySelector("[data-dmo-opening-fact-utilisation]");
      var obsInput = root.querySelector("[data-dmo-opening-fact-observations]");
      if (!dateInput || !utilInput || !obsInput) {
        // The editing surface is not rendered — no silent write of display facts.
        return;
      }

      var machineList = [];
      root.querySelectorAll("[data-dmo-opening-fact-machine]").forEach(function (box) {
        if (box.checked) { machineList.push(box.value); }
      });

      var release = withPending(root, "opening");
      api(basePath + "/aggregates/" + state.boquilhasId + "/opening-facts", "PUT", {
        expectedVersion: state.version,
        openingDate: dateInput.value,
        utilisationPercent: utilInput.value !== "" ? parseFloat(utilInput.value) : null,
        observations: obsInput.value !== "" ? obsInput.value : null,
        machines: machineList
      }).then(function (response) {
        release();
        if (response.ok) {
          window.location.reload();
          return;
        }
        renderMutationFailure(root, response);
      });
    }

    /* =======================================================================================
     * Histórico (local)
     * ====================================================================================== */

    function wireHistorico(root) {
      wireOpenRoutes(root);

      on(root, "[data-dmo-reopen-submit]", "click", function (event) {
        event.preventDefault();
        reopenAggregate(root, event.currentTarget);
      });
    }

    /* =======================================================================================
     * Novo — shared Tool orchestration + production-linked flow
     * ====================================================================================== */

    function wireNovo(root) {
      var flowLinked = root.querySelector("[data-dmo-flow='linked']");
      var productionRegion = root.querySelector("[data-dmo-boquilhas-region='production']");

      function applyFlow() {
        var linked = flowLinked ? flowLinked.checked : false;
        if (productionRegion) { productionRegion.hidden = !linked; }
        return linked;
      }

      onAny(root, "[data-dmo-flow]", "change", applyFlow);
      applyFlow();

      wireNovoProductions(root);
      wireNovoToolPicker(root);
      wireNovoCreate(root);
    }

    /** Route 13 (reference → productions, explicit selection) + route 14 (Job On ficha) + route 15. */
    function wireNovoProductions(root) {
      var searchInput = root.querySelector("[data-dmo-production-reference]");
      var searchButton = root.querySelector("[data-dmo-production-search-submit]");
      var container = root.querySelector("[data-dmo-productions]");
      var jobOnFacts = root.querySelector("[data-dmo-jobon-facts]");
      var selectedJobOn = root.querySelector("[data-dmo-selected-jobon]");
      var selectedJobOnVersion = root.querySelector("[data-dmo-selected-jobon-version]");
      var selectedBq = root.querySelector("[data-dmo-selected-bq]");
      var selectedTool = root.querySelector("[data-dmo-selected-tool]");
      var associateButton = root.querySelector("[data-dmo-associate-bq]");

      if (!searchButton || !container) { return; }

      function renderProductions(productions) {
        container.textContent = "";
        var empty = container.querySelector("[data-dmo-productions-empty]");
        if (empty) { empty.remove(); }

        if (productions.length === 0) {
          var message = document.createElement("p");
          message.className = "dmo-boquilhas__empty";
          message.setAttribute("data-dmo-productions-empty", "true");
          message.textContent = "Sem produções para esta referência.";
          container.appendChild(message);
          container.hidden = false;
          return;
        }

        var list = document.createElement("div");
        productions.forEach(function (production) {
          var label = document.createElement("label");
          label.className = "dmo-boquilhas__flow-option";
          var radio = document.createElement("input");
          radio.type = "radio";
          radio.name = "dmo-production";
          radio.setAttribute("data-dmo-production-option", production.jobonId);
          var text = document.createElement("span");
          text.textContent = production.reference + " — " + production.productionNumber + " (" + production.machine + ")";
          label.appendChild(radio);
          label.appendChild(text);
          list.appendChild(label);
        });
        container.appendChild(list);
        container.hidden = false;
        container.setAttribute("aria-label", "Produções encontradas");
      }

      function loadJobOn(jobonId) {
        api(basePath + "/jobons/" + jobonId, "GET").then(function (response) {
          if (!response.ok || !jobOnFacts) { return; }
          var ficha = response.body.ficha || {};
          if (selectedJobOn) { selectedJobOn.value = ficha.jobOnId || jobonId; }
          if (selectedJobOnVersion) { selectedJobOnVersion.value = ficha.version || 0; }

          jobOnFacts.hidden = false;
          var present = jobOnFacts.querySelector("[data-dmo-bq-present]");
          var missing = jobOnFacts.querySelector("[data-dmo-bq-missing]");
          var bq = (ficha.contexts || []).find(function (context) {
            return context.toolType === "BQ";
          });

          if (present) { present.hidden = !bq; }
          if (missing) { missing.hidden = !!bq; }
          if (associateButton) {
            associateButton.hidden = !!bq;
            associateButton.disabled = !!bq || !selectedTool || !selectedTool.value;
          }
          if (selectedBq) { selectedBq.value = bq ? bq.contextId : ""; }
        });
      }

      function searchProductions() {
        var reference = searchInput ? searchInput.value.trim() : "";
        if (!reference) { return; }

        api(basePath + "/productions?reference=" + encodeURIComponent(reference), "GET").then(function (response) {
          if (!response.ok) {
            renderErrors(root, response.body);
            return;
          }
          renderProductions(response.body.productions || []);
        }).then(function () {
          onAny(container, "[data-dmo-production-option]", "change", function (event) {
            loadJobOn(event.currentTarget.value);
          });
        });
      }

      if (searchButton) {
        searchButton.addEventListener("click", function (event) {
          event.preventDefault();
          searchProductions();
        });
      }

      if (searchInput) {
        searchInput.addEventListener("keydown", function (event) {
          if (event.key === "Enter") {
            event.preventDefault();
            searchProductions();
          }
        });
      }

      /* Route 15 — create the MISSING BQ context ONLY through IJobOnService (explicit human
         confirmation; the bq_contexts row is created by Job On's own code, never here). */
      if (associateButton) {
        associateButton.addEventListener("click", function (event) {
          event.preventDefault();
          var jobonId = selectedJobOn ? selectedJobOn.value : "";
          var toolId = selectedTool ? selectedTool.value : "";
          var version = selectedJobOnVersion ? parseInt(selectedJobOnVersion.value, 10) : 0;

          if (!jobonId || !toolId) { return; }

          associateButton.disabled = true;
          api(basePath + "/jobons/" + jobonId + "/bq-association", "POST", {
            toolId: toolId,
            expectedJobOnVersion: version
          }).then(function (response) {
            if (response.status === 201 && response.body && response.body.bqId) {
              if (selectedBq) { selectedBq.value = response.body.bqId; }
              if (selectedJobOnVersion) { selectedJobOnVersion.value = response.body.version; }
              var present = jobOnFacts ? jobOnFacts.querySelector("[data-dmo-bq-present]") : null;
              var missing = jobOnFacts ? jobOnFacts.querySelector("[data-dmo-bq-missing]") : null;
              if (present) { present.hidden = false; }
              if (missing) { missing.hidden = true; }
              associateButton.hidden = true;
              return;
            }
            associateButton.disabled = false;
            renderMutationFailure(root, response);
          });
        });
      }
    }

    /** The SHARED picker orchestration: BQ candidates only, never auto-selected, contextual
     * create returns the canonical tool_id and restores this origin state. */
    function wireNovoToolPicker(root) {
      var pickerRoot = root.querySelector("[data-dmo-picker]");
      var selectedTool = root.querySelector("[data-dmo-selected-tool]");
      var candidateMap = root.querySelector("[data-dmo-boquilhas-candidate-map]");
      var createPanel = root.querySelector("[data-dmo-boquilhas-tool-create-panel]");
      var createError = root.querySelector("[data-dmo-boquilhas-tool-create-error]");

      function renderCandidates(items) {
        if (!pickerRoot || !candidateMap) { return; }

        candidateMap.textContent = "";
        var list = pickerRoot.querySelector("[data-dmo-picker-candidates]");
        if (!list) { return; }

        list.textContent = "";
        items.forEach(function (item) {
          var candidate = document.createElement("li");
          candidate.className = "dmo-picker__candidate";
          candidate.setAttribute("data-dmo-candidate", item.toolId);
          candidate.setAttribute("data-dmo-candidate-key", item.toolId);
          candidate.setAttribute("aria-selected", "false");

          var facts = document.createElement("span");
          facts.className = "dmo-picker__candidate-facts";
          facts.textContent = item.reference + " — lote " + item.lot +
            (item.processo ? " (" + item.processo + ")" : "") +
            " · máquinas: " + (item.compatibleMachines || []).join(", ");

          var select = document.createElement("button");
          select.type = "button";
          select.className = "dmo-picker__select";
          select.setAttribute("data-dmo-select-candidate", item.toolId);
          select.textContent = "Selecionar";

          candidate.appendChild(facts);
          candidate.appendChild(select);
          list.appendChild(candidate);

          var mapping = document.createElement("span");
          mapping.setAttribute("data-dmo-boquilhas-candidate", item.toolId);
          mapping.setAttribute("data-dmo-boquilhas-candidate-tool", item.toolId);
          mapping.setAttribute("data-dmo-boquilhas-candidate-machines", (item.compatibleMachines || []).join(","));
          candidateMap.appendChild(mapping);
        });
      }

      function prefillMachines(toolId) {
        var mapping = candidateMap.querySelector("[data-dmo-boquilhas-candidate='" + toolId + "']");
        if (!mapping) { return; }

        var machines = (attribute(mapping, "data-dmo-boquilhas-candidate-machines") || "").split(",");
        var boxes = root.querySelectorAll("[data-dmo-opening-machine]");
        boxes.forEach(function (box) {
          box.checked = machines.indexOf(box.value) >= 0;
        });
      }

      function selectTool(toolId) {
        if (selectedTool) { selectedTool.value = toolId; }
        prefillMachines(toolId);
        if (window.dmoFocus && typeof window.dmoFocus.restoreFirst === "function") {
          window.dmoFocus.restoreFirst(pickerRoot);
        }
      }

      function searchTools(query) {
        var url = "/ferramentas/tools?type=BQ&limit=50" +
          (query ? "&query=" + encodeURIComponent(query) : "");
        api(url, "GET").then(function (response) {
          if (!response.ok) {
            renderErrors(root, response.body);
            return;
          }
          renderCandidates(response.body.items || []);
          if (candidateMap && candidateMap.parentElement) {
            candidateMap.parentElement.hidden = (response.body.items || []).length === 0;
          }
        });
      }

      function toolCreatePayload(panel) {
        var machines = panel ? panel.querySelectorAll("[data-dmo-boquilhas-tool-machine]") : [];
        var selectedMachines = [];
        machines.forEach(function (box) { if (box.checked) { selectedMachines.push(box.value); } });
        var quantityField = panel ? panel.querySelector("[data-dmo-boquilhas-tool-field='quantity']") : null;
        var quantity = quantityField && quantityField.value !== "" ? parseInt(quantityField.value, 10) : null;

        return {
          type: "BQ",
          reference: value(panel, "reference"),
          lot: value(panel, "lot"),
          processo: value(panel, "processo") || null,
          quantity: Number.isNaN(quantity) ? null : quantity,
          machines: selectedMachines
        };
      }

      function value(panel, name) {
        var element = panel ? panel.querySelector("[data-dmo-boquilhas-tool-field='" + name + "']") : null;
        return element && typeof element.value === "string" ? element.value : "";
      }

      function submitToolCreate() {
        if (!createPanel) { return; }
        if (createError) {
          createError.textContent = "";
          createError.hidden = true;
        }

        api(TOOL_CREATE_ENDPOINT, "POST", toolCreatePayload(createPanel)).then(function (response) {
          if (response.status === 201 && response.body && response.body.toolId) {
            selectTool(response.body.toolId);
            createPanel.hidden = true;
            return;
          }

          if (createError) {
            createError.textContent =
              (response.body && response.body.reason === "duplicate-identity" && response.body.message)
                ? response.body.message
                : ((response.body && response.body.errors ? response.body.errors.join(", ") : null) ||
                   "Não foi possível criar a ferramenta.");
            createError.hidden = false;
          }
        });
      }

      document.addEventListener(TOOL_PICKER_EVENT, function (event) {
        var detail = event.detail || {};
        var targetPicker = closest(event.target, "[data-dmo-picker]");

        if (!targetPicker || targetPicker !== pickerRoot) { return; }

        if (detail.kind === "search-requested") {
          searchTools(detail.query || "");
        } else if (detail.kind === "candidate-selected") {
          selectTool(detail.candidateKey);
        } else if (detail.kind === "create-requested") {
          if (createPanel) { createPanel.hidden = false; }
        } else if (detail.kind === "cancel-requested") {
          if (createPanel && !createPanel.hidden) {
            /* Cancel restores the origin with no Tool selected and no origin value changed
               (P2-T03 rule); the picker keeps its current state. */
            createPanel.hidden = true;
          }
        }
      }, false);

      on(root, "[data-dmo-boquilhas-tool-create-submit]", "click", function (event) {
        event.preventDefault();
        submitToolCreate();
      });

      on(root, "[data-dmo-boquilhas-tool-create-cancel]", "click", function (event) {
        event.preventDefault();
        if (createPanel) { createPanel.hidden = true; }
      });
    }

    /** Route 7 — create the aggregate (+ machine set + Início) transactionally. */
    function wireNovoCreate(root) {
      var selectedTool = root.querySelector("[data-dmo-selected-tool]");
      var selectedBq = root.querySelector("[data-dmo-selected-bq]");
      var flowStandalone = root.querySelector("[data-dmo-flow='standalone']");

      on(root, "[data-dmo-create-submit]", "click", function (event) {
        event.preventDefault();

        var machines = checkedMachines(root, "[data-dmo-opening-machine]");
        var quantityField = root.querySelector("[data-dmo-opening-field='initialQuantity']");
        var dateField = root.querySelector("[data-dmo-opening-field='openingDate']");
        var utilisationField = root.querySelector("[data-dmo-opening-field='utilisation']");
        var observationsField = root.querySelector("[data-dmo-opening-field='observations']");

        var toolId = selectedTool && selectedTool.value ? selectedTool.value : null;
        var bqId = selectedBq && selectedBq.value ? selectedBq.value : null;
        var standalone = flowStandalone ? flowStandalone.checked : !bqId;
        var payload = {
          bqId: standalone ? null : bqId,
          toolId: standalone ? toolId : null,
          machines: machines,
          initialQuantity: quantityField ? parseInt(quantityField.value, 10) || 0 : 0,
          openingDate: dateField ? dateField.value : "",
          utilisationPercent: utilisationField && utilisationField.value !== ""
            ? parseFloat(utilisationField.value)
            : null,
          observations: observationsField && observationsField.value !== ""
            ? observationsField.value
            : null
        };

        var button = event.currentTarget;
        button.disabled = true;
        api(basePath + "/aggregates", "POST", payload).then(function (response) {
          button.disabled = false;
          if (response.status === 201 && response.body && response.body.boquilhasId) {
            window.location.assign(basePath + "?boquilhasId=" + response.body.boquilhasId);
            return;
          }
          renderMutationFailure(root, response);
        });
      });
    }

    /* =======================================================================================
     * wiring
     * ====================================================================================== */

    function wire(root) {
      if (!root) { return; }

      var surface = attribute(root, "data-dmo-boquilhas-surface");

      if (surface === "novo") {
        wireNovo(root);
        return;
      }

      wireOpenRoutes(root);

      if (surface === "historico") {
        wireHistorico(root);
        return;
      }

      // index — Registo
      wireMovementLedger(root);
      wireRepairerResolution(root);

      on(root, "[data-dmo-movement-submit]", "click", function (event) {
        event.preventDefault();
        appendMovement(root, event.currentTarget);
      });

      on(root, "[data-dmo-edit-submit]", "click", function (event) {
        event.preventDefault();
        editMovement(root, event.currentTarget);
      });

      on(root, "[data-dmo-action='close']", "click", function (event) {
        event.preventDefault();
        closeAggregate(root, event.currentTarget);
      });

      on(root, "[data-dmo-action='reopen']", "click", function (event) {
        event.preventDefault();
        reopenAggregate(root, event.currentTarget);
      });

      on(root, "[data-dmo-action='opening']", "click", function (event) {
        event.preventDefault();
        updateOpeningFacts(root, event.currentTarget);
      });
    }

    function initialize() {
      var roots = document.querySelectorAll("[data-dmo-boquilhas-root]");
      roots.forEach(wire);
    }

    /* The accepted readyState branch (the D1/D2 harness drives the REAL adapter): when the DOM is
       already complete the adapter initializes immediately; otherwise on DOMContentLoaded. */
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", initialize);
    } else {
      initialize();
    }

    return {
      name: "dmoBoquilhas",
      wire: wire,
      renderConflict: renderConflict,
      renderErrors: renderErrors
    };
  })();
}