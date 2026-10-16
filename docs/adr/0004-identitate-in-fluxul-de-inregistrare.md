# 0004 - Identitatea in pasul de alegere a domeniilor

**Stare:** Acceptat
**Data:** 2026-09-06

## Context

Fluxul de inregistrare are trei pasi: register, alegerea domeniilor, login. Pasul din
mijloc se petrece inainte de autentificare, deci nu exista cookie din care sa se citeasca
cine e utilizatorul.

Solutia initiala trimitea id-ul in clar: `Register` redirectiona catre
`/Domain/SelectDomain?userId=42`, iar `SaveUserDomain` accepta acel `userId` dintr-un
camp ascuns de formular. Controllerul nu avea `[Authorize]`, deci actiunea era publica.

Consecinta: un POST catre `/Domain/SaveUserDomain` cu `userId=1` rescria domeniile
utilizatorului 1. Fara autentificare, fara nicio verificare. Id-urile fiind numere
consecutive, tot tabelul era la un `for` distanta.

## Decizie

Id-ul circula semnat, nu in clar. `RegistrationTokenProtector` foloseste
`ITimeLimitedDataProtector` din ASP.NET Core Data Protection:

- token-ul e semnat cu cheia aplicatiei, deci clientul nu il poate fabrica;
- e legat de un scop dedicat, deci nu poate fi refolosit in alt context;
- expira in 30 de minute, cat sa acopere pasul de inregistrare.

`SelectDomain` si `SaveUserDomain` desfac token-ul si, daca nu e valid, redirectioneaza
la register. Restul controllerului cere rol de Admin; cele doua actiuni sunt marcate
explicit `[AllowAnonymous]`.

Separat, aplicatia are acum o politica de autorizare implicita care cere utilizator
autentificat pe orice endpoint. Un controller nou este protejat din oficiu, iar accesul
public devine o decizie scrisa, nu o omisiune.

## Alternative respinse

**Autentificarea utilizatorului imediat dupa register**, cu `[Authorize]` pe pasul de
domenii. Ar fi fost mai simplu si ar fi eliminat token-ul cu totul, dar schimba fluxul
produsului: utilizatorul ar ajunge logat fara sa fi trecut prin ecranul de login. Decizia
apartine produsului, nu refactorizarii - de reluat daca fluxul se schimba oricum.

**Sesiune server-side** pentru id-ul in curs de inregistrare. Reintroduce starea de
sesiune pe care migrarea la cookie auth tocmai o eliminase.

## Consecinte

Cheile de Data Protection sunt, implicit, in profilul local al utilizatorului care ruleaza
procesul. In productie trebuie persistate intr-o locatie partajata si protejate, altfel
token-urile si cookie-urile de autentificare devin invalide la fiecare repornire si nu
functioneaza pe mai multe noduri. Exista un `TODO` in `Program.cs` pe aceasta linie.
