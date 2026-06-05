using CrossTrainingPlatform.Models;
using Microsoft.Data.SqlClient;

namespace CrossTrainingPlatform.Services.DatabaseServices
{
    /// <summary>Acces date pentru jocuri (Game).</summary>
    public class GameService
    {
        private readonly SqlConnectionFactory _factory;

        public GameService(SqlConnectionFactory factory)
        {
            _factory = factory;
        }

        private static Game Map(SqlDataReader r) => new Game
        {
            GameId = r.GetInt32(r.GetOrdinal("GameId")),
            GameName = r.GetString(r.GetOrdinal("GameName")),
            IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
            DomainId = r.IsDBNull(r.GetOrdinal("DomainId")) ? null : r.GetInt32(r.GetOrdinal("DomainId")),
            CreatedAt = r.IsDBNull(r.GetOrdinal("CreatedAt")) ? null : r.GetDateTime(r.GetOrdinal("CreatedAt")),
            Description = r.IsDBNull(r.GetOrdinal("Description")) ? string.Empty : r.GetString(r.GetOrdinal("Description")),
            DownloadLink = r.IsDBNull(r.GetOrdinal("DownloadLink")) ? null : r.GetString(r.GetOrdinal("DownloadLink"))
        };

        private const string SelectColumns =
            "GameId, GameName, IsActive, DomainId, CreatedAt, Description, DownloadLink";

        public async Task<List<Game>> GetAllAsync()
        {
            var list = new List<Game>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            string sql = $"SELECT {SelectColumns} FROM dbo.Game;";
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public async Task<List<Game>> GetByDomainAsync(int domainId)
        {
            var list = new List<Game>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            string sql = $"SELECT {SelectColumns} FROM dbo.Game WHERE DomainId = @domainId;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@domainId", domainId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(Map(reader));

            return list;
        }

        public async Task<Game?> GetByIdAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            string sql = $"SELECT {SelectColumns} FROM dbo.Game WHERE GameId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<int> CreateAsync(Game game)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO dbo.Game (GameName, IsActive, DomainId, CreatedAt, Description, DownloadLink)
                                 OUTPUT INSERTED.GameId
                                 VALUES (@name, @active, @domainId, @createdAt, @description, @downloadLink);";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", game.GameName);
            cmd.Parameters.AddWithValue("@active", game.IsActive);
            cmd.Parameters.AddWithValue("@domainId", (object?)game.DomainId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@createdAt", (object?)game.CreatedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description", (object?)game.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@downloadLink", (object?)game.DownloadLink ?? DBNull.Value);

            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task UpdateAsync(int id, Game game)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = @"UPDATE dbo.Game
                                 SET GameName = @name,
                                     Description = @description,
                                     DownloadLink = @downloadLink,
                                     DomainId = @domainId
                                 WHERE GameId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", game.GameName);
            cmd.Parameters.AddWithValue("@description", (object?)game.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@downloadLink", (object?)game.DownloadLink ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@domainId", (object?)game.DomainId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteAsync(int id)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            const string sql = "UPDATE dbo.Game SET IsActive = 0 WHERE GameId = @id;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
