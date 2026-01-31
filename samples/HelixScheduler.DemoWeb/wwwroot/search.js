const state = {
  apiBaseUrl: "",
  weekStart: null,
  resourceTypes: [],
  resources: [],
  resourceMap: new Map(),
  propertyDefinitions: new Map(),
  propertyNodes: [],
  propertyMap: new Map(),
  propertyChildren: new Map(),
  typePropertyMap: new Map(),
  requirements: [],
  rules: [],
  busyEvents: [],
  selectedRuleId: null,
  ancestorFilters: []
};

const requirementsEl = document.getElementById("requirements");
const weekLabelEl = document.getElementById("weekLabel");
  const calendarEl = document.getElementById("calendar");
  const busyCalendarEl = document.getElementById("busyCalendar");
  const rulesEl = document.getElementById("rules");
  const busyEl = document.getElementById("busy");
  const explainEl = document.getElementById("explain");
  const summaryEl = document.getElementById("searchSummary");
  const searchEmptyStateEl = document.getElementById("searchEmptyState");
const ruleImpactEl = document.getElementById("ruleImpactInfo");
const computeStatusEl = document.getElementById("computeStatus");
const busyNoteEl = document.getElementById("busyNote");
const errorEl = document.getElementById("searchError");
const apiStatusEl = document.getElementById("apiStatus");
const apiStatusMessageEl = document.getElementById("apiStatusMessage");
const apiRetryEl = document.getElementById("apiRetry");
  const payloadPreviewEl = document.getElementById("payloadPreview");
  const copyPayloadBtn = document.getElementById("copyPayload");
const ancestorFilterTypeEl = document.getElementById("ancestorFilterType");
const ancestorFilterDefinitionEl = document.getElementById("ancestorFilterDefinition");
const ancestorFilterPropertyEl = document.getElementById("ancestorFilterProperty");
const ancestorFilterIncludeDescendantsEl = document.getElementById("ancestorFilterIncludeDescendants");
const ancestorFilterMatchModeEl = document.getElementById("ancestorFilterMatchMode");
const ancestorFilterScopeEl = document.getElementById("ancestorFilterScope");
const ancestorFilterMatchAllEl = document.getElementById("ancestorFilterMatchAll");
const ancestorFilterChipsEl = document.getElementById("ancestorFilterChips");
const ancestorTypesWrapEl = document.getElementById("ancestorTypesWrap");
const ancestorFiltersWrapEl = document.getElementById("ancestorFiltersWrap");
const ancestorTypesAllEl = document.getElementById("ancestorTypesAll");
const ancestorTypesSpecificEl = document.getElementById("ancestorTypesSpecific");
const slotDurationWrapEl = document.getElementById("slotDurationWrap");
const includeRemainderWrapEl = document.getElementById("includeRemainderWrap");
let apiCheckInFlight = false;

  document.getElementById("prevWeek").addEventListener("click", () => shiftWeek(-7));
  document.getElementById("nextWeek").addEventListener("click", () => shiftWeek(7));
  document.getElementById("addRequirement").addEventListener("click", () => addRequirement());
  document.getElementById("computeSearch").addEventListener("click", () => computeSearch());
  document.getElementById("resetSearch").addEventListener("click", () => resetBuilder());
  apiRetryEl.addEventListener("click", () => retryApiReady());
  const showIdsToggle = document.getElementById("toggleShowIds");
  if (showIdsToggle) {
    showIdsToggle.addEventListener("change", () => {
      refreshSearchUiFromState();
    });
  }
  document.body.addEventListener("click", event => {
    const link = event.target.closest(".empty-actions a");
    if (!link || !link.getAttribute("href")?.startsWith("#")) {
      return;
    }
    event.preventDefault();
    const target = document.querySelector(link.getAttribute("href"));
    if (target) {
      target.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  });
const addAncestorFilterBtn = document.getElementById("addAncestorFilter");
const clearAncestorFiltersBtn = document.getElementById("clearAncestorFilters");
if (addAncestorFilterBtn) {
  addAncestorFilterBtn.addEventListener("click", () => addAncestorFilter());
}
if (clearAncestorFiltersBtn) {
  clearAncestorFiltersBtn.addEventListener("click", () => clearAncestorFilters());
}
if (ancestorFilterTypeEl) {
  ancestorFilterTypeEl.addEventListener("change", () => {
    renderAncestorFilterDefinitions();
    renderAncestorFilterProperties();
  });
}
if (ancestorFilterDefinitionEl) {
  ancestorFilterDefinitionEl.addEventListener("change", () => {
    renderAncestorFilterProperties();
  });
}
if (copyPayloadBtn) {
  copyPayloadBtn.addEventListener("click", () => copyPayload());
}
document.getElementById("toggleAncestors").addEventListener("change", () => {
  updateAncestorTypesVisibility();
  renderPayloadPreview();
});
document.getElementById("toggleSlotDuration").addEventListener("change", () => {
  updateSlotDurationVisibility();
  renderPayloadPreview();
});
document.querySelectorAll("input[name=\"ancestorRelationType\"]").forEach(input => {
  input.addEventListener("change", () => renderPayloadPreview());
});
if (ancestorTypesAllEl) {
  ancestorTypesAllEl.addEventListener("change", () => {
    updateAncestorTypesState();
    renderPayloadPreview();
  });
}

function createElement(tag, className, text) {
  const el = document.createElement(tag);
  if (className) {
    el.className = className;
  }
  if (text !== undefined) {
    el.textContent = text;
  }
  return el;
}

function refreshSearchUiFromState() {
  renderRequirements();
  const payloadInfo = buildSearchPayload();
  if (payloadInfo.payload) {
    renderSearchSummary(payloadInfo.requirements, payloadInfo.payload);
  }
  renderRules(state.rules || []);
  renderBusy(state.busyEvents || []);
  renderBusyCalendar(state.busyEvents || []);
}



function isoDate(date) {
  return date.toISOString().slice(0, 10);
}

function getWeekStart(date) {
  const day = date.getUTCDay();
  const diff = (day + 6) % 7;
  const monday = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
  monday.setUTCDate(monday.getUTCDate() - diff);
  return monday;
}

function shiftWeek(deltaDays) {
  state.weekStart.setUTCDate(state.weekStart.getUTCDate() + deltaDays);
  updateWeekLabel();
  if (state.requirements.length > 0) {
    computeSearch();
  }
}

function updateWeekLabel() {
  const end = new Date(state.weekStart);
  end.setUTCDate(end.getUTCDate() + 6);
  weekLabelEl.textContent = `${isoDate(state.weekStart)} -> ${isoDate(end)}`;
}

async function loadConfig() {
  const res = await fetch("/config");
  const data = await res.json();
  state.apiBaseUrl = data.apiBaseUrl.replace(/\/$/, "");
}

async function retryApiReady() {
  if (apiCheckInFlight) {
    return;
  }
  setApiStatus("waiting", "Checking API...");
  apiRetryEl.disabled = true;
  const ok = await checkApiOnce();
  if (ok) {
    await bootstrapAfterApiReady();
  } else {
    setApiStatus("error", "API not reachable yet. Start WebApi and retry.");
  }
  apiRetryEl.disabled = false;
}

async function loadCatalogs() {
  const [typesRes, resourcesRes, propertiesRes] = await Promise.all([
    fetch(`${state.apiBaseUrl}/api/catalog/resource-types`),
    fetch(`${state.apiBaseUrl}/api/catalog/resources?onlySchedulable=false`),
    fetch(`${state.apiBaseUrl}/api/catalog/properties`)
  ]);

  state.resourceTypes = await typesRes.json();
  state.resources = await resourcesRes.json();
  state.resourceMap = new Map(state.resources.map(r => [r.id, r]));

  const propertySchema = await propertiesRes.json();
  state.propertyNodes = propertySchema.nodes || [];
  state.propertyDefinitions = new Map((propertySchema.definitions || []).map(def => [def.id, def]));
  state.propertyMap = new Map(state.propertyNodes.map(node => [node.id, node]));
  state.propertyChildren = buildPropertyChildrenMap(state.propertyNodes);
  state.typePropertyMap = buildTypePropertyMap(propertySchema.typeMappings || []);

  renderAncestorFilterTypes();
  renderAncestorFilterDefinitions();
  renderAncestorFilterProperties();
  renderAncestorFilterChips();
}

function buildPropertyChildrenMap(properties) {
  const map = new Map();
  properties.forEach(property => {
    if (property.parentId == null) return;
    if (!map.has(property.parentId)) {
      map.set(property.parentId, []);
    }
    map.get(property.parentId).push(property.id);
  });
  return map;
}

function appendPropertyTreeOptions(container, definitionId, selectedNodeId) {
  const rootNode = getDefinitionRootNode(definitionId);
  if (!rootNode) {
    return;
  }

  const anyOption = document.createElement("option");
  anyOption.value = rootNode.id;
  anyOption.textContent = `Any ${DemoFormat.formatIdLabel(rootNode.label, rootNode.id)}`;
  if (selectedNodeId === rootNode.id) {
    anyOption.selected = true;
  }
  container.appendChild(anyOption);

  appendPropertyChildren(container, definitionId, rootNode.id, 1, selectedNodeId);
}

function appendPropertyChildren(container, definitionId, parentId, depth, selectedNodeId) {
  const children = (state.propertyChildren.get(parentId) || [])
    .map(id => state.propertyMap.get(id))
    .filter(node => node && node.definitionId === definitionId)
    .sort((a, b) => a.label.localeCompare(b.label));

  const prefix = `${"-".repeat(Math.max(1, depth) * 2)}>`;
  children.forEach(child => {
    const option = document.createElement("option");
    option.value = child.id;
    option.textContent = `${prefix}${DemoFormat.formatIdLabel(child.label, child.id)}`;
    if (selectedNodeId === child.id) {
      option.selected = true;
    }
    container.appendChild(option);
    appendPropertyChildren(container, definitionId, child.id, depth + 1, selectedNodeId);
  });
}

function getDefinitionRootNode(definitionId) {
  return state.propertyNodes.find(
    node => node.definitionId === definitionId && node.parentId == null
  );
}

function buildTypePropertyMap(typeMappings) {
  const map = new Map();
  typeMappings.forEach(link => {
    if (!map.has(link.resourceTypeId)) {
      map.set(link.resourceTypeId, new Set());
    }
    map.get(link.resourceTypeId).add(link.propertyDefinitionId);
  });
  return map;
}

function addRequirement() {
  const id = Date.now().toString(36);
  state.requirements.push({
    id,
    typeId: null,
    resourceId: null,
    definitionId: null,
    nodeId: null,
    includeDescendants: false,
    filters: [],
  });
  renderRequirements();
}

function resetBuilder() {
  state.requirements = [];
  state.ancestorFilters = [];
  addRequirement();
  renderRequirements();
  renderPayloadPreview();
  const summaryBody = summaryEl.querySelector(".query-summary-body") ?? summaryEl;
  summaryBody.textContent = "Waiting for query.";
  renderCalendarPlaceholder("Waiting for query. Build a request and click Compute availability.");
  busyCalendarEl.innerHTML = "";
  rulesEl.textContent = "No compute yet.";
  busyEl.textContent = "No compute yet.";
  explainEl.textContent = "Explain disabled.";
  busyNoteEl.textContent = "Busy intervals that remove or split availability.";
  errorEl.textContent = "";
  if (ancestorFilterChipsEl) {
    ancestorFilterChipsEl.textContent = "No ancestor filters.";
  }
  const slotDurationEl = document.getElementById("slotDurationMinutes");
  if (slotDurationEl) {
    slotDurationEl.value = "";
  }
  const slotDurationToggle = document.getElementById("toggleSlotDuration");
  if (slotDurationToggle) {
    slotDurationToggle.checked = false;
  }
  const includeRemainderEl = document.getElementById("includeRemainderSlot");
  if (includeRemainderEl) {
    includeRemainderEl.checked = false;
  }
  updateAncestorTypesVisibility();
  updateSlotDurationVisibility();
}

function renderRequirements() {
  if (state.requirements.length === 0) {
    requirementsEl.textContent = "Add a resource type.";
    renderPayloadPreview(null, "Add a resource type to see payload.");
    return;
  }

  requirementsEl.innerHTML = "";
  state.requirements.forEach((req, index) => {
    const card = document.createElement("div");
    card.className = "requirement-card";

    const header = document.createElement("div");
    header.className = "requirement-header";
    header.appendChild(createElement("strong", null, `Requirement #${index + 1}`));
    const removeBtn = document.createElement("button");
    removeBtn.className = "btn ghost small";
    removeBtn.textContent = "Remove";
    removeBtn.addEventListener("click", () => {
      state.requirements = state.requirements.filter(item => item.id !== req.id);
      renderRequirements();
    });
    header.appendChild(removeBtn);
    card.appendChild(header);

    const grid = document.createElement("div");
    grid.className = "requirement-grid";

    grid.appendChild(buildSelectRow(
      "Resource type",
      buildTypeSelect(req),
      "Search includes non-schedulable resource types (e.g., Site/Floor) for ancestor filtering."
    ));
    grid.appendChild(buildSelectRow(
      "Specific resource",
      buildResourceSelect(req),
      "Search includes non-schedulable resources (e.g., Site/Floor) for ancestor filtering."
    ));

    const typeLabel = req.typeId
      ? state.resourceTypes.find(t => t.id === req.typeId)?.label || "Type"
      : "Type";
    grid.appendChild(buildSelectRow(`${typeLabel} properties`, buildDefinitionSelect(req)));
    const note = document.createElement("p");
    note.className = "microcopy";
    note.textContent = "Properties are defined per resource type.";
    grid.appendChild(note);
    grid.appendChild(buildPropertyFilterRow(req));

    card.appendChild(grid);
    card.appendChild(buildFilterChips(req));
    card.appendChild(buildCandidatePreview(req));
    requirementsEl.appendChild(card);
  });

  renderPayloadPreview(buildSearchPayload().payload);
}

function buildSelectRow(labelText, element, tooltip) {
  const wrapper = document.createElement("div");
  wrapper.className = "input-row";
  const label = document.createElement("label");
  label.textContent = labelText;
  if (tooltip) {
    label.appendChild(document.createTextNode(" "));
    const info = createElement("span", "info-icon", "i");
    info.setAttribute("data-tooltip", tooltip);
    label.appendChild(info);
  }
  wrapper.appendChild(label);
  wrapper.appendChild(element);
  return wrapper;
}

function buildTypeSelect(req) {
  const select = document.createElement("select");
  select.className = "property-select";
  select.innerHTML = "<option value=\"\">Select type</option>";
  const sorted = [...state.resourceTypes].sort((a, b) => a.label.localeCompare(b.label));
  sorted.forEach(type => {
    const option = document.createElement("option");
    option.value = type.id;
    option.textContent = DemoFormat.formatTypeLabel(type);
    if (req.typeId === type.id) {
      option.selected = true;
    }
    select.appendChild(option);
  });

  select.addEventListener("change", () => {
    req.typeId = select.value ? Number(select.value) : null;
    req.resourceId = null;
    req.definitionId = null;
    req.nodeId = null;
    req.filters = [];
    renderRequirements();
  });

  return select;
}

function buildResourceSelect(req) {
  const select = document.createElement("select");
  select.className = "property-select";
  select.innerHTML = "<option value=\"\">Any resource of this type</option>";

  if (req.typeId) {
    const resources = state.resources
      .filter(resource => resource.typeId === req.typeId)
      .sort((a, b) => a.name.localeCompare(b.name));
    resources.forEach(resource => {
      const option = document.createElement("option");
      option.value = resource.id;
      option.textContent = DemoFormat.formatIdLabel(resource.name, resource.id);
      if (req.resourceId === resource.id) {
        option.selected = true;
      }
      select.appendChild(option);
    });
  }

  select.addEventListener("change", () => {
    req.resourceId = select.value ? Number(select.value) : null;
    renderRequirements();
  });

  return select;
}

function buildDefinitionSelect(req) {
  const select = document.createElement("select");
  select.className = "property-select";
  select.innerHTML = "<option value=\"\">Select definition</option>";

  const allowed = req.typeId ? state.typePropertyMap.get(req.typeId) : null;
  if (allowed && allowed.size > 0) {
    const defs = Array.from(allowed)
      .map(id => state.propertyDefinitions.get(id))
      .filter(Boolean)
      .sort((a, b) => a.label.localeCompare(b.label));
    defs.forEach(def => {
      const option = document.createElement("option");
      option.value = def.id;
      option.textContent = DemoFormat.formatDefinitionLabel(def);
      if (req.definitionId === def.id) {
        option.selected = true;
      }
      select.appendChild(option);
    });
  }

  select.addEventListener("change", () => {
    req.definitionId = select.value ? Number(select.value) : null;
    req.nodeId = null;
    renderRequirements();
  });

  return select;
}

function buildPropertyFilterRow(req) {
  const wrapper = document.createElement("div");
  wrapper.className = "property-filter-row";
  const nodeSelect = document.createElement("select");
  nodeSelect.className = "property-select";
  nodeSelect.innerHTML = "<option value=\"\">Select property</option>";

  if (req.definitionId) {
    appendPropertyTreeOptions(nodeSelect, req.definitionId, req.nodeId);
  }

  nodeSelect.addEventListener("change", () => {
    const nodeId = nodeSelect.value ? Number(nodeSelect.value) : null;
    if (!nodeId) {
      req.nodeId = null;
      return;
    }

    req.filters.push({
      propertyId: nodeId,
      includeDescendants: req.includeDescendants
    });
    req.nodeId = null;
    renderRequirements();
  });

  const includeLabel = document.createElement("label");
  includeLabel.className = "toggle";
  const includeToggle = document.createElement("input");
  includeToggle.type = "checkbox";
  includeToggle.checked = req.includeDescendants;
  includeToggle.addEventListener("change", () => {
    req.includeDescendants = includeToggle.checked;
  });
  includeLabel.appendChild(includeToggle);
  const includeText = document.createElement("span");
  includeText.textContent = "Include property descendants";
  const info = createElement("span", "info-icon", "i");
  info.setAttribute("data-tooltip", "When enabled, child properties in the hierarchy are included in the filter.");
  includeText.appendChild(document.createTextNode(" "));
  includeText.appendChild(info);
  includeLabel.appendChild(includeText);

  const addBtn = document.createElement("button");
  addBtn.className = "btn small";
  addBtn.textContent = "Add filter";
  addBtn.addEventListener("click", () => {
    if (!req.nodeId) {
      return;
    }
    req.filters.push({
      propertyId: req.nodeId,
      includeDescendants: req.includeDescendants
    });
    req.nodeId = null;
    renderRequirements();
  });

  const row = document.createElement("div");
  row.className = "input-row";
  row.appendChild(nodeSelect);
  row.appendChild(addBtn);
  wrapper.appendChild(row);
  wrapper.appendChild(includeLabel);
  return wrapper;
}

function buildFilterChips(req) {
  const chips = document.createElement("div");
  chips.className = "chip-list";
  if (req.filters.length === 0) {
    chips.textContent = "No property filters.";
    return chips;
  }

  req.filters.forEach((filter, index) => {
    const chip = document.createElement("span");
    chip.className = "chip";
    chip.textContent = formatFilterLabel(filter);
    const remove = document.createElement("button");
    remove.className = "btn ghost small";
    remove.textContent = "x";
    remove.addEventListener("click", () => {
      req.filters.splice(index, 1);
      renderRequirements();
    });
    chip.appendChild(remove);
    chips.appendChild(chip);
  });
  return chips;
}

function buildCandidatePreview(req) {
  const preview = document.createElement("div");
  preview.className = "candidate-preview";

  if (!req.typeId) {
    preview.textContent = "Select a resource type to see matching resources.";
    return preview;
  }

  const matches = getMatchingResources(req);
  const names = matches.slice(0, 10).map(resource => DemoFormat.formatIdLabel(resource.name, resource.id));
  preview.appendChild(createElement("strong", null, `Matching resources: ${matches.length}`));
  const suffix = matches.length > names.length ? ` +${matches.length - names.length} more` : "";
  preview.appendChild(createElement("span", "slot-meta", `${names.length ? names.join(", ") : "No matches"}${suffix}`));
  return preview;
}

function getMatchingResources(req) {
  if (!req.typeId) {
    return [];
  }
  let candidates = state.resources.filter(resource => resource.typeId === req.typeId);

  if (req.resourceId) {
    const picked = candidates.find(resource => resource.id === req.resourceId);
    return picked ? [picked] : [];
  }

  req.filters.forEach(filter => {
    const allowed = new Set(expandPropertyIds(filter.propertyId, filter.includeDescendants));
    candidates = candidates.filter(resource => {
      const props = resource.properties || [];
      return props.some(prop => allowed.has(prop.id));
    });
  });

  return candidates;
}

function expandPropertyIds(propertyId, includeDescendants) {
  if (!includeDescendants) {
    return [propertyId];
  }
  const result = [propertyId];
  const stack = [propertyId];
  while (stack.length > 0) {
    const current = stack.pop();
    const children = state.propertyChildren.get(current) || [];
    children.forEach(child => {
      result.push(child);
      stack.push(child);
    });
  }
  return result;
}

function formatFilterLabel(filter) {
  const node = state.propertyMap.get(filter.propertyId);
  if (!node) {
    return `#${filter.propertyId}`;
  }
  const definition = state.propertyDefinitions.get(node.definitionId);
  const prefix = definition ? `${definition.label}: ` : "";
  const suffix = filter.includeDescendants ? " (include property descendants)" : "";
  return `${prefix}${node.label}${suffix}`;
}

async function computeSearch() {
  errorEl.textContent = "";
  setComputeStatus("Computing...");
  const payloadInfo = buildSearchPayload();
  renderPayloadPreview(payloadInfo.payload, payloadInfo.message);
  const requirements = payloadInfo.requirements;
  const payload = payloadInfo.payload;
  if (!payload) {
    errorEl.textContent = payloadInfo.message || "Unable to build request payload.";
    setComputeStatus("Waiting");
    renderCalendarPlaceholder("Waiting for query. Build a request and click Compute availability.");
    return;
  }

  const previewResources = new Set();
  payload.requiredResourceIds.forEach(id => previewResources.add(id));
  payload.resourceOrGroups.forEach(group => group.forEach(id => previewResources.add(id)));

  const res = await fetch(`${state.apiBaseUrl}/api/availability/compute`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const text = await res.text();
    errorEl.textContent = text || "Availability request failed.";
    setComputeStatus("Failed");
    return;
  }

    const data = await res.json();
    renderCalendar(data.slots || []);
    renderExplain(data.explanations || []);
    renderSearchSummary(requirements, payload);
    renderSearchEmptyState(data.slots || [], data.explanations || [], requirements, payload);

  await loadBusySummary(Array.from(previewResources));
  setComputeStatus("Computed");
}

async function loadBusySummary(resourceIds) {
  if (resourceIds.length === 0) {
    rulesEl.textContent = "No resources for busy summary.";
    busyEl.textContent = "No resources for busy summary.";
    busyCalendarEl.innerHTML = "";
    busyNoteEl.textContent = "Busy intervals that remove or split availability.";
    return;
  }

  const limited = resourceIds.slice(0, 30);
  if (resourceIds.length > limited.length) {
    busyNoteEl.textContent = `Busy preview based on ${limited.length} resources (limited from ${resourceIds.length}).`;
  } else {
    busyNoteEl.textContent = "Busy intervals that remove or split availability.";
  }

  const res = await fetch(`${state.apiBaseUrl}/api/demo/summary`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      fromDate: isoDate(state.weekStart),
      toDate: isoDate(new Date(state.weekStart.getTime() + 6 * 24 * 60 * 60 * 1000)),
      resourceIds: limited
    })
  });

  if (!res.ok) {
    return;
  }

  const data = await res.json();
  state.rules = data.rules || [];
  state.busyEvents = data.busyEvents || [];
  state.selectedRuleId = null;
  renderRules(state.rules);
  updateRuleImpact(null);
  highlightRuleCards();
  renderBusy(state.busyEvents);
  renderBusyCalendar(state.busyEvents);
}

  function renderSearchSummary(requirements, payload) {
    const intent = buildSearchIntent(requirements);
    const bodyEl = summaryEl.querySelector(".query-summary-body") ?? summaryEl;
    bodyEl.innerHTML = "";
    bodyEl.appendChild(buildSummaryRow("Intent", intent));
    bodyEl.appendChild(buildSummaryRow("Range", `Week ${payload.fromDate} -> ${payload.toDate}`));
    bodyEl.appendChild(buildSummaryRow("Health", formatSearchHealthSummary(requirements, payload)));
    bodyEl.appendChild(buildSummaryRow("Explain", payload.explain ? "On" : "Off"));
    bodyEl.appendChild(buildSummaryRow("Ancestors", payload.includeResourceAncestors ? "On" : "Off"));
    bodyEl.appendChild(buildSummaryRow("Relation types", formatRelationTypesSummary(payload)));
    bodyEl.appendChild(buildSummaryRow("Ancestor filters", formatAncestorFiltersSummary(payload)));
    bodyEl.appendChild(buildSummaryRow("Slot duration", formatSlotDurationSummary(payload)));
  }

  function formatSearchHealthSummary(requirements, payload) {
    const requirementCount = requirements.length;
    const propertyFilterCount = requirements.reduce((total, req) => total + (req.filters?.length || 0), 0);
    const label = requirementCount === 0 ? "Incomplete" : "Ready";
    return `${label} · Requirements ${requirementCount} · Property filters ${propertyFilterCount}`;
  }

function setComputeStatus(message) {
  if (!computeStatusEl) return;
  computeStatusEl.textContent = message;
}

function buildSearchIntent(requirements) {
  if (requirements.length === 0) {
    return "Looking for availability with no requirements.";
  }

  const clauses = requirements.map(req => {
    const typeLabel = DemoFormat.formatTypeLabel(state.resourceTypes.find(t => t.id === req.typeId));
    const filters = req.filters.map(formatFilterLabel).join(" AND ");
    if (req.resourceId) {
      const resource = state.resourceMap.get(req.resourceId);
      const resourceLabel = resource ? DemoFormat.formatIdLabel(resource.name, resource.id) : `#${req.resourceId}`;
      return `${typeLabel} = ${resourceLabel}${filters ? ` AND ${filters}` : ""}`;
    }
    return `${typeLabel} = any${filters ? ` AND ${filters}` : ""}`;
  });

  return `Looking for availability where ${clauses.join(" AND ")}.`;
}

function formatRelationTypesSummary(payload) {
  if (!payload.includeResourceAncestors) {
    return "n/a";
  }
  const types = payload.ancestorRelationTypes || [];
  return types.length > 0 ? types.join(", ") : "All";
}

function formatAncestorFiltersSummary(payload) {
  const filters = payload.ancestorFilters || [];
  if (filters.length === 0) {
    return "None";
  }
  const formatted = filters.map(filter => {
      const typeLabel = DemoFormat.formatTypeLabel(state.resourceTypes.find(t => t.id === filter.resourceTypeId));
      const propertyLabels = (filter.propertyIds || [])
        .map(id => DemoFormat.formatPropertyLabelSimple(id, state.propertyMap))
        .join(", ");
      return `${typeLabel}: ${propertyLabels}`;
    });
  if (formatted.length <= 2) {
    return formatted.join(" | ");
  }
  return `${formatted.slice(0, 2).join(" | ")} +${formatted.length - 2} more`;
}

function formatSlotDurationSummary(payload) {
  if (!payload.slotDurationMinutes) {
    return "Off";
  }
  const remainder = payload.includeRemainderSlot ? "on" : "off";
  return `${payload.slotDurationMinutes} min (remainder ${remainder})`;
}

function renderCalendar(slots) {
  if (!calendarEl) {
    return;
  }
  if (!slots || slots.length === 0) {
    calendarEl.innerHTML = "";
    calendarEl.classList.add("empty");
    calendarEl.innerHTML = "<div class=\"empty-message\">No availability slots for the selected range.</div>";
    return;
  }
  calendarEl.classList.remove("empty");
    calendarEl.innerHTML = "";
    const days = Array.from({ length: 7 }, (_, idx) => {
      const date = new Date(state.weekStart);
    date.setUTCDate(date.getUTCDate() + idx);
    return { date, slots: [] };
  });

  slots.forEach(slot => {
    const start = new Date(slot.startUtc);
    const dateKey = isoDate(start);
    const day = days.find(d => isoDate(d.date) === dateKey);
    if (day) {
      day.slots.push(slot);
    }
  });

  days.forEach(day => {
    const dayEl = document.createElement("div");
    dayEl.className = "day";
    const title = document.createElement("h3");
    title.textContent = `${day.date.toLocaleDateString("en-GB", { weekday: "short", timeZone: "UTC" })} ${isoDate(day.date)}`;
    dayEl.appendChild(title);

    if (day.slots.length === 0) {
      const empty = document.createElement("div");
      empty.className = "slot empty";
      empty.textContent = "No slots";
      dayEl.appendChild(empty);
    } else {
      day.slots.forEach(slot => {
        const entry = document.createElement("div");
        entry.className = "slot";
        entry.dataset.startUtc = slot.startUtc;
        entry.dataset.endUtc = slot.endUtc;
        entry.dataset.resourceIds = (slot.resourceIds || []).join(",");
        const timeText = `${formatUtcTime(slot.startUtc)}-${formatUtcTime(slot.endUtc)} UTC`;
        entry.appendChild(document.createTextNode(timeText));
        const meta = createElement("span", "slot-meta");
        appendResourceTags(meta, slot.resourceIds);
        entry.appendChild(meta);
        entry.addEventListener("mouseenter", () => showSlotTooltip(entry, slot));
        entry.addEventListener("mouseleave", () => hideSlotTooltip(entry));
        dayEl.appendChild(entry);
      });
    }

    calendarEl.appendChild(dayEl);
  });
}

function createTooltipIcon(kind) {
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("viewBox", "0 0 20 20");
  svg.setAttribute("fill", "none");
  svg.setAttribute("stroke", "currentColor");
  svg.setAttribute("stroke-width", "2");
  svg.setAttribute("stroke-linecap", "round");
  svg.setAttribute("stroke-linejoin", "round");
  svg.classList.add("tooltip-icon", kind);
  const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
  if (kind === "match") {
    path.setAttribute("d", "M5 10l3 3 7-7");
  } else if (kind === "none") {
    path.setAttribute("d", "M6 10h8");
  } else {
    path.setAttribute("d", "M6 6l8 8M14 6l-8 8");
  }
  svg.appendChild(path);
  return svg;
}

function appendTooltipLine(container, kind, text) {
  const line = document.createElement("div");
  line.className = "tooltip-item";
  line.appendChild(createTooltipIcon(kind));
  line.appendChild(document.createTextNode(text));
  container.appendChild(line);
}

function showSlotTooltip(element, slot) {
  if (!document.getElementById("toggleExplain").checked) return;
  hideSlotTooltip(element);
  const rules = state.rules.filter(rule =>
    doesRuleApplyToSlot(rule, slot.startUtc, slot.endUtc, slot.resourceIds || []));
  const busyConflicts = state.busyEvents.filter(busy =>
    overlapsUtc(slot.startUtc, slot.endUtc, busy.startUtc, busy.endUtc));

  const tooltip = createElement("div", "tooltip");
  tooltip.appendChild(createElement("div", "tooltip-title", "Slot analysis"));
  tooltip.appendChild(createElement("div", "tooltip-section", "Rules matched"));
  if (rules.length === 0) {
    appendTooltipLine(tooltip, "none", "No matching rules");
  } else {
    rules.forEach(rule => {
      appendTooltipLine(
        tooltip,
        "match",
        `${DemoFormat.formatRuleLabel(rule)} - ${rule.title || "Untitled"}`
      );
    });
  }
  tooltip.appendChild(createElement("div", "tooltip-section", "Busy overlaps"));
  if (busyConflicts.length === 0) {
    appendTooltipLine(tooltip, "none", "No busy overlaps");
  } else {
    busyConflicts.forEach(busy => {
      appendTooltipLine(
        tooltip,
        "block",
        `${DemoFormat.formatBusyLabel(busy)} - ${formatUtcTime(busy.startUtc)}-${formatUtcTime(busy.endUtc)}`
      );
    });
  }

  element.appendChild(tooltip);
}

function hideSlotTooltip(element) {
  const tooltip = element.querySelector(".tooltip");
  if (tooltip) {
    tooltip.remove();
  }
}

function overlapsUtc(startA, endA, startB, endB) {
  const aStart = new Date(normalizeUtc(startA)).getTime();
  const aEnd = new Date(normalizeUtc(endA)).getTime();
  const bStart = new Date(normalizeUtc(startB)).getTime();
  const bEnd = new Date(normalizeUtc(endB)).getTime();
  return aStart < bEnd && aEnd > bStart;
}

function doesRuleApplyToSlot(rule, slotStart, slotEnd, slotResources) {
  if (!rule || !slotStart || !slotEnd) return false;
  if (!rule.resourceIds || rule.resourceIds.length === 0) return false;
  const required = rule.resourceIds.every(id => slotResources.includes(id));
  if (!required) return false;

  const start = new Date(normalizeUtc(slotStart));
  const end = new Date(normalizeUtc(slotEnd));
  if (!isRuleDateMatch(rule, start)) return false;
  return isWithinTimeRange(rule, start, end);
}

function countSlotsForRule(rule) {
  const slots = calendarEl.querySelectorAll(".slot");
  let count = 0;
  slots.forEach(el => {
    if (el.classList.contains("empty")) return;
    const startUtc = el.dataset.startUtc;
    const endUtc = el.dataset.endUtc;
    const resourceIds = el.dataset.resourceIds
      ? el.dataset.resourceIds.split(",").map(Number)
      : [];
    if (doesRuleApplyToSlot(rule, startUtc, endUtc, resourceIds)) {
      count += 1;
    }
  });
  return count;
}

function updateRuleImpact(rule) {
  if (!ruleImpactEl) {
    return;
  }
  if (!state.selectedRuleId) {
    ruleImpactEl.textContent = "Click a rule to see how many slots it contributes to.";
    return;
  }
  const resolved = rule || state.rules.find(item => item.id === state.selectedRuleId);
  if (!resolved) {
    ruleImpactEl.textContent = "Click a rule to see how many slots it contributes to.";
    return;
  }
  const count = countSlotsForRule(resolved);
  ruleImpactEl.textContent = `${DemoFormat.formatRuleLabel(resolved)} contributes to ${count} slot${count === 1 ? "" : "s"} in the selected range.`;
}

function highlightRuleCards() {
  const cards = rulesEl.querySelectorAll(".card");
  cards.forEach(card => {
    const active = Number(card.dataset.ruleId) === state.selectedRuleId;
    card.classList.toggle("selected", active);
    card.classList.toggle("dimmed", state.selectedRuleId && !active);
  });
}

function isRuleDateMatch(rule, date) {
  const dateOnly = isoDate(date);
  if (rule.singleDateUtc) {
    return rule.singleDateUtc === dateOnly;
  }
  if (rule.fromDateUtc && dateOnly < rule.fromDateUtc) return false;
  if (rule.toDateUtc && dateOnly > rule.toDateUtc) return false;
  if (rule.kind === 1 && rule.daysOfWeekMask != null) {
    const bit = 1 << date.getUTCDay();
    return (rule.daysOfWeekMask & bit) === bit;
  }
  return true;
}

function isWithinTimeRange(rule, start, end) {
  if (!rule.startTime || !rule.endTime) return false;
  const [startH, startM] = rule.startTime.split(":").map(Number);
  const [endH, endM] = rule.endTime.split(":").map(Number);
  const slotStartMinutes = start.getUTCHours() * 60 + start.getUTCMinutes();
  const slotEndMinutes = end.getUTCHours() * 60 + end.getUTCMinutes();
  const ruleStart = startH * 60 + startM;
  const ruleEnd = endH * 60 + endM;
  return slotStartMinutes >= ruleStart && slotEndMinutes <= ruleEnd;
}

function renderBusyCalendar(busyEvents) {
  busyCalendarEl.innerHTML = "";
  const days = Array.from({ length: 7 }, (_, idx) => {
    const date = new Date(state.weekStart);
    date.setUTCDate(date.getUTCDate() + idx);
    return { date, slots: [] };
  });

  busyEvents.forEach(busy => {
    const start = new Date(busy.startUtc);
    const dateKey = isoDate(start);
    const day = days.find(d => isoDate(d.date) === dateKey);
    if (day) {
      day.slots.push(busy);
    }
  });

  days.forEach(day => {
    const dayEl = document.createElement("div");
    dayEl.className = "day";
    const title = document.createElement("h3");
    title.textContent = `${day.date.toLocaleDateString("en-GB", { weekday: "short", timeZone: "UTC" })} ${isoDate(day.date)}`;
    dayEl.appendChild(title);

    if (day.slots.length === 0) {
      const empty = document.createElement("div");
      empty.className = "slot empty";
      empty.textContent = "No busy slots";
      dayEl.appendChild(empty);
    } else {
      day.slots.forEach(busy => {
        const entry = document.createElement("div");
        entry.className = "slot";
        const timeText = `${formatUtcTime(busy.startUtc)}-${formatUtcTime(busy.endUtc)} UTC`;
        entry.appendChild(document.createTextNode(timeText));
        const meta = createElement("span", "slot-meta");
        appendResourceTags(meta, busy.resourceIds);
        entry.appendChild(meta);
        dayEl.appendChild(entry);
      });
    }

    busyCalendarEl.appendChild(dayEl);
  });
}

function renderRules(rules) {
  if (rules.length === 0) {
    rulesEl.textContent = "No rules.";
    return;
  }

  rulesEl.innerHTML = "";
    rules.forEach(rule => {
      const card = document.createElement("div");
      card.className = "card rule-card";
      card.title = "Click to highlight related slots.";
    card.dataset.ruleId = rule.id;
    const dateRange = formatRuleDateRange(rule);
    const titleText = rule.title || DemoFormat.formatRuleLabel(rule);
    card.appendChild(createElement("strong", null, titleText));
    const metaRow = createElement("div", "rule-meta");
    metaRow.appendChild(createElement("span", "slot-meta", DemoFormat.formatRuleLabel(rule)));
    metaRow.appendChild(createElement("span", "rule-time", `${formatTimeRange(rule.startTime, rule.endTime)} UTC`));
    card.appendChild(metaRow);
    if (dateRange) {
      card.appendChild(createElement("div", "rule-date", dateRange));
    }
    appendResourceTags(card, rule.resourceIds);
    card.addEventListener("click", () => {
      state.selectedRuleId = state.selectedRuleId === rule.id ? null : rule.id;
      updateRuleImpact(rule);
      highlightRuleCards();
    });
    rulesEl.appendChild(card);
  });
}

function renderBusy(busyEvents) {
  if (busyEvents.length === 0) {
    busyEl.textContent = "No busy slots.";
    return;
  }

  busyEl.innerHTML = "";
  busyEvents.forEach(busy => {
    const card = document.createElement("div");
    card.className = "card";
    const titleText = busy.title || DemoFormat.formatBusyLabel(busy);
    card.appendChild(createElement("strong", null, titleText));
    card.appendChild(createElement("div", "rule-time", `${formatUtcDateTime(busy.startUtc)} - ${formatUtcDateTime(busy.endUtc)}`));
    appendResourceTags(card, busy.resourceIds);
    busyEl.appendChild(card);
  });
}

  function renderExplain(explanations) {
    if (!document.getElementById("toggleExplain").checked) {
      explainEl.textContent = "Explain disabled.";
      return;
  }
  if (explanations.length === 0) {
    explainEl.textContent = "No explanations.";
    return;
  }
  explainEl.innerHTML = "";
    explanations.forEach(exp => {
      const card = document.createElement("div");
      card.className = "card";
      card.innerHTML = `
        <strong>${exp.reason}</strong><br/>
        ${exp.message}
      `;
      explainEl.appendChild(card);
    });
  }

  function renderSearchEmptyState(slots, explanations, requirements, payload) {
    if (!searchEmptyStateEl) {
      return;
    }
    if (slots && slots.length > 0) {
      searchEmptyStateEl.style.display = "none";
      searchEmptyStateEl.textContent = "";
      return;
    }

    const requirementCount = requirements.length;
    const propertyCount = requirements.reduce((total, req) => total + (req.filters?.length || 0), 0);
    const reason = explanations?.[0]?.reason ?? null;
    const lines = [];

    if (requirementCount === 0) {
      lines.push("Add at least one resource type.");
    } else {
      switch (reason) {
        case "NoPositiveRule":
          lines.push("No positive rules apply in this range.");
          break;
        case "FullyBlockedByBusy":
          lines.push("Busy events block availability in this range.");
          break;
        case "FullyBlockedByNegativeRule":
          lines.push("Negative rules block availability in this range.");
          break;
        case "PartiallyBlocked":
          lines.push("Rules or busy events block availability in this range.");
          break;
        default:
          lines.push("No availability found for the current filters.");
          break;
      }
      if (propertyCount > 0) {
        lines.push("Property filters may be too restrictive.");
      }
      lines.push("Try widening the date range or adjusting filters.");
    }

    searchEmptyStateEl.innerHTML = "";
    searchEmptyStateEl.appendChild(createElement("strong", null, "No availability found"));
    lines.forEach(text => {
      searchEmptyStateEl.appendChild(createElement("div", null, text));
    });
    const quick = createElement("div", "empty-actions");
    const rulesLink = createElement("a", "link", "See rules");
    rulesLink.href = "#rules";
    const busyLink = createElement("a", "link", "See busy calendar");
    busyLink.href = "#busyCalendar";
    quick.appendChild(rulesLink);
    quick.appendChild(document.createTextNode(" · "));
    quick.appendChild(busyLink);
    searchEmptyStateEl.appendChild(quick);
    searchEmptyStateEl.style.display = "block";
  }

function normalizeUtc(iso) {
  if (!iso) return iso;
  return /z$/i.test(iso) || /[+-]\d{2}:\d{2}$/.test(iso) ? iso : `${iso}Z`;
}

function formatUtcTime(iso) {
  return new Date(normalizeUtc(iso)).toLocaleTimeString("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "UTC"
  });
}

function formatUtcDateTime(iso) {
  return new Date(normalizeUtc(iso)).toLocaleString("en-GB", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "UTC"
  });
}

function formatTimeRange(start, end) {
  if (!start || !end) return "n/a";
  return `${start} - ${end}`;
}

function formatRuleDateRange(rule) {
  if (rule.singleDateUtc) {
    return `Date: ${rule.singleDateUtc}`;
  }
  if (rule.fromDateUtc && rule.toDateUtc) {
    return `Range: ${rule.fromDateUtc} -> ${rule.toDateUtc}`;
  }
  if (rule.fromDateUtc) {
    return `From: ${rule.fromDateUtc}`;
  }
  if (rule.toDateUtc) {
    return `Until: ${rule.toDateUtc}`;
  }
  return "";
}

function appendResourceTags(container, resourceIds) {
  if (!resourceIds || resourceIds.length === 0) {
    container.appendChild(createElement("span", "slot-meta", "No resources"));
    return;
  }
    resourceIds.forEach(id => {
      const resource = state.resourceMap.get(id);
      const name = resource ? DemoFormat.formatIdLabel(resource.name, resource.id) : `#${id}`;
      const type = getResourceType(resource);
      const tag = createElement("span", `tag tag-${type}`, name);
      container.appendChild(tag);
    });
}

function getResourceType(resource) {
  if (!resource) return "other";
  const key = (resource.typeKey || resource.typeLabel || "").toLowerCase();
  if (key.includes("doctor")) return "doctor";
  if (key.includes("room")) return "room";
  return "other";
}

function renderCalendarPlaceholder(message) {
  if (!calendarEl) {
    return;
  }
  calendarEl.classList.add("empty");
  calendarEl.innerHTML = `<div class="empty-message">${message}</div>`;
}

function parseAncestorTypes() {
  if (ancestorTypesAllEl?.checked) {
    return [];
  }
  const inputs = document.querySelectorAll("input[name=\"ancestorRelationType\"]");
  const selected = [];
  inputs.forEach(input => {
    if (input.checked) {
      selected.push(input.value);
    }
  });
  return selected;
}

function parseSlotDurationMinutes() {
  const enabled = document.getElementById("toggleSlotDuration")?.checked;
  if (!enabled) {
    return null;
  }
  const input = document.getElementById("slotDurationMinutes");
  if (!input) {
    return null;
  }
  const value = Number.parseInt(input.value, 10);
  return Number.isFinite(value) && value > 0 ? value : null;
}

function buildSearchPayload() {
  const requirements = state.requirements.filter(req => req.typeId);
  if (requirements.length === 0) {
    return { payload: null, requirements, message: "Add a resource type to see payload." };
  }

  const groupsResult = buildRequirementGroups(requirements);
  if (groupsResult.error) {
    return { payload: null, requirements, message: groupsResult.error };
  }

  const includeAncestorsToggle = document.getElementById("toggleAncestors").checked;
  const ancestorTypes = parseAncestorTypes();
  const slotDurationMinutes = parseSlotDurationMinutes();
  const includeRemainderSlot = document.getElementById("includeRemainderSlot").checked;
  const ancestorFilters = buildAncestorFiltersPayload();
  const includeAncestorsEffective = includeAncestorsToggle || ancestorFilters.length > 0;

  const payload = {
    fromDate: isoDate(state.weekStart),
    toDate: isoDate(new Date(state.weekStart.getTime() + 6 * 24 * 60 * 60 * 1000)),
    requiredResourceIds: groupsResult.requiredResourceIds,
    resourceOrGroups: groupsResult.resourceOrGroups,
    explain: document.getElementById("toggleExplain").checked,
    includeResourceAncestors: includeAncestorsEffective,
    ancestorRelationTypes: ancestorTypes.length > 0 ? ancestorTypes : null,
    ancestorMode: includeAncestorsEffective ? "perGroup" : null,
    slotDurationMinutes: slotDurationMinutes ?? null,
    includeRemainderSlot: slotDurationMinutes ? includeRemainderSlot : false,
    ancestorFilters: ancestorFilters.length > 0 ? ancestorFilters : null
  };

  return { payload, requirements, message: "" };
}

function buildRequirementGroups(requirements) {
  const requiredResourceIds = [];
  const resourceOrGroups = [];
  for (const req of requirements) {
    const candidates = getMatchingResources(req);
    if (req.resourceId) {
      if (candidates.length === 0) {
        return { error: "Selected resource does not match the chosen type." };
      }
      requiredResourceIds.push(req.resourceId);
      continue;
    }

    if (candidates.length === 0) {
      return { error: "Some requirements have no matching resources." };
    }

    resourceOrGroups.push(candidates.map(resource => resource.id));
  }

  if (requiredResourceIds.length === 0 && resourceOrGroups.length === 0) {
    return { error: "Add at least one resource type." };
  }

  return { requiredResourceIds, resourceOrGroups };
}

function renderPayloadPreview(payload, message) {
  if (!payloadPreviewEl) {
    return;
  }
  if (!payload) {
    payloadPreviewEl.textContent = message || "Configure a query to see payload.";
    return;
  }
  payloadPreviewEl.textContent = JSON.stringify(payload, null, 2);
}

async function copyPayload() {
  if (!payloadPreviewEl) {
    return;
  }
  try {
    await navigator.clipboard.writeText(payloadPreviewEl.textContent);
  } catch {
    const textarea = document.createElement("textarea");
    textarea.value = payloadPreviewEl.textContent;
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand("copy");
    document.body.removeChild(textarea);
  }
}

function renderAncestorFilterTypes() {
  if (!ancestorFilterTypeEl) {
    return;
  }
  ancestorFilterTypeEl.innerHTML = "";
  const placeholder = document.createElement("option");
  placeholder.value = "";
  placeholder.textContent = "Select type";
  ancestorFilterTypeEl.appendChild(placeholder);
  const sorted = [...state.resourceTypes].sort((a, b) => a.label.localeCompare(b.label));
  sorted.forEach(type => {
    const option = document.createElement("option");
    option.value = type.id;
    option.textContent = DemoFormat.formatTypeLabel(type);
    ancestorFilterTypeEl.appendChild(option);
  });
}

function renderAncestorFilterDefinitions() {
  if (!ancestorFilterDefinitionEl) {
    return;
  }
  ancestorFilterDefinitionEl.innerHTML = "";
  const placeholder = document.createElement("option");
  placeholder.value = "";
  placeholder.textContent = "Select definition";
  ancestorFilterDefinitionEl.appendChild(placeholder);
  const typeId = Number(ancestorFilterTypeEl.value);
  if (!Number.isFinite(typeId) || typeId <= 0) {
    return;
  }
  const allowed = state.typePropertyMap.get(typeId);
  if (!allowed || allowed.size === 0) {
    return;
  }
  const defs = Array.from(allowed)
    .map(id => state.propertyDefinitions.get(id))
    .filter(Boolean)
    .sort((a, b) => a.label.localeCompare(b.label));
  defs.forEach(def => {
    const option = document.createElement("option");
    option.value = def.id;
      option.textContent = DemoFormat.formatDefinitionLabel(def);
    ancestorFilterDefinitionEl.appendChild(option);
  });
}

function renderAncestorFilterProperties() {
  if (!ancestorFilterPropertyEl) {
    return;
  }
  ancestorFilterPropertyEl.innerHTML = "";
  const placeholder = document.createElement("option");
  placeholder.value = "";
  placeholder.textContent = "Select property";
  ancestorFilterPropertyEl.appendChild(placeholder);

  const definitionId = Number(ancestorFilterDefinitionEl.value);
  if (!Number.isFinite(definitionId) || definitionId <= 0) {
    return;
  }
  appendPropertyTreeOptions(ancestorFilterPropertyEl, definitionId, null);
}

function addAncestorFilter() {
  if (!ancestorFilterTypeEl || !ancestorFilterPropertyEl) {
    return;
  }
  const typeId = Number(ancestorFilterTypeEl.value);
  const propertyId = Number(ancestorFilterPropertyEl.value);
  if (!Number.isFinite(typeId) || typeId <= 0 || !Number.isFinite(propertyId) || propertyId <= 0) {
    return;
  }

  const filter = {
    resourceTypeId: typeId,
    propertyIds: [propertyId],
    includePropertyDescendants: ancestorFilterIncludeDescendantsEl?.checked ?? false,
    matchMode: ancestorFilterMatchModeEl?.value || "or",
    scope: ancestorFilterScopeEl?.value || "anyAncestor",
    matchAllAncestors: ancestorFilterMatchAllEl?.checked ?? false
  };

  const existing = state.ancestorFilters.find(item =>
    item.resourceTypeId === filter.resourceTypeId
    && item.includePropertyDescendants === filter.includePropertyDescendants
    && item.matchMode === filter.matchMode
    && item.scope === filter.scope
    && item.matchAllAncestors === filter.matchAllAncestors);

  if (existing) {
    if (!existing.propertyIds.includes(propertyId)) {
      existing.propertyIds.push(propertyId);
    }
  } else {
    state.ancestorFilters.push(filter);
  }

  renderAncestorFilterChips();
  updateAncestorTypesVisibility();
  renderPayloadPreview();
}

function clearAncestorFilters() {
  state.ancestorFilters = [];
  renderAncestorFilterChips();
  updateAncestorTypesVisibility();
  renderPayloadPreview();
}

function renderAncestorFilterChips() {
  if (!ancestorFilterChipsEl) {
    return;
  }
  if (state.ancestorFilters.length === 0) {
    ancestorFilterChipsEl.textContent = "No ancestor filters.";
    return;
  }

  ancestorFilterChipsEl.innerHTML = "";
  state.ancestorFilters.forEach((filter, index) => {
    const typeLabel = state.resourceTypes.find(t => t.id === filter.resourceTypeId)?.label || `Type ${filter.resourceTypeId}`;
    const propertyLabels = filter.propertyIds
      .map(id => DemoFormat.formatPropertyLabelSimple(id, state.propertyMap))
      .join(", ");
    const chip = createElement(
      "span",
      "chip",
      `${typeLabel}: ${propertyLabels} | ${filter.matchMode} | ${filter.scope}${filter.matchAllAncestors ? " | all ancestors" : ""}`);
    const remove = createElement("button", "btn ghost small", "x");
    remove.addEventListener("click", () => {
      state.ancestorFilters.splice(index, 1);
      renderAncestorFilterChips();
      updateAncestorTypesVisibility();
      renderPayloadPreview();
    });
    chip.appendChild(remove);
    ancestorFilterChipsEl.appendChild(chip);
  });
}

function buildAncestorFiltersPayload() {
  return state.ancestorFilters.map(filter => ({
    resourceTypeId: filter.resourceTypeId,
    propertyIds: filter.propertyIds,
    includePropertyDescendants: filter.includePropertyDescendants,
    matchMode: filter.matchMode,
    scope: filter.scope,
    matchAllAncestors: filter.matchAllAncestors
  }));
}

function updateAncestorTypesVisibility() {
  const includeAncestors = document.getElementById("toggleAncestors").checked;
  if (ancestorTypesWrapEl) {
    ancestorTypesWrapEl.style.display = includeAncestors || state.ancestorFilters.length > 0 ? "block" : "none";
  }
  if (!ancestorFiltersWrapEl) {
    return;
  }
  ancestorFiltersWrapEl.style.display = includeAncestors || state.ancestorFilters.length > 0 ? "block" : "none";
  updateAncestorTypesState();
}

function updateAncestorTypesState() {
  if (!ancestorTypesAllEl) {
    return;
  }
  const inputs = document.querySelectorAll("input[name=\"ancestorRelationType\"]");
  if (ancestorTypesAllEl.checked) {
    if (ancestorTypesSpecificEl) {
      ancestorTypesSpecificEl.style.display = "none";
    }
    inputs.forEach(input => {
      input.checked = true;
      input.disabled = true;
    });
  } else {
    if (ancestorTypesSpecificEl) {
      ancestorTypesSpecificEl.style.display = "flex";
    }
    inputs.forEach(input => {
      input.disabled = false;
    });
  }
}

function updateSlotDurationVisibility() {
  if (!slotDurationWrapEl) {
    return;
  }
  const enabled = document.getElementById("toggleSlotDuration")?.checked;
  slotDurationWrapEl.style.display = enabled ? "block" : "none";
  if (!enabled) {
    const input = document.getElementById("slotDurationMinutes");
    if (input) {
      input.value = "";
    }
    const includeRemainderEl = document.getElementById("includeRemainderSlot");
    if (includeRemainderEl) {
      includeRemainderEl.checked = false;
    }
    if (includeRemainderWrapEl) {
      includeRemainderWrapEl.style.display = "none";
    }
  } else if (includeRemainderWrapEl) {
    includeRemainderWrapEl.style.display = "flex";
  }
}

function buildSummaryRow(labelText, valueText) {
  const row = createElement("div", "summary-row");
  row.appendChild(createElement("span", null, labelText));
  row.appendChild(createElement("div", null, valueText));
  return row;
}

async function init() {
  state.weekStart = getWeekStart(new Date());
  updateWeekLabel();
  await loadConfig();
  await waitForApiReady();
}

init();

async function bootstrapAfterApiReady() {
  await loadCatalogs();
  resetBuilder();
  updateAncestorTypesVisibility();
  updateAncestorTypesState();
  updateSlotDurationVisibility();
  setApiStatus("hidden", "");
}

async function waitForApiReady() {
  apiCheckInFlight = true;
  setApiStatus("waiting", "Waiting for API...");
  let delay = 1200;
  while (true) {
    const ok = await checkApiOnce();
    if (ok) {
      await bootstrapAfterApiReady();
      apiCheckInFlight = false;
      return;
    }
    setApiStatus("waiting", "Waiting for API...");
    await delayMs(delay);
    delay = Math.min(4000, Math.floor(delay * 1.2));
  }
}

function delayMs(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function checkApiOnce() {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 4000);
    const res = await fetch(`${state.apiBaseUrl}/health`, { signal: controller.signal });
    clearTimeout(timeoutId);
    return res.ok;
  } catch {
    return false;
  }
}

function setApiStatus(stateName, message) {
  apiStatusEl.classList.remove("waiting", "error", "hidden");
  apiStatusEl.classList.add(stateName);
  apiStatusMessageEl.textContent = message;
}



