/* =====================================================================
   CrossTrainingPlatform - Baseline pentru migrarile EF Core
   De rulat O SINGURA DATA, pe fiecare baza de date care exista deja
   inainte de introducerea migrarilor.

   Ce face:
     1. Adauga indexul IX_AppUser_RoleId, pe care modelul EF il asteapta
        (EF Core il creeaza prin conventie pe cheia straina catre rol).
     2. Creeaza tabelul de istoric al migrarilor si inregistreaza migrarea
        initiala ca fiind deja aplicata.

   Dupa rularea acestui script, `dotnet ef database update` nu va incerca sa
   recreeze schema: va porni de la migrarile urmatoare.

   Pe o baza noua, goala, NU se ruleaza acest script - se ruleaza direct
   `dotnet ef database update`, care creeaza schema si isi scrie singur istoricul.
   ===================================================================== */

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------
   PASUL 1: Indexul asteptat de model
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AppUser_RoleId' AND object_id = OBJECT_ID('dbo.AppUser'))
    CREATE INDEX IX_AppUser_RoleId ON dbo.AppUser (RoleId);
GO

/* ---------------------------------------------------------------------
   PASUL 2: Istoricul migrarilor
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory
    (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260906114356_InitialSchema')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260906114356_InitialSchema', N'8.0.11');
END
GO

PRINT 'Baseline aplicat. Migrarile EF Core pot fi rulate de acum incolo.';
GO
