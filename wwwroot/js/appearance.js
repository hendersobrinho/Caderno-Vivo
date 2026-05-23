// appearance.js — Caderno Vivo system appearance panel
// Persists user preferences in localStorage and applies them site-wide.
// Loaded once in _Layout.cshtml; survives navigation between pages.

(function () {
  'use strict';

  const STORE_KEY = 'cv:appearance';
  const DEFAULTS = {
    theme:    'light',
    accent:   'violet',
    sidebar:  'ink',
    neutral:  'stone',
    brand:    'caveat',
    density:  'compact',
  };

  function load() {
    try {
      return { ...DEFAULTS, ...JSON.parse(localStorage.getItem(STORE_KEY) || '{}') };
    } catch (_) {
      return { ...DEFAULTS };
    }
  }

  function save(s) {
    try { localStorage.setItem(STORE_KEY, JSON.stringify(s)); } catch (_) {}
  }

  function apply(s) {
    const h = document.documentElement;
    h.setAttribute('data-theme',    s.theme);
    h.setAttribute('data-sidebar',  s.sidebar);
    h.setAttribute('data-brand',    s.brand);
    h.setAttribute('data-density',  s.density);

    if (s.neutral && s.neutral !== 'stone')
      h.setAttribute('data-neutral', s.neutral);
    else
      h.removeAttribute('data-neutral');

    if (s.accent && s.accent !== 'blue')
      h.setAttribute('data-accent', s.accent);
    else
      h.removeAttribute('data-accent');
  }

  // ─── Apply ASAP (before DOMContentLoaded) to avoid FOUC ───
  let state = load();
  apply(state);

  // Expose for early-init inline use
  window.CVAppearance = { load, save, apply, DEFAULTS };

  // ─── Wire up the panel once DOM ready ───
  function init() {
    const trigger = document.getElementById('appearance-btn');
    const panel = document.getElementById('appearance-panel');
    if (!trigger || !panel) return;

    function syncControls() {
      // selects
      panel.querySelectorAll('select[data-key]').forEach(sel => {
        sel.value = state[sel.dataset.key];
      });
      // segmented / swatches
      panel.querySelectorAll('[data-group]').forEach(grp => {
        const key = grp.dataset.group;
        grp.querySelectorAll('button[data-val]').forEach(btn => {
          btn.classList.toggle('active', btn.dataset.val === state[key]);
        });
      });
    }

    function update(key, value) {
      state = { ...state, [key]: value };
      save(state);
      apply(state);
      syncControls();
      blinkSaved();
    }

    let savedTimer;
    function blinkSaved() {
      const tag = panel.querySelector('.ap-saved');
      if (!tag) return;
      tag.style.opacity = '1';
      clearTimeout(savedTimer);
      savedTimer = setTimeout(() => { tag.style.opacity = '0.5'; }, 1200);
    }

    // selects
    panel.querySelectorAll('select[data-key]').forEach(sel => {
      sel.addEventListener('change', () => update(sel.dataset.key, sel.value));
    });

    // grouped buttons (segmented + swatches)
    panel.querySelectorAll('[data-group]').forEach(grp => {
      const key = grp.dataset.group;
      grp.querySelectorAll('button[data-val]').forEach(btn => {
        btn.addEventListener('click', () => update(key, btn.dataset.val));
      });
    });

    // reset
    const reset = panel.querySelector('.ap-reset');
    if (reset) {
      reset.addEventListener('click', () => {
        state = { ...DEFAULTS };
        save(state);
        apply(state);
        syncControls();
        blinkSaved();
      });
    }

    // toggle open / close
    function open() {
      panel.hidden = false;
      trigger.setAttribute('aria-expanded', 'true');
      syncControls();
    }
    function close() {
      panel.hidden = true;
      trigger.setAttribute('aria-expanded', 'false');
    }

    trigger.addEventListener('click', (e) => {
      e.stopPropagation();
      panel.hidden ? open() : close();
    });

    document.addEventListener('click', (e) => {
      if (panel.hidden) return;
      if (panel.contains(e.target) || trigger.contains(e.target)) return;
      close();
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && !panel.hidden) close();
    });

    // close button inside panel
    panel.querySelector('.ap-close')?.addEventListener('click', close);

    syncControls();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
