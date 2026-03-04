using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Reflection;

namespace DotNetWorkQueue.Transport.SqlServer.Decorator
{
    internal static class MockSqlException
    {
        /// <summary>
        /// Creates a SqlException with the specified error number via reflection.
        /// SqlException has no public constructor, so we must use internal APIs.
        /// The SqlClient team keeps these signatures stable for external reflection use.
        /// </summary>
        public static SqlException Create(int errorNumber)
        {
            // Step 1: Create SqlError via internal constructor
            var sqlErrorType = typeof(SqlError);
            var sqlErrorCtor = sqlErrorType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(c =>
                {
                    var p = c.GetParameters();
                    return p.Length == 9
                        && p[0].ParameterType == typeof(int)
                        && p[1].ParameterType == typeof(byte)
                        && p[2].ParameterType == typeof(byte)
                        && p[3].ParameterType == typeof(string)
                        && p[4].ParameterType == typeof(string)
                        && p[5].ParameterType == typeof(string)
                        && p[6].ParameterType == typeof(int)
                        && p[7].ParameterType == typeof(int)
                        && p[8].ParameterType == typeof(Exception);
                });

            // Fallback to 8-parameter overload (without win32ErrorCode)
            if (sqlErrorCtor == null)
            {
                sqlErrorCtor = sqlErrorType
                    .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(c =>
                    {
                        var p = c.GetParameters();
                        return p.Length == 8
                            && p[0].ParameterType == typeof(int)
                            && p[1].ParameterType == typeof(byte)
                            && p[2].ParameterType == typeof(byte)
                            && p[3].ParameterType == typeof(string)
                            && p[4].ParameterType == typeof(string)
                            && p[5].ParameterType == typeof(string)
                            && p[6].ParameterType == typeof(int)
                            && p[7].ParameterType == typeof(Exception);
                    });

                if (sqlErrorCtor == null)
                    throw new InvalidOperationException("Could not find SqlError internal constructor via reflection.");

                var sqlError8 = (SqlError)sqlErrorCtor.Invoke(new object[]
                {
                    errorNumber, (byte)1, (byte)13, "test_server",
                    "Policy chaos testing", "test_procedure", 0, null
                });
                return CreateExceptionFromError(sqlError8);
            }

            var sqlError = (SqlError)sqlErrorCtor.Invoke(new object[]
            {
                errorNumber, (byte)1, (byte)13, "test_server",
                "Policy chaos testing", "test_procedure", 0, 0, null
            });
            return CreateExceptionFromError(sqlError);
        }

        private static SqlException CreateExceptionFromError(SqlError sqlError)
        {
            // Step 2: Create SqlErrorCollection and add the error
            var collectionType = typeof(SqlErrorCollection);
            var collection = (SqlErrorCollection)collectionType
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0]
                .Invoke(null);

            collectionType
                .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(collection, new object[] { sqlError });

            // Step 3: Create SqlException via internal static CreateException
            var createMethod = typeof(SqlException)
                .GetMethod("CreateException",
                    BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    new[] { typeof(SqlErrorCollection), typeof(string) },
                    null);

            if (createMethod == null)
                throw new InvalidOperationException("Could not find SqlException.CreateException method via reflection.");

            return (SqlException)createMethod.Invoke(null, new object[] { collection, "11.0.0" });
        }
    }
}
