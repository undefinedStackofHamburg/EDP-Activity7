// Injects the shared sidebar + topbar into the app-shell on every page
(function injectShell() {
  const sidebarHTML = `
  <aside class="sidebar" id="sidebar">
    <div class="sidebar-brand">
      <div class="brand-icon">
        <img src="LibSysLogox64.png" alt="LibSys"/>
      </div>
      <div class="brand-text">Lib<span>Sys</span></div>
    </div>
    <nav class="sidebar-nav">
      <div class="nav-section-label">Main</div>
      <a class="nav-item" data-page="dashboard" href="dashboard.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>
        <span class="nav-label">Dashboard</span>
      </a>
      <a class="nav-item" data-page="loans" href="loans.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2"/><rect x="9" y="3" width="6" height="4" rx="1"/><path d="M9 12h6M9 16h4"/></svg>
        <span class="nav-label">Loans</span>
      </a>
      <div class="nav-section-label" style="margin-top:8px">Catalog</div>
      <a class="nav-item" data-page="books" href="books.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>
        <span class="nav-label">Books</span>
      </a>
      <a class="nav-item" data-page="authors" href="authors.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
        <span class="nav-label">Authors</span>
      </a>
      <a class="nav-item" data-page="categories" href="categories.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
        <span class="nav-label">Categories</span>
      </a>
      <div class="nav-section-label" style="margin-top:8px">People</div>
      <a class="nav-item" data-page="members" href="members.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>
        <span class="nav-label">Members</span>
      </a>
      <a class="nav-item" data-page="users" href="users.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="8" r="4"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/><circle cx="19" cy="8" r="2.5"/><path d="M22 14c1.5.5 2 1.5 2 2.5"/><circle cx="5" cy="8" r="2.5"/><path d="M2 14c-1.5.5-2 1.5-2 2.5"/></svg>
        <span class="nav-label">Users</span>
      </a>
      <div class="nav-section-label" style="margin-top:8px">Tools</div>
      <a class="nav-item" data-page="reports" href="reports.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><polyline points="6 9 6 2 18 2 18 9"/><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/></svg>
        <span class="nav-label">Reports</span>
      </a>
      <a class="nav-item" data-page="about" href="about.html">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
        <span class="nav-label">About</span>
      </a>
    </nav>
    <div class="sidebar-footer">
      <a class="nav-item" id="logout-btn" href="index.html" style="color:var(--muted)">
        <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
        <span class="nav-label">Sign Out</span>
      </a>
    </div>
  </aside>`;

  const topbarHTML = `
  <header class="topbar">
    <button class="topbar-toggle" id="sidebar-toggle">
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/></svg>
    </button>
    <span class="topbar-title" id="topbar-title">LibSys</span>
    <div class="topbar-actions">
      <span id="topbar-username" style="font-size:0.72rem;color:var(--muted);font-family:var(--font-mono)"></span>
      <div class="avatar" style="background:rgba(232,98,26,0.15);color:var(--accent2);font-size:0.65rem;margin-left:6px" id="topbar-avatar">AD</div>
    </div>
  </header>`;

  const shell = document.getElementById('app-shell');
  if (!shell) return;
  shell.insertAdjacentHTML('afterbegin', sidebarHTML);
  const mainArea = shell.querySelector('.main-area');
  if (mainArea) mainArea.insertAdjacentHTML('afterbegin', topbarHTML);

  // Show logged-in user name + initials in topbar
  try {
    const u = JSON.parse(sessionStorage.getItem('libsys_user') || '{}');
    if (u.username) {
      const displayName = u.fullName || u.username;
      const parts = displayName.split(' ');
      const ini = ((parts[0]?.[0] || '') + (parts[1]?.[0] || '')).toUpperCase() || 'AD';
      const avatarEl = document.getElementById('topbar-avatar');
      if (avatarEl) avatarEl.textContent = ini;
      const nameEl = document.getElementById('topbar-username');
      if (nameEl) nameEl.textContent = displayName;
    }
  } catch (_) {}
})();
