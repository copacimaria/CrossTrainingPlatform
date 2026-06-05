using CrossTrainingPlatform.Models;
using Microsoft.Data.SqlClient;

namespace CrossTrainingPlatform.Services.DatabaseServices
{
    /// <summary>Acces date pentru domenii (Domain).</summary>
    public class DomainService
    {
        private readonly SqlConnectionFactory _factory;

        public DomainService(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        private static Domain Map(SqlDataReader r) => new Domain
        {
            DomainId = r.GetInt32(r.GetOrdinal("DomainId")),
            DomainName = r.GetString(r.GetOrdinal("DomainName")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            GameId = r.IsDBNull(r.GetOrdinal("GameId")) ? null : r.GetInt32(r.GetOrdinal("GameId"))
        };

        public async Task<List<Domain>> GetAllAsync()
        {
            var list = new List<Domain>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT DomainId, DomainName, IsActive, GameId FROM dbo.Domain;";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public async Task<List<Domain>> GetActiveAsync()
        {
            var list = new List<Domain>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT DomainId, DomainName, IsActive, GameId FROM dbo.Domain WHERE IsActive = 1;";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public async Task<Domain?> GetByIdAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "SELECT DomainId, DomainName, IsActive, GameId FROM dbo.Domain WHERE DomainId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<List<Domain>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids?.ToList() ?? new List<int>();
            if (idList.Count == 0)
                return new List<Domain>();

            var list = new List<Domain>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            // construim parametri @id0, @id1, ... ca sa evitam SQL injection
            var paramNames = idList.Select((_, i) => "@id" + i).ToArray();
            string sql = $"SELECT DomainId, DomainName, IsActive, GameId FROM dbo.Domain WHERE DomainId IN ({string.Join(",", paramNames)});";

            await using var cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < idList.Count; i++)
                cmd.Parameters.AddWithValue(paramNames[i], idList[i]);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public async Task<int> CreateAsync(Domain domain)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO dbo.Domain (DomainName, IsActive, GameId)
                                 OUTPUT INSERTED.DomainId
                                 VALUES (@name, @active, @gameId);";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", domain.DomainName);
            cmd.Parameters.AddWithValue("@active", domain.IsActive);
            cmd.Parameters.AddWithValue("@gameId", (object?)domain.GameId ?? DBNull.Value);

            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task UpdateAsync(int id, Domain domain)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"UPDATE dbo.Domain
                                 SET DomainName = @name, IsActive = @active, GameId = @gameId
                                 WHERE DomainId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", domain.DomainName);
            cmd.Parameters.AddWithValue("@active", domain.IsActive);
            cmd.Parameters.AddWithValue("@gameId", (object?)domain.GameId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "DELETE FROM dbo.Domain WHERE DomainId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
