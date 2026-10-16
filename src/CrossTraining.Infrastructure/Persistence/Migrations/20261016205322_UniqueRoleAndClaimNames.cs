using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossTraining.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Nume unice pentru roluri si drepturi.
    ///
    /// Autorizarea gaseste drepturile dupa nume, iar rolurile de sistem sunt recunoscute
    /// tot dupa nume, deci un duplicat ar fi o problema de securitate, nu doar de afisare.
    ///
    /// Pe o baza care are deja duplicate, crearea indexului ar pica cu un mesaj generic de
    /// la SQL Server. Verificarea de dinainte spune ce e de facut. Duplicatele nu se
    /// rezolva automat: alegerea randului care ramane (si a rolurilor legate de el) e o
    /// decizie, nu o regula.
    /// </summary>
    public partial class UniqueRoleAndClaimNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM dbo.AppRole GROUP BY RoleName HAVING COUNT(*) > 1) " +
                "BEGIN " +
                "  THROW 50001, N'Exista roluri cu acelasi nume. Redenumiti duplicatele, apoi rulati din nou migrarea: " +
                "SELECT RoleName, COUNT(*) FROM dbo.AppRole GROUP BY RoleName HAVING COUNT(*) > 1;', 1; " +
                "END;");

            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM dbo.AppClaim GROUP BY ClaimName HAVING COUNT(*) > 1) " +
                "BEGIN " +
                "  THROW 50002, N'Exista drepturi cu acelasi nume. Redenumiti duplicatele, apoi rulati din nou migrarea: " +
                "SELECT ClaimName, COUNT(*) FROM dbo.AppClaim GROUP BY ClaimName HAVING COUNT(*) > 1;', 1; " +
                "END;");

            migrationBuilder.CreateIndex(
                name: "UX_AppRole_RoleName",
                table: "AppRole",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AppClaim_ClaimName",
                table: "AppClaim",
                column: "ClaimName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AppRole_RoleName",
                table: "AppRole");

            migrationBuilder.DropIndex(
                name: "UX_AppClaim_ClaimName",
                table: "AppClaim");
        }
    }
}
