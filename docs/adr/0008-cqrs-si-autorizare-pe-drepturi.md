# 0008 - CQRS cu MediatR și autorizare pe drepturi

**Stare:** Acceptat
**Data:** 2026-09-06

## Context

**Stratul de servicii nu își justifica existența.** `ClaimService`, `RoleService` și
`DomainService` erau pass-through pur: metode care apelau metode de repository, una la
una. Un strat care doar redirecționează este cost fără beneficiu — și, mai rău, ascunde
faptul că nu există niciun loc unde să pui validare, tranzacții sau logare fără să le
repeți în fiecare metodă.

**Validarea era în trei locuri.** DataAnnotations pe modelele de formular,
`string.IsNullOrWhiteSpace` în controllere, reguli de business în servicii — fără nicio
regulă care să spună ce unde merge.

**Autorizarea nu folosea sistemul de drepturi.** Exista o schemă completă rol → drept în
baza de date, dar fiecare controller scria `[Authorize(Roles = "Admin")]`. Sistemul de
drepturi era mobilier.

**Permisiunile erau înghețate în cookie.** O permisiune revocată rămânea valabilă până la
expirare, adică o oră. Un cont dezactivat continua să funcționeze la fel de mult.

## Decizie

### Un handler per caz de utilizare

Serviciile dispar. Fiecare caz de utilizare devine o comandă sau o interogare, cu
handlerul și validatorul în același fișier — felii verticale, grupate pe funcționalitate
în `Application/Features/`. Când se schimbă un caz de utilizare, se schimbă un fișier.

Distincția `ICommand<T>` / `IQuery<T>` nu e decorativă: pipeline-ul o citește ca să decidă
ce se aplică.

### Trei preocupări transversale, o singură dată

| Behavior | Ce face | De ce în pipeline |
|---|---|---|
| `LoggingBehavior` | un rând per caz de utilizare, cu durata | înainte, niciun serviciu nu injecta `ILogger` |
| `ValidationBehavior` | rulează validatoarele FluentValidation | altfel se repetă la începutul fiecărui handler |
| `UnitOfWorkBehavior` | tranzacție în jurul comenzilor | două salvări în același handler nu mai pot rămâne pe jumătate |

Ordinea e intenționată: logarea vede tot, inclusiv eșecurile de validare; validarea
respinge înainte să se deschidă o tranzacție; tranzacția se deschide doar pentru comenzi
valide.

### Unde se validează ce

Trei niveluri, cu o regulă clară pentru fiecare — asta înlocuiește validarea împrăștiată,
nu o mută:

- **Modelele de formular (Web)** poartă regulile de afișare: `[Required]`, lungimi,
  format de email. Ele alimentează validarea din browser și mesajele de lângă câmp.
- **Validatoarele de comandă (Application)** apără cazul de utilizare indiferent de
  apelant: invarianți ca „un id trebuie să fie pozitiv" sau „numele nu poate fi gol".
  Eșecul e o excepție, pentru că înseamnă cerere malformată.
- **Handlerul** păstrează regulile care cer acces la date sau compară câmpuri între ele:
  „emailul e deja luat", „parola nouă e identică cu cea veche". Acestea se întorc ca
  rezultat, nu ca excepție — sunt răspunsuri de business, nu date invalide.

### Autorizare pe drepturi

`[Authorize(Roles = "Admin")]` devine `[HasPermission(Permissions.Roles.Manage)]`.
Politicile se construiesc la cerere dintr-un `IAuthorizationPolicyProvider`, deci nu
există o listă de înregistrat manual care s-ar desincroniza de constante la prima
adăugare uitată.

**Nu există scurtătură pentru rolul Admin în cod.** Ar fi fost tentant: un `if` care
lasă rolul Admin să treacă orice. Dar atunci sursa de adevăr ar fi rămas în cod, iar
sistemul de drepturi ar fi rămas decorativ. Rolul Admin primește toate drepturile prin
migrarea `SeedPermissions`, deci comportamentul e același — dar configurabil din baza de
date, ceea ce era tot rostul.

### Reîmprospătarea identității

`OnValidatePrincipal` recitește rolul și drepturile la un interval scurt (implicit 5
minute, configurabil) și rescrie cookie-ul. Un cont dispărut sau dezactivat duce la
respingerea sesiunii.

## Alternative respinse

**MediatR 13.** Începând cu versiunea 13, MediatR cere licență comercială. Versiunea e
fixată la 12.4.1 (Apache 2.0), la fel cum s-a ales Shouldly în locul FluentAssertions 8.
O urcare la 13 este o decizie de licențiere, nu una tehnică — de aceea constrângerea e
scrisă lângă versiune, în `Directory.Packages.props`.

**Un dispatcher scris de mână**, ca să nu existe dependința deloc. Ar fi vreo sută de
linii și ar da aceeași arhitectură. Respins pentru că versiunea 12 e liberă și
întreținută, iar un mediator propriu ar fi cod de întreținut fără câștig.

**CQRS doar pe comenzi**, cu citirile rămase în servicii subțiri. Ar fi fost mai puțin
cod, dar două stiluri în același strat: unde cauți un caz de utilizare depinde de dacă
scrie sau citește.

## Consecințe

**Mai multe fișiere, fiecare mai mic.** Șase servicii au devenit 31 de cazuri de
utilizare. Nu e un câștig în volum de cod — e un câștig în locul unde se face o
schimbare: adăugarea unui caz de utilizare nu mai cere o linie de înregistrare în DI și
nu mai atinge o interfață pe care o folosesc alte zece metode.

**Pentru CRUD simplu, ceremonia e vizibilă.** `DeactivateClaimCommand` are un validator
de o linie și un handler de opt. Este prețul uniformității; alternativa — CRUD direct
pentru cazurile banale — ar însemna două stiluri și o graniță pe care nimeni nu ar ști
unde s-o traseze.

**Ecranele de administrare depind acum de date.** Fără migrarea `SeedPermissions`, toate
ar fi inaccesibile. Riscul e real, deci e acoperit de teste care verifică pe o bază creată
din migrări că drepturile există și că rolul Admin le are.

**Un test cu toate drepturile nu dovedește nimic despre refuz.** De aceea există și un
test cu un utilizator autentificat fără niciun drept, care trebuie să primească 403.

**Costul revalidării** este o interogare per sesiune activă la fiecare interval — nu la
fiecare cerere. Intervalul e configurabil tocmai ca să poată fi testat: cu valoarea
implicită, un test ar trebui să aștepte cinci minute.
