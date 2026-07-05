// Logica paginii AppRoles/Details: cautare claim in rol, adaugare/eliminare drepturi.

function getToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

async function parseJson(response) {
    const text = await response.text();
    try { return JSON.parse(text); }
    catch { console.error('Raspuns invalid:', text); return null; }
}

// ---------------- FILTRE ----------------
function resetClaimInRoleFilters() {
    const roleId = document.getElementById('roleId').value;
    window.location.href = `/AppRoles/Details?id=${roleId}`;
}

function searchClaimInRole() {
    const claim = document.getElementById('claimSearchInRole').value;
    const roleId = document.getElementById('roleId').value;
    window.location.href = `/AppRoles/Details?id=${roleId}&claimSearchInRole=${encodeURIComponent(claim)}`;
}

document.addEventListener('keydown', function (e) {
    const keyCode = e.keyCode || e.which;
    if (keyCode === 13 && document.activeElement.id === 'claimSearchInRole') {
        e.preventDefault();
        searchClaimInRole();
    }
});

// ---------------- REMOVE CLAIM ----------------
async function loadRemoveClaim(claimId, roleId) {
    if (!roleId || !claimId) { alert('Datele nu exista!'); return; }

    const response = await fetch('/AppRoles/RemoveClaim', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getToken() },
        body: `roleId=${encodeURIComponent(roleId)}&claimId=${encodeURIComponent(claimId)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastClaimRemoved', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

// ---------------- ADD CLAIM ----------------
function loadAddClaim(id) {
    fetch(`/AppRoles/AddClaim/${id}`)
        .then(r => r.text())
        .then(html => {
            const container = document.getElementById('contentAddClaim');
            if (!container) return;
            container.innerHTML = html;
            const modalEl = document.getElementById('addClaimModal');
            if (modalEl) new bootstrap.Modal(modalEl, { backdrop: true }).show();
        })
        .catch(err => console.error(err));
}

async function addClaimFct(idRole) {
    const claimToken = document.getElementById('selectedClaimToken').value;
    if (!idRole || !claimToken) { alert('Selectati un claim!'); return; }

    const response = await fetch('/AppRoles/AddClaim', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getToken() },
        body: `roleId=${encodeURIComponent(idRole)}&claimToken=${encodeURIComponent(claimToken)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastClaimAdded', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

// Cautare in dropdown-ul din modalul de adaugare (autocomplete pe claims disponibile).
function searchClaim() {
    const searchTerm = document.getElementById('claimSearch').value;
    const allClaimsJson = document.getElementById('allClaimsJson').value;
    const allClaims = JSON.parse(allClaimsJson);

    fetch('/AppRoles/SearchClaim', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ searchTerm, claims: allClaims })
    })
        .then(response => response.json())
        .then(res => {
            const results = document.getElementById('searchResults');
            let list = '';
            if (res.success && Array.isArray(res.data) && res.data.length > 0) {
                res.data.forEach(item => {
                    list += `<li class="dropdown-item" data-token="${item.token}" onclick="selectClaim(this)">${item.claimName}</li>`;
                });
            } else {
                list = '<li class="dropdown-item disabled">No results found</li>';
            }
            results.innerHTML = list;
            results.style.display = 'block';
        });
}

function selectClaim(li) {
    document.getElementById('claimSearch').value = li.textContent;
    document.getElementById('selectedClaimToken').value = li.getAttribute('data-token');
    const results = document.getElementById('searchResults');
    results.innerHTML = '';
    results.style.display = 'none';
}

// ---------------- TOASTS + CLEANUP ----------------
document.addEventListener('DOMContentLoaded', function () {
    const toasts = [
        { key: 'showToastClaimAdded', text: 'Dreptul a fost adaugat!', bg: 'green' },
        { key: 'showToastClaimRemoved', text: 'Dreptul a fost eliminat!', bg: 'red' }
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
