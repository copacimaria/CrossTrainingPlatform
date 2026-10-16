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
    const claimId = document.getElementById('selectedClaimId').value;
    if (!idRole || !claimId) { alert('Selectati un claim!'); return; }

    const response = await fetch('/AppRoles/AddClaim', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': getToken() },
        body: `roleId=${encodeURIComponent(idRole)}&claimId=${encodeURIComponent(claimId)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastClaimAdded', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

// Autocomplete pe claim-urile care mai pot fi adaugate rolului.
// Filtrarea se face in baza de date: trimitem doar rolul si termenul cautat.
let claimSearchTimer = null;

function searchClaim() {
    clearTimeout(claimSearchTimer);
    claimSearchTimer = setTimeout(runClaimSearch, 200);
}

async function runClaimSearch() {
    const results = document.getElementById('searchResults');
    if (!results) return;

    const term = document.getElementById('claimSearch').value;
    const roleId = document.getElementById('addClaimRoleId').value;

    let data = [];
    try {
        const response = await fetch(
            `/AppRoles/SearchAvailableClaims?roleId=${encodeURIComponent(roleId)}&term=${encodeURIComponent(term)}`);
        data = await response.json();
    } catch (err) {
        console.error(err);
    }

    renderClaimResults(results, data);
}

// Construit prin DOM, nu prin innerHTML: numele claim-ului vine din baza de date si
// ar fi fost interpretat ca HTML daca il concatenam intr-un string.
function renderClaimResults(results, data) {
    results.replaceChildren();

    if (!Array.isArray(data) || data.length === 0) {
        const empty = document.createElement('li');
        empty.className = 'dropdown-item disabled';
        empty.textContent = 'No results found';
        results.appendChild(empty);
    } else {
        data.forEach(item => {
            const li = document.createElement('li');
            li.className = 'dropdown-item';
            li.dataset.claimId = item.claimId;
            li.textContent = item.claimName;
            li.addEventListener('click', () => selectClaim(li));
            results.appendChild(li);
        });
    }

    results.style.display = 'block';
}

function selectClaim(li) {
    document.getElementById('claimSearch').value = li.textContent;
    document.getElementById('selectedClaimId').value = li.dataset.claimId;
    const results = document.getElementById('searchResults');
    results.replaceChildren();
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
