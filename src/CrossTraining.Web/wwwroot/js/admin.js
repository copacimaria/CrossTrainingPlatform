// Comportamentul comun al ecranelor de administrare (drepturi, roluri, domenii, jocuri).
//
// View-urile nu contin JavaScript: elementele declara ce fac prin atribute, iar fisierul
// acesta le leaga prin cateva listener-e delegate la nivel de document. Asa functioneaza si
// pentru continutul incarcat ulterior in modale, iar CSP-ul poate interzice scripturile
// inline ('unsafe-inline' lipseste din script-src).
//
//   <button data-modal-url="/AppClaims/Edit/5" data-modal="#editClaimModal">
//       la click cere partialul, il pune in .modal-body si deschide modalul;
//
//   <form method="post" data-ajax-form data-toast="Salvat!" data-toast-color="green"
//         data-confirm="Sigur?">
//       se trimite prin fetch. La reusita pagina se reincarca si afiseaza mesajul din
//       data-toast; la esec, erorile apar in [data-form-errors] din formular (sau intr-un
//       toast, daca formularul nu are unde sa le arate);
//
//   <textarea data-autosize>
//       inaltimea creste odata cu textul.
//
// Erorile vin in forma comuna din AjaxResponseExtensions si AjaxExceptionHandler: un
// ProblemDetails cu lista "errors" (400, 404, 500).
(function () {
    'use strict';

    const TOAST_KEY = 'admin.toast';

    // ---------------- CERERI ----------------

    async function send(url, init) {
        // X-Requested-With: serverul trateaza cererea ca AJAX. Cookie-ul de autentificare
        // raspunde atunci cu 401/403 in loc de redirect spre pagina de login, iar o eroare
        // neasteptata vine ca JSON, nu ca pagina HTML.
        const response = await fetch(url, {
            ...init,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        // Sesiunea a expirat: reincarcarea paginii duce la login.
        if (response.status === 401) {
            window.location.reload();
            return null;
        }

        return response;
    }

    async function errorsOf(response) {
        if (response.status === 403)
            return ['Nu aveti dreptul necesar pentru aceasta operatie.'];

        try {
            const body = await response.json();
            if (body && Array.isArray(body.errors) && body.errors.length > 0)
                return body.errors;
        } catch {
            // Nu e JSON (de exemplu, raspunsul unui proxy): mesajul generic de mai jos.
        }

        return [`Cererea nu a reusit (cod ${response.status}).`];
    }

    // ---------------- MODALE ----------------

    async function openModal(trigger) {
        const modal = document.querySelector(trigger.dataset.modal);
        const body = modal && modal.querySelector('.modal-body');
        if (!body) return;

        let response;
        try {
            response = await send(trigger.dataset.modalUrl);
        } catch (err) {
            console.error(err);
            showToast('Serverul nu a putut fi contactat.', 'red');
            return;
        }

        if (!response) return;
        if (!response.ok) {
            showToast((await errorsOf(response)).join('\n'), 'red');
            return;
        }

        // HTML randat de server (Razor), deci deja codificat.
        body.innerHTML = await response.text();
        bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    // Dupa deschidere, nu inainte: un modal ascuns nu are dimensiuni, deci inaltimea
    // textului nu se poate calcula, iar focusul nu se poate muta in el.
    document.addEventListener('shown.bs.modal', function (e) {
        e.target.querySelectorAll('textarea[data-autosize]').forEach(autosize);

        const first = e.target.querySelector(
            '.modal-body input:not([type="hidden"]), .modal-body select, .modal-body textarea');
        if (first) first.focus();
    });

    // ---------------- FORMULARE ----------------

    async function submitForm(form) {
        if (form.dataset.confirm && !window.confirm(form.dataset.confirm)) return;

        // Fara trimiteri duble la click repetat.
        const submit = form.querySelector('[type="submit"]');
        if (submit) submit.disabled = true;

        try {
            // FormData contine si token-ul antiforgery pus de tag helper-ul <form>.
            const response = await send(form.action, { method: 'POST', body: new FormData(form) });
            if (!response) return;

            const result = response.ok ? await response.json().catch(() => null) : null;
            if (result && result.success) {
                rememberToast(form.dataset.toast, form.dataset.toastColor);
                window.location.reload();
                return;
            }

            showErrors(form, response.ok ? ['Raspuns neasteptat de la server.'] : await errorsOf(response));
        } catch (err) {
            console.error(err);
            showErrors(form, ['Serverul nu a putut fi contactat.']);
        } finally {
            if (submit) {
                submit.disabled = false;

                // Un buton dezactivat pierde focusul, care ajunge pe <body>, adica in afara
                // modalului: tasta Escape nu l-ar mai inchide dupa o eroare.
                if (document.activeElement === document.body) submit.focus();
            }
        }
    }

    function showErrors(form, errors) {
        const box = form.querySelector('[data-form-errors]');
        if (!box) {
            showToast(errors.join('\n'), 'red');
            return;
        }

        // Construit prin DOM: mesajele pot contine nume introduse de utilizatori.
        const list = document.createElement('ul');
        list.className = 'mb-0';
        errors.forEach(text => {
            const item = document.createElement('li');
            item.textContent = text;
            list.appendChild(item);
        });

        box.replaceChildren(list);
        box.hidden = false;
    }

    // ---------------- MESAJE (TOASTS) ----------------

    // Culorile din data-toast-color devin clase din tema (toast-success etc., in app.css).
    const TOAST_KINDS = { green: 'success', red: 'danger', grey: 'info' };

    function showToast(text, color) {
        // Toastify scrie textul cu innerText (escapeMarkup implicit), nu ca HTML.
        Toastify({
            text: text,
            duration: 3500,
            close: true,
            gravity: 'bottom',
            position: 'center',
            className: 'toast-' + (TOAST_KINDS[color] || 'success')
        }).showToast();
    }

    // Mesajul de reusita supravietuieste reincarcarii paginii.
    function rememberToast(text, color) {
        if (!text) return;
        try {
            sessionStorage.setItem(TOAST_KEY, JSON.stringify({ text: text, color: color }));
        } catch {
            // Stocare indisponibila (mod privat): operatia a reusit, doar mesajul lipseste.
        }
    }

    function showRememberedToast() {
        let toast = null;
        try {
            toast = JSON.parse(sessionStorage.getItem(TOAST_KEY));
            sessionStorage.removeItem(TOAST_KEY);
        } catch {
            return;
        }

        if (toast && toast.text) showToast(toast.text, toast.color);
    }

    // ---------------- TEXT CARE CRESTE ----------------

    function autosize(textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
    }

    // ---------------- LEGAREA ATRIBUTELOR ----------------

    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-modal-url]');
        if (!trigger) return;

        e.preventDefault();
        openModal(trigger);
    });

    // Un element care nu e buton (cardul "+" de la jocuri) raspunde si la tastatura.
    document.addEventListener('keydown', function (e) {
        if ((e.key === 'Enter' || e.key === ' ') && e.target.matches('[data-modal-url][role="button"]')) {
            e.preventDefault();
            openModal(e.target);
        }
    });

    document.addEventListener('submit', function (e) {
        const form = e.target.closest('form[data-ajax-form]');
        if (!form) return;

        e.preventDefault();
        submitForm(form);
    });

    document.addEventListener('input', function (e) {
        if (e.target.matches('textarea[data-autosize]')) autosize(e.target);
    });

    document.addEventListener('DOMContentLoaded', showRememberedToast);
})();
