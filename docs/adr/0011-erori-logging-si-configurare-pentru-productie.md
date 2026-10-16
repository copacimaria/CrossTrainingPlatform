# 0011 - Erori, logging și configurare pentru producție

**Stare:** Acceptat
**Data:** 2026-10-17

## Context

Codul era bine organizat, dar aplicația nu era pregătită să ruleze altundeva decât pe
mașina de dezvoltare:

- **erorile neașteptate** ajungeau la JavaScript ca pagină HTML, pe care nu o putea citi,
  deci utilizatorul vedea „Eroare necunoscută”;
- **logurile** erau text simplu, fără structură și fără niciun rând per cerere HTTP;
- **nu exista niciun endpoint** prin care un orchestrator să afle dacă aplicația trăiește
  și dacă ajunge la baza de date;
- **cheile Data Protection** nu aveau o locație configurabilă, deci pe mai multe instanțe
  sau într-un container fiecare repornire delogha toți utilizatorii;
- **în spatele unui reverse proxy**, limitarea încercărilor de login vedea un singur IP.

## Decizie

### Erori

Un `IExceptionHandler` (`AjaxExceptionHandler`) tratează erorile din cererile trimise de
JavaScript: răspunde cu un `ProblemDetails` standard, care conține și câmpurile citite de
JS (`success`, `errors`) plus codul cererii. Cererile de pagină ajung în continuare la
`/Home/Error`, care afișează același cod. Detaliile excepției nu ajung niciodată la client.
Excepția o loghează middleware-ul, o singură dată.

### Logging

**Serilog**, cu evenimente structurate. Proprietățile (`UseCase`, `ElapsedMs`, `RequestPath`,
`TraceId`) rămân câmpuri, nu text, deci se pot filtra.

- **Local:** text lizibil.
- **În rest:** JSON compact, câte un eveniment pe linie, gata de colectat.
- **Per cerere:** câte un rând (`UseSerilogRequestLogging`). Verificările de sănătate se
  loghează la nivelul cel mai jos.
- **Refuzuri de business:** se loghează ca refuz, cu motivul (`LoggingBehavior` +
  `IUseCaseOutcome`), nu ca reușită.
- **Ce nu se loghează:** cererile în sine, pentru că login-ul conține parola.

### Sănătate

- `/health`: procesul răspunde. Folosit pentru repornire automată.
- `/health/ready`: aplicația ajunge la baza de date. Folosit pentru trimiterea de trafic.

Ambele sunt anonime. Verificarea bazei e înregistrată în Infrastructure, ca `AppDbContext`
să rămână cunoscut doar acolo.

### Configurare pentru producție

| Cheie | Rol | Implicit |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | baza de date | obligatorie |
| `DataProtection__KeysPath` | folder comun și persistent pentru chei | locația implicită a mașinii |
| `ReverseProxy__KnownProxies__0..n` | proxy-urile de la care se acceptă `X-Forwarded-For` | niciunul |

Antetele de proxy se citesc doar de la proxy-urile declarate. Altfel oricine ar putea
trimite `X-Forwarded-For` și ar ocoli limitarea pe IP.

## Alternative respinse

**`Microsoft.Extensions.Logging` simplu, cu formatter JSON.** Ar fi mers fără dependințe
noi, dar ROADMAP-ul cerea Serilog, iar request logging-ul și îmbogățirea evenimentelor ar fi
trebuit scrise de mână.

**`Log.Logger` static, cu bootstrap logger.** Este configurația uzuală, dar testele pornesc
multe aplicații în același proces, iar loggerul static „înghețat” nu se poate reconfigura.
Fiecare host primește loggerul lui (`preserveStaticLogger`).

**Antetele de proxy acceptate de oriunde.** Mai simplu de configurat, dar deschide exact
breșa pe care limitarea de încercări o închide.

**Chei Data Protection în baza de date.** Ar fi rezolvat partajarea între instanțe fără un
folder comun, dar cere încă o migrare, iar cheile ar sta necriptate fără un certificat.
Folderul configurabil acoperă cazul de acum și lasă loc pentru certificat mai târziu.

## Consecințe

**`SetApplicationName("CrossTraining")` schimbă izolarea cheilor.** Cookie-urile emise
înainte devin invalide o singură dată, deci utilizatorii se loghează din nou.

**Pe mai multe mașini, cheile trebuie și protejate.** Folderul comun nu e de ajuns: cheile
trebuie criptate cu un certificat comun (`ProtectKeysWithCertificate`). Nu e făcut, pentru
că aplicația rulează pe o singură instanță.

**Nu există încă OpenTelemetry.** Corelarea se face prin `TraceId`/`RequestId` în loguri și
prin codul afișat utilizatorului, ceea ce ajunge pentru o singură aplicație. Tracing-ul
distribuit rămâne în ROADMAP.
