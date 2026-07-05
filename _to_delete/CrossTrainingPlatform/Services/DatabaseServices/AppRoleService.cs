using CrossTrainingPlatform.Models.UserManagementModels;
using Microsoft.Data.SqlClient;

namespace CrossTrainingPlatform.Services.DatabaseServices
{
    /// <summary>
    /// Acces date pentru roluri (AppRole) si legaturile rol-claim (AppClaim_AppRole).
    /// </summary>
    public class AppRoleService
    {
        private readonly SqlConnectionFactory _factory;

        public AppRoleService(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        private static AppRole Map(SqlDataReader r) => new AppRole
        {
            RoleId = r.GetInt32(r.GetOrdinal("RoleId")),
            RoleName = r.GetString(r.GetOrdinal("RoleName")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
        };

        public async Task<List<AppRole>> GetAllAsync()
        {
            var list = new List<AppRole>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT RoleId, RoleName, IsActive FROM dbo.AppRole;";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public async Task<AppRole?> GetByIdAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT RoleId, RoleName, IsActive FROM dbo.AppRole WHERE RoleId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<int> CreateAsync(string roleName)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO dbo.AppRole (RoleName, IsActive)
                                 OUTPUT INSERTED.RoleId
                                 VALUES (@name, 1);";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", roleName);

            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task UpdateNameAsync(int id, string roleName)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "UPDATE dbo.AppRole SET RoleName = @name WHERE RoleId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", roleName);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "UPDATE dbo.AppRole SET IsActive = 0 WHERE RoleId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Claim-urile asociate unui rol, prin tabela pivot AppClaim_AppRole.</summary>
        public async Task<List<AppClaim>> GetClaimsForRoleAsync(int roleId)
        {
            var list = new List<AppClaim>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"SELECT c.ClaimId, c.ClaimName, c.IsActive
                                 FROM dbo.AppClaim c
                                 INNER JOIN dbo.AppClaim_AppRole p ON p.ClaimId = c.ClaimId
                                 WHERE p.RoleId = @roleId;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@roleId", roleId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AppClaim
                {
                    ClaimId = reader.GetInt32(reader.GetOrdinal("ClaimId")),
                    ClaimName = reader.GetString(reader.GetOrdinal("ClaimName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return list;
        }

        /// <summary>Id-urile claim-urilor deja atasate unui rol.</summary>
        public async Task<List<int>> GetClaimIdsForRoleAsync(int roleId)
        {
            var ids = new List<int>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT ClaimId FROM dbo.AppClaim_AppRole WHERE RoleId = @roleId;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@roleId", roleId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                ids.Add(reader.GetInt32(0));

            return ids;
        }

        public async Task AddClaimAsync(int roleId, int claimId)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            // evitam dublurile pe cheia compusa
            const string sql = @"IF NOT EXISTS (SELECT 1 FROM dbo.AppClaim_AppRole WHERE RoleId = @roleId AND ClaimId = @claimId)
                                 INSERT INTO dbo.AppClaim_AppRole (ClaimId, RoleId) VALUES (@claimId, @roleId);";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@roleId", roleId);
            cmd.Parameters.AddWithValue("@claimId", claimId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RemoveClaimAsync(int roleId, int claimId)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "DELETE FROM dbo.AppClaim_AppRole WHERE RoleId = @roleId AND ClaimId = @claimId;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@roleId", roleId);
            cmd.Parameters.AddWithValue("@claimId", claimId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
