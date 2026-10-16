# 0001 - Arhitectura pe patru straturi

**Stare:** Acceptat
**Data:** 2025-07-06

## Context

Prima versiune a aplicatiei era un singur proiect MVC in care controllerele
deschideau conexiuni SQL direct, prin `SqlConnectionFactory`, si construiau
interogari cu string-uri. Logica de business traia in controllere, iar modelele
erau folosite simultan ca entitati de baza de date si ca modele de view.
Nimic nu era testabil fara o baza de date reala.

## Decizie

Solutia se imparte in patru proiecte, cu dependintele orientate spre interior:

- `CrossTraining.Domain` - entitati si contracte de repository. Fara nicio dependinta.
- `CrossTraining.Application` - cazurile de utilizare si porturile catre infrastructura.
- `CrossTraining.Infrastructure` - EF Core, repository-uri, hashing.
- `CrossTraining.Web` - controllere, view-uri, autentificare, compunerea DI.

Accesul la date se face prin EF Core, cu maparile in Fluent API
(`Infrastructure/Persistence/Configurations`), pastrand schema existenta a bazei -
nu se genereaza o schema noua din model.

`Web` refera `Infrastructure` doar ca sa poata apela `AddInfrastructure` la pornire.
Controllerele lucreaza exclusiv prin interfetele din `Application`.

## Consecinte

**In favoare.** Logica de business poate fi testata fara baza de date. Schimbarea
furnizorului de persistenta atinge un singur proiect. Regula de dependinta e
verificabila automat.

**Impotriva.** Mai multa ceremonie pentru operatii simple: un CRUD banal trece
prin controller, serviciu, repository. Pentru entitatile fara logica proprie
(`Claim`, `Role`, `Domain`) stratul de serviciu a ajuns pass-through pur - un cost
real, de rezolvat in etapa 6 din [ROADMAP](../ROADMAP.md).

**De aparat activ.** Nimic din compilator nu impiedica un controller sa injecteze
`AppDbContext`. Regula trebuie impusa printr-un test de arhitectura (etapa 3).
