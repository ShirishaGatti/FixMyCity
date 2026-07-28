// =============================================
// FILE: ItemListTask1\Infrastructure\DbLogger.cs
// PROJECT: FoodOrderingSystem (MVC project)
//
// Logs business/data errors to the ErrorLogs table in SQL Server.
// Use for: BusinessException — track patterns, query from DB
// =============================================
using Microsoft.Practices.EnterpriseLibrary.Data;

using System;
using System.Data.Common;
using System.Data.Entity;

namespace FixMyCity.Infrastructure
{
    public static class DbLogger
    {
        private static readonly Microsoft.Practices.EnterpriseLibrary.Data.Database _db =
            DatabaseFactory.CreateDatabase();

        public static void Log(
            Exception ex,
            string source,
            string userId = null)
        {
            try
            {
                DbCommand cmd =
                    _db.GetStoredProcCommand("InsertErrorLog");

                _db.AddInParameter(
                    cmd,
                    "@LogLevel",
                    System.Data.DbType.String,
                    "ERROR");

                _db.AddInParameter(
                    cmd,
                    "@Message",
                    System.Data.DbType.String,
                    ex.Message ?? "");

                _db.AddInParameter(
                    cmd,
                    "@StackTrace",
                    System.Data.DbType.String,
                    ex.StackTrace ?? "");

                _db.AddInParameter(
                    cmd,
                    "@Source",
                    System.Data.DbType.String,
                    source ?? "");

                _db.AddInParameter(
                    cmd,
                    "@UserId",
                    System.Data.DbType.Int32,
                    1);

                _db.ExecuteNonQuery(cmd);
            }
            catch (Exception)
            {
                FileLogger.Log(
                    ex,
                    source + " [DB logging failed]");
            }
        }
    }
}
/*
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace ItemListTask1.Infrastructure
{
    public static class DbLogger
    {
        public static void Log(Exception ex, string source, string userId = null)
        {
            try
            {
                string connStr = ConfigurationManager
                    .ConnectionStrings["constr"].ConnectionString;

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("InsertErrorLog", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@LogLevel", ex.Message ?? "");
                    cmd.Parameters.AddWithValue("@Message", ex.Message ?? "");
                    cmd.Parameters.AddWithValue("@StackTrace", ex.StackTrace ?? "");
                    cmd.Parameters.AddWithValue("@Source", source ?? "");
                 //   cmd.Parameters.AddWithValue("@InnerMessage", ex.Message ?? ""); 
                 //   cmd.Parameters.AddWithValue("@ExtraInfo", ex.Message ?? "");
             //       cmd.Parameters.AddWithValue("@ServerName", "PC132");
                    cmd.Parameters.AddWithValue("@UserId", 1 );
                    //cmd.Parameters.AddWithValue("@LoggedAt", DateTime.Now);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch(Exception EX)
            {
                // If DB log fails, fall back to file — never lose the error
                FileLogger.Log(ex, source + " [DB log failed, fell back to file]");
            }
        }
    }
}*/





// =============================================
// FILE: ItemListTask1\Infrastructure\FileLogger.cs
// PROJECT: FoodOrderingSystem (MVC project)
//
// Logs infra/system errors to a text file under App_Data/Logs/
// Use for: DataAccessException, SqlException — infra team reads these
// =============================================
