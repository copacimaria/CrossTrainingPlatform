# Roadmap

Ce mai e de facut, in ordinea in care are sens sa se faca. Fiecare etapa lasa
solutia intr-o stare valida - nu exista etapa care sa depinda de una viitoare
ca sa compileze sau sa functioneze.

---

## Etapa 1 - Fundatia de build ✅

- [x] Reparat build-ul (redenumire `CreationDate` -> `CreatedAt` lasata pe jumatate)
- [x] `.editorconfig` cu stil si conventii de numire
- [x] `Directory.Build.props` cu `TreatWarningsAsErrors` si analizoare
- [x] `Directory.Packages.props` (Central Package Management)
- [x] `global.json` care pineaza SDK-ul
- [x] Sters `_to_delete/` (29 MB de cod mort versionat); `001_*.sql` recuperat in `db/`
- [x] `README.md` cu diagrama straturilor
- [x] CI pe GitHub Actions: build + format + test
- [x] Dependabot pentru NuGet si GitHub Actions

---

## Etapa 2 - Securitate ✅

- [x] **IDOR in `DomainController.SaveUserDomain`.** Actiunea nu cerea autentificare si
      lua `userId` din formular: oricine putea rescrie domeniile oricarui cont cu un POST.
      Id-ul circula acum ca token semnat cu Data Protection, valabil 30 de minute
      (`Web/Security/RegistrationTokenProtector.cs`). Controllerul cere rol de Admin
      implicit; cele doua actiuni din inregistrare sunt marcate `[AllowAnonymous]`.
- [x] **`AppRolesController.SearchClaim`** primea lista de claim-uri din body-ul clientului
      si o filtra server-side. Inlocuit cu `SearchAvailableClaims`, care filtreaza in baza
      de date pornind de la rol, cu escapare de wildcard-uri LIKE si limita de rezultate.
- [x] **XSS stocat in autocomplete-ul de claim-uri.** Rezultatele erau concatenate in
      `innerHTML`, deci un claim numit `<img onerror=...>` executa cod in browserul
      oricarui admin. Lista se construieste acum prin DOM, cu `textContent`.
- [x] **`RemoveClaim`** are `[ValidateAntiForgeryToken]`.
- [x] **Base64 folosit ca "token"** in `AddClaim` - eliminat. Era encoding, nu securitate.
- [x] **Rate limiting** pe login si register: 10 incercari / 5 minute per IP, cu
      `Retry-After` in raspuns. Verificat: a 11-a cerere primeste 429.
- [x] **Cookie hardening**: nume propriu, `HttpOnly`, `SameSite=Lax`, `SecurePolicy=Always`.
- [x] **Autorizare implicita.** `FallbackPolicy` cere utilizator autentificat pe orice
      endpoint; exceptiile sunt explicite. Inainte, un controller nou fara `[Authorize]`
      era public din oficiu.
- [x] **Security headers**: CSP, `X-Content-Type-Options`, `X-Frame-Options`,
      `Referrer-Policy`, `Permissions-Policy`. Kestrel nu mai trimite headerul `Server`.
- [x] **Connection string-ul** scos din `appsettings.json` versionat, mutat in user-secrets.
      Lipsa lui da acum o eroare care spune exact ce comanda trebuie rulata.
- [x] **Pagina dedicata de acces interzis** (`/UserAuth/AccessDenied`), cu `Url.IsLocalUrl`
      pe `returnUrl` ca sa nu devina open redirect. Inainte, 403-ul era ascuns prin
      redirect la pagina de prezentare.

### Decis, nu implementat

- **Enumerarea de conturi la Register.** Mesajul "Exista deja un cont cu acest email"
  spune unui atacator ce adrese sunt inregistrate. Nu se poate elimina fara verificare
  pe email: altfel, un utilizator care isi reintroduce propriul email nu ar avea cum sa
  afle de ce nu merge. Pana intra trimiterea de email, mitigarea este rate limiting-ul
  de mai sus, care e activ si pe register. De reluat cand exista infrastructura de email.

### CSP efectiv ✅ (in etapa 9)

CSP-ul avea `script-src 'self' 'unsafe-inline'`, pentru ca view-urile de administrare
foloseau **29 de atribute `onclick`** (in 14 fisiere) pe care politica le-ar fi blocat.

- [x] Atributele `onclick` inlocuite cu atribute `data-*`, legate de `admin.js`.
- [x] `'unsafe-inline'` scos din `script-src`; un test randeaza paginile si pica la orice
      script inline.

## Etapa 3 - Testabilitate ✅

62 de teste, toate rulate la fiecare build. Structura:

| Proiect | Ce acopera | Are nevoie de |
|---|---|---|
| `tests/CrossTraining.UnitTests` | 26 teste: `PasswordService` (Argon2 real), `AuthService`, `UserService`, `GameService` | nimic |
| `tests/CrossTraining.ArchitectureTests` | 7 teste: regula de dependinta dintre straturi, cu NetArchTest | nimic |
| `tests/CrossTraining.IntegrationTests` | 23 teste de pipeline (WebApplicationFactory) + 6 pe SQL Server real | SQL Server pentru ultimele 6 |

- [x] Proiect de teste unitare cu xUnit + NSubstitute + Shouldly.
- [x] Proiect de teste de arhitectura cu NetArchTest: `Domain` fara dependinte spre
      exterior, `Application` fara EF Core, controllerele fara `Infrastructure`,
      fiecare repository cu interfata in `Domain`.
- [x] Teste de integrare pe pipeline-ul real: headere de securitate, politica de
      autorizare implicita, rate limiting, si o regresie explicita pentru IDOR-ul din
      ADR-0004.
- [x] Teste de repository pe SQL Server real, cu baza de date proprie recreata din
      `db/002_Recreate_Schema.sql`.
- [x] `TimeProvider` injectat in `AuthService` si `GameService`. Nu mai exista niciun
      `DateTime.Now` in `src/`.
- [x] CI ruleaza testele cu acoperire de cod si publica rezultatele ca artifact.

### De ce nu Testcontainers

Roadmap-ul initial prevedea Testcontainers. Nu are sens aici: masina de dezvoltare are
deja SQL Server Express, iar in CI un service container face aceeasi treaba. Testcontainers
ar fi cerut Docker local - care nu exista pe masina de dezvoltare - si o imagine de un
giga descarcata la fiecare rulare. `SqlServerFixture` citeste connection string-ul din
`CROSSTRAINING_TEST_SQL`, cade pe `localhost\SQLEXPRESS` daca variabila lipseste, si
marcheaza testele ca **sarite** daca nu ajunge la niciun server - nu ca picate.

### Verificat, nu doar scris

Testele de escapare LIKE au fost validate prin mutatie: cu `EscapeLikePattern` inlocuit
cu identitate, ambele pica. Un test care trece si cu bug-ul prezent nu apara nimic.

### De urmarit

`AuthService` si `GameService` scriu acum `CreatedAt` in **UTC**, nu in ora locala.
Randurile create inainte de aceasta schimbare sunt in ora locala, deci coloana are date
mixte. Nimic nu compara sau nu afiseaza `CreatedAt` in acest moment, dar daca apare un
raport pe interval de timp, datele vechi trebuie normalizate mai intai.

## Etapa 4 - Curatenia straturilor ✅

- [x] **Porturile unificate** in `Application/Abstractions/` (`Persistence/`, `Security/`).
      `Domain` nu mai contine nicio interfata - doar entitati. Inainte, contractele de
      repository stateau in `Domain/Interfaces` iar `IPasswordService` in
      `Application/Interfaces`, fara nicio regula care sa spuna care unde merge.
- [x] **`Application/ViewModels/` desfiintat.** Modelele de formular au trecut in
      `Web/Models/`, cu atributele de validare pentru UI. Catre Application pleaca
      `record`-uri din `Application/Requests/`.
- [x] **`X.PagedList` scos din `CrossTraining.Application.csproj`.** Era o dependinta de
      UI intr-un strat care nu are voie sa stie ce e o pagina de rezultate. In `Web` este
      acum declarata explicit, nu luata transitiv.
- [x] **Application nu mai intoarce entitati.** Fiecare caz de utilizare intoarce un DTO
      plat. Inclusiv `AuthResult`, care expunea `AppUser` cu tot cu `PasswordHash` si
      acum expune `AuthenticatedUserDto` cu trei campuri.
- [x] **Entitatile nu mai apar in semnaturile actiunilor.** `[Bind("ClaimName")] AppClaim`
      si `Edit(int id, Game game)` au fost inlocuite cu modele de formular dedicate,
      cu lungimi aliniate la schema. 24 de directive `@model` actualizate.

### Verificat, nu doar scris

Trei teste de arhitectura noi apara regulile: `Domain` nu mai contine nicio interfata,
`Application` nu depinde de `X.PagedList` sau `Microsoft.AspNetCore`, si nicio actiune de
controller nu primeste un tip din assembly-ul `Domain`.

Compilatorul verifica interiorul fiecarui view, dar **nu** verifica daca modelul pe care
controllerul il paseaza unui partial e de tipul pe care partialul il declara - aceea e o
eroare de randare. De aceea au intrat 12 teste care randeaza efectiv paginile de
administrare, cu utilizator autentificat de test si date reale in baza de test. Validate
prin mutatie: cu `@model` gresit pe un singur partial, testul pica.

### Reparat pe parcurs

Formularele de joc trimiteau erorile de validare ca HTML catre un JS care astepta JSON,
deci orice camp invalid se afisa ca "Eroare necunoscuta". Acum intorc JSON, ca restul.

Cele doua clase de test care ating baza de date primeau fiecare propriul fixture, iar
fixture-ul recreeaza schema cu `DROP TABLE`: rulate in paralel, isi stergeau tabelele
una alteia (deadlock in SQL Server). Fixture-ul e acum partajat printr-o colectiune xUnit,
deci schema se creeaza o singura data pe rulare si clasele ruleaza secvential.

### Amanat, cu motiv

`PagedResult<T>` era prevazut aici, dar paginarea se face inca in memorie, in controller.
Un tip nou de paginare fara paginare la nivel de baza de date ar fi fost ceremonie fara
continut. Trece in etapa 5, impreuna cu `Skip`/`Take`.

## Etapa 5 - Persistenta ✅

- [x] **Migrari EF Core**, cu `dotnet-ef` fixat ca unealta locala in
      `.config/dotnet-tools.json`. Migrarea `InitialSchema` descrie schema *existenta*;
      bazele care exista deja o marcheaza ca aplicata prin `db/003_EF_Baseline.sql`.
      Un `IDesignTimeDbContextFactory` permite generarea migrarilor fara sa porneasca
      aplicatia web.
- [x] **Configurarile aliniate la schema reala.** `HasMaxLength` in loc de
      `nvarchar(max)` peste tot, si doua chei straine corectate din `Restrict` in
      `SetNull` - modelul spunea altceva decat face baza de date.
- [x] **Unit of Work.** Repository-urile pregatesc modificarile, serviciul salveaza o
      singura data. Nicio metoda de repository nu mai apeleaza `SaveChangesAsync`.
- [x] **Soft-delete uniform** prin `ISoftDeletable` si filtru global in `AppDbContext`.
      `Domain` si `AppClaim` nu se mai sterg fizic. Ecranele de administrare cer explicit
      `IgnoreQueryFilters()`.
- [x] **Paginare si cautare in baza de date.** `PagedResult<T>` (amanat din etapa 4) plus
      `COUNT` si `OFFSET/FETCH` in SQL. Inainte se incarca tot tabelul si se pagina in
      memorie.
- [x] **`GameService.GetGamesForUserAsync`**: doua interogari in loc de trei, cu domeniul
      adus prin `Include` in loc de join in memorie.
- [x] `AsNoTracking()` pe interogarile de citire.

### Un bug gasit pe drum

EF a avertizat ca `RoleClaim` are un principal filtrat si o navigatie obligatorie.
Avertismentul era corect: `GetClaimsForRoleAsync` proiecta `rc.Claim!` din tabelul pivot,
deci o legatura catre un drept dezactivat ar fi produs `null` in lista de permisiuni, iar
citirea `ClaimName` ar fi aruncat. Interogarea porneste acum de la `Claims`.

### Verificat, nu presupus

Baseline-ul e onest doar daca migrarea produce exact schema existenta. Am creat doua baze
de date - una din `db/002_Recreate_Schema.sql`, una din migrare - si le-am comparat
coloana cu coloana (28), index cu index, cheie straina cu cheie straina: **identice**.
Comparatia a scos un index (`IX_AppUser_RoleId`) pe care EF il creeaza prin conventie si
care lipsea din script; a fost adaugat in script, nu scos din model.

Filtrul global e acoperit de 7 teste pe SQL Server real, validate prin mutatie: cu filtrul
inversat, trei dintre ele pica.

CI ruleaza acum `dotnet ef migrations has-pending-model-changes`: daca modelul se schimba
fara migrare, pica build-ul, nu deploy-ul.

### Schimbari de comportament

- **Un cont dezactivat nu se mai poate autentifica.** `IsActive` nu era verificat nicaieri
  la login. Verificarea de duplicat de la inregistrare ignora insa filtrul, altfel emailul
  unui cont dezactivat ar parea liber si inserarea ar pica pe indexul unic.
- **Stergerea unui domeniu sau a unui drept il dezactiveaza.** Randul ramane vizibil in
  ecranul de administrare, cu `IsActive = No`.

## Etapa 6 - Cazuri de utilizare si autorizare ✅

- [x] **Serviciile pass-through au disparut.** `ClaimService`, `RoleService` si
      `DomainService` erau metode care apelau metode de repository, una la una. Cele sase
      servicii au devenit **31 de cazuri de utilizare**, grupate pe functionalitate in
      `Application/Features/`, cu comanda, validatorul si handlerul in acelasi fisier.
- [x] **CQRS cu MediatR** (versiunea 12, Apache 2.0 - vezi nota de licentiere de mai jos),
      cu trei pipeline behaviors: logare, validare, tranzactie. Ordinea e intentionata:
      logarea vede tot, validarea respinge inainte sa se deschida o tranzactie, tranzactia
      se deschide doar pentru comenzi valide.
- [x] **FluentValidation**, cu o regula clara pentru ce unde se valideaza: formatul si
      mesajele pentru utilizator raman pe modelele de formular din Web; invariantii
      cazului de utilizare intra in validatoare; regulile care cer acces la date raman in
      handler si se intorc ca rezultat, nu ca exceptie.
- [x] **Autorizare pe drepturi.** `[Authorize(Roles = "Admin")]` a devenit
      `[HasPermission(...)]`. Politicile se construiesc la cerere, deci nu exista o lista
      de inregistrat manual care s-ar desincroniza de constante.
- [x] **Reimprospatarea permisiunilor.** `OnValidatePrincipal` reciteste rolul si
      drepturile la interval scurt. Fereastra de revocare scade de la o ora (expirarea
      cookie-ului) la cinci minute, iar un cont dezactivat este deconectat.

### Migrarea care face autorizarea sa functioneze

Trecerea la drepturi ar fi lasat toate ecranele de administrare inaccesibile: politicile
ar fi cautat drepturi care nu exista in baza. Migrarea `SeedPermissions` insereaza
drepturile, se asigura ca rolurile de baza exista si le acorda rolului Admin - totul
idempotent, deci merge si pe baze existente, si pe baze create doar din migrari.

**Nu exista scurtatura pentru rolul Admin in cod.** Ar fi fost mai simplu un `if` care
lasa rolul Admin sa treaca orice, dar atunci sursa de adevar ar fi ramas in cod si
sistemul de drepturi ar fi ramas decorativ.

### Verificat, nu presupus

- Trei reguli de arhitectura noi: `Domain` nu depinde de MediatR sau FluentValidation,
  handlerele stau in feliile de functionalitate, iar controllerele nu ating porturile de
  persistenta (ar ocoli pipeline-ul, deci si validarea, si tranzactia, si logarea).
- Teste pe o baza creata **din migrari** ca drepturile chiar exista si ca rolul Admin le
  are - altfel migrarea ar putea fi gresita fara sa se observe.
- Un test cu utilizator autentificat **fara niciun drept**, care trebuie sa primeasca 403.
  Testele care ruleaza cu toate drepturile ar fi verzi si cu o politica scrisa gresit.
- Testele de revalidare parcurg fluxul real: login prin formular, cookie emis de
  aplicatie, drept revocat in baza, cerere noua. Validate prin mutatie - cu
  reimprospatarea dezactivata, ambele pica.
- Smoke test end-to-end pe aplicatia pornita: inregistrare, promovare la Admin, login,
  acces la paginile protejate de drepturi.

### Nota de licentiere

MediatR 12.x este Apache 2.0; incepand cu versiunea 13, proiectul cere licenta comerciala,
la fel ca FluentAssertions 8. Versiunea este fixata la 12.4.1, iar constrangerea este
scrisa langa ea in `Directory.Packages.props`. O urcare la 13 este o decizie de
licentiere, nu una tehnica.

## Etapa 6b - Protectia drepturilor si rolurilor de sistem ✅

Vezi [ADR-0009](adr/0009-protectia-drepturilor-si-rolurilor-de-sistem.md).

- [x] **Drepturile si rolurile de sistem nu mai pot fi stricate din ecran.** Dezactivarea
      rolului Admin sau a dreptului `roles.manage` lasa aplicatia fara administrator, cu
      refacere posibila doar din SQL. Acum sunt refuzate redenumirea si dezactivarea
      drepturilor din `Permissions.All` si a rolurilor `User`/`Admin`, plus scoaterea unui
      drept de sistem din rolul Admin.
- [x] **Escaladarea prin redenumire, inchisa.** Cine avea `claims.manage` putea redenumi in
      `roles.manage` un drept deja acordat rolului sau. Numele drepturilor si rolurilor de
      sistem sunt acum rezervate.
- [x] **Nume unice**: `UX_AppClaim_ClaimName` si `UX_AppRole_RoleName`, inclusiv peste
      randurile dezactivate. Migrarea `UniqueRoleAndClaimNames` se opreste cu un mesaj clar
      daca baza are deja duplicate.
- [x] **Reactivare** pentru roluri si drepturi. Lista de roluri arata acum si rolurile
      dezactivate; ambele liste au coloana de stare si eticheta „Sistem”.
- [x] **Interfata dupa drepturi.** Meniul de administrare nu mai depinde de rolul "Admin",
      iar butoanele din view-uri folosesc `User.HasPermission(...)`, aceeasi regula ca
      autorizarea. `domains.view` este folosit acum pentru lista de domenii.
- [x] **Refuzurile ajung in ecran.** Comenzile pe roluri si drepturi intorc
      `OperationResult`; controllerele nu mai raspund `success: true` indiferent de rezultat.
- [x] `Permissions` mutat din `Web/Security` in `Application/Authorization`, ca sa-l poata
      folosi si cazurile de utilizare.

### Descoperit pe drum

View-urile de administrare ascundeau butoanele dupa claim-uri cu nume vechi
(`"CreateClaim"`, `"EditClaim"`, `"DeleteClaim"`, `"CreateRoles"`, `"EditRoles"`,
`"DeleteRoles"`, `"AddClaim"`, `"RemoveClaim"`), pe care sistemul de drepturi nu le mai
emite. Pe o baza creata din migrari, un administrator nu vedea niciun buton de creare,
editare sau stergere. Ecranul de jocuri folosea `User.IsInRole("Admin")`.

Butoanele noi de reactivare folosesc listeners delegati, nu `onclick`. Numarul de
atribute inline din sectiunea CSP de mai sus nu a crescut (in etapa 9 au fost scoase toate).

### Verificat, nu presupus

- 49 de teste noi: reguli pe handlere (unitare), index unic si comparatie fara diferenta
  de litere pe SQL Server real, meniul randat pentru utilizatori cu drepturi diferite,
  plus cereri POST reale din ecran (dezactivarea rolului Admin e refuzata, reactivarea
  unui drept merge).
- Validate prin mutatie: meniul intors la `IsInRole("Admin")`, protectia rolului Admin
  scoasa din `RevokeClaimFromRoleHandler` si controllerul care ignora din nou rezultatul
  comenzii - fiecare face cel putin un test sa pice.
- `dotnet ef migrations has-pending-model-changes`: modelul si migrarile sunt sincrone.

### De urmarit

- **Jocurile dezactivate** nu au inca reactivare: lista de jocuri le ascunde.
- **`RegisterUserHandler` atribuie `RoleId = 1`**, nu rolul numit `User`. Pe o baza unde
  id-ul 1 apartine altui rol, conturile noi ar primi rolul gresit.
- **`SeedPermissions` pe o baza veche.** Migrarea insereaza `User` cu `RoleId = 1` daca nu
  gaseste un rol cu acest nume. Daca id-ul 1 e ocupat de un rol cu alt nume (baza locala
  de dezvoltare are, in tabelele `_TD`, "Platform User" si "App Admin"), insertul pica pe
  cheia primara. De verificat inainte de a aduce o asemenea baza la zi.

## Etapa 7 - Observabilitate si configurare pentru productie ✅ (fara OpenTelemetry)

Vezi [ADR-0011](adr/0011-erori-logging-si-configurare-pentru-productie.md).

- [x] **Serilog** cu logging structurat: text lizibil local, JSON compact in rest, un rand
      per cerere HTTP. Refuzurile de business apar ca refuz, cu motivul, nu ca reusita.
- [x] **Tratarea globala a erorilor**: cererile din JS primesc un `ProblemDetails` cu
      mesaj si codul cererii; paginile ajung la `/Home/Error`, cu acelasi cod.
- [x] **Health checks**: `/health` (procesul raspunde) si `/health/ready` (ajunge la baza
      de date), anonime.
- [x] **Data Protection**: nume de aplicatie fix si folder de chei configurabil
      (`DataProtection:KeysPath`).
- [x] **Reverse proxy**: `X-Forwarded-For` acceptat doar de la proxy-urile declarate
      (`ReverseProxy:KnownProxies`).
- [x] Corelare: codul cererii apare in log, pe pagina de eroare si in raspunsul JSON.
- [ ] OpenTelemetry: trace-uri si metrici.
- [ ] Cheile Data Protection protejate cu certificat, pentru mai multe instante.

### Reparat pe parcurs

- `/Home/Privacy` dadea 500: actiunea exista, view-ul nu. Actiunea a fost scoasa.
- Request logging-ul Serilog scrie implicit in `Log.Logger`, pe care aplicatia il lasa
  intentionat neconfigurat (testele pornesc multe aplicatii in acelasi proces), deci
  randurile de cerere se pierdeau. Primeste acum explicit logger-ul aplicatiei.

## Etapa 8 - Domeniu bogat si mai putina ceremonie ✅

Vezi [ADR-0010](adr/0010-domeniu-bogat-si-o-singura-sursa-pentru-invarianti.md).

- [x] **Regulile de business in entitati.** Setteri si constructori privati; starea se
      schimba prin metode care isi verifica regulile si intorc `DomainResult`.
- [x] **`Email` value object**, validat si normalizat o singura data; coloana ramane
      `nvarchar(256)` prin value converter.
- [x] **Rolul ca radacina a legaturilor rol-drept**: `Grant`/`Revoke` trec prin `AppRole`.
- [x] **Linkurile de joc doar `http`/`https`** - inainte, `javascript:...` ajungea intr-un `href`.
- [x] **FluentValidation scos**, impreuna cu validatoarele si `ValidationBehavior`, care
      dublau regulile din formulare si din entitati. Un caz de utilizare are acum doua
      clase, nu trei, iar o cerere care ocoleste formularul primeste mesaj, nu eroare 500.
- [x] **Existenta datelor verificata in handlere** (domeniul unui joc, domeniile alese de
      utilizator) - un id gresit nu mai ajunge la cheia straina.
- [x] Regula de arhitectura noua: `Entitatile_NuAuSetteriPublici`.

### Verificat, nu presupus

- Teste unitare noi direct pe entitati (`tests/CrossTraining.UnitTests/Domain`), fara
  mock-uri si fara baza de date.
- Pe SQL Server real: comparatia pe `Email` se traduce in SQL si nu tine cont de litere.
- Validate prin mutatie: protectia drepturilor de sistem scoasa din `AppClaim`, regula de
  link scoasa din `Game`, raspunsul JSON la erori AJAX dezactivat, verificarea bazei scoasa
  din `/health/ready`, un setter public pe o entitate - fiecare face cel putin un test sa pice.
- Pornire pe baza locala reala: `/health/ready` raspunde 200, login-ul ajunge la baza.

### Schimbari de comportament

- **Un joc cu link care nu e adresa http/https** se citeste normal, dar nu se mai poate
  salva pana nu primeste un link valid (in baza locala: "jjj", "ff", "ffffff").
- **Cookie-urile vechi devin invalide o data**, din cauza numelui de aplicatie fixat in
  Data Protection: utilizatorii se logheaza din nou.

## Etapa 9 - .NET 10, raspunsuri de eroare uniforme, un singur stil de front-end ✅

Vezi [ADR-0012](adr/0012-raspunsuri-ajax-si-comportament-declarativ.md).

- [x] **.NET 10** (LTS, suport pana in noiembrie 2028). Pachetele Microsoft, EF Core si
      instrumentul `dotnet-ef` la 10.0.13; `KnownNetworks` (scos din uz) devine
      `KnownIPNetworks`.
- [x] **`System.Security.Cryptography.Pkcs` fixat la 10.0.13**: dependinta tranzitiva a
      `Microsoft.Data.SqlClient` aducea 9.0.11, cu doua vulnerabilitati de tip denial of
      service (CVE-2026-84008, CVE-2026-70162).
- [x] **Coduri HTTP cu sens pentru cererile din JS**: `400` pentru formular invalid si
      refuz, `404` pentru o inregistrare lipsa, `500` pentru eroare neasteptata. Aceeasi
      forma `ProblemDetails` peste tot (`success`, `errors`, `traceId`). Inainte, toate
      plecau cu `200`.
- [x] **Un singur fisier JS comun** (`admin.js`) in locul a patru fisiere cu cod copiat.
      View-urile declara comportamentul prin atribute `data-*`; operatiile obisnuite nu mai
      cer JS scris pe ecran.
- [x] **Domeniile in acelasi stil** cu drepturile si rolurile: tabel cu stare, modale,
      formulare AJAX. Inainte erau pagini separate, nestilizate.
- [x] **CSP efectiv**: `script-src 'self'`, fara `'unsafe-inline'`.

### Reparat pe parcurs

- **Filtrul din detaliile unui rol nu facea nimic**: termenul nu ajungea la controller.
- **Termenul cautat disparea** dupa filtrare in listele de drepturi si roluri, iar
  paginarea pierdea filtrul (`ViewBag.SelectedClaim`/`SelectedRole` nu erau setate).
- **Layout-ul cerea `CrossTraining.Web.styles.css`**, un fisier care nu exista (proiectul
  nu are CSS izolat pe view). Fiecare pagina facea o cerere esuata; pentru un vizitator
  neautentificat, cererea era redirectionata spre login.
- **Modalele se deschideau de doua ori** (`data-bs-toggle` plus `new bootstrap.Modal()` in
  JS), iar fundalurile ramase erau sterse manual la fiecare inchidere.
- O sesiune expirata deschidea pagina de login in interiorul modalului; acum pagina se
  reincarca si ajunge la login.

### Verificat, nu presupus

- Teste noi pe SQL Server real: `400` pentru formular invalid si nume duplicat, `404`
  pentru modalele unei inregistrari lipsa (drepturi, roluri, jocuri, domenii), filtrul din
  detaliile rolului, dezactivarea unui domeniu prin casuta debifata.
- Teste noi fara baza: CSP-ul are exact `script-src 'self'`; paginile anonime si cele de
  administrare (inclusiv modalele) nu au script inline; resursele referite de layout exista.
- Validate prin mutatie (11): `onclick` intr-un modal, `<script>` inline in layout, un
  `href="javascript:"`, `'unsafe-inline'` readus, refuz si formular invalid cu `200`,
  inregistrare lipsa cu `400`, filtrul din detaliile rolului ignorat, termenul de cautare
  pierdut, `NotFound()` gol pentru domenii, legatura spre `styles.css` readusa - fiecare
  face cel putin un test sa pice.
- `admin.js` verificat in browser, pe baza de test, pe toate ecranele: deschiderea
  modalelor, erorile in modal (nume duplicat, link invalid), crearea, editarea, stergerea
  si reactivarea cu mesajul de confirmare, autocomplete-ul de drepturi, filtrele, cardurile
  de joc, tastatura pe cardul „+”. Nicio incalcare de CSP in consola.

### Schimbari de comportament

- Refuzurile apar ca `400` in tab-ul Network al browserului.
- Ecranele de domenii sunt modale; stergerea se trimite la `POST /Domain/DeleteConfirmed/{id}`.

## Etapa 10 - Redesign: o platforma de aplicatii educationale ✅

- [x] **O singura foaie de stil** (`app.css`) in locul a sase (`site.css`, `layout.css`,
      `auth.css`, `games.css`, `admin.css` si o copie a iconitelor Bootstrap, incarcata de
      doua ori). Tema intunecata: aurora si grila in fundal, panouri de sticla, degrade
      cyan-violet, animatii oprite pentru `prefers-reduced-motion`.
- [x] **Navigare noua**: bara de sus cu Catalog, Despre, meniul Administrare (dupa drepturi)
      si meniul contului (profil, domenii, parola, delogare). Pe telefon, un panou.
      Bara laterala si `layout.js` au fost scoase.
- [x] **Catalogul ca un magazin de aplicatii**: carduri cu iconita si culoare proprie,
      domeniu, descriere, eticheta „Recomandat”, cautare instantanee (si tasta `/`), filtre
      pe domeniu si „doar recomandate”. Recomandatele apar primele.
- [x] **Pagina de prezentare** (Despre): hero cu hologram animat, avantaje, pasii de
      inceput, indemn la inregistrare.
- [x] **Contul**: autentificare si inregistrare pe doua coloane, alegerea domeniilor ca
      dale selectabile, profil cu avatar si actiuni.
- [x] **Administrarea**: file intre Roluri / Drepturi / Domenii, tabele cu stare, confirmari
      de stergere cu iconita, modale cu „Renunta” / actiune.
- [x] **Interfata in romana**, inclusiv mesajele de validare (cele implicite erau in
      engleza) si mesajul de autentificare esuata.

### Verificat, nu presupus

- Toate paginile parcurse in browser, pe baza de test, la latime de desktop si de telefon
  (375 px): fara derulare orizontala, fara incalcari de CSP in consola.
- Reparat pe parcurs: inelul rotit al hologramei latea pagina; filtrele de domeniu ocupau
  mai multe randuri (acum un rand derulabil); mesajele toast se intindeau pe toata latimea.
- Testele existente trec neschimbate, cu o exceptie: meniul de administrare se cauta dupa
  `id="adminMenu"`, nu dupa textul vechi „Admin Settings”. Teste noi pentru initiale si
  culori (`DisplayExtensionsTests`).

---

## Datorie tehnica fara etapa alocata

- [ ] **Localizare.** Interfata si mesajele sunt acum toate in romana (etapa 10), dar
      scrise direct in cod si in view-uri. Pentru o a doua limba, de trecut prin
      `IStringLocalizer`.
- [x] ~~**Domeniu anemic.**~~ Rezolvat in etapa 8.
- [x] ~~**Front-end**: cod JS copiat intre fisiere.~~ Rezolvat in etapa 9 (`admin.js`).
      Ramane: fara bundling si fara minificare (cache busting-ul exista, prin
      `asp-append-version`).
- [x] ~~**Trecerea la .NET 10**~~: facuta in etapa 9.
