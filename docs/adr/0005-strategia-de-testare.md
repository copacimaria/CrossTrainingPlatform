# 0005 - Strategia de testare

**Stare:** Acceptat
**Data:** 2026-09-06

## Context

Solutia nu avea niciun test. Nicio schimbare nu putea fi facuta cu incredere, iar
regulile de arhitectura din [ADR-0001](0001-arhitectura-pe-straturi.md) nu erau aparate
de nimic: compilatorul permite oricand unui controller sa injecteze `AppDbContext`.

## Decizie

Trei proiecte, fiecare cu un rol distinct si cu un cost de rulare distinct.

**`CrossTraining.UnitTests`** - logica de business, cu dependintele inlocuite prin
NSubstitute. Exceptia este `PasswordService`, testat pe implementarea reala de Argon2id:
tocmai comportamentul criptografic ne intereseaza, un mock nu ar demonstra nimic.

**`CrossTraining.ArchitectureTests`** - regula de dependinta, cu NetArchTest. Este
singurul loc din solutie unde ADR-0001 devine executabil.

**`CrossTraining.IntegrationTests`** - doua categorii:
- pipeline-ul HTTP prin `WebApplicationFactory`, fara baza de date. Acopera exact ce
  nu poate fi testat unitar: ordinea middleware-urilor, politica de autorizare
  implicita, headerele de securitate, limitatorul de rata.
- interogarile de repository, pe SQL Server real. Traducerea LINQ catre SQL si semantica
  `LIKE` apartin furnizorului de baza de date; un `DbContext` in memorie ar fi testat
  altceva decat ce ruleaza in productie.

### Instrumente

**Shouldly**, nu FluentAssertions: incepand cu versiunea 8, FluentAssertions cere licenta
platita pentru uz comercial. Shouldly este Apache 2.0 si acopera aceleasi nevoi.

**SQL Server real, nu Testcontainers.** Masina de dezvoltare are deja SQL Server Express,
iar in CI exista un service container. Testcontainers ar fi cerut Docker local - care nu
e instalat - si descarcarea unei imagini de un giga. `SqlServerFixture` isi ia connection
string-ul din `CROSSTRAINING_TEST_SQL`, cade pe instanta locala daca variabila lipseste,
si marcheaza testele ca sarite daca nu ajunge la niciun server.

**`TimeProvider` injectat**, nu `DateTime.Now`. Un serviciu care citeste direct ceasul
sistemului nu poate fi testat determinist.

## Consecinte

**Testele sarite sunt o capcana.** Un test sarit arata verde. Daca SQL Server lipseste
si din CI, cele sase teste de persistenta dispar fara ca nimeni sa observe. De aceea
service container-ul din workflow nu este optional, iar motivul sarii este afisat explicit.

**Testele trebuie sa poata pica.** Cele doua teste de escapare `LIKE` au fost validate
prin mutatie: cu escaparea scoasa, ambele pica. Un test care trece si cu bug-ul prezent
da o falsa siguranta - mai rau decat lipsa lui.

**Costul unei rulari.** Testele unitare si de arhitectura ruleaza in ~2 secunde. Cele de
persistenta recreeaza schema la fiecare pornire de fixture, ceea ce adauga ~1 secunda.
Acceptabil acum; daca numarul lor creste, schema ar trebui creata o singura data si
datele izolate prin tranzactii per test.
