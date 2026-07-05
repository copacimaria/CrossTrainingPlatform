using CrossTrainingPlatform.Models.UserManagementModels;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace CrossTrainingPlatform.Services.DatabaseServices
{
    /// <summary>
    /// Acces date pentru utilizatori (AppUser).
    /// DomainList este stocat in baza ca text JSON (ex: "[1]") si convertit la List&lt;int&gt;.
    /// </summary>
    public class AppUserService
    {
        private readonly SqlConnectionFactory _factory;

        public AppUserService(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        private static List<int> ParseDomainList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<int>();
            try
            {
                return JsonConvert.DeserializeObject<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private static AppUser Map(SqlDataReader r) => new AppUser
        {
            UserId = r.GetInt32(r.GetOrdinal("UserId")),
            Name = r.GetString(r.GetOrdinal("Name")),
            FamilyName = r.GetString(r.GetOrdinal("FamilyName")),
            Email = r.GetString(r.GetOrdinal("Email")),
            AccPassword = r.GetString(r.GetOrdinal("AccPassword")),
            CreationDate = r.IsDBNull(r.GetOrdinal("CreationDate")) ? null : r.GetDateTime(r.GetOrdinal("CreationDate")),
            isActive = r.GetBoolean(r.GetOrdinal("isActive")),
            RoleId = r.IsDBNull(r.GetOrdinal("RoleId")) ? null : r.GetInt32(r.GetOrdinal("RoleId")),
            DomainList = ParseDomainList(r.IsDBNull(r.GetOrdinal("DomainList")) ? null : r.GetString(r.GetOrdinal("DomainList")))
        };

        private const string SelectColumns =
            "UserId, Name, FamilyName, Email, AccPassword, CreationDate, isActive, RoleId, DomainList";

        public async Task<AppUser?> GetByIdAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            string sql = $"SELECT {SelectColumns} FROM dbo.AppUser WHERE UserId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            string sql = $"SELECT {SelectColumns} FROM dbo.AppUser WHERE Email = @email;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<int> CreateAsync(AppUser user)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO dbo.AppUser
                                    (Name, FamilyName, Email, AccPassword, CreationDate, isActive, RoleId, DomainList)
                                 OUTPUT INSERTED.UserId
                                 VALUES (@name, @family, @email, @pass, @created, @active, @roleId, @domainList);";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = user.Name;
            cmd.Parameters.Add("@family", SqlDbType.NVarChar).Value = user.FamilyName;
            cmd.Parameters.Add("@email", SqlDbType.NVarChar).Value = user.Email;
            cmd.Parameters.Add("@pass", SqlDbType.NVarChar).Value = user.AccPassword;
            cmd.Parameters.AddWithValue("@created", (object?)user.CreationDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@active", user.isActive);
            cmd.Parameters.AddWithValue("@roleId", (object?)user.RoleId ?? DBNull.Value);
            cmd.Parameters.Add("@domainList", SqlDbType.NVarChar).Value = JsonConvert.SerializeObject(user.DomainList ?? new List<int>());

            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task UpdatePasswordAsync(int userId, string newPassword)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "UPDATE dbo.AppUser SET AccPassword = @pass WHERE UserId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@pass", SqlDbType.NVarChar).Value = newPassword;
            cmd.Parameters.AddWithValue("@id", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateDomainListAsync(int userId, List<int> domainIds)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "UPDATE dbo.AppUser SET DomainList = @domainList WHERE UserId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@domainList", JsonConvert.SerializeObject(domainIds ?? new List<int>()));
            cmd.Parameters.AddWithValue("@id", userId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
