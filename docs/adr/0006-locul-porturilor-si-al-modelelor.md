# 0006 - Locul porturilor și al modelelor

**Stare:** Acceptat
**Data:** 2026-09-06

## Context

Straturile existau ca proiecte, dar granițele lor erau poroase în trei feluri.

**Porturile erau împărțite arbitrar.** Interfețele de repository stăteau în
`Domain/Interfaces`, iar `IPasswordService` în `Application/Interfaces`. Nicio regulă nu
spunea care unde merge, deci fiecare contract nou era o decizie de la zero.

**Application cunoștea stratul de prezentare.** `Application/ViewModels/` conținea
modele de formular cu `DataAnnotations`, iar `AppRolesClaimsViewModel` expunea
`IPagedList<AppClaim>` — un tip din `X.PagedList`, bibliotecă de UI, declarată ca
dependință a proiectului Application.

**Entitățile circulau până în browser.** Acțiunile legau direct pe entități
(`[Bind("ClaimName")] AppClaim`, `Edit(int id, Game game)`) și le pasau view-urilor.
Asta înseamnă risc de over-posting la intrare și cuplare view ↔ schemă de bază de date
la ieșire: o coloană redenumită sparge un ecran.

## Decizie

**Toate porturile în `Application/Abstractions/`**, grupate pe capabilitate
(`Persistence/`, `Security/`). `Domain` rămâne strict entități — nicio interfață.

**Modelele de formular în `Web/Models/`**, cu atributele de validare pentru UI. Către
Application pleacă un `record` din `Application/Requests/`, fără nimic din contextul
afișării. Conversia se face printr-un `ToRequest()` pe modelul de formular.

**Application nu întoarce entități.** Fiecare caz de utilizare întoarce un DTO plat din
`Application/DTOs/`. Inclusiv `AuthResult`, care expunea `AppUser` cu tot cu
`PasswordHash` și acum expune `AuthenticatedUserDto` — trei câmpuri.

**Entitățile nu apar în semnăturile acțiunilor**, nici ca parametru, nici ca model de
view.

## Alternative respinse

**Porturile în Domain, în stil DDD clasic**, cu repository-urile ca parte a modelului de
domeniu. Ar fi fost la fel de consistent și ar fi cerut mai puține mutări. Respins pentru
că `IPasswordService` nu are cum să devină un concept de domeniu fără să forțeze
lucrurile, iar o convenție care are excepții de la prima aplicare nu e o convenție.

**Păstrarea ViewModels în Application, doar fără `X.PagedList`.** Ar fi rezolvat simptomul
vizibil, dar `DataAnnotations` cu mesaje în română pentru utilizator ar fi rămas într-un
strat care nu știe nimic despre cine citește mesajul.

## Consecințe

**Există duplicare, intenționat.** `RegisterViewModel` și `RegisterRequest` au aceleași
patru câmpuri. Nu e cod redundant: unul poartă reguli de afișare și se schimbă când se
schimbă ecranul, celălalt poartă contractul cazului de utilizare și se schimbă când se
schimbă regula de business. Când vor începe să difere, separarea se va fi plătit.

**Regulile sunt executabile.** Trei teste de arhitectură noi le apără: `Domain` nu mai
conține nicio interfață, `Application` nu depinde de `X.PagedList` sau `Microsoft.AspNetCore`,
iar nicio acțiune de controller nu primește un tip din assembly-ul `Domain`. Fără ele,
regulile s-ar eroda la prima grabă.

**Compilatorul nu acoperă tot.** Directiva `@model` a unui view este verificată la build,
dar potrivirea dintre modelul pe care controllerul îl pasează unui partial și tipul pe
care partialul îl declară se vede abia la randare. De aceea etapa a adăugat teste care
randează efectiv paginile de administrare, cu un utilizator autentificat de test și date
reale în baza de test.

**Proiecția rămâne în memorie.** `GameService` încă face trei interogări și le împerechează
în cod, doar că acum rezultatul e un DTO în loc de o entitate cu navigație încărcată
manual. Interogarea propriu-zisă se rezolvă în etapa 5.

**`PagedResult<T>` a fost amânat.** Roadmap-ul îl prevedea aici, dar paginarea se face
încă în memorie, în controller. Un tip nou de paginare fără paginare la nivel de bază de
date ar fi fost ceremonie fără conținut. Intră în etapa 5, împreună cu `Skip`/`Take`.
