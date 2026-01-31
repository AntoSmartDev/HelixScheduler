window.DemoFormat = {
  showIdsEnabled: function () {
    const toggle = document.getElementById("toggleShowIds");
    return toggle ? toggle.checked : false;
  },
  formatIdLabel: function (label, id) {
    if (!label) {
      return `#${id}`;
    }
    return this.showIdsEnabled() ? `${label} (#${id})` : label;
  },
  formatTypeLabel: function (type) {
    if (!type) {
      return "Type";
    }
    if (!type.label) {
      return `Type #${type.id}`;
    }
    return this.formatIdLabel(type.label, type.id);
  },
  formatDefinitionLabel: function (def) {
    if (!def) {
      return "Definition";
    }
    if (!def.label) {
      return `Definition #${def.id}`;
    }
    return this.formatIdLabel(def.label, def.id);
  },
  formatPropertyLabelSimple: function (id, propertyMap) {
    const node = propertyMap?.get(id);
    if (!node) {
      return `#${id}`;
    }
    return this.formatIdLabel(node.label, node.id);
  },
  formatResourceLabel: function (id, resourceMap) {
    const resource = resourceMap?.get(id);
    if (!resource) {
      return `#${id}`;
    }
    return this.formatIdLabel(resource.name, resource.id);
  },
  formatRuleLabel: function (rule) {
    if (!rule) {
      return "Rule";
    }
    return this.showIdsEnabled() ? `Rule #${rule.id}` : "Rule";
  },
  formatBusyLabel: function (busy) {
    if (!busy) {
      return "Busy";
    }
    return this.showIdsEnabled() ? `Busy #${busy.id}` : "Busy";
  }
};

function initNavPopovers() {
  const links = document.querySelectorAll(".nav-link[data-tooltip]");
  if (!links.length) {
    return;
  }

  let popover = null;
  let activeLink = null;

  function ensurePopover() {
    if (popover) {
      return popover;
    }
    popover = document.createElement("div");
    popover.className = "nav-popover";
    document.body.appendChild(popover);
    return popover;
  }

  function showPopover(link) {
    const text = link.getAttribute("data-tooltip");
    if (!text) {
      return;
    }
    const el = ensurePopover();
    el.textContent = text;
    el.style.opacity = "1";
    el.style.pointerEvents = "none";
    const rect = link.getBoundingClientRect();
    const popRect = el.getBoundingClientRect();
    const spacing = 8;
    let top = rect.bottom + spacing;
    const centerX = rect.left + rect.width / 2;
    let left = centerX - popRect.width / 2;
    const maxLeft = window.innerWidth - popRect.width - 8;
    if (left < 8) left = 8;
    if (left > maxLeft) left = maxLeft;
    let flipped = false;
    if (top + popRect.height > window.innerHeight) {
      top = rect.top - popRect.height - spacing;
      flipped = true;
    }
    const arrowLeft = Math.max(12, Math.min(popRect.width - 12, centerX - left));
    el.style.setProperty("--arrow-left", `${arrowLeft}px`);
    if (flipped) {
      el.classList.add("flipped");
    } else {
      el.classList.remove("flipped");
    }
    el.style.top = `${Math.max(8, top)}px`;
    el.style.left = `${left}px`;
  }

  function hidePopover() {
    if (!popover) {
      return;
    }
    popover.style.opacity = "0";
  }

  function initPayloadToggles() {
    const toggles = document.querySelectorAll(".toggle-payload");
    toggles.forEach(btn => {
      btn.addEventListener("click", () => {
        const targetId = btn.getAttribute("data-target");
        const target = targetId ? document.getElementById(targetId) : null;
        if (!target) {
          return;
        }
        const expanded = target.classList.toggle("expanded");
        btn.textContent = expanded ? "Collapse payload" : "Expand payload";
      });
    });
  }

  links.forEach(link => {
    link.addEventListener("mouseenter", () => {
      activeLink = link;
      showPopover(link);
    });
    link.addEventListener("mouseleave", () => {
      if (activeLink === link) {
        activeLink = null;
      }
      hidePopover();
    });
    link.addEventListener("focus", () => {
      activeLink = link;
      showPopover(link);
    });
    link.addEventListener("blur", () => {
      if (activeLink === link) {
        activeLink = null;
      }
      hidePopover();
    });
  });

  window.addEventListener("scroll", () => {
    if (activeLink) {
      showPopover(activeLink);
    }
  });
  window.addEventListener("resize", () => {
    if (activeLink) {
      showPopover(activeLink);
    }
  });

  initPayloadToggles();
}

function initInfoPopovers() {
  const icons = document.querySelectorAll(".info-icon[data-tooltip]");
  if (!icons.length) {
    return;
  }

  let popover = null;
  let activeIcon = null;

  function ensurePopover() {
    if (popover) {
      return popover;
    }
    popover = document.createElement("div");
    popover.className = "info-popover";
    document.body.appendChild(popover);
    return popover;
  }

  function showPopover(icon) {
    const text = icon.getAttribute("data-tooltip");
    if (!text) {
      return;
    }
    const el = ensurePopover();
    el.textContent = text;
    el.style.opacity = "1";
    el.style.pointerEvents = "none";
    const rect = icon.getBoundingClientRect();
    const popRect = el.getBoundingClientRect();
    const spacing = 8;
    let top = rect.bottom + spacing;
    const centerX = rect.left + rect.width / 2;
    let left = centerX - popRect.width / 2;
    const maxLeft = window.innerWidth - popRect.width - 8;
    if (left < 8) left = 8;
    if (left > maxLeft) left = maxLeft;
    let flipped = false;
    if (top + popRect.height > window.innerHeight) {
      top = rect.top - popRect.height - spacing;
      flipped = true;
    }
    const arrowLeft = Math.max(12, Math.min(popRect.width - 12, centerX - left));
    el.style.setProperty("--arrow-left", `${arrowLeft}px`);
    if (flipped) {
      el.classList.add("flipped");
    } else {
      el.classList.remove("flipped");
    }
    el.style.top = `${Math.max(8, top)}px`;
    el.style.left = `${left}px`;
  }

  function hidePopover() {
    if (!popover) {
      return;
    }
    popover.style.opacity = "0";
  }

  icons.forEach(icon => {
    icon.addEventListener("mouseenter", () => {
      activeIcon = icon;
      showPopover(icon);
    });
    icon.addEventListener("mouseleave", () => {
      if (activeIcon === icon) {
        activeIcon = null;
      }
      hidePopover();
    });
    icon.addEventListener("focus", () => {
      activeIcon = icon;
      showPopover(icon);
    });
    icon.addEventListener("blur", () => {
      if (activeIcon === icon) {
        activeIcon = null;
      }
      hidePopover();
    });
  });

  window.addEventListener("scroll", () => {
    if (activeIcon) {
      showPopover(activeIcon);
    }
  });
  window.addEventListener("resize", () => {
    if (activeIcon) {
      showPopover(activeIcon);
    }
  });
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", () => {
    initNavPopovers();
    initInfoPopovers();
  });
} else {
  initNavPopovers();
  initInfoPopovers();
}
