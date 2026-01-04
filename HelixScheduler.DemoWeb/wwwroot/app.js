const state = {
  apiBaseUrl: "",
  resources: [],
  resourceMap: new Map(),
  selectedIds: new Set(),
  weekStart: null,
  rules: [],
  busyEvents: [],
  selectedRuleId: null,
  selectedBusyId: null,
  propertyIds: [],
  propertySchema: null,
  propertyDefinitions: new Map(),
  propertyNodes: [],
  typePropertyMap: new Map(),
  propertyMap: new Map(),
  propertyChildren: new Map(),
  selectedPropertyId: null,
};

const calendarEl = document.getElementById("calendar");
const resourceListEl = document.getElementById("resourceList");
const rulesEl = document.getElementById("rules");
const busyEl = document.getElementById("busy");
const explainEl = document.getElementById("explain");
const weekLabelEl = document.getElementById("weekLabel");
const busyCalendarEl = document.getElementById("busyCalendar");
const selectionInfoEl = document.getElementById("selectionInfo");
const querySummaryEl = document.getElementById("querySummary");
const computeStatusEl = document.getElementById("computeStatus");
const propertyGroupsEl = document.getElementById("propertyGroups");
const propertyChipsEl = document.getElementById("propertyChips");
const apiStatusEl = document.getElementById("apiStatus");
const apiStatusMessageEl = document.getElementById("apiStatusMessage");
const apiRetryEl = document.getElementById("apiRetry");
let apiCheckInFlight = false;

document.getElementById("prevWeek").addEventListener("click", () => shiftWeek(-7));
document.getElementById("nextWeek").addEventListener("click", () => shiftWeek(7));
document.getElementById("refresh").addEventListener("click", () => loadAll());
document.getElementById("resetDemo").addEventListener("click", () => resetDemo());
apiRetryEl.addEventListener("click", () => retryApiReady());
document.getElementById("toggleDescendants").addEventListener("change", () => {
  if (!state.selectedPropertyId) {
    return;
  }
  state.propertyIds = getSelectedPropertyIds();
  renderPropertyChips();
  renderResourceList();
  loadAll();
});

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
  loadAll();
}

function formatRangeLabel(start) {
  const end = new Date(start);
  end.setUTCDate(end.getUTCDate() + 6);
  return `${isoDate(start)} -> ${isoDate(end)}`;
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

async function loadResources() {
  const res = await fetch(`${state.apiBaseUrl}/api/catalog/resources?onlySchedulable=true`);
  const resources = await res.json();
  state.resources = resources;
  state.resourceMap = new Map(resources.map(r => [r.id, r]));

  const defaultIds = new Set();
  resources.forEach(r => {
    if (r.name.toLowerCase().includes("doctor 7")) {
      defaultIds.add(r.id);
    }
    if (r.name.toLowerCase().includes("room 1")) {
      defaultIds.add(r.id);
    }
  });
  state.selectedIds = defaultIds.size > 0 ? defaultIds : new Set(resources.slice(0, 2).map(r => r.id));

  renderResourceList();
}

async function loadPropertyCatalog() {
  const res = await fetch(`${state.apiBaseUrl}/api/catalog/properties`);
  if (!res.ok) {
    return;
  }

  const data = await res.json();
  state.propertySchema = data;
  state.propertyNodes = data.nodes || [];
  state.propertyDefinitions = new Map((data.definitions || []).map(def => [def.id, def]));
  state.propertyMap = new Map(state.propertyNodes.map(node => [node.id, node]));
  state.propertyChildren = buildPropertyChildrenMap(state.propertyNodes);
  state.typePropertyMap = buildTypePropertyMap(data.typeMappings || []);
  renderPropertyGroups();
}

function renderResourceList() {
  if (state.resources.length === 0) {
    resourceListEl.textContent = "No resources.";
    return;
  }

  resourceListEl.innerHTML = "";
  const filteredResources = filterResourcesByProperty(state.resources);
  if (filteredResources.length === 0) {
    resourceListEl.textContent = "No resources for selected property.";
    state.selectedIds = new Set();
    return;
  }

  const validIds = new Set(filteredResources.map(r => r.id));
  state.selectedIds = new Set(Array.from(state.selectedIds).filter(id => validIds.has(id)));

  const grouped = groupResourcesByType(filteredResources);
  grouped.forEach(({ typeLabel, items }) => {
    const title = document.createElement("div");
    title.className = "resource-group-title";
    title.textContent = `Type: ${typeLabel}`;
    resourceListEl.appendChild(title);

    items.forEach(resource => {
    const item = document.createElement("label");
    item.className = "resource-item";
    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.checked = state.selectedIds.has(resource.id);
    checkbox.addEventListener("change", () => {
      if (checkbox.checked) {
        state.selectedIds.add(resource.id);
      } else {
        state.selectedIds.delete(resource.id);
      }
      renderResourceList();
      loadAll();
    });
    const name = document.createElement("span");
    name.textContent = `${resource.name} (#${resource.id})`;
    const typeLabel = resource.typeLabel || "Unknown";
    const typeMeta = document.createElement("span");
    typeMeta.className = "type-pill";
    typeMeta.textContent = typeLabel;
    item.appendChild(checkbox);
    item.appendChild(name);
    item.appendChild(typeMeta);
    const props = document.createElement("div");
    props.className = "resource-properties";
    const label = createElement("span", "slot-meta", `${typeLabel} properties:`);
    props.appendChild(label);
    const tagContainer = createElement("div");
    const hasTags = appendPropertyTags(tagContainer, resource.properties || []);
    if (!hasTags) {
      tagContainer.appendChild(createElement("span", "slot-meta", "No properties."));
    }
    props.appendChild(tagContainer);
    if (checkbox.checked) {
      props.classList.add("visible");
    }
    item.appendChild(props);
    resourceListEl.appendChild(item);
    });
  });

  refreshPropertyFilterForResources();
}

async function loadAvailability() {
  const explain = document.getElementById("toggleExplain").checked;
  const includeDescendants = document.getElementById("toggleDescendants").checked;
  const fromDate = isoDate(state.weekStart);
  const toDate = isoDate(new Date(state.weekStart.getTime() + 6 * 24 * 60 * 60 * 1000));
  const resourceIds = Array.from(state.selectedIds);

  if (resourceIds.length === 0) {
    renderCalendar([]);
    renderExplain([]);
    return;
  }

  const payload = {
    fromDate,
    toDate,
    requiredResourceIds: resourceIds,
    propertyIds: state.propertyIds,
    includeDescendants,
    explain,
  };

  const res = await fetch(`${state.apiBaseUrl}/api/availability/compute`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || "Availability request failed.");
  }

  const data = await res.json();
  renderCalendar(data.slots || []);
  renderExplain(data.explanations || []);
}

async function loadDemoSummary() {
  const fromDate = isoDate(state.weekStart);
  const toDate = isoDate(new Date(state.weekStart.getTime() + 6 * 24 * 60 * 60 * 1000));
  const resourceIds = Array.from(state.selectedIds);

  if (resourceIds.length === 0) {
    rulesEl.textContent = "Select at least one resource.";
    busyEl.textContent = "Select at least one resource.";
    busyCalendarEl.innerHTML = "";
    return;
  }

  const res = await fetch(`${state.apiBaseUrl}/api/demo/summary`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ fromDate, toDate, resourceIds })
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || "Demo summary request failed.");
  }

  const data = await res.json();
  state.rules = data.rules || [];
  state.busyEvents = data.busyEvents || [];
  renderRules(state.rules);
  renderBusy(state.busyEvents);
  renderBusyCalendar(state.busyEvents);
  clearHighlights();
}

function renderCalendar(slots) {
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
        entry.dataset.busyId = busy.id;
        const timeText = `${formatUtcTime(busy.startUtc)}-${formatUtcTime(busy.endUtc)} UTC`;
        entry.appendChild(document.createTextNode(timeText));
        const meta = createElement("span", "slot-meta");
        appendResourceTags(meta, busy.resourceIds);
        entry.appendChild(meta);
        entry.addEventListener("click", () => handleBusySelection(busy));
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
    card.dataset.ruleId = rule.id;
    const resourceNames = rule.resourceIds
      .map(id => state.resourceMap.get(id)?.name || `#${id}`)
      .join(", ");
    const dateRange = formatRuleDateRange(rule);
    const title = createElement("strong", null, rule.title || `Rule ${rule.id}`);
    card.appendChild(title);
    card.appendChild(document.createTextNode(" "));
    card.appendChild(createElement("span", "slot-meta", `Rule #${rule.id}`));
    card.appendChild(createElement("br"));
    if (dateRange) {
      card.appendChild(document.createTextNode(dateRange));
      card.appendChild(createElement("br"));
    }
    card.appendChild(document.createTextNode(`${formatTimeRange(rule.startTime, rule.endTime)} UTC`));
    card.appendChild(createElement("br"));
    appendResourceTags(card, rule.resourceIds);
    card.addEventListener("click", () => handleRuleSelection(rule));
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
    card.className = "card busy-card";
    card.dataset.busyId = busy.id;
    const resourceNames = busy.resourceIds
      .map(id => state.resourceMap.get(id)?.name || `#${id}`)
      .join(", ");
    card.appendChild(createElement("strong", null, busy.title || `Busy ${busy.id}`));
    card.appendChild(createElement("br"));
    card.appendChild(createElement("span", null, `${formatUtcDateTime(busy.startUtc)} - ${formatUtcDateTime(busy.endUtc)}`));
    card.appendChild(createElement("br"));
    appendResourceTags(card, busy.resourceIds);
    card.addEventListener("click", () => handleBusySelection(busy));
    busyEl.appendChild(card);
  });
}

function setSelectedProperty(propertyId) {
  state.selectedPropertyId = Number.isInteger(propertyId) && propertyId > 0 ? propertyId : null;
  state.propertyIds = state.selectedPropertyId ? getSelectedPropertyIds() : [];
  renderPropertyChips();
  renderResourceList();
  loadAll();
}

function renderPropertyChips() {
  if (state.propertyIds.length === 0) {
    propertyChipsEl.textContent = "No property filter.";
    return;
  }

  propertyChipsEl.innerHTML = "";
  state.propertyIds.forEach(id => {
    const chip = document.createElement("span");
    chip.className = "chip";
    const node = state.propertyMap.get(id);
    const definition = node ? state.propertyDefinitions.get(node.definitionId) : null;
    const prefix = definition ? `${definition.label}: ` : "";
    const label = node?.label || `#${id}`;
    chip.textContent = `${prefix}${label} (#${id})`;
    propertyChipsEl.appendChild(chip);
  });
}

function renderQuerySummary() {
  if (!querySummaryEl) {
    return;
  }

  const fromDate = isoDate(state.weekStart);
  const toDate = isoDate(new Date(state.weekStart.getTime() + 6 * 24 * 60 * 60 * 1000));
  const explain = document.getElementById("toggleExplain").checked;
  const includeDesc = document.getElementById("toggleDescendants").checked;

  const resources = Array.from(state.selectedIds).map(id => {
    const resource = state.resourceMap.get(id);
    if (!resource) {
      return {
        name: `#${id}`,
        typeLabel: "Unknown",
        typeKey: null,
      };
    }
    return resource;
  });

  const propertyText = state.selectedPropertyId
    ? formatPropertyLabel(state.selectedPropertyId, includeDesc)
    : null;
  const intent = buildExplorerIntent(resources, propertyText);

  querySummaryEl.innerHTML = "";
  querySummaryEl.appendChild(createElement("strong", null, "Current availability query"));
  querySummaryEl.appendChild(buildSummaryRow("Intent", intent));
  querySummaryEl.appendChild(buildSummaryRow("Range", `Week ${fromDate} -> ${toDate}`));
  querySummaryEl.appendChild(buildSummaryRow("Explain", explain ? "On" : "Off"));
}

function formatPropertyLabel(propertyId, includeDesc) {
  const node = state.propertyMap.get(propertyId);
  if (!node) {
    return `#${propertyId}`;
  }
  const definition = state.propertyDefinitions.get(node.definitionId);
  const prefix = definition ? `${definition.label}: ` : "";
  const suffix = includeDesc ? " (include descendants)" : "";
  return `${prefix}${node.label}${suffix}`;
}

function buildExplorerIntent(resources, propertyText) {
  if (resources.length === 0) {
    return "Looking for availability with no selected resources.";
  }

  const grouped = new Map();
  resources.forEach(resource => {
    const type = resource.typeLabel || resource.typeKey || "Unknown";
    if (!grouped.has(type)) {
      grouped.set(type, []);
    }
    grouped.get(type).push(resource.name || "Unknown");
  });

  const clauses = Array.from(grouped.entries()).map(([type, names]) => {
    if (names.length === 1) {
      return `${type} = ${names[0]}`;
    }
    return `${type} = ${names.join(" + ")}`;
  });

  const propertyClause = propertyText ? ` AND ${propertyText}` : "";
  return `Looking for availability where ${clauses.join(" AND ")}${propertyClause}.`;
}

function buildSummaryRow(labelText, valueText) {
  const row = createElement("div", "summary-row");
  row.appendChild(createElement("span", null, labelText));
  row.appendChild(createElement("div", null, valueText));
  return row;
}

function groupResourcesByType(resources) {
  const map = new Map();
  resources.forEach(resource => {
    const label = resource.typeLabel || resource.typeKey || "Unknown";
    if (!map.has(label)) {
      map.set(label, []);
    }
    map.get(label).push(resource);
  });

  return Array.from(map.entries())
    .sort((a, b) => a[0].localeCompare(b[0]))
    .map(([typeLabel, items]) => ({
      typeLabel,
      items: items.sort((a, b) => a.name.localeCompare(b.name))
    }));
}

function buildTypeLabelMap() {
  const map = new Map();
  state.resources.forEach(resource => {
    const label = resource.typeLabel || resource.typeKey || "Unknown";
    if (!map.has(resource.typeId)) {
      map.set(resource.typeId, label);
    }
  });
  return map;
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

function getAllowedDefinitionIds() {
  if (!state.typePropertyMap || state.typePropertyMap.size === 0) {
    return null;
  }
  const selectedResources = Array.from(state.selectedIds)
    .map(id => state.resourceMap.get(id))
    .filter(Boolean);
  if (selectedResources.length === 0) {
    return null;
  }

  let allowed = null;
  selectedResources.forEach(resource => {
    const defs = state.typePropertyMap.get(resource.typeId) || new Set();
    if (allowed === null) {
      allowed = new Set(defs);
    } else {
      allowed = intersectSets(allowed, defs);
    }
  });

  return allowed ?? new Set();
}

function intersectSets(left, right) {
  const result = new Set();
  left.forEach(value => {
    if (right.has(value)) {
      result.add(value);
    }
  });
  return result;
}

function refreshPropertyFilterForResources() {
  if (!state.selectedPropertyId) {
    renderPropertyGroups();
    renderPropertyChips();
    return;
  }

  const allowed = getAllowedDefinitionIds();
  if (allowed) {
    const node = state.propertyMap.get(state.selectedPropertyId);
    if (!node || !allowed.has(node.definitionId)) {
      state.selectedPropertyId = null;
      state.propertyIds = [];
    }
  }

  renderPropertyGroups();
  renderPropertyChips();
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

function renderPropertyGroups() {
  if (!propertyGroupsEl) {
    return;
  }

  propertyGroupsEl.innerHTML = "";
  if (!state.propertyNodes || state.propertyNodes.length === 0) {
    propertyGroupsEl.textContent = "No properties.";
    return;
  }

  const typeLabelMap = buildTypeLabelMap();
  const entries = Array.from(state.typePropertyMap.entries())
    .map(([typeId, defs]) => ({
      typeId,
      typeLabel: typeLabelMap.get(typeId) || `Type #${typeId}`,
      definitionIds: Array.from(defs)
    }))
    .sort((a, b) => a.typeLabel.localeCompare(b.typeLabel));

  if (entries.length === 0) {
    propertyGroupsEl.textContent = "No properties.";
    return;
  }

  const selectedNode = state.selectedPropertyId ? state.propertyMap.get(state.selectedPropertyId) : null;

  entries.forEach(entry => {
    const group = document.createElement("div");
    group.className = "property-group";
    const title = document.createElement("h3");
    title.textContent = `${entry.typeLabel} properties`;
    group.appendChild(title);

    const select = document.createElement("select");
    select.className = "property-select";
    select.innerHTML = "<option value=\"\">All properties</option>";

    const definitionIds = entry.definitionIds
      .map(id => state.propertyDefinitions.get(id))
      .filter(Boolean)
      .sort((a, b) => a.label.localeCompare(b.label))
      .map(def => def.id);

    definitionIds.forEach(definitionId => {
      const definition = state.propertyDefinitions.get(definitionId);
      const nodes = state.propertyNodes
        .filter(node => node.definitionId === definitionId && node.parentId != null)
        .sort((a, b) => a.label.localeCompare(b.label));
      if (nodes.length === 0) {
        return;
      }

      const optGroup = document.createElement("optgroup");
      optGroup.label = definition ? definition.label : `Definition #${definitionId}`;
      nodes.forEach(node => {
        const option = document.createElement("option");
        option.value = node.id;
        option.textContent = `${node.label} (#${node.id})`;
        if (selectedNode && selectedNode.id === node.id) {
          option.selected = true;
        }
        optGroup.appendChild(option);
      });
      select.appendChild(optGroup);
    });

    select.addEventListener("change", () => {
      const selected = Number(select.value);
      if (Number.isInteger(selected) && selected > 0) {
        state.selectedPropertyId = selected;
      } else {
        state.selectedPropertyId = null;
      }

      state.propertyIds = state.selectedPropertyId ? getSelectedPropertyIds() : [];
      renderPropertyChips();
      renderResourceList();
      loadAll();
      renderPropertyGroups();
    });

    group.appendChild(select);
    propertyGroupsEl.appendChild(group);
  });
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

function getSelectedPropertyIds() {
  if (!state.selectedPropertyId) {
    return [];
  }
  const includeDescendants = document.getElementById("toggleDescendants").checked;
  if (!includeDescendants) {
    return [state.selectedPropertyId];
  }
  const descendants = collectDescendants(state.selectedPropertyId);
  return [state.selectedPropertyId, ...descendants];
}

function collectDescendants(propertyId) {
  const result = [];
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

function filterResourcesByProperty(resources) {
  if (!state.selectedPropertyId || state.propertyIds.length === 0) {
    return resources;
  }
  const allowed = new Set(state.propertyIds);
  return resources.filter(resource => {
    const props = resource.properties || [];
    return props.some(prop => allowed.has(prop.id));
  });
}

function appendPropertyTags(container, properties) {
  if (!properties || properties.length === 0) {
    return false;
  }
  properties.forEach(prop => {
    const node = state.propertyMap.get(prop.id);
    const definition = node
      ? state.propertyDefinitions.get(node.definitionId)
      : prop.parentId
        ? state.propertyMap.get(prop.parentId)
        : null;
    const prefix = definition ? `${definition.label}: ` : "";
    const tag = createElement("span", "property-tag", `${prefix}${prop.label} (#${prop.id})`);
    container.appendChild(tag);
  });
  return true;
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

function formatResourceNames(resourceIds) {
  if (!resourceIds || resourceIds.length === 0) return "Resources: n/a";
  const names = resourceIds.map(id => state.resourceMap.get(id)?.name || `#${id}`);
  return `Resources: ${names.join(", ")}`;
}

function appendResourceTags(container, resourceIds) {
  if (!resourceIds || resourceIds.length === 0) {
    container.appendChild(createElement("span", "slot-meta", "No resources"));
    return;
  }
  resourceIds.forEach(id => {
    const resource = state.resourceMap.get(id);
    const name = resource?.name || `#${id}`;
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

function handleRuleSelection(rule) {
  state.selectedRuleId = state.selectedRuleId === rule.id ? null : rule.id;
  state.selectedBusyId = null;
  applyRuleHighlight(rule);
}

function handleBusySelection(busy) {
  state.selectedBusyId = state.selectedBusyId === busy.id ? null : busy.id;
  state.selectedRuleId = null;
  applyBusyHighlight(busy);
}

function clearHighlights() {
  selectionInfoEl.textContent = "Click a rule or busy interval to highlight related slots.";
  calendarEl.querySelectorAll(".slot").forEach(el => {
    el.classList.remove("highlight", "dimmed");
  });
  document.querySelectorAll(".rule-card").forEach(el => {
    el.classList.remove("selected", "dimmed");
  });
  document.querySelectorAll(".busy-card").forEach(el => {
    el.classList.remove("selected");
  });
}

function applyRuleHighlight(rule) {
  if (!state.selectedRuleId) {
    clearHighlights();
    return;
  }

  const count = countSlotsForRule(rule);
  selectionInfoEl.textContent = `Rule #${rule.id} contributes to ${count} slot${count === 1 ? "" : "s"} in the selected range.`;
  document.querySelectorAll(".rule-card").forEach(el => {
    el.classList.toggle("selected", Number(el.dataset.ruleId) === rule.id);
    el.classList.toggle("dimmed", Number(el.dataset.ruleId) !== rule.id);
  });

  calendarEl.querySelectorAll(".slot").forEach(el => {
    if (el.classList.contains("empty")) return;
    const startUtc = el.dataset.startUtc;
    const endUtc = el.dataset.endUtc;
    const resourceIds = el.dataset.resourceIds
      ? el.dataset.resourceIds.split(",").map(Number)
      : [];
    const match = doesRuleApplyToSlot(rule, startUtc, endUtc, resourceIds);
    el.classList.toggle("highlight", match);
    el.classList.toggle("dimmed", !match);
  });
}

function applyBusyHighlight(busy) {
  if (!state.selectedBusyId) {
    clearHighlights();
    return;
  }

  selectionInfoEl.textContent = `Busy interval blocks slots overlapping ${formatUtcTime(busy.startUtc)}-${formatUtcTime(busy.endUtc)}.`;
  document.querySelectorAll(".busy-card").forEach(el => {
    el.classList.toggle("selected", Number(el.dataset.busyId) === busy.id);
  });

  calendarEl.querySelectorAll(".slot").forEach(el => {
    if (el.classList.contains("empty")) return;
    const startUtc = el.dataset.startUtc;
    const endUtc = el.dataset.endUtc;
    const overlap = overlapsUtc(startUtc, endUtc, busy.startUtc, busy.endUtc);
    el.classList.toggle("highlight", overlap);
    el.classList.toggle("dimmed", !overlap);
  });
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

function showSlotTooltip(element, slot) {
  if (!document.getElementById("toggleExplain").checked) return;
  hideSlotTooltip(element);
  const rules = state.rules.filter(rule =>
    doesRuleApplyToSlot(rule, slot.startUtc, slot.endUtc, slot.resourceIds || []));
  const busyConflicts = state.busyEvents.filter(busy =>
    overlapsUtc(slot.startUtc, slot.endUtc, busy.startUtc, busy.endUtc));

  const tooltip = createElement("div", "tooltip");
  tooltip.appendChild(createElement("strong", null, "Slot analysis"));
  tooltip.appendChild(createElement("div", null, "Rules (match):"));
  if (rules.length === 0) {
    tooltip.appendChild(createElement("div", null, "- [NONE] No matching rules"));
  } else {
    rules.forEach(rule => {
      tooltip.appendChild(createElement("div", null, `- [MATCH] Rule #${rule.id} - ${rule.title || "Untitled"}`));
    });
  }
  tooltip.appendChild(createElement("div", null, "Busy overlaps (block):"));
  if (busyConflicts.length === 0) {
    tooltip.appendChild(createElement("div", null, "- [NONE] No busy overlaps"));
  } else {
    busyConflicts.forEach(busy => {
      tooltip.appendChild(createElement(
        "div",
        null,
        `- [BLOCK] Busy #${busy.id} - ${formatUtcTime(busy.startUtc)}-${formatUtcTime(busy.endUtc)}`));
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

async function resetDemo() {
  try {
    const res = await fetch(`${state.apiBaseUrl}/api/demo/reset`, { method: "POST" });
    if (res.status === 404) {
      alert("Reset is only available in Development.");
      return;
    }
    if (!res.ok) {
      alert("Reset failed.");
      return;
    }
    await loadAll();
  } catch (err) {
    alert(err.message);
  }
}

async function loadAll() {
  weekLabelEl.textContent = formatRangeLabel(state.weekStart);
  renderQuerySummary();
  setComputeStatus("Computing...");
  try {
    await Promise.all([loadAvailability(), loadDemoSummary()]);
    setComputeStatus("Computed");
  } catch (err) {
    setComputeStatus("Failed");
    calendarEl.innerHTML = "";
    const day = createElement("div", "day");
    day.appendChild(createElement("div", "slot empty", err.message));
    calendarEl.appendChild(day);
  }
}

function setComputeStatus(message) {
  if (!computeStatusEl) return;
  computeStatusEl.textContent = message;
}

async function init() {
  state.weekStart = getWeekStart(new Date());
  await loadConfig();
  await waitForApiReady();
}

init();

async function bootstrapAfterApiReady() {
  await loadPropertyCatalog();
  await loadResources();
  renderPropertyChips();
  await loadAll();
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
