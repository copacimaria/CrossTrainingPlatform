/* =====================================================================
   CrossTrainingPlatform - Schema completa (recreare de la zero)
   Mediu: DEVELOPMENT. Scriptul STERGE si recreeaza tabelele.
   NU rula pe o baza cu date reale.

   Design:
     - Parole stocate ca hash Argon2id in coloana PasswordHash NVARCHAR(256)
     - Toate coloanele text sunt NVARCHAR (diacritice romanesti)
     - User <-> Domain: many-to-many prin tabelul UserDomain
     - Role <-> Claim: many-to-many prin tabelul AppClaim_AppRole
     - Game apartine unui Domain (o singura relatie; fara Domain.GameId)
     - Email unic; indexuri pe cheile straine
   ===================================================================== */

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------
   PASUL 0: Stergere tabele existente (in ordinea dependintelor)
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.UserDomain', 'U')       IS NOT NULL DROP TABLE dbo.UserDomain;
IF OBJECT_ID('dbo.AppClaim_AppRole', 'U') IS NOT NULL DROP TABLE dbo.AppClaim_AppRole;
IF OBJECT_ID('dbo.Game', 'U')             IS NOT NULL DROP TABLE dbo.Game;
IF OBJECT_ID('dbo.AppUser', 'U')          IS NOT NULL DROP TABLE dbo.AppUser;
IF OBJECT_ID('dbo.AppClaim', 'U')         IS NOT NULL DROP TABLE dbo.AppClaim;
IF OBJECT_ID('dbo.AppRole', 'U')          IS NOT NULL DROP TABLE dbo.AppRole;
IF OBJECT_ID('dbo.Domain', 'U')           IS NOT NULL DROP TABLE dbo.Domain;
GO

/* ---------------------------------------------------------------------
   PASUL 1: Tabele independente
   --------------------------------------------------------------------- */
CREATE TABLE dbo.AppRole
(
    RoleId   INT IDENTITY(1,1) NOT NULL,
    RoleName NVARCHAR(100)     NOT NULL,
    IsActive BIT               NOT NULL CONSTRAINT DF_AppRole_IsActive DEFAULT (1),
    CONSTRAINT PK_AppRole PRIMARY KEY (RoleId)
);
GO

CREATE TABLE dbo.AppClaim
(
    ClaimId   INT IDENTITY(1,1) NOT NULL,
    ClaimName NVARCHAR(100)     NOT NULL,
    IsActive  BIT               NOT NULL CONSTRAINT DF_AppClaim_IsActive DEFAULT (1),
    CONSTRAINT PK_AppClaim PRIMARY KEY (ClaimId)
);
GO

CREATE TABLE dbo.Domain
(
    DomainId   INT IDENTITY(1,1) NOT NULL,
    DomainName NVARCHAR(150)     NOT NULL,
    IsActive   BIT               NOT NULL CONSTRAINT DF_Domain_IsActive DEFAULT (1),
    CONSTRAINT PK_Domain PRIMARY KEY (DomainId)
);
GO

/* ---------------------------------------------------------------------
   PASUL 2: AppUser (depinde de AppRole)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.AppUser
(
    UserId       INT IDENTITY(1,1) NOT NULL,
    Name         NVARCHAR(100)     NOT NULL,
    FamilyName   NVARCHAR(100)     NOT NULL,
    Email        NVARCHAR(256)     NOT NULL,
    PasswordHash NVARCHAR(256)     NOT NULL,   -- hash Argon2id (format PHC)
    CreationDate DATETIME2         NULL,
    isActive     BIT               NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1),
    RoleId       INT               NULL,
    CONSTRAINT PK_AppUser PRIMARY KEY (UserId),
    CONSTRAINT FK_AppUser_AppRole FOREIGN KEY (RoleId)
        REFERENCES dbo.AppRole (RoleId) ON DELETE SET NULL
);
GO

-- Email unic (nu doi useri cu acelasi email) + folosit la fiecare login
CREATE UNIQUE INDEX UX_AppUser_Email ON dbo.AppUser (Email);
GO

/* ---------------------------------------------------------------------
   PASUL 3: Game (depinde de Domain)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Game
(
    GameId       INT IDENTITY(1,1) NOT NULL,
    GameName     NVARCHAR(100)     NOT NULL,
    IsActive     BIT               NOT NULL CONSTRAINT DF_Game_IsActive DEFAULT (1),
    CreatedAt    DATETIME2         NULL,
    Description  NVARCHAR(500)     NULL,
    DownloadLink NVARCHAR(500)     NULL,
    DomainId     INT               NULL,
    CONSTRAINT PK_Game PRIMARY KEY (GameId),
    CONSTRAINT FK_Game_Domain FOREIGN KEY (DomainId)
        REFERENCES dbo.Domain (DomainId) ON DELETE SET NULL
);
GO

CREATE INDEX IX_Game_DomainId ON dbo.Game (DomainId);
GO

/* ---------------------------------------------------------------------
   PASUL 4: Tabele pivot (many-to-many)
   --------------------------------------------------------------------- */
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
GO

CREATE INDEX IX_UserDomain_DomainId ON dbo.UserDomain (DomainId);
GO

CREATE TABLE dbo.AppClaim_AppRole
(
    RoleId  INT NOT NULL,
    ClaimId INT NOT NULL,
    CONSTRAINT PK_AppClaim_AppRole PRIMARY KEY (RoleId, ClaimId),
    CONSTRAINT FK_ClaimRole_Role FOREIGN KEY (RoleId)
        REFERENCES dbo.AppRole (RoleId) ON DELETE CASCADE,
    CONSTRAINT FK_ClaimRole_Claim FOREIGN KEY (ClaimId)
        REFERENCES dbo.AppClaim (ClaimId) ON DELETE CASCADE
);
GO

CREATE INDEX IX_ClaimRole_ClaimId ON dbo.AppClaim_AppRole (ClaimId);
GO

/* ---------------------------------------------------------------------
   PASUL 5: Date de baza minime (rol + cateva domenii de test)
   Userul admin il creezi prin pagina Register (parola va fi hash-uita).
   Rolul cu RoleId = 1 este atribuit automat userilor noi (vezi AuthService).
   --------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.AppRole ON;
INSERT INTO dbo.AppRole (RoleId, RoleName, IsActive) VALUES (1, N'User', 1);
INSERT INTO dbo.AppRole (RoleId, RoleName, IsActive) VALUES (2, N'Admin', 1);
SET IDENTITY_INSERT dbo.AppRole OFF;
GO

INSERT INTO dbo.Domain (DomainName, IsActive) VALUES
    (N'Matematică', 1),
    (N'Limbi străine', 1),
    (N'Programare', 1);
GO

PRINT 'Schema CrossTrainingPlatform a fost recreata cu succes.';
GO
