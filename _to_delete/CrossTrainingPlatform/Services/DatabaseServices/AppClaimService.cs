using CrossTrainingPlatform.Models.UserManagementModels;
using Microsoft.Data.SqlClient;

namespace CrossTrainingPlatform.Services.DatabaseServices
{
    /// <summary>Acces date pentru permisiuni (AppClaim).</summary>
    public class AppClaimService
    {
        private readonly SqlConnectionFactory _factory;

        public AppClaimService(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        private static AppClaim Map(SqlDataReader r) => new AppClaim
        {
            ClaimId = r.GetInt32(r.GetOrdinal("ClaimId")),
            ClaimName = r.GetString(r.GetOrdinal("ClaimName")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
        };

        public async Task<List<AppClaim>> GetAllAsync()
        {
            var list = new List<AppClaim>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT ClaimId, ClaimName, IsActive FROM dbo.AppClaim;";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public async Task<AppClaim?> GetByIdAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT ClaimId, ClaimName, IsActive FROM dbo.AppClaim WHERE ClaimId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<int> CreateAsync(string claimName)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO dbo.AppClaim (ClaimName, IsActive)
                                 OUTPUT INSERTED.ClaimId
                                 VALUES (@name, 1);";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", claimName);

            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task UpdateNameAsync(int id, string claimName)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "UPDATE dbo.AppClaim SET ClaimName = @name WHERE ClaimId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", claimName);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            // FK-ul pe pivot are ON DELETE CASCADE, deci se curata si legaturile
            const string sql = "DELETE FROM dbo.AppClaim WHERE ClaimId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
