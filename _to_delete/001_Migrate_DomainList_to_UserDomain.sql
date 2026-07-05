/* =====================================================================
   Migrare: coloana JSON DomainList  ->  tabel pivot UserDomain
   Platforma: CrossTrainingPlatform (SQL Server)

   Ruleaza acest script O SINGURA DATA, dupa backup-ul bazei.
   Pasii 1-2 sunt siguri (creeaza tabelul si muta datele).
   Pasul 3 (stergerea coloanei vechi) este comentat - decomenteaza-l
   doar dupa ce ai verificat ca migrarea a reusit.
   ===================================================================== */

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------
   PASUL 1: Creare tabel pivot UserDomain (many-to-many User <-> Domain)
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.UserDomain', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDomain
    (
        UserId   INT NOT NULL,
        DomainId INT NOT NULL,
        CONSTRAINT PK_UserDomain PRIMARY KEY (UserId, DomainId),
        CONSTRAINT FK_UserDomain_AppUser FOREIGN KEY (UserId)
            REFERENCES dbo.AppUser (UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserDomain_Domain FOREIGN KEY (DomainId)
            REFERENCES dbo.Domain (DomainId) ON DELETE CASCADE
    );

    PRINT 'Tabelul dbo.UserDomain a fost creat.';
END
ELSE
    PRINT 'Tabelul dbo.UserDomain exista deja - se sare peste creare.';
GO

/* ---------------------------------------------------------------------
   PASUL 2: Migrare date din coloana JSON DomainList (ex: "[1,3]")
   Foloseste OPENJSON (SQL Server 2016+).
   --------------------------------------------------------------------- */
IF COL_LENGTH('dbo.AppUser', 'DomainList') IS NOT NULL
BEGIN
    INSERT INTO dbo.UserDomain (UserId, DomainId)
    SELECT DISTINCT u.UserId, TRY_CAST(j.[value] AS INT) AS DomainId
    FROM dbo.AppUser u
    CROSS APPLY OPENJSON(u.DomainList) j
    WHERE ISJSON(u.DomainList) = 1
      AND TRY_CAST(j.[value] AS INT) IS NOT NULL
      -- doar domenii existente
      AND EXISTS (SELECT 1 FROM dbo.Domain d WHERE d.DomainId = TRY_CAST(j.[value] AS INT))
      -- evitam dublurile daca scriptul e rulat din nou
      AND NOT EXISTS (
            SELECT 1 FROM dbo.UserDomain ud
            WHERE ud.UserId = u.UserId
              AND ud.DomainId = TRY_CAST(j.[value] AS INT)
      );

    PRINT 'Datele din DomainList au fost migrate in UserDomain.';
END
ELSE
    PRINT 'Coloana DomainList nu exista - migrarea datelor a fost sarita.';
GO

/* ---------------------------------------------------------------------
   PASUL 3 (OPTIONAL): Stergerea coloanei vechi DomainList.
   Decomenteaza DOAR dupa ce ai verificat rezultatul migrarii cu:
       SELECT * FROM dbo.UserDomain ORDER BY UserId;
   --------------------------------------------------------------------- */
-- IF COL_LENGTH('dbo.AppUser', 'DomainList') IS NOT NULL
-- BEGIN
--     ALTER TABLE dbo.AppUser DROP COLUMN DomainList;
--     PRINT 'Coloana DomainList a fost stearsa.';
-- END
-- GO
