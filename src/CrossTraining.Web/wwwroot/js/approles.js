// Logica paginii AppRoles (Index): filtrare + modale CRUD prin AJAX + toasts.

function getToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

async function parseJson(response) {
    const text = await response.text();
    try { return JSON.parse(text); }
    catch { console.error('Raspuns invalid:', text); return null; }
}

function loadIntoModal(url, containerId, modalId) {
    fetch(url)
        .then(r => r.text())
        .then(html => {
            const container = document.getElementById(containerId);
            if (!container) return;
            container.innerHTML = html;
            const modalEl = document.getElementById(modalId);
            if (modalEl) new bootstrap.Modal(modalEl, { backdrop: true }).show();
        })
        .catch(err => console.error(err));
}

// ---------------- FILTRE ----------------
function resetRoleFilters() {
    window.location.href = '/AppRoles/Index';
}

function searchRole() {
    const role = document.getElementById('RoleSearch').value;
    window.location.href = `/AppRoles/Index?RoleSearch=${encodeURIComponent(role)}`;
}

// ---------------- LOAD MODALE ----------------
function loadCreateRole() {
    loadIntoModal('/AppRoles/Create', 'contentAddRole', 'createRoleModal');
}
function loadEditRole(id) {
    loadIntoModal(`/AppRoles/Edit/${id}`, 'contentEditRole', 'editRoleModal');
}
function loadDeleteRole(id) {
    loadIntoModal(`/AppRoles/Delete/${id}`, 'contentDeleteRole', 'deleteRoleModal');
}

// ---------------- ACTIUNI ----------------
async function createRole() {
    const roleName = document.getElementById('roleNameCreate').value;
    if (!roleName) { alert('Completati toate datele!'); return; }

    const response = await fetch('/AppRoles/Create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getToken() },
        body: `RoleName=${encodeURIComponent(roleName)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastRoleCreated', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

async function saveEditRole(roleId) {
    const roleName = document.getElementById('roleNameEdit').value;
    if (!roleName) { alert('Completati datele!'); return; }

    const response = await fetch(`/AppRoles/Edit/${roleId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getToken() },
        body: `id=${encodeURIComponent(roleId)}&roleName=${encodeURIComponent(roleName)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastRoleEdited', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

async function postDeleteRole(roleId) {
    const response = await fetch('/AppRoles/DeleteConfirmed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getToken() },
        body: `id=${encodeURIComponent(roleId)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastRoleDeleted', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

// ---------------- TOASTS + CLEANUP ----------------
document.addEventListener('DOMContentLoaded', function () {
    const toasts = [
        { key: 'showToastRoleCreated', text: 'Rolul a fost creat!', bg: 'grey' },
        { key: 'showToastRoleEdited', text: 'Rolul a fost editat!', bg: 'green' },
        { key: 'showToastRoleDeleted', text: 'Rolul a fost sters!', bg: 'red' }
    ];
    toasts.forEach(t => {
        if (sessionStorage.getItem(t.key) === 'true') {
            Toastify({
                text: t.text, duration: 3000, close: true,
                gravity: 'bottom', position: 'center', style: { background: t.bg }
            }).showToast();
            sessionStorage.removeItem(t.key);
        }
    });
});

document.addEventListener('hidden.bs.modal', function () {
    document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
    document.body.classList.remove('modal-open');
    document.body.style.removeProperty('padding-right');
});
