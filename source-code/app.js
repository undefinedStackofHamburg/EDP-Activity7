// ============================================================
//  app.js  —  LibSys Frontend Data Layer
//  ──────────────────────────────────────
//  WHAT CHANGED from the original:
//  The hardcoded DB object is GONE. Every function now makes
//  a real HTTP request to the C# backend API.
//
//  HOW fetch() WORKS:
//  fetch(url)              → GET request (read data)
//  fetch(url, {method:'POST', body: JSON.stringify(data)})
//                          → POST request (create/send data)
//  fetch(url, {method:'PUT', ...})  → update
//  fetch(url, {method:'DELETE'})    → delete
//
//  Every API call returns a Promise. We use async/await to
//  wait for the response without freezing the browser.
//
//  API BASE URL — change this if your C# API runs elsewhere
// ============================================================

const API = 'http://localhost:5000/api';

// ── SHARED FETCH HELPER ───────────────────────────────────────
// Wraps fetch() to handle JSON parsing and errors in one place.
// Every API call in this file goes through here.
async function apiFetch(path, options = {}) {
  try {
    const res = await fetch(`${API}${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...options
    });
    const json = await res.json();

    if (!res.ok || !json.success) {
      // Show error toast and throw so callers can catch it
      toast(json.message || 'An error occurred.', 'error');
      throw new Error(json.message || 'API error');
    }
    return json.data; // return the actual data payload
  } catch (err) {
    if (err.message === 'Failed to fetch') {
      toast('Cannot reach the API server. Is it running?', 'error');
    }
    throw err;
  }
}

// ── HELPERS ───────────────────────────────────────────────────
// These still work the same way — just operate on data arrays
// returned by the API instead of the old local DB object.

function authorName(authors, id) {
  const a = authors?.find(a => a.id === id);
  return a ? `${a.firstName} ${a.lastName}` : '—';
}
function categoryName(categories, id) {
  const c = categories?.find(c => c.id === id);
  return c ? c.name : '—';
}
function memberName(members, id) {
  const m = members?.find(m => m.id === id);
  return m ? `${m.firstName} ${m.lastName}` : '—';
}

function statusBadge(s) {
  const map = {
    Active:    'status-active',
    Returned:  'status-returned',
    Overdue:   'status-overdue',
    Suspended: 'status-suspended',
    Expired:   'status-expired'
  };
  return `<span class="status ${map[s] || ''}">${s}</span>`;
}

function avatarColor(name) {
  const colors = [
    ['#E8842A','#FEE4CC'],['#F5C842','#FEF3CC'],['#7ABF68','#E6F5E1'],
    ['#6090D8','#E1EBFA'],['#C87890','#FAE1E8'],['#A068D8','#EDE1FA'],
  ];
  let hash = 0;
  for (const c of name) hash = (hash * 31 + c.charCodeAt(0)) % colors.length;
  return colors[Math.abs(hash)];
}
function initials(fn, ln) {
  return ((fn?.[0] || '') + (ln?.[0] || '')).toUpperCase();
}
function formatDate(d) {
  if (!d) return '<span style="color:var(--muted)">—</span>';
  return new Date(d).toLocaleDateString('en-PH', {
    year: 'numeric', month: 'short', day: 'numeric'
  });
}

// ── NAVIGATION ────────────────────────────────────────────────
const PAGE_FILES = {
  dashboard:  'dashboard.html',
  loans:      'loans.html',
  books:      'books.html',
  authors:    'authors.html',
  categories: 'categories.html',
  members:    'members.html',
  reports:    'reports.html',
  about:      'about.html',
};
function navigate(page) {
  if (PAGE_FILES[page]) window.location.href = PAGE_FILES[page];
}

// ── SIDEBAR / TOAST / MODAL ───────────────────────────────────
function toggleSidebar() {
  document.querySelector('.sidebar')?.classList.toggle('collapsed');
}
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
  document.getElementById('toast-container')?.appendChild(el);
  setTimeout(() => el.remove(), 3500);
}
function openModal(id)  { document.getElementById(id)?.classList.add('open'); }
function closeModal(id) { document.getElementById(id)?.classList.remove('open'); }

// ── LOADING STATE HELPER ──────────────────────────────────────
// Shows a "Loading…" row in a table while waiting for API
function tableLoading(tbodyId, cols) {
  const el = document.getElementById(tbodyId);
  if (el) el.innerHTML = `
    <tr><td colspan="${cols}" style="text-align:center;padding:40px;color:var(--muted)">
      Loading…
    </td></tr>`;
}

// ═════════════════════════════════════════════════════════════
//  DASHBOARD
// ═════════════════════════════════════════════════════════════
async function renderDashboard() {
  try {
    // Fetch all data in parallel — much faster than sequential calls
    const [books, members, loans] = await Promise.all([
      apiFetch('/books'),
      apiFetch('/members'),
      apiFetch('/loans')
    ]);

    const totalBooks   = books.length;
    const totalCopies  = books.reduce((s, b) => s + b.total, 0);
    const totalAvail   = books.reduce((s, b) => s + b.available, 0);
    const activeLoans  = loans.filter(l => l.status === 'Active').length;
    const overdueLoans = loans.filter(l => l.status === 'Overdue').length;
    const totalFines   = loans.reduce((s, l) => s + l.fineAmount, 0);
    const availPct     = totalCopies ? Math.round((totalAvail / totalCopies) * 100) : 0;
    const loanPct      = totalCopies ? Math.round(((totalCopies - totalAvail) / totalCopies) * 100) : 0;

    document.getElementById('stat-books').textContent          = totalBooks;
    document.getElementById('stat-members').textContent        = members.length;
    document.getElementById('stat-active-loans').textContent   = activeLoans;
    document.getElementById('stat-overdue').textContent        = overdueLoans;
    document.getElementById('stat-fines').textContent          = `₱${totalFines.toFixed(2)}`;
    document.getElementById('stat-available-inline').textContent = totalAvail;
    document.getElementById('ring-pct-label').textContent      = availPct + '%';

    // Ring chart arcs
    const C = 2 * Math.PI * 50;
    const availArc = (availPct / 100) * C;
    const loanArc  = (loanPct  / 100) * C;
    const rAvail = document.getElementById('ring-avail');
    const rLoan  = document.getElementById('ring-loan');
    if (rAvail) {
      rAvail.style.strokeDasharray  = `${availArc} ${C}`;
      rAvail.style.strokeDashoffset = '0';
    }
    if (rLoan) {
      rLoan.style.strokeDasharray  = `${loanArc} ${C}`;
      rLoan.style.strokeDashoffset = `-${availArc}`;
    }

    // Activity feed
    const feed = document.getElementById('db-activity-list');
    if (!feed) return;
    const recent = [...loans].reverse().slice(0, 7);
    const statusDot = {
      Active: '#4A7DC0', Returned: 'var(--accent)',
      Overdue: 'var(--red)', Suspended: 'var(--amber)', Expired: '#aaa'
    };
    feed.innerHTML = recent.map(l => {
      const nameParts = (l.memberName || '').split(' ');
      const [ac, bc] = avatarColor(l.memberName || 'x');
      const dot = statusDot[l.status] || '#aaa';
      return `<div class="db-activity-row">
        <div class="db-act-dot" style="background:${dot}"></div>
        <div class="avatar db-act-avatar"
             style="background:${bc};color:${ac};font-size:0.6rem;width:28px;height:28px">
          ${initials(nameParts[0] || '', nameParts[1] || '')}
        </div>
        <div class="db-act-info">
          <span class="db-act-name">${l.memberName}</span>
          <span class="db-act-book">${(l.bookTitle || '—').slice(0, 28)}${(l.bookTitle || '').length > 28 ? '…' : ''}</span>
        </div>
        <div class="db-act-right">
          ${statusBadge(l.status)}
          <span class="db-act-date">${formatDate(l.dueDate)}</span>
        </div>
      </div>`;
    }).join('');

  } catch (err) {
    console.error('Dashboard error:', err);
  }
}

// ═════════════════════════════════════════════════════════════
//  BOOKS
// ═════════════════════════════════════════════════════════════
let bookSearch = '';

async function renderBooks() {
  tableLoading('books-tbody', 7);
  try {
    const books = await apiFetch('/books');
    const filtered = books.filter(b => {
      const s = bookSearch.toLowerCase();
      return !s
        || b.title.toLowerCase().includes(s)
        || (b.authorName || '').toLowerCase().includes(s)
        || (b.isbn || '').includes(s);
    });

    document.getElementById('books-count').textContent = `${filtered.length} books`;
    const tbody = document.getElementById('books-tbody');

    if (!filtered.length) {
      tbody.innerHTML = `<tr><td colspan="7">
        <div class="empty-state"><p>No books found.</p></div>
      </td></tr>`;
      return;
    }

    tbody.innerHTML = filtered.map(b => {
      const pct = Math.round((b.available / b.total) * 100);
      const barColor = b.available === 0 ? 'var(--red)'
        : b.available < b.total * 0.3 ? 'var(--amber)' : 'var(--sage)';
      return `<tr>
        <td><span class="td-mono">#${b.id}</span></td>
        <td>
          <div class="td-title">${b.title}</div>
          <div class="td-sub">${b.authorName}</div>
        </td>
        <td><span class="pill pill-accent">${b.categoryName}</span></td>
        <td class="td-mono">${b.isbn}</td>
        <td>
          <div style="font-size:0.78rem">
            <span style="color:var(--sage)">${b.available}</span>
            <span style="color:var(--muted)"> / ${b.total}</span>
          </div>
          <div style="margin-top:4px;height:4px;background:var(--border);border-radius:2px;width:60px">
            <div style="height:100%;width:${pct}%;background:${barColor};border-radius:2px"></div>
          </div>
        </td>
        <td>${b.available > 0
          ? '<span class="avail-yes">Available</span>'
          : '<span class="avail-no">Fully Borrowed</span>'}</td>
        <td>
          <div style="display:flex;gap:5px">
            <button class="btn btn-sm btn-secondary btn-icon" onclick="editBook(${b.id})" title="Edit book">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
            </button>
            <button class="btn btn-sm btn-primary"
              onclick="prefillLoan(${b.id})"
              ${b.available === 0 ? 'disabled style="opacity:0.4;cursor:not-allowed"' : ''}>
              + Loan
            </button>
          </div>
        </td>
      </tr>`;
    }).join('');
  } catch (err) {
    console.error('Books error:', err);
  }
}

async function editBook(id) {
  // Open instantly, populate while modal is visible
  document.getElementById('edit-book-title-display').textContent = 'Loading…';
  document.getElementById('edit-book-author-display').textContent = '—';
  document.getElementById('edit-book-cat-display').textContent = '—';
  document.getElementById('edit-book-isbn-display').textContent = '—';
  document.getElementById('edit-book-year-display').textContent = '—';
  document.getElementById('edit-book-stock-display').textContent = '—';
  document.getElementById('edit-book-add-copies').value = 1;
  openModal('modal-edit-book');
  try {
    const [book, authors, categories] = await Promise.all([
      apiFetch(`/books/${id}`),
      apiFetch('/authors'),
      apiFetch('/categories')
    ]);
    const author   = authors.find(a => a.id === book.authorId);
    const category = categories.find(c => c.id === book.catId);
    // Store originals in hidden inputs for the PUT payload
    document.getElementById('edit-book-id').value    = book.id;
    document.getElementById('edit-book-total').value = book.total;
    document.getElementById('edit-book-avail').value = book.available;
    document.getElementById('edit-book-author').value = book.authorId;
    document.getElementById('edit-book-cat').value    = book.catId;
    document.getElementById('edit-book-isbn').value   = book.isbn    || '';
    document.getElementById('edit-book-year').value   = book.yearPub || '';
    document.getElementById('edit-book-title-val').value = book.title;
    // Display card
    document.getElementById('edit-book-title-display').textContent  = book.title;
    document.getElementById('edit-book-author-display').textContent = book.authorName || (author ? `${author.firstName} ${author.lastName}` : '—');
    document.getElementById('edit-book-cat-display').textContent    = book.categoryName || (category ? category.name : '—');
    document.getElementById('edit-book-isbn-display').textContent   = book.isbn    || '—';
    document.getElementById('edit-book-year-display').textContent   = book.yearPub || '—';
    document.getElementById('edit-book-stock-display').textContent  = `${book.available} available / ${book.total} total`;
  } catch (err) {
    document.getElementById('edit-book-title-display').textContent = 'Failed to load.';
  }
}

async function saveEditBook() {
  const id        = parseInt(document.getElementById('edit-book-id').value);
  const addCopies = parseInt(document.getElementById('edit-book-add-copies').value);
  const curTotal  = parseInt(document.getElementById('edit-book-total').value);
  const curAvail  = parseInt(document.getElementById('edit-book-avail').value);
  if (!addCopies || addCopies < 1) { toast('Enter at least 1 copy to add.', 'error'); return; }
  // Borrowed copies stay untouched; only total and available grow
  const newTotal = curTotal + addCopies;
  const newAvail = curAvail + addCopies;
  try {
    await apiFetch(`/books/${id}`, {
      method: 'PUT',
      body: JSON.stringify({
        title:    document.getElementById('edit-book-title-val').value,
        authorId: parseInt(document.getElementById('edit-book-author').value),
        catId:    parseInt(document.getElementById('edit-book-cat').value),
        isbn:     document.getElementById('edit-book-isbn').value.trim(),
        yearPub:  parseInt(document.getElementById('edit-book-year').value),
        total:    newTotal,
        available: newAvail,
      })
    });
    closeModal('modal-edit-book');
    toast(`${addCopies} ${addCopies === 1 ? 'copy' : 'copies'} added!`);
    renderBooks();
  } catch (err) { /* handled by apiFetch */ }
}

async function addBook() {
  try {
    const [authors, categories] = await Promise.all([
      apiFetch('/authors'),
      apiFetch('/categories')
    ]);
    document.getElementById('add-book-author').innerHTML =
      authors.map(a => `<option value="${a.id}">${a.firstName} ${a.lastName}</option>`).join('');
    document.getElementById('add-book-cat').innerHTML =
      categories.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
    openModal('modal-add-book');
  } catch (err) { /* handled */ }
}

async function saveAddBook() {
  const title = document.getElementById('add-book-title').value.trim();
  if (!title) { toast('Title is required', 'error'); return; }
  try {
    await apiFetch('/books', {
      method: 'POST',
      body: JSON.stringify({
        title,
        authorId: parseInt(document.getElementById('add-book-author').value),
        catId:    parseInt(document.getElementById('add-book-cat').value),
        isbn:     document.getElementById('add-book-isbn').value,
        yearPub:  parseInt(document.getElementById('add-book-year').value) || new Date().getFullYear(),
        total:    parseInt(document.getElementById('add-book-total').value) || 1,
      })
    });
    closeModal('modal-add-book');
    toast('Book added!');
    renderBooks();
  } catch (err) { /* handled */ }
}

// ═════════════════════════════════════════════════════════════
//  MEMBERS
// ═════════════════════════════════════════════════════════════
let memberSearch = '';

async function renderMembers() {
  tableLoading('members-tbody', 5);
  try {
    const [members, loans] = await Promise.all([
      apiFetch('/members'),
      apiFetch('/loans')
    ]);
    const filtered = members.filter(m => {
      const s = memberSearch.toLowerCase();
      const name = `${m.firstName} ${m.lastName}`.toLowerCase();
      return !s || name.includes(s) || (m.phone || '').includes(s);
    });
    document.getElementById('members-count').textContent = `${filtered.length} members`;
    const tbody = document.getElementById('members-tbody');

    tbody.innerHTML = filtered.map(m => {
      const [ac, bc] = avatarColor(`${m.firstName}${m.lastName}`);
      const activeLoans = loans.filter(l => l.memberId === m.id && l.status !== 'Returned').length;
      const totalLoans  = loans.filter(l => l.memberId === m.id).length;
      return `<tr>
        <td>
          <div style="display:flex;align-items:center;gap:10px">
            <div class="avatar"
                 style="background:${bc};color:${ac};font-size:0.7rem;font-weight:700">
              ${initials(m.firstName, m.lastName)}
            </div>
            <div>
              <div class="td-title" style="font-size:0.82rem">${m.firstName} ${m.lastName}</div>
              <div class="td-sub">${m.phone}</div>
            </div>
          </div>
        </td>
        <td>${formatDate(m.membershipDate)}</td>
        <td>${statusBadge(m.status)}</td>
        <td>
          <span class="pill pill-accent">${activeLoans} active</span>
          <span class="pill" style="margin-left:3px;color:var(--muted)">${totalLoans} total</span>
        </td>
        <td>
          <div style="display:flex;gap:5px">
            <button class="btn btn-sm btn-secondary btn-icon" onclick="editMember(${m.id})" title="Edit member">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
            </button>
            <button class="btn btn-sm btn-primary"
              onclick="prefillLoanMember(${m.id})"
              ${m.status !== 'Active' ? 'disabled style="opacity:0.4;cursor:not-allowed"' : ''}>
              + Loan
            </button>
          </div>
        </td>
      </tr>`;
    }).join('');
  } catch (err) { console.error('Members error:', err); }
}

async function editMember(id) {
  try {
    const m = await apiFetch(`/members/${id}`);
    document.getElementById('edit-member-id').value     = m.id;
    document.getElementById('edit-member-fn').value     = m.firstName;
    document.getElementById('edit-member-ln').value     = m.lastName;
    document.getElementById('edit-member-phone').value  = m.phone;
    document.getElementById('edit-member-date').value   = m.membershipDate?.slice(0, 10);
    document.getElementById('edit-member-status').value = m.status;
    openModal('modal-edit-member');
  } catch (err) { /* handled */ }
}

async function saveEditMember() {
  const id = parseInt(document.getElementById('edit-member-id').value);
  const fn = document.getElementById('edit-member-fn').value.trim();
  const ln = document.getElementById('edit-member-ln').value.trim();
  if (!fn || !ln) { toast('Name is required.', 'error'); return; }
  try {
    await apiFetch(`/members/${id}`, {
      method: 'PUT',
      body: JSON.stringify({
        firstName:      fn,
        lastName:       ln,
        phone:          document.getElementById('edit-member-phone').value.trim(),
        membershipDate: document.getElementById('edit-member-date').value,
        status:         document.getElementById('edit-member-status').value,
      })
    });
    closeModal('modal-edit-member');
    toast('Member updated!');
    renderMembers();
  } catch (err) { /* handled */ }
}

function addMember() { openModal('modal-add-member'); }

async function saveAddMember() {
  const fn = document.getElementById('add-member-fn').value.trim();
  const ln = document.getElementById('add-member-ln').value.trim();
  if (!fn || !ln) { toast('Name is required', 'error'); return; }
  try {
    await apiFetch('/members', {
      method: 'POST',
      body: JSON.stringify({
        firstName:      fn,
        lastName:       ln,
        phone:          document.getElementById('add-member-phone').value,
        membershipDate: document.getElementById('add-member-date').value || new Date().toISOString().slice(0, 10),
      })
    });
    closeModal('modal-add-member');
    toast('Member registered!');
    renderMembers();
  } catch (err) { /* handled */ }
}

// ═════════════════════════════════════════════════════════════
//  LOANS
// ═════════════════════════════════════════════════════════════
let loanFilter = 'all';

async function renderLoans() {
  tableLoading('loans-tbody', 9);
  try {
    const loans = await apiFetch('/loans');
    const filtered = loanFilter === 'all'
      ? loans
      : loans.filter(l => l.status.toLowerCase() === loanFilter);

    document.getElementById('loans-count').textContent = `${filtered.length} records`;
    const tbody = document.getElementById('loans-tbody');

    tbody.innerHTML = filtered.map(l => {
      const nameParts = (l.memberName || '').split(' ');
      const [ac, bc] = avatarColor(l.memberName || 'x');
      return `<tr>
        <td class="td-mono">#${l.id}</td>
        <td>
          <div style="display:flex;align-items:center;gap:8px">
            <div class="avatar" style="background:${bc};color:${ac};font-size:0.65rem">
              ${initials(nameParts[0] || '', nameParts[1] || '')}
            </div>
            <span style="font-size:0.8rem;font-weight:500">${l.memberName}</span>
          </div>
        </td>
        <td>
          <div style="font-size:0.78rem;font-weight:600;color:var(--snow);
               max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">
            ${l.bookTitle}
          </div>
        </td>
        <td>${formatDate(l.loanDate)}</td>
        <td>${formatDate(l.dueDate)}</td>
        <td>${formatDate(l.returnDate)}</td>
        <td>${statusBadge(l.status)}</td>
        <td class="td-mono" style="color:${l.fineAmount > 0 ? 'var(--amber)' : 'var(--muted)'}">
          ₱${(l.fineAmount || 0).toFixed(2)}
        </td>
        <td>
          ${l.status !== 'Returned'
            ? `<button class="btn btn-sm btn-primary" onclick="returnBook(${l.id})"><svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 14 4 9 9 4"/><path d="M20 20v-7a4 4 0 0 0-4-4H4"/></svg> Return</button>`
            : `<button class="btn btn-sm btn-ghost-red" onclick="deleteLoan(${l.id})">Delete</button>`
          }
        </td>
      </tr>`;
    }).join('');
  } catch (err) { console.error('Loans error:', err); }
}

async function openAddLoan() {
  try {
    const [books, members] = await Promise.all([
      apiFetch('/books'),
      apiFetch('/members')
    ]);
    const available = books.filter(b => b.available > 0);
    const active    = members.filter(m => m.status === 'Active');

    document.getElementById('loan-book').innerHTML =
      available.map(b => `<option value="${b.id}">${b.title} (${b.available} avail.)</option>`).join('');
    document.getElementById('loan-member').innerHTML =
      active.map(m => `<option value="${m.id}">${m.firstName} ${m.lastName}</option>`).join('');
    document.getElementById('loan-days').value = 14;
    openModal('modal-add-loan');
  } catch (err) { /* handled */ }
}

async function saveAddLoan() {
  try {
    const result = await apiFetch('/loans', {
      method: 'POST',
      body: JSON.stringify({
        bookId:   parseInt(document.getElementById('loan-book').value),
        memberId: parseInt(document.getElementById('loan-member').value),
        days:     parseInt(document.getElementById('loan-days').value) || 14,
      })
    });
    closeModal('modal-add-loan');
    toast(`Loan created! Due: ${formatDate(result?.dueDate)}`);
    renderLoans();
  } catch (err) { /* handled */ }
}

async function returnBook(loanId) {
  try {
    const loan = await apiFetch(`/loans/${loanId}`);
    document.getElementById('return-loan-id').value = loanId;
    document.getElementById('return-loan-info').textContent =
      `${loan.memberName} — ${loan.bookTitle}`;
    document.getElementById('return-date').value = new Date().toISOString().slice(0, 10);
    openModal('modal-return-book');
  } catch (err) { /* handled */ }
}

async function saveReturn() {
  const loanId     = parseInt(document.getElementById('return-loan-id').value);
  const returnDate = document.getElementById('return-date').value;
  try {
    const result = await apiFetch(`/loans/${loanId}/return`, {
      method: 'POST',
      body: JSON.stringify({ loanId, returnDate: new Date(returnDate).toISOString() })
    });
    closeModal('modal-return-book');
    const fine = result?.fine || 0;
    toast(
      fine > 0 ? `Book returned. Fine: ₱${fine.toFixed(2)}` : 'Book returned successfully!',
      fine > 0 ? 'warn' : 'success'
    );
    renderLoans();
  } catch (err) { /* handled */ }
}

async function deleteLoan(loanId) {
  if (!confirm('Delete this returned loan record?')) return;
  try {
    await apiFetch(`/loans/${loanId}`, { method: 'DELETE' });
    toast('Loan record deleted.');
    renderLoans();
  } catch (err) { /* handled */ }
}

function prefillLoan(bookId) {
  sessionStorage.setItem('prefill_loan_book', bookId);
  window.location.href = 'loans.html';
}
function prefillLoanMember(memberId) {
  sessionStorage.setItem('prefill_loan_member', memberId);
  window.location.href = 'loans.html';
}

// ═════════════════════════════════════════════════════════════
//  AUTHORS
// ═════════════════════════════════════════════════════════════
async function renderAuthors() {
  tableLoading('authors-tbody', 6);
  try {
    const [authors, books] = await Promise.all([
      apiFetch('/authors'),
      apiFetch('/books')
    ]);
    const tbody = document.getElementById('authors-tbody');
    tbody.innerHTML = authors.map(a => {
      const bookCount = books.filter(b => b.authorId === a.id).length;
      return `<tr>
        <td class="td-mono">#${a.id}</td>
        <td class="td-title">${a.firstName} ${a.lastName}</td>
        <td><span class="pill">${a.nationality}</span></td>
        <td>${a.birthYear}</td>
        <td><span class="pill pill-accent">${bookCount} book${bookCount !== 1 ? 's' : ''}</span></td>
        <td><button class="btn btn-sm btn-secondary btn-icon" onclick="editAuthor(${a.id})" title="Edit author">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
        </button></td>
      </tr>`;
    }).join('');
  } catch (err) { console.error('Authors error:', err); }
}

async function editAuthor(id) {
  try {
    const a = await apiFetch(`/authors/${id}`);
    document.getElementById('edit-author-id').value   = a.id;
    document.getElementById('edit-author-fn').value   = a.firstName;
    document.getElementById('edit-author-ln').value   = a.lastName;
    document.getElementById('edit-author-nat').value  = a.nationality;
    document.getElementById('edit-author-year').value = a.birthYear;
    openModal('modal-edit-author');
  } catch (err) { /* handled */ }
}

async function saveEditAuthor() {
  const id = parseInt(document.getElementById('edit-author-id').value);
  const fn = document.getElementById('edit-author-fn').value.trim();
  const ln = document.getElementById('edit-author-ln').value.trim();
  if (!fn || !ln) { toast('Name is required.', 'error'); return; }
  try {
    await apiFetch(`/authors/${id}`, {
      method: 'PUT',
      body: JSON.stringify({
        firstName:   fn,
        lastName:    ln,
        nationality: document.getElementById('edit-author-nat').value.trim(),
        birthYear:   parseInt(document.getElementById('edit-author-year').value),
      })
    });
    closeModal('modal-edit-author');
    toast('Author updated!');
    renderAuthors();
  } catch (err) { /* handled */ }
}

function addAuthor() { openModal('modal-add-author'); }

async function saveAddAuthor() {
  const fn = document.getElementById('add-author-fn').value.trim();
  const ln = document.getElementById('add-author-ln').value.trim();
  if (!fn || !ln) { toast('Name is required', 'error'); return; }
  try {
    await apiFetch('/authors', {
      method: 'POST',
      body: JSON.stringify({
        firstName:   fn,
        lastName:    ln,
        nationality: document.getElementById('add-author-nat').value,
        birthYear:   parseInt(document.getElementById('add-author-year').value) || 2000,
      })
    });
    closeModal('modal-add-author');
    toast('Author added!');
    renderAuthors();
  } catch (err) { /* handled */ }
}

// ═════════════════════════════════════════════════════════════
//  CATEGORIES
// ═════════════════════════════════════════════════════════════
async function renderCategories() {
  tableLoading('categories-tbody', 5);
  try {
    const [categories, books] = await Promise.all([
      apiFetch('/categories'),
      apiFetch('/books')
    ]);
    const tbody = document.getElementById('categories-tbody');
    tbody.innerHTML = categories.map(c => {
      const bookCount = books.filter(b => b.catId === c.id).length;
      return `<tr>
        <td class="td-mono">#${c.id}</td>
        <td class="td-title">${c.name}</td>
        <td style="color:var(--ghost);font-size:0.78rem">${c.description}</td>
        <td><span class="pill pill-accent">${bookCount} book${bookCount !== 1 ? 's' : ''}</span></td>
        <td><button class="btn btn-sm btn-secondary btn-icon" onclick="editCategory(${c.id})" title="Edit category">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
        </button></td>
      </tr>`;
    }).join('');
  } catch (err) { console.error('Categories error:', err); }
}

async function editCategory(id) {
  try {
    const c = await apiFetch(`/categories/${id}`);
    document.getElementById('edit-cat-id').value   = c.id;
    document.getElementById('edit-cat-name').value = c.name;
    document.getElementById('edit-cat-desc').value = c.description;
    openModal('modal-edit-cat');
  } catch (err) { /* handled */ }
}

async function saveEditCategory() {
  const id = parseInt(document.getElementById('edit-cat-id').value);
  try {
    await apiFetch(`/categories/${id}`, {
      method: 'PUT',
      body: JSON.stringify({
        name:        document.getElementById('edit-cat-name').value,
        description: document.getElementById('edit-cat-desc').value,
      })
    });
    closeModal('modal-edit-cat');
    toast('Category updated!');
    renderCategories();
  } catch (err) { /* handled */ }
}

function addCategory() { openModal('modal-add-cat'); }

async function saveAddCategory() {
  const name = document.getElementById('add-cat-name').value.trim();
  if (!name) { toast('Name required', 'error'); return; }
  try {
    await apiFetch('/categories', {
      method: 'POST',
      body: JSON.stringify({
        name,
        description: document.getElementById('add-cat-desc').value,
      })
    });
    closeModal('modal-add-cat');
    toast('Category added!');
    renderCategories();
  } catch (err) { /* handled */ }
}

// ═════════════════════════════════════════════════════════════
//  REPORTS — download Excel files from the API
// ═════════════════════════════════════════════════════════════
// These trigger a file download by navigating to the API URL.
// The browser shows a Save As dialog automatically.

function downloadReport(type) {
  // Directly navigate to the endpoint — the C# API streams
  // the .xlsx file as a download response
  window.open(`${API}/reports/${type}`, '_blank');
  toast(`Generating ${type} report…`, 'success');
}

// ═════════════════════════════════════════════════════════════
//  INIT — runs on every page load
// ═════════════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', () => {
  // Close modals when clicking outside them
  document.querySelectorAll('.modal-overlay').forEach(overlay => {
    overlay.addEventListener('click', e => {
      if (e.target === overlay) overlay.classList.remove('open');
    });
  });

  // Handle cross-page loan prefill (from Books or Members page)
  const prefillBook   = sessionStorage.getItem('prefill_loan_book');
  const prefillMember = sessionStorage.getItem('prefill_loan_member');
  if ((prefillBook || prefillMember) && document.getElementById('loan-book')) {
    sessionStorage.removeItem('prefill_loan_book');
    sessionStorage.removeItem('prefill_loan_member');
    setTimeout(async () => {
      await openAddLoan();
      if (prefillBook)   document.getElementById('loan-book').value   = prefillBook;
      if (prefillMember) document.getElementById('loan-member').value = prefillMember;
    }, 200);
  }
});
