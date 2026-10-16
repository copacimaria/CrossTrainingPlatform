# 0009 - Protecția drepturilor și rolurilor de sistem

**Stare:** Acceptat
**Data:** 2026-10-17

## Context

ADR-0008 a mutat autorizarea pe drepturi și a lăsat baza de date sursa de adevăr. A
rămas însă o problemă: datele de care depinde autorizarea se puteau strica din chiar
ecranele de administrare.

**Un singur click lăsa aplicația fără administrator.** Dezactivarea rolului Admin sau
a dreptului `roles.manage` scotea, prin filtrul global de soft-delete, drepturile din
toți administratorii în cel mult cinci minute. Nu exista nicio comandă de reactivare
pentru roluri sau drepturi, iar lista de roluri nici nu le mai afișa pe cele
dezactivate. Refacerea era posibilă doar direct în baza de date.

**Drepturile erau identificate printr-un nume editabil și neunic.** Autorizarea caută
dreptul după `ClaimName`. Cine avea `claims.manage` putea redenumi în `roles.manage` un
drept deja acordat rolului său și, de acolo, își putea acorda orice. Fără index unic,
putea exista și un al doilea `roles.manage`.

**Interfața nu folosea sistemul de drepturi.** Meniul de administrare apărea după
`User.IsInRole("Admin")`, iar butoanele din ecrane căutau claim-uri cu nume vechi
(`"CreateClaim"`, `"EditRoles"`…) pe care noul sistem nu le mai emite. Pe o bază creată
din migrări, un administrator nu vedea niciun buton de creare, editare sau ștergere.

**Refuzurile treceau drept reușite.** Controllerele răspundeau `success: true`
indiferent de rezultatul comenzii.

## Decizie

### Drepturile de sistem sunt cele verificate de cod

Lista drepturilor (`Permissions`) se mută din Web în `Application/Authorization/`,
alături de `SystemRoles` (`User`, `Admin`). Le folosesc atât atributele de autorizare din
controllere, cât și cazurile de utilizare. „De sistem” nu e o etichetă pusă de mână: e
orice drept pe care codul îl verifică și orice rol de care depinde codul (`User` e
atribuit conturilor noi, `Admin` deține administrarea).

| Operație | Regula |
|---|---|
| Redenumirea unui drept sau rol de sistem | refuzată |
| Dezactivarea unui drept sau rol de sistem | refuzată |
| Crearea sau redenumirea în numele unui drept sau rol de sistem | refuzată (nume rezervat) |
| Scoaterea unui drept de sistem din rolul Admin | refuzată |
| Acordarea unui drept inexistent sau dezactivat | refuzată cu mesaj, nu lăsată pe seama cheii străine |

Regulile stau în handlere, nu în view-uri: o cerere trimisă direct, fără buton, e
refuzată la fel. Refuzul se întoarce ca `OperationResult`, iar controllerul îl trimite
mai departe ca `{ success: false, errors: [...] }`.

### Nume unice

Index unic pe `AppClaim.ClaimName` și pe `AppRole.RoleName`, inclusiv peste rândurile
dezactivate, ca reactivarea să nu poată produce un duplicat. Handlerele verifică numele
înainte de salvare printr-o interogare SQL, deci compară după colația bazei (fără
diferență între litere mari și mici), exact ca indexul. Migrarea
`UniqueRoleAndClaimNames` se oprește cu un mesaj explicit dacă baza are deja duplicate.

### Reactivare

Rolurile și drepturile dezactivate apar în listele de administrare, marcate ca atare,
cu un buton de reactivare. Reactivarea unui rol sau drept readuce și legăturile lui:
acestea nu se șterg la dezactivare, doar nu mai sunt văzute de filtrul global.

### Interfața urmează drepturile

`User.HasPermission(...)` aplică aceeași regulă ca `PermissionAuthorizationHandler`.
Meniul, butoanele și legăturile din view-uri o folosesc. Nicio decizie din interfață nu
mai depinde de numele rolului.

Odată cu asta, `domains.view` a devenit folosit: lista de domenii cere `domains.view`,
modificările cer `domains.manage`, la fel ca la roluri și drepturi.

## Alternative respinse

**O coloană `IsSystem` în baza de date.** Ar fi mutat în date ceva ce ține de cod: un
drept e „de sistem” pentru că un `[HasPermission]` îl verifică, nu pentru că un rând are
un flag. Flagul ar fi putut fi șters cu un `UPDATE` și ar fi cerut o schimbare de schemă
în plus.

**Rolurile de sistem identificate după id (1 și 2).** Migrarea `SeedPermissions` le
caută după nume, iar pe bazele vechi id-urile nu sunt garantate. Numele devine sigur
abia cu indexul unic și cu interdicția de redenumire, care vin împreună.

**Invariantul „mereu cel puțin un utilizator activ cu `roles.manage`”.** Mai precis decât
protecția rolului Admin, dar aplicația nu are încă ecran de schimbare a rolului unui
utilizator, deci singurele căi de blocare erau cele de mai sus. Se reevaluează când
apare acel ecran.

**Doar ascunderea butoanelor.** Ar fi oprit click-ul greșit, nu și cererea trimisă direct.

## Consecințe

**Rolul Admin nu poate fi restrâns din interfață.** Cine are nevoie de un administrator
cu drepturi parțiale creează un rol nou. Nu e o scurtătură de autorizare: Admin nu
primește nimic din cod, drepturile lui vin tot din baza de date. Protecția doar
împiedică pierderea lor.

**Un drept nou devine protejat automat** când constanta lui intră în `Permissions`.
Migrarea care îl inserează rămâne obligatorie, ca înainte.

**Două cereri simultane cu același nume** pot trece amândouă de verificarea din handler.
Pe a doua o oprește indexul, cu o eroare 500 în loc de mesaj. Cazul e rar și nu lasă
date greșite.

**O bază cu duplicate nu migrează singură.** Alegerea rândului care rămâne, și a
rolurilor legate de el, e o decizie, nu o regulă automată.

**Neacoperit încă:** jocurile dezactivate nu au reactivare (lista de jocuri le ascunde),
iar conturile noi primesc în continuare rolul cu `RoleId = 1`, nu rolul numit `User`.
