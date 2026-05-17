// ── AUTH GUARD ──────────────────────────────────────────────────────────────
// Redirects to login if not authenticated (sessionStorage flag).
(function() {
  const isLoginPage = window.location.pathname.endsWith('index.html')
    || window.location.pathname === '/'
    || window.location.pathname.endsWith('/');

  if (!isLoginPage && !sessionStorage.getItem('libsys_auth')) {
    window.location.href = 'index.html';
    return;
  }
})();

// ── PAGE MAP ────────────────────────────────────────────────────────────────
const PAGES = {
  dashboard:  { file: 'dashboard.html',  title: 'Dashboard' },
  loans:      { file: 'loans.html',      title: 'Loans' },
  books:      { file: 'books.html',      title: 'Books' },
  authors:    { file: 'authors.html',    title: 'Authors' },
  categories: { file: 'categories.html', title: 'Categories' },
  members:    { file: 'members.html',    title: 'Members' },
  users:      { file: 'users.html',      title: 'User Management' },
  reports:    { file: 'reports.html',    title: 'Reports' },
  about:      { file: 'about.html',      title: 'About' },
};

// ── DETECT CURRENT PAGE ─────────────────────────────────────────────────────
const _filename = window.location.pathname.split('/').pop().replace('.html', '') || 'dashboard';
const CURRENT_PAGE = Object.keys(PAGES).includes(_filename) ? _filename : 'dashboard';

// ── INJECT SIDEBAR + TOPBAR ─────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  // Mark active nav item
  document.querySelectorAll('.nav-item[data-page]').forEach(el => {
    el.classList.toggle('active', el.dataset.page === CURRENT_PAGE);
  });

  // Set topbar title
  const titleEl = document.getElementById('topbar-title');
  if (titleEl) titleEl.textContent = PAGES[CURRENT_PAGE]?.title || 'LibSys';

  // Set document title
  document.title = `LibSys — ${PAGES[CURRENT_PAGE]?.title || 'App'}`;

  // Sidebar nav clicks → navigate to file
  document.querySelectorAll('.nav-item[data-page]').forEach(el => {
    el.addEventListener('click', e => {
      e.preventDefault();
      const page = el.dataset.page;
      if (page && PAGES[page]) window.location.href = PAGES[page].file;
    });
  });

  // Sidebar toggle
  document.getElementById('sidebar-toggle')?.addEventListener('click', () => {
    document.querySelector('.sidebar').classList.toggle('collapsed');
  });

  // Logout
  document.getElementById('logout-btn')?.addEventListener('click', () => {
    sessionStorage.removeItem('libsys_auth');
    sessionStorage.removeItem('libsys_user');
    window.location.href = 'index.html';
  });

  // Toast container must exist
  if (!document.getElementById('toast-container')) {
    const tc = document.createElement('div');
    tc.className = 'toast-container';
    tc.id = 'toast-container';
    document.body.appendChild(tc);
  }

  // Close modals on overlay click
  document.querySelectorAll('.modal-overlay').forEach(overlay => {
    overlay.addEventListener('click', e => {
      if (e.target === overlay) overlay.classList.remove('open');
    });
  });
});

// ── NAVIGATE HELPER (cross-page) ────────────────────────────────────────────
function navigateTo(page) {
  if (PAGES[page]) window.location.href = PAGES[page].file;
}

// ── SIDEBAR TOGGLE ───────────────────────────────────────────────────────────
function toggleSidebar() {
  document.querySelector('.sidebar').classList.toggle('collapsed');
}

// ── TOAST ────────────────────────────────────────────────────────────────────
function toast(msg, type = 'success') {
  const svgIcons = {
    success: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>',
    error:   '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>',
    warn:    '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
    info:    '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>',
  };
  const el = document.createElement('div');
  el.className = `toast toast-${type}`;
  el.innerHTML = `<span style="display:flex;align-items:center">${svgIcons[type] || svgIcons.info}</span><span>${msg}</span>`;
  document.getElementById('toast-container').appendChild(el);
  setTimeout(() => el.remove(), 3500);
}

// ── MODAL ────────────────────────────────────────────────────────────────────
function openModal(id)  { document.getElementById(id)?.classList.add('open'); }
function closeModal(id) { document.getElementById(id)?.classList.remove('open'); }
