// ============================================================
//  Data/DbContext.cs
//  ─────────────────
//  What is this?
//  This class is the "door" to your MySQL database.
//  Every controller that needs to talk to the DB gets a
//  connection through this class.
//
//  How it works:
//  1. The connection string is read from appsettings.json
//  2. CreateConnection() opens a new MySQL connection
//  3. Dapper uses that connection to run SQL queries
//
//  Why open/close connections per query?
//  MySQL has a limited number of simultaneous connections.
//  Opening only when needed and closing immediately after
//  keeps the app efficient and avoids "too many connections"
//  errors — this pattern is called connection pooling.
// ============================================================

using MySql.Data.MySqlClient;

namespace LibSys.API.Data;

public class LibSysDbContext
{
    // The connection string read from appsettings.json
    private readonly string _connectionString;

    // Constructor — called once at startup by dependency injection
    // IConfiguration is how ASP.NET Core reads appsettings.json
    public LibSysDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("LibSysDb")
            ?? throw new InvalidOperationException(
                "Connection string 'LibSysDb' not found in appsettings.json");
    }

    // Call this every time you want to run a SQL query.
    // Returns a ready-to-use MySQL connection.
    // Controllers use it like:
    //   using var conn = _db.CreateConnection();
    //   var items = conn.Query<Category>("SELECT ...");
    public MySqlConnection CreateConnection()
        => new MySqlConnection(_connectionString);
}
