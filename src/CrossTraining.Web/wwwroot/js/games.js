// Pagina Games: intoarcerea cardurilor la click. Modalele, formularele si mesajele le
// trateaza admin.js.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.flip-card').forEach(card => {
        const suitable = card.querySelector('.suitable');

        card.addEventListener('click', function (e) {
            // Butoanele de editare si stergere si linkul de descarcare nu intorc cardul.
            if (e.target.closest('a, button, form, [data-modal-url]')) return;

            const expanded = card.getAttribute('data-expanded') === 'true';
            card.classList.toggle('flipped');
            card.classList.toggle('flip-card-expanded', !expanded);
            card.setAttribute('data-expanded', String(!expanded));
            if (suitable) suitable.style.display = expanded ? 'block' : 'none';
        });
    });
});
