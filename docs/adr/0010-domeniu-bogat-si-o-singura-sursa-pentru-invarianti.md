# 0010 - Domeniu bogat și o singură sursă pentru invarianți

**Stare:** Acceptat
**Data:** 2026-10-17
**Modifică:** ADR-0006 (ce conține Domain), ADR-0008 (unde se validează ce), ADR-0009 (locul listei de drepturi)

## Context

O evaluare a arhitecturii a găsit două probleme legate între ele.

**Clean Architecture fără domeniu.** Proiectul avea patru straturi, iar stratul Domain,
cel care ar trebui să fie nucleul, conținea doar entități cu setteri publici. Nicio
regulă de business nu stătea în ele. Regulile erau împărțite între validatoarele
FluentValidation și handlere, iar oricine putea scrie `claim.IsActive = false` peste un
drept de sistem, ocolind regula din handler. Forma arhitecturii promitea un nucleu de
business pe care conținutul nu îl avea.

**Ceremonie supradimensionată.** Fiecare caz de utilizare avea trei clase: comanda,
handlerul și validatorul. Validatoarele repetau ce verificau deja formularele din Web
(lungimi, câmpuri obligatorii) și ce ar fi trebuit să verifice entitatea. Peste ele,
un `ValidationBehavior` arunca o excepție pe care nimeni nu o trata, așa că o cerere
care ocolea formularul ajungea la utilizator ca eroare 500.

## Decizie

### Regulile stau în entități

Entitățile au setteri privați și constructori privați, folosiți doar de EF Core. Starea
se schimbă numai prin metode care își verifică regulile:

| Entitate | Ce impune |
|---|---|
| `AppClaim` | nume valid și nerezervat; drepturile de sistem nu se redenumesc și nu se dezactivează |
| `AppRole` | la fel pentru rolurile User și Admin; acordarea și revocarea trec prin rol, iar Admin nu pierde un drept de sistem |
| `AppUser` | nume și prenume obligatorii; parolă de minim 6 caractere; cel puțin un domeniu ales |
| `Game` | nume, descriere și link în limitele coloanelor; linkul doar `http`/`https` |
| `TrainingDomain` | nume obligatoriu, în limita coloanei |

Refuzul se întoarce ca `DomainResult`, cu motivul pe înțelesul utilizatorului, nu ca
excepție. Handlerul îl transformă în `OperationResult`.

**`Email` devine value object**: se validează și se normalizează (fără spații, cu litere
mici) o singură dată, la creare. Pentru EF Core rămâne o coloană `nvarchar(256)`, printr-un
value converter. Comparațiile se traduc tot în SQL, deci folosesc colația bazei, ca indexul
unic.

**Lista drepturilor și a rolurilor de sistem** (`Permissions`, `SystemRoles`) se mută din
Application în `Domain/Authorization`: entitățile au nevoie de ea ca să-și aplice regulile.

### O singură sursă pentru fiecare tip de regulă

| Unde | Ce | De ce acolo |
|---|---|---|
| Formularele din Web | `[Required]`, lungimi, format | validare în browser și mesaj lângă câmp |
| Entitățile | invarianții de business | se aplică oricine ar apela entitatea |
| Handlerele | regulile care cer acces la date | numele e deja luat? domeniul ales există? |

FluentValidation, validatoarele și `ValidationBehavior` au fost scoase. Pipeline-ul
MediatR rămâne cu două preocupări transversale: **logare** și **tranzacție**.

### O regulă de arhitectură o păstrează

Un test nou, `Entitatile_NuAuSetteriPublici`, pică dacă o entitate primește un setter
public. Fără el, încapsularea s-ar eroda la prima grabă.

## Alternative respinse

**Renunțarea la MediatR și CQRS**, pentru și mai puțină ceremonie. Pipeline-ul aduce
logarea și tranzacțiile pentru toate cazurile de utilizare, fără să se repete în fiecare
handler; acela e câștigul real al MediatR. Ceremonia în plus venea din validatoarele care
dublau alte reguli, nu din MediatR.

**Păstrarea FluentValidation lângă domeniul bogat.** Ar fi însemnat aceleași reguli în
două locuri, deci exact duplicarea care trebuia eliminată.

**Setteri publici, plus metode cu reguli.** Mai puține schimbări, dar regulile ar fi rămas
ocolibile, deci decorative.

**Value objects pentru toate câmpurile** (`RoleName`, `ClaimName`, `PasswordHash`). Ar fi
dat mai multă formă decât conținut. `Email` are reguli proprii (format, normalizare);
numele au doar lungime, pe care entitatea o verifică direct.

## Consecințe

**Un caz de utilizare are două clase, nu trei:** comanda și handlerul. Regulile se citesc
în entitate, într-un singur loc.

**Testele construiesc entitățile prin metodele de domeniu.** Testele unitare care au
nevoie de o entitate „încărcată din bază”, cu id sau cu nume de sistem, o construiesc
prin reflexie, exact cum o face EF Core (`tests/CrossTraining.UnitTests/Loaded.cs`).

**Datele vechi pot încălca regulile noi.** Un joc cu link de forma `jjj` se citește
normal, dar nu se mai poate salva până nu primește o adresă `http`/`https` validă.

**Mesajele de business sunt în română și stau în domeniu.** Dacă aplicația va avea mai
multe limbi, mesajele vor trebui extrase. Datoria apare deja în ROADMAP („Cod bilingv”).
