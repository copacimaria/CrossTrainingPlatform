# 0002 - Argon2id pentru hashing-ul parolelor

**Stare:** Acceptat
**Data:** 2025-07-06

## Context

Aplicatia isi gestioneaza singura autentificarea, fara ASP.NET Identity, deci
alegerea algoritmului de hashing este a noastra. Versiunea anterioara folosea
PBKDF2, care rezista slab la atacuri paralelizate pe GPU.

## Decizie

Argon2id, prin `Isopoh.Cryptography.Argon2`, cu parametrii:

| Parametru | Valoare |
|---|---|
| Memorie | 64 MB |
| Iteratii | 3 |
| Paralelism | 2 |
| Salt | 16 octeti, aleatori per parola |
| Lungime hash | 32 octeti |

Rezultatul se stocheaza in format PHC
(`$argon2id$v=19$m=65536,t=3,p=2$<salt>$<hash>`) in `AppUser.PasswordHash`,
`NVARCHAR(256)`.

## Consecinte

**In favoare.** Argon2id este castigatorul Password Hashing Competition si e
rezistent atat la atacuri GPU (prin costul de memorie) cat si side-channel
(varianta `id`). Formatul PHC isi poarta parametrii in el, deci parametrii pot fi
cresctuti in timp fara sa invalideze hash-urile deja stocate: la urmatorul login
reusit, hash-ul se poate recalcula cu parametrii noi.

**Impotriva.** 64 MB de memorie alocati per verificare de parola. La un varf de
login-uri simultane, asta conteaza - cu atat mai mult cu cat nu exista inca rate
limiting pe endpoint-ul de login (etapa 2 din [ROADMAP](../ROADMAP.md)).

**De stiut.** `Verify` inghite orice exceptie si intoarce `false`, ca un hash
invalid sau gol (cont vechi, migrat) sa fie tratat ca parola gresita, nu ca eroare
de server.
