// Logica paginii Games: modale create/edit/delete prin AJAX, flip cards, toasts.
// Extras din blocul inline <script> al view-ului Index (preferinta: JS in fisiere dedicate).

// Curata backdrop-ul ramas dupa inchiderea unui modal.
document.addEventListener('hidden.bs.modal', function () {
    document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
    document.body.classList.remove('modal-open');
    document.body.style.removeProperty('padding-right');
});

// Flip cards
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.flip-card').forEach(card => {
        const suitableEl = card.querySelector('.suitable');
        card.addEventListener('click', function () {
            const isExpanded = this.getAttribute('data-expanded') === 'true';
            this.classList.toggle('flipped');

            if (isExpanded) {
                this.classList.remove('flip-card-expanded');
                this.setAttribute('data-expanded', 'false');
                if (suitableEl) suitableEl.style.display = 'block';
            } else {
                this.classList.add('flip-card-expanded');
                this.setAttribute('data-expanded', 'true');
                if (suitableEl) suitableEl.style.display = 'none';
            }
        });
    });

    showPendingToasts();
});

function getToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

function autoExpand(container) {
    const textBoxes = container.querySelectorAll('textarea, input[type="text"]');
    textBoxes.forEach(tb => {
        const resize = () => {
            tb.style.height = 'auto';
            tb.style.height = tb.scrollHeight + 'px';
        };
        resize();
        tb.addEventListener('focus', resize);
        tb.addEventListener('input', resize);
    });
}

// ---------------- CREATE ----------------
function loadCreateGame() {
    fetch('/Games/Create')
        .then(r => r.text())
        .then(html => {
            const container = document.getElementById('contentAddGame');
            if (!container) return;
            container.innerHTML = html;
            const modalEl = document.getElementById('createGameModal');
            if (modalEl) new bootstrap.Modal(modalEl, { backdrop: true }).show();
            autoExpand(container);
        })
        .catch(err => console.error(err));
}

async function createGame() {
    const gameName = document.getElementById('gameNameCreate').value.trim();
    const domainId = document.getElementById('domainCreate').value;
    const downloadLink = document.getElementById('linkCreate').value.trim();
    const description = document.getElementById('descriptionCreate').value.trim();

    if (!gameName || !domainId || !description) {
        alert('Completati toate campurile obligatorii!');
        return;
    }

    const data = new URLSearchParams();
    data.append('GameName', gameName);
    data.append('DomainId', domainId);
    data.append('DownloadLink', downloadLink);
    data.append('Description', description);

    const response = await fetch('/Games/Create', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getToken()
        },
        body: data.toString()
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastGameAdded', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

// ---------------- EDIT ----------------
function loadEditgame(gameId) {
    fetch(`/Games/Edit/${gameId}`)
        .then(r => r.text())
        .then(html => {
            const container = document.getElementById('contentEditGame');
            if (!container) return;
            container.innerHTML = html;
            const modalEl = document.getElementById('editGameModal');
            if (modalEl) new bootstrap.Modal(modalEl, { backdrop: true }).show();
            autoExpand(container);
        })
        .catch(err => console.error(err));
}

async function submitEditGame(gameId) {
    const container = document.getElementById('contentEditGame');
    const gameName = container.querySelector('[name="GameName"]').value.trim();
    const description = container.querySelector('[name="Description"]').value.trim();
    const downloadLink = container.querySelector('[name="DownloadLink"]').value.trim();
    const domainId = document.getElementById('domainEdit').value;

    if (!gameName || !description) {
        alert('Completati toate campurile obligatorii!');
        return;
    }

    const data = new URLSearchParams();
    data.append('GameId', gameId);
    data.append('GameName', gameName);
    data.append('DomainId', domainId);
    data.append('Description', description);
    data.append('DownloadLink', downloadLink);

    const response = await fetch(`/Games/Edit/${gameId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getToken()
        },
        body: data.toString()
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastGameEdited', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare necunoscuta');
    }
}

// ---------------- DELETE ----------------
function loadDeleteGame(id) {
    fetch(`/Games/Delete/${id}`)
        .then(r => r.text())
        .then(html => {
            const container = document.getElementById('contentDeleteGame');
            if (!container) return;
            container.innerHTML = html;
            const modalEl = document.getElementById('deleteGameModal');
            if (modalEl) new bootstrap.Modal(modalEl).show();
        })
        .catch(err => console.error(err));
}

async function deleteGame() {
    const gameId = document.getElementById('deleteGameId').value;

    const data = new URLSearchParams();
    data.append('gameId', gameId);

    const response = await fetch('/Games/PostDelete', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getToken()
        },
        body: data.toString()
    });

    const result = await parseJson(response);
    if (result && result.success) {
        sessionStorage.setItem('showToastGameDeleted', 'true');
        location.reload();
    } else {
        alert(result && result.errors ? result.errors.join("\n") : 'Eroare la stergere');
    }
}

// ---------------- HELPERS ----------------
async function parseJson(response) {
    const text = await response.text();
    try {
        return JSON.parse(text);
    } catch {
        console.error('Raspuns invalid:', text);
        return null;
    }
}

function showPendingToasts() {
    const toasts = [
        { key: 'showToastGameAdded', text: 'The game was added!', bg: 'grey' },
        { key: 'showToastGameDeleted', text: 'The game was removed!', bg: 'red' },
        { key: 'showToastGameEdited', text: 'The game was edited!', bg: 'green' }
    ];

    toasts.forEach(t => {
        if (sessionStorage.getItem(t.key) === 'true') {
            Toastify({
                text: t.text,
                duration: 3000,
                close: true,
                gravity: 'bottom',
                position: 'center',
                style: { background: t.bg }
            }).showToast();
            sessionStorage.removeItem(t.key);
        }
    });
}
