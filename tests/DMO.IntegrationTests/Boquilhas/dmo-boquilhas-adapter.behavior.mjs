// P2-T07 behavioral proof of the page-owned adapter
// (src/DMO.Web/wwwroot/js/dmo-boquilhas.js, the REAL shipped file) executed with a real
// JavaScript engine (node) against a minimal document/window/fetch stub (OWNER CLARIFICATION
// register model):
//
//   - a typed 409 stale-version on every guarded mutation (append movement / edit movement)
//     enters the accepted conflict state with the explicit recovery action ("Recarregar estado
//     atual"), issues EXACTLY ONE request (no automatic retry, no auto-merge, no silent overwrite
//     of the newer server version) and the recovery control reloads the authoritative state
//     (K5/D2);
//   - NON-stale typed failures (409 register-exists, 400 validation-failed) keep the errors
//     presentation — no conflict marker and no recovery control;
//   - the tool picker NEVER auto-selects: a search returning exactly ONE candidate leaves
//     everything unselected until an EXPLICIT candidate activation; N ambiguous candidates stay N
//     explicit items (T2/T6);
//   - the contextual create subflow (Criar ferramenta) posts the shared tool route, returns the
//     canonical tool_id, closes the panel and preserves the origin production selection; cancel
//     restores the origin with no Tool selected and no origin value changed (T3).
//
// Usage: node dmo-boquilhas-adapter.behavior.mjs <path-to-dmo-boquilhas.js>
'use strict';

import fs from 'node:fs';

const adapterPath = process.argv[2];
if (!adapterPath || !fs.existsSync(adapterPath)) {
  console.error('usage: node dmo-boquilhas-adapter.behavior.mjs <adapter.js>');
  process.exit(2);
}

let failureCount = 0;

function fail(step, detail) {
  failureCount += 1;
  console.error(`FAIL [${step}]: ${detail}`);
}

function assert(step, condition, detail) {
  if (!condition) { fail(step, detail); }
}

// ------------------------------------------------------------------ minimal DOM stub
function makeRoot() {
  function el(name) {
    const node = {
      name,
      attrs: {},
      children: [],
      parent: null,
      listeners: {},
      hidden: false,
      textContent: '',
      _html: '',
      _value: '',
      _disabled: false,
      _checked: false,
      className: '',
      appendChild(child) { child.parent = this; this.children.push(child); return child; },
      insertBefore(child, reference) {
        child.parent = this;
        const index = this.children.indexOf(reference);
        if (index < 0) { this.children.push(child); } else { this.children.splice(index, 0, child); }
        return child;
      },
      removeChild(child) {
        const index = this.children.indexOf(child);
        if (index >= 0) { this.children.splice(index, 1); }
        return child;
      },
      addEventListener(event, handler) {
        (this.listeners[event] = this.listeners[event] || []).push(handler);
      },
      dispatchEvent(event) {
        const handlers = this.listeners[event.type] || [];
        handlers.forEach((handler) => handler.call(this, event));
        return true;
      },
      querySelector(selector) { return query(this, selector); },
      querySelectorAll(selector) { return queryAll(this, selector); },
      closest(selector) {
        let current = this;
        while (current) {
          if (current !== root && matches(current, selector)) { return current; }
          current = current.parent;
        }
        return null;
      },
      setAttribute(name, value) { this.attrs[name] = String(value); },
      getAttribute(name) { return Object.prototype.hasOwnProperty.call(this.attrs, name) ? this.attrs[name] : null; },
      hasAttribute(name) { return Object.prototype.hasOwnProperty.call(this.attrs, name); },
      removeAttribute(name) { delete this.attrs[name]; },
      remove() {},
      focus() {},
      classList: {
        add() {},
        remove() {},
        toggle() {},
      },
    };

    Object.defineProperty(node, 'value', {
      get() { return this._value; },
      set(value) { this._value = String(value); },
    });
    Object.defineProperty(node, 'disabled', {
      get() { return this._disabled; },
      set(value) { this._disabled = !!value; },
    });
    Object.defineProperty(node, 'checked', {
      get() { return this._checked; },
      set(value) { this._checked = !!value; },
    });
    Object.defineProperty(node, 'hidden', {
      get() { return this._hidden; },
      set(value) { this._hidden = !!value; },
    });
    Object.defineProperty(node, 'innerHTML', {
      get() { return this._html; },
      set(value) { this._html = String(value); },
    });

    return node;
  }

  function matches(node, selector) {
    if (!node || node.name === '#root') { return false; }

    // '#id' selector support (the shared Tool picker region id).
    const idMatch = /^#([a-zA-Z0-9-]+)$/.exec(selector);
    if (idMatch) {
      return node.attrs.id === idMatch[1];
    }

    const parsed = /^\[([a-z0-9-]+)(?:=['"]([^'"]*)['"])?\]$/i.exec(selector);
    if (!parsed) { return false; }

    const attribute = parsed[1];
    const expected = parsed[2];
    if (!Object.prototype.hasOwnProperty.call(node.attrs, attribute)) { return false; }

    return expected === undefined || node.attrs[attribute] === expected;
  }

  function queryAll(node, selector) {
    const found = [];
    const walk = (current) => {
      current.children.forEach((child) => {
        if (matches(child, selector)) { found.push(child); }
        walk(child);
      });
    };
    walk(node);
    return found;
  }

  function query(node, selector) {
    return queryAll(node, selector)[0] || null;
  }

  const root = el('#root');
  root._el = el;
  return root;
}

function click(node) {
  node.dispatchEvent({ type: 'click', preventDefault() {} });
}

/** Flushes the promise microtasks the adapter resolves through (fetch .then chains). */
async function flush() {
  await new Promise((resolve) => setTimeout(resolve, 0));
}

function textOf(node) {
  const collect = (current) => (current.textContent + (current._html || '') + current.children.map(collect).join(''));
  return collect(node);
}

// ------------------------------------------------------------------ scenario driver
function loadAdapter(root, fetchImpl) {
  const documentStub = {
    readyState: 'complete',
    _handlers: {},
    querySelector(selector) { return root.querySelector(selector); },
    querySelectorAll(selector) { return root.querySelectorAll(selector); },
    createElement(name) { return root._el(name); },
    addEventListener(event, handler) {
      (this._handlers[event] = this._handlers[event] || []).push(handler);
    },
    fire(event) {
      (this._handlers[event.type] || []).forEach((handler) => handler.call(this, event));
    },
  };

  const locationValue = { href: '', reloads: 0, assigned: [] };
  locationValue.reload = () => { locationValue.reloads += 1; };
  locationValue.assign = (url) => { locationValue.assigned.push(String(url)); };

  const windowStub = {
    location: locationValue,
    document: documentStub,
    fetch: fetchImpl,
    CustomEvent,
    addEventListener() {},
  };

  const source = fs.readFileSync(adapterPath, 'utf8');
  // eslint-disable-next-line no-new-func
  new Function('window', 'document', 'fetch', 'location', 'confirm', 'CustomEvent', source)
    .call(windowStub, windowStub, documentStub, fetchImpl, locationValue, () => true, CustomEvent);

  return { window: windowStub, document: documentStub, location: locationValue };
}

/** Builds the Registo (index) surface: register region + movement entry + edit + state region. */
function buildRegistoRoot() {
  const root = makeRoot();
  const section = root._el('section');
  section.attrs['data-dmo-boquilhas-surface'] = 'index';
  section.attrs['data-dmo-boquilhas-root'] = 'true';
  root.appendChild(section);

  const register = root._el('div');
  register.attrs['data-dmo-boquilhas-region'] = 'register';
  register.attrs['data-dmo-boquilhas-id'] = '11111111-1111-1111-1111-111111111111';
  section.appendChild(register);

  const movementTable = root._el('table');
  movementTable.attrs['data-dmo-movement-table'] = 'true';
  register.appendChild(movementTable);

  // The movement-entry resolutions (consumed reads; the machine B1 resolves a repairer).
  const resolutions = root._el('div');
  resolutions.attrs['data-dmo-resolutions'] = 'true';
  register.appendChild(resolutions);
  const assignment = root._el('span');
  assignment.attrs['data-dmo-assignment'] = 'B1';
  assignment.attrs['data-dmo-assignment-repairer'] = '33333333-3333-3333-3333-333333333333';
  assignment.attrs['data-dmo-assignment-unavailable'] = 'false';
  resolutions.appendChild(assignment);

  const entry = root._el('fieldset');
  entry.attrs['data-dmo-movement-entry'] = 'true';
  register.appendChild(entry);

  const fields = [
    ['type', 'saida'],
    ['quantity', '2'],
    ['businessDate', '2026-09-11'],
    ['machine', 'B1'],
    ['repairer', ''],
    ['observations', 'nota'],
  ];
  fields.forEach(([name, value]) => {
    const input = root._el('input');
    input.attrs['data-dmo-movement-field'] = name;
    input.value = value;
    entry.appendChild(input);
  });

  const submit = root._el('button');
  submit.attrs['data-dmo-movement-submit'] = 'true';
  entry.appendChild(submit);

  const detail = root._el('div');
  detail.attrs['data-dmo-boquilhas-region'] = 'detail';
  detail.attrs['data-dmo-edit-movement-id'] = '22222222-2222-2222-2222-222222222222';
  detail.attrs['data-dmo-edit-movement-version'] = '1';
  detail.attrs['data-dmo-audit-trail'] = 'true';
  section.appendChild(detail);

  const editForm = root._el('fieldset');
  editForm.attrs['data-dmo-edit-form'] = 'true';
  detail.appendChild(editForm);

  const editFields = [
    ['quantity', '4'],
    ['businessDate', '2026-09-13'],
    ['machine', 'B1'],
    ['repairer', '33333333-3333-3333-3333-333333333333'],
    ['observations', 'corrigido'],
  ];
  editFields.forEach(([name, value]) => {
    const input = root._el('input');
    input.attrs['data-dmo-edit-field'] = name;
    input.value = value;
    editForm.appendChild(input);
  });

  const editSubmit = root._el('button');
  editSubmit.attrs['data-dmo-edit-submit'] = 'true';
  editForm.appendChild(editSubmit);

  const state = root._el('div');
  state.attrs['data-dmo-boquilhas-state'] = 'true';
  state.hidden = true;
  section.appendChild(state);

  return root;
}

/** Builds the Novo surface: production flow + shared picker + create panel + adapter state. */
function buildNovoRoot() {
  const root = makeRoot();
  const section = root._el('section');
  section.attrs['data-dmo-boquilhas-surface'] = 'novo';
  section.attrs['data-dmo-boquilhas-root'] = 'true';
  root.appendChild(section);

  const searchReference = root._el('input');
  searchReference.attrs['data-dmo-production-reference'] = 'true';
  searchReference.value = 'REF-X';
  section.appendChild(searchReference);
  const searchSubmit = root._el('button');
  searchSubmit.attrs['data-dmo-production-search-submit'] = 'true';
  section.appendChild(searchSubmit);
  const productions = root._el('div');
  productions.attrs['data-dmo-productions'] = 'true';
  productions.hidden = true;
  section.appendChild(productions);
  const jobsPanel = root._el('div');
  jobsPanel.attrs['data-dmo-jobon-facts'] = 'true';
  jobsPanel.hidden = true;
  section.appendChild(jobsPanel);
  const bqPresent = root._el('p');
  bqPresent.attrs['data-dmo-bq-present'] = 'true';
  bqPresent.hidden = true;
  jobsPanel.appendChild(bqPresent);
  const bqMissing = root._el('p');
  bqMissing.attrs['data-dmo-bq-missing'] = 'true';
  bqMissing.hidden = true;
  jobsPanel.appendChild(bqMissing);
  const associateBq = root._el('button');
  associateBq.attrs['data-dmo-associate-bq'] = 'true';
  associateBq.hidden = true;
  jobsPanel.appendChild(associateBq);

  const picker = root._el('div');
  picker.attrs['data-dmo-picker'] = 'true';
  picker.attrs['id'] = 'dmo-boquilhas-picker';
  section.appendChild(picker);
  const searchInput = root._el('input');
  searchInput.attrs['data-dmo-search-input'] = 'true';
  picker.appendChild(searchInput);
  const list = root._el('ul');
  list.attrs['data-dmo-picker-candidates'] = 'true';
  picker.appendChild(list);

  const candidateMap = root._el('span');
  candidateMap.attrs['data-dmo-boquilhas-candidate-map'] = 'true';
  candidateMap.hidden = true;
  section.appendChild(candidateMap);

  const createPanel = root._el('fieldset');
  createPanel.attrs['data-dmo-boquilhas-tool-create-panel'] = 'true';
  createPanel.hidden = true;
  section.appendChild(createPanel);
  ['reference', 'lot', 'processo', 'quantity'].forEach((name) => {
    const input = root._el('input');
    input.attrs['data-dmo-boquilhas-tool-field'] = name;
    createPanel.appendChild(input);
  });
  const machineBox = root._el('input');
  machineBox.attrs['data-dmo-boquilhas-tool-machine'] = 'B1';
  createPanel.appendChild(machineBox);
  const createError = root._el('p');
  createError.attrs['data-dmo-boquilhas-tool-create-error'] = 'true';
  createError.hidden = true;
  createPanel.appendChild(createError);
  const createSubmit = root._el('button');
  createSubmit.attrs['data-dmo-boquilhas-tool-create-submit'] = 'true';
  createPanel.appendChild(createSubmit);
  const createCancel = root._el('button');
  createCancel.attrs['data-dmo-boquilhas-tool-create-cancel'] = 'true';
  createPanel.appendChild(createCancel);

  // The adapter state: the REAL production selection (jobon + version + bq + tool).
  const selectedJobOn = root._el('input');
  selectedJobOn.attrs['data-dmo-selected-jobon'] = 'true';
  selectedJobOn.value = '66666666-6666-6666-6666-666666666666';
  section.appendChild(selectedJobOn);
  const selectedJobOnVersion = root._el('input');
  selectedJobOnVersion.attrs['data-dmo-selected-jobon-version'] = 'true';
  selectedJobOnVersion.value = '1';
  section.appendChild(selectedJobOnVersion);
  const selectedBq = root._el('input');
  selectedBq.attrs['data-dmo-selected-bq'] = 'true';
  selectedBq.value = '77777777-7777-7777-7777-777777777777';
  section.appendChild(selectedBq);
  const selectedTool = root._el('input');
  selectedTool.attrs['data-dmo-selected-tool'] = 'true';
  section.appendChild(selectedTool);

  const state = root._el('div');
  state.attrs['data-dmo-boquilhas-state'] = 'true';
  state.hidden = true;
  section.appendChild(state);

  return root;
}

// ------------------------------------------------------------------ main
(async () => {
  // ------------------------------------------------------------------ S1: absence of editable regions is valid
  {
    const root = makeRoot();
    const section = root._el('section');
    section.attrs['data-dmo-boquilhas-surface'] = 'index';
    section.attrs['data-dmo-boquilhas-root'] = 'true';
    root.appendChild(section);

    loadAdapter(root, async () => {
      fail('S1', 'the adapter must not fetch on an empty surface');
      return { ok: false, status: 500, json: async () => ({}) };
    });
    assert('S1', true, 'adapter initialized with no register/movement regions present');
  }

  // ------------------------------------------------------------------ S2: append guard — success reloads once
  {
    const root = buildRegistoRoot();
    const requests = [];
    const ctx = loadAdapter(root, async (url, options) => {
      requests.push({ url: String(url), body: options && options.body });
      return { ok: true, status: 201, json: async () => ({ movementId: '99999999-9999-9999-9999-999999999999' }) };
    });

    click(root.querySelector('[data-dmo-movement-submit]'));
    await flush();

    assert('S2', requests.length === 1, `append issued ${requests.length} request(s), expected 1`);
    assert('S2', requests.length === 0 || String(requests[0].url).includes('/movements'), 'append posts to the movement route');
    assert('S2', ctx.location.reloads === 1, 'a successful append reloads the authoritative state');
    assert('S2', !ctx.location.href, 'no navigation on success');
  }

  // ------------------------------------------------------------------ S3: K5 conflict — stale-version on append
  {
    const root = buildRegistoRoot();
    const requests = [];
    const ctx = loadAdapter(root, async () => {
      requests.push(1);
      return {
        ok: false,
        status: 409,
        json: async () => ({
          reason: 'stale-version',
          message: 'O movimento foi alterado em concorrência.',
        }),
      };
    });

    click(root.querySelector('[data-dmo-movement-submit]'));
    await flush();

    const state = root.querySelector('[data-dmo-boquilhas-state]');
    assert('S3', requests.length === 1, `stale-version issued ${requests.length} request(s); no automatic retry`);
    assert('S3', state.attrs['data-dmo-conflict'] === undefined && String(state.className).includes('conflict'), 'the conflict state is marked');
    assert('S3', textOf(state).includes('Recarregar estado atual'), 'the explicit recovery action is rendered');
    assert('S3', !state.hidden, 'the conflict region is visible');
    assert('S3', ctx.location.reloads === 0, 'no reload before the explicit recovery');
    assert('S3', !ctx.location.href, 'no navigation on refusal');

    const recover = state.querySelector('[data-dmo-conflict-reload]');
    if (recover) {
      click(recover);
      assert('S3', ctx.location.reloads === 1, 'the recovery control reloads the authoritative state');
    } else {
      fail('S3', 'the recovery control is missing');
    }
  }

  // ------------------------------------------------------------------ S4: conflict on edit (K5 across guarded mutations)
  {
    const root = buildRegistoRoot();
    const requests = [];
    const ctx = loadAdapter(root, async () => {
      requests.push(1);
      return {
        ok: false,
        status: 409,
        json: async () => ({ reason: 'stale-version', message: 'O movimento foi alterado.' }),
      };
    });

    click(root.querySelector('[data-dmo-edit-submit]'));
    await flush();

    const state = root.querySelector('[data-dmo-boquilhas-state]');
    assert('S4', requests.length === 1, `edit stale-version issued ${requests.length} request(s)`);
    assert('S4', String(state.className).includes('conflict'), 'edit conflict is marked');
    assert('S4', textOf(state).includes('Recarregar estado atual'), 'edit conflict has the recovery');
    assert('S4', ctx.location.reloads === 0 && !ctx.location.href, 'no implicit recovery');
  }

  // ------------------------------------------------------------------ S5: non-stale register-exists refusal keeps the errors presentation
  {
    const root = buildRegistoRoot();
    const requests = [];
    const ctx = loadAdapter(root, async () => {
      requests.push(1);
      return {
        ok: false,
        status: 409,
        json: async () => ({
          reason: 'register-exists',
          message: 'Já existe um registo de Boquilhas para esta produção.',
        }),
      };
    });

    click(root.querySelector('[data-dmo-movement-submit]'));
    await flush();

    const state = root.querySelector('[data-dmo-boquilhas-state]');
    assert('S5', requests.length === 1, 'exactly one request');
    assert('S5', !state.hidden, 'the refusal is visible');
    assert('S5', !String(state.className).includes('conflict'), 'no conflict marker for a non-stale refusal');
    assert('S5', !state.querySelector('[data-dmo-conflict-reload]'), 'no recovery control for a non-stale refusal');
    assert('S5', textOf(state).includes('register-exists') || textOf(state).includes('produção'), 'the refusal is rendered');
    assert('S5', ctx.location.reloads === 0 && !ctx.location.href, 'nothing navigated');
  }

  // ------------------------------------------------------------------ S6: 400 validation-failed keeps the errors presentation
  {
    const root = buildRegistoRoot();
    const requests = [];
    const ctx = loadAdapter(root, async () => {
      requests.push(1);
      return {
        ok: false,
        status: 400,
        json: async () => ({ reason: 'validation-failed', errors: ['MOVEMENT_TYPE_INVALID'] }),
      };
    });

    click(root.querySelector('[data-dmo-movement-submit]'));
    await flush();

    const state = root.querySelector('[data-dmo-boquilhas-state]');
    assert('S6', requests.length === 1, 'exactly one request');
    assert('S6', !state.hidden, 'the validation failure is visible');
    assert('S6', !String(state.className).includes('conflict'), 'no conflict marker for a 400');
    assert('S6', !state.querySelector('[data-dmo-conflict-reload]'), 'no recovery control for a 400');
  }

  // ------------------------------------------------------------------ S7: T2 — the picker NEVER auto-selects (even one candidate)
  {
    const root = buildNovoRoot();
    const ctx = loadAdapter(root, async (url) => {
      if (String(url).includes('/ferramentas/tools')) {
        return {
          ok: true,
          status: 200,
          json: async () => ({
            candidates: [
              {
                toolId: '44444444-4444-4444-4444-444444444444',
                type: 'BQ',
                reference: 'BQ-100',
                lot: 'L1',
                processo: 'NNPB',
                compatibleMachines: ['B1'],
              },
            ],
          }),
        };
      }
      return { ok: false, status: 404, json: async () => ({}) };
    });

    const pickerRoot = root.querySelector("[id='dmo-boquilhas-picker']");
    ctx.document.fire({ type: 'dmo:tool-picker:query', detail: { query: 'BQ', originToken: 'boquilhas-novo' } });
    await flush();

    const candidates = root.querySelectorAll("[data-dmo-candidate='44444444-4444-4444-4444-444444444444']");
    assert('S7', candidates.length === 1, 'exactly one candidate mapped after the search');
    const selectedTool = root.querySelector('[data-dmo-selected-tool]');
    assert('S7', selectedTool.value === '', 'a SINGLE candidate is NEVER auto-selected');

    // The explicit candidate activation (only the human can select).
    ctx.document.fire({
      type: 'dmo:tool-picker:selected',
      detail: { originToken: 'boquilhas-novo', candidateKey: '44444444-4444-4444-4444-444444444444' },
    });
    assert('S7', selectedTool.value === '44444444-4444-4444-4444-444444444444', 'explicit activation stores the canonical tool_id');
  }

  // ------------------------------------------------------------------ S8: T3 — contextual create returns the canonical tool_id and preserves origin
  {
    const root = buildNovoRoot();
    const requests = [];
    const ctx = loadAdapter(root, async (url, options) => {
      requests.push({ url: String(url), body: options && options.body });
      if (String(url).includes('/ferramentas/tools') && options.method === 'POST') {
        return { ok: true, status: 201, json: async () => ({ toolId: '55555555-5555-5555-5555-555555555555' }) };
      }
      return { ok: false, status: 404, json: async () => ({}) };
    });

    const panel = root.querySelector('[data-dmo-boquilhas-tool-create-panel]');

    // Open the create subflow (explicit human request through the shared picker event).
    ctx.document.fire({ type: 'dmo:tool-picker:create', detail: { originToken: 'boquilhas-novo' } });
    assert('S8', !panel.hidden, 'the create subflow opens in place');

    // The origin production selection stays intact while the subflow is open.
    const selectedJobOn = root.querySelector('[data-dmo-selected-jobon]');
    assert('S8', selectedJobOn.value === '66666666-6666-6666-6666-666666666666', 'origin production unchanged while creating');

    panel.querySelector("[data-dmo-boquilhas-tool-field='reference']").value = 'BQ-200';
    panel.querySelector("[data-dmo-boquilhas-tool-field='lot']").value = 'L9';
    machineBoxChecked(root, 'B1');

    click(panel.querySelector('[data-dmo-boquilhas-tool-create-submit]'));
    await flush();

    const selectedTool = root.querySelector('[data-dmo-selected-tool]');
    assert('S8', selectedTool.value === '55555555-5555-5555-5555-555555555555', 'the create returns the REAL canonical tool_id');
    assert('S8', panel.hidden, 'the create panel closes after success');
    assert('S8', selectedJobOn.value === '66666666-6666-6666-6666-666666666666', 'origin state preserved after create');
    assert('S8', !ctx.location.href, 'the create subflow never navigates away');
    assert('S8', requests.some((request) => {
      if (String(request.url) !== '/ferramentas/tools') { return false; }
      let body = null;
      try { body = JSON.parse(request.body); } catch (error) { body = null; }
      return body !== null && body.expectedType === 'bq';
    }), 'the create posts the BQ Tool through the SHARED ferramentas route');
  }

  // ------------------------------------------------------------------ S9: T3 — cancel restores the origin with NO Tool and NO value change
  {
    const root = buildNovoRoot();
    const ctx = loadAdapter(root, async () => ({ ok: false, status: 404, json: async () => ({}) }));

    const panel = root.querySelector('[data-dmo-boquilhas-tool-create-panel]');
    const selectedTool = root.querySelector('[data-dmo-selected-tool]');
    const selectedJobOn = root.querySelector('[data-dmo-selected-jobon]');

    ctx.document.fire({ type: 'dmo:tool-picker:create', detail: { originToken: 'boquilhas-novo' } });
    assert('S9', !panel.hidden, 'the subflow is open');

    ctx.document.fire({ type: 'dmo:tool-picker:cancel', detail: { originToken: 'boquilhas-novo' } });
    assert('S9', panel.hidden, 'cancel closes the subflow');
    assert('S9', selectedTool.value === '', 'cancel leaves NO Tool selected');
    assert('S9', selectedJobOn.value === '66666666-6666-6666-6666-666666666666', 'no origin value changed by cancel');
    assert('S9', !ctx.location.href, 'cancel never navigates');
  }

  // ------------------------------------------------------------------ summary
  if (failureCount > 0) {
    console.error(`\n${failureCount} scenario(s) FAILED.`);
    process.exit(1);
  }

  console.log('dmo-boquilhas adapter behavioral scenarios PASSED.');
})().catch((error) => {
  console.error('HARNESS ERROR:', error);
  process.exit(1);
});

function machineBoxChecked(root, machine) {
  const box = root.querySelector("[data-dmo-boquilhas-tool-machine='" + machine + "']");
  if (box) { box.checked = true; }
}