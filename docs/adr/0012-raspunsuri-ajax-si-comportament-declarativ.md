# 0012 - Răspunsuri AJAX uniforme și comportament declarativ în view-uri

**Stare:** Acceptat
**Data:** 2026-10-17

## Context

Ecranele de administrare (drepturi, roluri, jocuri) lucrează prin modale încărcate cu
`fetch` și formulare trimise din JavaScript. Trei probleme țineau de felul în care erau
legate între ele:

- **Codurile HTTP nu spuneau nimic.** Un refuz de business sau un formular invalid plecau
  cu `200` și `{ success: false }`, iar o înregistrare lipsă cu `200` și un câmp `message`
  pe care JS-ul nu îl citea. Logurile, monitorizarea și testele vedeau orice ca reușită.
- **JS-ul era copiat.** Patru fișiere repetau aceleași funcții (`getToken`, `parseJson`,
  `loadIntoModal`, toasts, curățarea modalelor), fiecare cu câte o funcție pe acțiune
  (`createClaim`, `editClaim`, `createRole`...), legate din view prin 29 de atribute
  `onclick` în 14 fișiere.
- **CSP-ul nu putea fi strict.** Atributele `onclick` sunt script inline, deci
  `script-src` trebuia să păstreze `'unsafe-inline'`, adică exact ce face CSP-ul inutil
  împotriva unui script injectat.

Pe deasupra, butoanele aveau și `data-bs-toggle="modal"`, și `new bootstrap.Modal(...).show()`
în JS: modalul se deschidea de două ori, iar fundalurile rămase erau șterse „de mână” la
fiecare închidere. Ecranele de domenii erau pagini separate, cu alt stil.

## Decizie

### Codurile HTTP

Toate acțiunile apelate din JS răspund prin `AjaxResponseExtensions`:

| Situație | Răspuns |
|---|---|
| reușită | `200 { success: true }` |
| formular invalid | `400` `ProblemDetails`, cu mesajele din `ModelState` |
| refuz de business (`OperationResult`) | `400` `ProblemDetails`, cu motivul |
| înregistrare lipsă (modal) | `404` `ProblemDetails`, cu un mesaj |
| eroare neașteptată | `500` `ProblemDetails` (`AjaxExceptionHandler`, ADR-0011) |

Orice eșec are aceeași formă: `success: false`, lista `errors` și `traceId`. JS-ul citește
doar `errors`, indiferent de cod.

### Comportamentul se declară în view, nu se scrie în el

View-urile nu conțin JavaScript. Elementele spun ce fac prin atribute, iar `admin.js`,
încărcat din layout, le leagă prin câteva listener-e delegate la nivel de document:

- `data-modal-url` + `data-modal`: încarcă partialul și deschide modalul;
- `<form data-ajax-form>`: formular trimis prin `fetch`, cu token-ul antiforgery pus de
  tag helper-ul `<form>`; opțional `data-toast`, `data-toast-color`, `data-confirm`;
- `[data-form-errors]`: locul din formular unde apar erorile;
- `textarea[data-autosize]`: înălțimea crește cu textul.

Fiecare acțiune care modifică ceva este un `<form method="post">`, inclusiv butoanele de
pe rând (reactivare, eliminarea unui drept din rol). Filtrele sunt formulare `GET`
obișnuite, fără JS. Fișierele pe pagină rămân doar pentru ce e specific paginii:
autocomplete-ul de drepturi (`approles-details.js`) și întoarcerea cardurilor de joc
(`games.js`).

Cererile trimit `X-Requested-With`, deci cookie-ul de autentificare răspunde cu
`401`/`403` în loc de redirect spre login: o sesiune expirată reîncarcă pagina, iar o
pagină de login nu mai ajunge într-un modal.

### CSP strict

`script-src 'self'`, fără `'unsafe-inline'`. Un test (`InlineScriptScanner`) randează
paginile și modalele reale și pică dacă găsește un atribut de eveniment, un `<script>`
fără `src` sau o adresă `javascript:`.

## Alternative respinse

**Refuzurile cu `200` și `success: false`.** JS-ul ar fi mers la fel, dar codul HTTP
rămânea fără sens pentru loguri, monitorizare și proxy-uri, iar un test nu putea deosebi
un refuz de o reușită fără să citească corpul răspunsului.

**CSP cu nonce.** Un nonce acoperă elementele `<script>`, nu atributele de eveniment:
`onclick` ar fi cerut în continuare `'unsafe-inline'` sau `'unsafe-hashes'`.

**htmx sau Alpine.js.** htmx face același lucru prin atribute, dar e o dependință în plus
pentru circa 200 de rânduri de cod propriu; Alpine evaluează expresii din atribute și
cere `'unsafe-eval'`, adică o slăbire a CSP-ului în locul celei eliminate.

**Câte un fișier JS pe ecran, curățat.** Ar fi păstrat duplicarea: fiecare ecran nou ar
fi copiat din nou încărcarea modalelor, trimiterea formularelor și afișarea erorilor.

## Consecințe

**Un ecran nou de administrare nu cere JavaScript** pentru operațiile obișnuite: un
partial cu un `<form data-ajax-form>` și un buton cu `data-modal-url`.

**Refuzurile apar ca `400` în tab-ul Network** al browserului. Este comportamentul dorit,
nu o eroare.

**Ecranele de domenii s-au schimbat.** Crearea, editarea și ștergerea se fac în modale;
acțiunile `GET` întorc partiale, iar ștergerea se trimite la `POST /Domain/DeleteConfirmed/{id}`
(înainte `POST /Domain/Delete/{id}`), ca la drepturi și roluri.

**`style-src` păstrează `'unsafe-inline'`.** View-urile și Bootstrap folosesc atribute
`style`, iar un stil injectat nu poate rula cod.

**Comportamentul din browser nu are teste automate.** Testele acoperă contractul de pe
server (coduri, forma erorilor, atributele randate, lipsa scripturilor inline);
`admin.js` a fost verificat manual, în browser, pe toate ecranele.
