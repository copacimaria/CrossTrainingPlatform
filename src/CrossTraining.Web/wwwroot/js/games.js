// Pagina Catalog: cautare instantanee, filtrare dupa domeniu si "doar recomandate", plus
// descrierea completa la click pe card. Modalele, formularele si mesajele le trateaza
// admin.js. Totul lucreaza pe cardurile deja randate de server: nicio cerere in plus.
(function () {
    'use strict';

    // Fara diacritice si fara diferenta de litere: "stiinte" gaseste si varianta cu diacritice.
    function normalize(text) {
        return (text || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
    }

    document.addEventListener('DOMContentLoaded', function () {
        const catalog = document.querySelector('[data-catalog]');
        if (!catalog) return;

        const cards = [...catalog.querySelectorAll('[data-app]')];
        const search = document.querySelector('[data-catalog-search]');
        const domainChips = [...document.querySelectorAll('[data-filter-domain]')];
        const recommendedChip = document.querySelector('[data-filter-recommended]');
        const counter = document.querySelector('[data-catalog-count]');
        const empty = document.querySelector('[data-catalog-empty]');

        // Textul in care se cauta, calculat o singura data.
        const searchable = new Map(cards.map(card => [card, normalize(card.textContent)]));

        let domain = '';
        let onlyRecommended = false;

        function apply() {
            const term = normalize(search ? search.value.trim() : '');
            let visible = 0;

            cards.forEach(card => {
                const match =
                    (!term || searchable.get(card).includes(term)) &&
                    (!domain || card.dataset.domain === domain) &&
                    (!onlyRecommended || card.dataset.recommended === 'true');

                card.hidden = !match;
                if (match) visible++;
            });

            if (counter) counter.textContent = visible === 1 ? '1 aplicatie' : `${visible} aplicatii`;
            if (empty) empty.hidden = cards.length === 0 || visible > 0;
        }

        if (search) search.addEventListener('input', apply);

        domainChips.forEach(chip => chip.addEventListener('click', function () {
            domain = chip.dataset.filterDomain;
            domainChips.forEach(c => c.setAttribute('aria-pressed', String(c === chip)));
            apply();
        }));

        if (recommendedChip) {
            recommendedChip.addEventListener('click', function () {
                onlyRecommended = !onlyRecommended;
                recommendedChip.setAttribute('aria-pressed', String(onlyRecommended));
                apply();
            });
        }

        // "/" muta focusul in cautare, ca in majoritatea cataloagelor.
        document.addEventListener('keydown', function (e) {
            if (e.key !== '/' || !search) return;
            if (e.target.closest('input, textarea, select, [contenteditable="true"]')) return;

            e.preventDefault();
            search.focus();
        });

        // Click pe card: descrierea completa. Butoanele si linkurile isi pastreaza rolul.
        catalog.addEventListener('click', function (e) {
            if (e.target.closest('a, button, form, [data-modal-url]')) return;

            const card = e.target.closest('[data-app]');
            if (card) card.classList.toggle('is-expanded');
        });
    });
})();
