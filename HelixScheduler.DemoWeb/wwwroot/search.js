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
};

const requirementsEl = document.getElementById("requirements");
const weekLabelEl = document.getElementById("weekLabel");
const calendarEl = document.getElementById("calendar");
const busyCalendarEl = document.getElementById("busyCalendar");
const rulesEl = document.getElementById("rules");
const busyEl = document.getElementById("busy");
const explainEl = document.getElementById("explain");
const summaryEl = document.getElementById("searchSummary");
const ruleImpactEl = document.getElementById("ruleImpactInfo");
const computeStatusEl = document.getElementById("computeStatus");
const busyNoteEl = document.getElementById("busyNote");
const errorEl = document.getElementById("searchError");
const apiStatusEl = document.getElementById("apiStatus");
const apiStatusMessageEl = document.getElementById("apiStatusMessage");
const apiRetryEl = document.getElementById("apiRetry");
let apiCheckInFlight = false;

document.getElementById("prevWeek").addEventListener("click", () => shiftWeek(-7));
document.getElementById("nextWeek").addEventListener("click", () => shiftWeek(7));
document.getElementById("addRequirement").addEventListener("click", () => addRequirement());
document.getElementById("computeSearch").addEventListener("click", () => computeSearch());
document.getElementById("resetSearch").addEventListener("click", () => resetBuilder());
apiRetryEl.addEventListener("click", () => retryApiReady());

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
    fetch(`${state.apiBaseUrl}/api/catalog/resources?onlySchedulable=true`),
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
  addRequirement();
  renderRequirements();
  summaryEl.textContent = "Waiting for query.";
  calendarEl.innerHTML = "";
  busyCalendarEl.innerHTML = "";
  rulesEl.textContent = "No compute yet.";
  busyEl.textContent = "No compute yet.";
  explainEl.textContent = "Explain disabled.";
  busyNoteEl.textContent = "Busy intervals that remove or split availability.";
  errorEl.textContent = "";
}

function renderRequirements() {
  if (state.requirements.length === 0) {
    requirementsEl.textContent = "Add a resource type.";
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

    grid.appendChild(buildSelectRow("Resource type", buildTypeSelect(req)));
    grid.appendChild(buildSelectRow("Specific resource", buildResourceSelect(req)));

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
}

function buildSelectRow(labelText, element) {
  const wrapper = document.createElement("div");
  wrapper.className = "input-row";
  const label = document.createElement("label");
  label.textContent = labelText;
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
    option.textContent = `${type.label} (#${type.id})`;
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
      option.textContent = `${resource.name} (#${resource.id})`;
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
      option.textContent = `${def.label} (#${def.id})`;
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
    const nodes = state.propertyNodes
      .filter(node => node.definitionId === req.definitionId && node.parentId !== null)
      .sort((a, b) => a.label.localeCompare(b.label));
    nodes.forEach(node => {
      const option = document.createElement("option");
      option.value = node.id;
      option.textContent = `${node.label} (#${node.id})`;
      if (req.nodeId === node.id) {
        option.selected = true;
      }
      nodeSelect.appendChild(option);
    });
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
  includeText.textContent = "Include descendants";
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
  const names = matches.slice(0, 10).map(resource => resource.name);
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
  const suffix = filter.includeDescendants ? " (include descendants)" : "";
  return `${prefix}${node.label}${suffix}`;
}

async function computeSearch() {
  errorEl.textContent = "";
  setComputeStatus("Computing...");
  const requirements = state.requirements.filter(req => req.typeId);
  if (requirements.length === 0) {
    errorEl.textContent = "Add at least one resource type.";
    setComputeStatus("Waiting");
    return;
  }

  const requiredResourceIds = [];
  const resourceOrGroups = [];
  const previewResources = new Set();

  for (const req of requirements) {
    const candidates = getMatchingResources(req);
    if (req.resourceId) {
      if (candidates.length === 0) {
        errorEl.textContent = "Selected resource does not match the chosen type.";
        return;
      }
      requiredResourceIds.push(req.resourceId);
      previewResources.add(req.resourceId);
      continue;
    }

    if (candidates.length === 0) {
      errorEl.textContent = "Some requirements have no matching resources.";
      setComputeStatus("Waiting");
      return;
    }

    const groupIds = candidates.map(resource => resource.id);
    resourceOrGroups.push(groupIds);
    groupIds.forEach(id => previewResources.add(id));
  }

  const payload = {
    fromDate: isoDate(state.weekStart),
    toDate: isoDate(new Date(state.weekStart.getTime() + 6 * 24 * 60 * 60 * 1000)),
    requiredResourceIds,
    resourceOrGroups,
    explain: document.getElementById("toggleExplain").checked
  };

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
  summaryEl.innerHTML = "";
  summaryEl.appendChild(createElement("strong", null, "Current availability query"));
  summaryEl.appendChild(buildSummaryRow("Intent", intent));
  summaryEl.appendChild(buildSummaryRow("Range", `Week ${payload.fromDate} -> ${payload.toDate}`));
  summaryEl.appendChild(buildSummaryRow("Explain", payload.explain ? "On" : "Off"));
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
    const typeLabel = state.resourceTypes.find(t => t.id === req.typeId)?.label || `#${req.typeId}`;
    const filters = req.filters.map(formatFilterLabel).join(" AND ");
    if (req.resourceId) {
      const resource = state.resourceMap.get(req.resourceId);
      const resourceLabel = resource ? resource.name : `#${req.resourceId}`;
      return `${typeLabel} = ${resourceLabel}${filters ? ` AND ${filters}` : ""}`;
    }
    return `${typeLabel} = any${filters ? ` AND ${filters}` : ""}`;
  });

  return `Looking for availability where ${clauses.join(" AND ")}.`;
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
  ruleImpactEl.textContent = `Rule #${resolved.id} contributes to ${count} slot${count === 1 ? "" : "s"} in the selected range.`;
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
    card.className = "card";
    card.dataset.ruleId = rule.id;
    const dateRange = formatRuleDateRange(rule);
    card.appendChild(createElement("strong", null, rule.title || `Rule ${rule.id}`));
    card.appendChild(createElement("br"));
    if (dateRange) {
      card.appendChild(document.createTextNode(dateRange));
      card.appendChild(createElement("br"));
    }
    card.appendChild(document.createTextNode(`${formatTimeRange(rule.startTime, rule.endTime)} UTC`));
    card.appendChild(createElement("br"));
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
    card.appendChild(createElement("strong", null, busy.title || `Busy ${busy.id}`));
    card.appendChild(createElement("br"));
    card.appendChild(createElement("span", null, `${formatUtcDateTime(busy.startUtc)} - ${formatUtcDateTime(busy.endUtc)}`));
    card.appendChild(createElement("br"));
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
