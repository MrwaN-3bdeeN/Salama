(function() {
  function getPreferredTheme() {
    var stored = localStorage.getItem('salama_theme');
    if (stored) return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('salama_theme', theme);
    updateIcon(theme);
  }

  function updateIcon(theme) {
    var icon = document.getElementById('themeIcon');
    if (icon) {
      icon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
    }
  }

  function toggleTheme() {
    var current = document.documentElement.getAttribute('data-theme');
    applyTheme(current === 'dark' ? 'light' : 'dark');
  }

  applyTheme(getPreferredTheme());

  window.toggleTheme = toggleTheme;

  document.addEventListener('DOMContentLoaded', function() {
    updateIcon(document.documentElement.getAttribute('data-theme'));
  });

  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
    if (!localStorage.getItem('salama_theme')) {
      applyTheme(e.matches ? 'dark' : 'light');
    }
  });
})();
