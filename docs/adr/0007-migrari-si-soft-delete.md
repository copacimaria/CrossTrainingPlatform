# 0007 - Migrări EF Core și soft-delete uniform

**Stare:** Acceptat
**Data:** 2026-09-06

## Context

**Nu exista cale de upgrade pentru o bază cu date.** Singurul artefact de schemă era
`db/002_Recreate_Schema.sql`, care începe cu `DROP TABLE`. Orice modificare de schemă
însemna fie pierderea datelor, fie un `ALTER` scris de mână și neversionat.

**Modelul EF diverga de baza reală**, tăcut: configurările declarau `nvarchar(max)` peste
tot deși coloanele sunt `NVARCHAR(100)`/`(150)`/`(256)`/`(500)`, iar două chei străine erau
declarate `Restrict` în model deși baza le are `ON DELETE SET NULL`.

**Soft-delete-ul era pe jumătate.** `AppRole` și `Game` se dezactivau, `Domain` și
`AppClaim` se ștergeau fizic. Nimic nu explica diferența, iar ștergerea unui domeniu
lăsa jocurile fără domeniu, ireversibil.

**Fiecare repository își salva singur modificările.** O operație care atinge două
repository-uri putea rămâne pe jumătate aplicată.

## Decizie

### Migrări, cu baseline

Migrările EF Core devin sursa de adevăr pentru schemă. `dotnet-ef` este fixat ca unealtă
locală în `.config/dotnet-tools.json`, deci toată lumea și CI-ul folosesc aceeași versiune.

Migrarea `InitialSchema` descrie schema **existentă**, nu una nouă. Bazele care există
deja o marchează ca aplicată prin `db/003_EF_Baseline.sql`, fără să atingă datele.

Un `IDesignTimeDbContextFactory` permite generarea migrărilor fără să pornească aplicația
web, deci fără să depindă de user-secrets sau de mediul de rulare.

### Soft-delete uniform, cu filtru global

Toate cele cinci entități implementează `ISoftDeletable`, iar `AppDbContext` aplică
automat un filtru global. Rândurile dezactivate dispar din orice interogare; ecranele de
administrare cer explicit `IgnoreQueryFilters()`.

Tabelele de legătură primesc filtre pereche (`RoleClaim`, `UserDomain`), altfel EF
avertizează — pe bună dreptate — că o legătură către un principal filtrat rămâne în
rezultat cu navigația goală.

### Unit of Work

Repository-urile pregătesc modificările; salvarea se face o singură dată, prin
`IUnitOfWork`, din serviciul care orchestrează cazul de utilizare.

### Paginare în baza de date

`PagedResult<T>` poartă rândurile paginii plus totalul. Filtrarea, `COUNT` și
`OFFSET/FETCH` se execută în SQL. Componenta de paginare din view primește un
`StaticPagedList`, deci biblioteca de UI nu mai vede niciodată setul complet.

## Consecințe

### Schimbări de comportament, intenționate

- **Un cont dezactivat nu se mai poate autentifica.** Înainte, `IsActive` nu era verificat
  nicăieri la login. Verificarea de duplicat de la înregistrare folosește însă
  `EmailExistsAsync`, care ignoră filtrul: emailul are index unic, deci un cont dezactivat
  blochează în continuare adresa. Fără această excepție, adresa ar părea liberă și
  inserarea ar pica pe index.
- **Ștergerea unui domeniu sau a unui drept îl dezactivează**, nu îl mai șterge fizic.
  Rândul rămâne vizibil în ecranul de administrare, cu `IsActive = No`.
- **Un drept dezactivat dispare din permisiunile din cookie** prin filtru, nu printr-un
  `Where` scris manual în `AuthService`.

### Un bug găsit prin avertismentul EF

`GetClaimsForRoleAsync` proiecta `rc.Claim!` din tabelul pivot. Cu filtrul activ, o
legătură către un drept dezactivat rămânea în rezultat, dar navigația era `null` — deci
lista de permisiuni ar fi conținut `null`, iar citirea `ClaimName` ar fi aruncat. Interogarea
pornește acum de la `Claims`, unde filtrul se aplică direct pe entitatea proiectată.

### Verificat, nu presupus

Baseline-ul este onest doar dacă migrarea produce exact schema existentă. Am creat două
baze de date — una din `db/002_Recreate_Schema.sql`, una din migrare — și le-am comparat
coloană cu coloană, index cu index, cheie străină cu cheie străină: identice. Comparația
a scos la iveală un index (`IX_AppUser_RoleId`) pe care EF îl creează prin convenție și
care lipsea din script; a fost adăugat în script și în baseline, nu scos din model.

CI rulează `dotnet ef migrations has-pending-model-changes`: dacă cineva schimbă modelul
fără să genereze migrarea, build-ul pică, nu deploy-ul.

### Costuri

- Filtrele de pe tabelele de legătură referă navigații, deci adaugă un JOIN la
  interogările pe pivot. Acceptabil la volumul actual.
- `IgnoreQueryFilters()` trebuie ținut minte în fiecare interogare de administrare. Riscul
  este asimetric în favoarea noastră: dacă se uită, un ecran de admin arată mai puțin decât
  ar trebui — vizibil imediat. Varianta inversă, fără filtru global, ar fi însemnat că
  uitarea unui `Where(x => x.IsActive)` expune date pe care utilizatorul nu ar trebui să
  le vadă.
- `AppUser.CreatedAt` conține în continuare date mixte (ora locală înainte de etapa 3,
  UTC după). Nimic nu compară valorile momentan; de normalizat înainte de primul raport
  pe interval de timp.
