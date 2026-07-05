// Logica paginii AppClaims: filtrare + modale CRUD prin AJAX + toasts.
// Extras din blocul inline <script> al view-ului Index.

function getToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

async function parseJson(response) {
    const text = await response.text();
    try {
        return JSON.parse(text);
    } catch {
        console.error('Raspuns invalid:', text);
        return null;
    }
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
function resetClaimFilters() {
    window.location.href = '/AppClaims/Index';
}

function searchClaim() {
    const claim = document.getElementById('claimSearch').value;
    window.location.href = `/AppClaims/Index?claimSearch=${encodeURIComponent(claim)}`;
}

// ---------------- LOAD MODALE ----------------
function viewRolesInClaim(id) {
    loadIntoModal(`/AppClaims/Details/${id}`, 'contentDetailsClaim', 'detailsClaimModal');
}

function loadCreateClaim() {
    loadIntoModal('/AppClaims/Create', 'contentAddClaim', 'createClaimModal');
}

function loadEditClaim(id) {
    loadIntoModal(`/AppClaims/Edit/${id}`, 'contentEditClaim', 'editClaimModal');
}

function loadDeleteClaim(id) {
    loadIntoModal(`/AppClaims/Delete/${id}`, 'contentDeleteClaim', 'deleteClaimModal');
}

// ---------------- ACTIUNI ----------------
async function createClaim() {
    const claimName = document.getElementById('claimNameCreate').value;
    if (!claimName) { alert('Completati toate datele!'); return; }

    const response = await fetch('/AppClaims/Create', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getToken()
        },
        body: `ClaimName=${encodeURIComponent(claimName)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastClaimCreated', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

async function editClaim(claimId) {
    const claimName = document.getElementById('claimNameEdit').value;
    if (!claimName) { alert('Completati toate datele!'); return; }

    const response = await fetch(`/AppClaims/Edit/${claimId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getToken()
        },
        body: `id=${encodeURIComponent(claimId)}&ClaimName=${encodeURIComponent(claimName)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastClaimEdited', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

async function postDeleteClaim(claimId) {
    const response = await fetch('/AppClaims/DeleteConfirmed', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getToken()
        },
        body: `id=${encodeURIComponent(claimId)}`
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastClaimDeleted', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

// ---------------- TOASTS + CLEANUP ----------------
document.addEventListener('DOMContentLoaded', function () {
    const toasts = [
        { key: 'showToastClaimCreated', text: 'Claim-ul a fost creat!', bg: 'grey' },
        { key: 'showToastClaimEdited', text: 'Claim-ul a fost editat!', bg: 'green' },
        { key: 'showToastClaimDeleted', text: 'Claim-ul a fost sters!', bg: 'red' }
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
