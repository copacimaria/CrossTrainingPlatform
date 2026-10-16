using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossTraining.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Insereaza drepturile pe care le cere autorizarea pe permisiuni si le acorda
    /// rolului Admin.
    ///
    /// Fara aceasta migrare, trecerea de la [Authorize(Roles = "Admin")] la
    /// [HasPermission(...)] ar lasa ecranele de administrare inaccesibile: politicile ar
    /// cauta drepturi care nu exista in baza de date.
    ///
    /// Migrarea nu schimba modelul, doar datele - de aceea a fost generata goala si
    /// completata manual cu SQL idempotent, care poate fi rulat de mai multe ori fara
    /// efecte secundare.
    /// </summary>
    public partial class SeedPermissions : Migration
    {
        private const string PermissionList =
            "N'claims.view', N'claims.manage', " +
            "N'roles.view', N'roles.manage', " +
            "N'domains.view', N'domains.manage', " +
            "N'games.manage'";

        private static readonly string[] Permissions =
        [
            "claims.view",
            "claims.manage",
            "roles.view",
            "roles.manage",
            "domains.view",
            "domains.manage",
            "games.manage"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rolurile de baza. Pe o baza existenta sunt deja acolo (scriptul 002 le-a
            // inserat); pe una creata doar din migrari nu ar exista deloc, iar acordarea
            // de mai jos nu ar avea catre cine sa se faca. RoleId = 1 este atribuit
            // automat conturilor noi, deci id-urile sunt fixate explicit.
            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM dbo.AppRole WHERE RoleName = N'User') " +
                "BEGIN " +
                "  SET IDENTITY_INSERT dbo.AppRole ON; " +
                "  INSERT INTO dbo.AppRole (RoleId, RoleName, IsActive) VALUES (1, N'User', 1); " +
                "  SET IDENTITY_INSERT dbo.AppRole OFF; " +
                "END;");

            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM dbo.AppRole WHERE RoleName = N'Admin') " +
                "BEGIN " +
                "  SET IDENTITY_INSERT dbo.AppRole ON; " +
                "  INSERT INTO dbo.AppRole (RoleId, RoleName, IsActive) VALUES (2, N'Admin', 1); " +
                "  SET IDENTITY_INSERT dbo.AppRole OFF; " +
                "END;");

            foreach (var permission in Permissions)
            {
                migrationBuilder.Sql(
                    $"IF NOT EXISTS (SELECT 1 FROM dbo.AppClaim WHERE ClaimName = N'{permission}') " +
                    $"INSERT INTO dbo.AppClaim (ClaimName, IsActive) VALUES (N'{permission}', 1);");
            }

            // Acordarea catre rolul Admin, daca exista. Pe o baza fara rol Admin nu are ce
            // acorda, iar migrarea trece fara sa faca nimic.
            migrationBuilder.Sql(
                "INSERT INTO dbo.AppClaim_AppRole (RoleId, ClaimId) " +
                "SELECT r.RoleId, c.ClaimId " +
                "FROM dbo.AppRole r " +
                "CROSS JOIN dbo.AppClaim c " +
                "WHERE r.RoleName = N'Admin' " +
                $"  AND c.ClaimName IN ({PermissionList}) " +
                "  AND NOT EXISTS (SELECT 1 FROM dbo.AppClaim_AppRole x " +
                "                  WHERE x.RoleId = r.RoleId AND x.ClaimId = c.ClaimId);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Se sterg intai legaturile, apoi drepturile: altfel cheia straina blocheaza.
            migrationBuilder.Sql(
                "DELETE x FROM dbo.AppClaim_AppRole x " +
                "JOIN dbo.AppClaim c ON c.ClaimId = x.ClaimId " +
                $"WHERE c.ClaimName IN ({PermissionList});");

            migrationBuilder.Sql($"DELETE FROM dbo.AppClaim WHERE ClaimName IN ({PermissionList});");
        }
    }
}
