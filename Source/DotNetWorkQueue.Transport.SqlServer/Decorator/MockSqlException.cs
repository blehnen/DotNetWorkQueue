using Microsoft.Data.SqlClient;

namespace DotNetWorkQueue.Transport.SqlServer.Decorator
{
    internal static class MockSqlException
    {
        public static SqlException Create()
        {
            SqlException exception = null;
            try
            {
                // Use a guaranteed-to-fail connection string
                // The Connection Timeout is set low to fail quickly
                SqlConnection conn = new SqlConnection(@"Data Source=.;Database=GUARANTEED_TO_FAIL;Connection Timeout=1");
                conn.Open();
            }
            catch (SqlException ex)
            {
                exception = ex;
            }

            return exception;
        }
    }
}
