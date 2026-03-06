using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;


namespace Patient_Access_Experian_Project_API.Tests.TestUtilities
{
    public static class DatabaseFactory
    {
        public static (SqliteConnection Conn, PatientAccessDbContext Db) CreateSqliteInMemoryDb()
        {
            // SQLite in-memory database persists only while the connection stays open
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();

            var options = new DbContextOptionsBuilder<PatientAccessDbContext>()
                .UseSqlite(conn)
                .EnableSensitiveDataLogging()
                .Options;

            var db = new PatientAccessDbContext(options);

            // Create schema from the model (no migrations)
            db.Database.EnsureCreated();

            return (conn, db);
        }
    }
}
