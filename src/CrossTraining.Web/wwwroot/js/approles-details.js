// Pagina AppRoles/Details: autocomplete-ul din modalul de adaugare a unui drept in rol.
// Modalul, trimiterea formularului si mesajele le trateaza admin.js.
//
// Partialul modalului se incarca dupa pagina, deci listener-ul e delegat la nivel de
// document. Filtrarea se face in baza de date: se trimit doar rolul (in URL-ul din
// data-claim-autocomplete) si termenul cautat.
(function () {
    'use strict';

    const NOT_SELECTED = 'Alegeti un drept din lista.';
    let timer = null;

    document.addEventListener('input', function (e) {
        const input = e.target;
        if (!input.matches('[data-claim-autocomplete]')) return;

        // Textul scris nu e inca o alegere: formularul nu pleaca pana la click pe un rezultat.
        input.form.querySelector('[name="claimId"]').value = '';
        input.setCustomValidity(NOT_SELECTED);

        clearTimeout(timer);
        timer = setTimeout(() => search(input), 200);
    });

    async function search(input) {
        const url = new URL(input.dataset.claimAutocomplete, window.location.origin);
        url.searchParams.set('term', input.value);

        let data = [];
        try {
            const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (response.ok) data = await response.json();
        } catch (err) {
            console.error(err);
        }

        render(input, data);
    }

    // Construit prin DOM, nu prin innerHTML: numele dreptului vine din baza de date si
    // ar fi fost interpretat ca HTML daca il concatenam intr-un string.
    function render(input, data) {
        const results = input.form.querySelector('#searchResults');
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
                li.textContent = item.claimName;
                li.addEventListener('click', () => select(input, item));
                results.appendChild(li);
            });
        }

        results.style.display = 'block';
    }

    function select(input, item) {
        input.value = item.claimName;
        input.setCustomValidity('');
        input.form.querySelector('[name="claimId"]').value = item.claimId;

        const results = input.form.querySelector('#searchResults');
        results.replaceChildren();
        results.style.display = 'none';
    }
})();
