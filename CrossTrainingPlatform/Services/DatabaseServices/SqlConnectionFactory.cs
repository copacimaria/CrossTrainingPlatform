using Microsoft.Data.SqlClient;

namespace CrossTrainingPlatform.Services.DatabaseServices
{
    /// <summary>
    /// Centralizeaza crearea conexiunilor catre SQL Server.
    /// Connection string-ul vine din appsettings.json (ConnectionStrings:DefaultConnection).
    /// </summary>
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Lipseste connection string-ul 'DefaultConnection'.");
        }

        public SqlConnection Create() => new SqlConnection(_connectionString);
    }
}
