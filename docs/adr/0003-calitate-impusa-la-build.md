# 0003 - Regulile de calitate se impun la build, nu prin conventie

**Stare:** Acceptat
**Data:** 2026-09-06

## Context

Solutia ajunsese in starea in care nu compila: o redenumire de proprietate
(`CreationDate` -> `CreatedAt`) fusese aplicata in entitate dar nu si in cele doua
locuri care o foloseau. Nu exista CI, deci nimic nu semnala asta.

Nu exista `.editorconfig`, versiunile de pachete erau duplicate in fiecare `.csproj`,
iar 29 MB de cod mort (`_to_delete/`) erau versionati in repo, cu tot cu `bin/` si `obj/`.

## Decizie

Regulile se impun mecanic, nu prin disciplina:

- `TreatWarningsAsErrors=true` in `Directory.Build.props`. Un warning netratat
  devine datorie tehnica, deci nu are voie sa treaca.
- `AnalysisMode=Recommended` si `EnforceCodeStyleInBuild=true` - regulile de stil
  din `.editorconfig` se verifica la compilare, nu doar in IDE.
- Central Package Management prin `Directory.Packages.props` - versiunile NuGet se
  declara o singura data.
- `global.json` pineaza SDK-ul, ca build-ul local sa fie identic cu cel din CI.
- CI pe GitHub Actions ruleaza restore, build Release, `dotnet format --verify-no-changes`
  si teste, la fiecare push si pull request.

Daca o regula de analizor chiar nu isi merita zgomotul, se dezactiveaza explicit
in `.editorconfig`, cu motivul scris langa ea. Nu se suprima punctual prin `#pragma`,
pentru ca asa suprimarea devine invizibila la review.

## Consecinte

**In favoare.** Prima activare a analizoarelor a scos imediat sase probleme reale
(cinci `CA1861`, un `IDE0052` pe un `ILogger` injectat si nefolosit) intr-un cod
care raporta zero warning-uri inainte. Un build rosu e mai ieftin decat un review
care rateaza aceleasi lucruri.

**Impotriva.** Actualizarea SDK-ului sau a nivelului de analiza poate introduce
warning-uri noi care blocheaza build-ul pana sunt tratate. E un cost acceptat:
alternativa e ca acelasi cod sa se acumuleze netratat.

**Deocamdata neacoperit.** Regula de dependinta intre straturi nu e verificata de
nimic; asta cere un test de arhitectura (etapa 3 din [ROADMAP](../ROADMAP.md)).
