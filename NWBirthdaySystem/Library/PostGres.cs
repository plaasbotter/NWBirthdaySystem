using Npgsql;
using NWBirthdaySystem.Models;
using Serilog;
using System.Reflection;

namespace NWBirthdaySystem.Library
{
    internal class PostGres : IDatabaseContext
    {
        private readonly ILogger _logger;
        private readonly object _connectionLock = new object();
        private readonly NpgsqlConnection _con;

        public PostGres(ILogger logger, string connectionString)
        {
            _logger = logger;
            _con = new NpgsqlConnection(connectionString);
            _con.OpenAsync().Wait();
        }

        public bool TestConnection()
        {
            if (_con.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    _con.OpenAsync().Wait();
                }
                catch (Exception err)
                {
                    _logger.Error(err, "[{0}]", "DatabaseContext.TestConnection");
                    Thread.Sleep(112358);
                    return false;
                }
            }
            return true;
        }

        public void CheckAndCreateTables(string tableName, object tableObject, bool forceDrop)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.CheckAndCreateTables", "Reconnecting...");
                }
                if (forceDrop)
                {
                    string deleteQuery = $"DROP TABLE \"{tableName}\"";
                    using (var cmd = new NpgsqlCommand(deleteQuery, _con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                bool exists = false;
                string query = $"SELECT EXISTS(SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tableName}')";
                using (var cmd = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            exists = rdr.GetBoolean(0);
                        }
                    }
                }
                if (exists == false)
                {
                    CreateTableWithObject(tableName, tableObject);
                }
                else
                {
                    _logger.Information($"Table {tableName} exists", "DatabaseContext.CheckAndCreateTables");
                }
            }
        }

        public void CreateTableWithObject(string tableName, object input)
        {
            while (TestConnection() == false)
            {
                _logger.Warning("[{0}] [{1}]", "DatabaseContext.CreateTableWithObject", "Reconnecting...");
            }
            List<string> primaryKeys = new List<string>();
            bool isIdentity = false;
            string query = $"CREATE TABLE \"{tableName}\" (";
            foreach (PropertyInfo prop in input.GetType().GetProperties())
            {
                if (prop.CustomAttributes.Count() > 0)
                {
                    List<CustomAttributeData>? custumAttributes = prop.CustomAttributes.ToList();
                    foreach (CustomAttributeData custumAttribute in custumAttributes)
                    {
                        switch (custumAttribute.AttributeType.Name)
                        {
                            case "PrimaryKeyAttribute":
                                primaryKeys.Add(prop.Name);
                                break;
                            case "PrimaryKeyAutoIncrementAttribute":
                                primaryKeys.Add(prop.Name);
                                isIdentity = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
                var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (type != null)
                {
                    query += $"\"{prop.Name}\"";
                    if (type == typeof(string))
                    {
                        if (prop.Name == "Path")
                        {
                            query += $" text, ";
                        }
                        else
                        {
                            query += $" character varying(100), ";
                        }
                    }
                    if (type == typeof(int))
                    {
                        query += $" integer, ";
                    }
                    if (type == typeof(short))
                    {
                        query += $" smallint, ";
                    }
                    if (type == typeof(long))
                    {
                        query += $" bigint, ";
                    }
                    if (type == typeof(DateTime))
                    {
                        query += $" timestamp without time zone, ";
                    }
                    if (type == typeof(Guid))
                    {
                        query += $" uuid, ";
                    }
                    if (type == typeof(bool))
                    {
                        query += $" boolean, ";
                    }
                    if (isIdentity == true)
                    {
                        query = query.Substring(0, query.Length - 2);
                        query += " NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 ), ";
                        isIdentity = false;
                    }
                }
            }
            if (primaryKeys.Count > 0)
            {
                query += $" PRIMARY KEY (\"{string.Join("\",\"", primaryKeys)}\")";
            }
            query += ");";
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
            {
                cmd.ExecuteNonQuery();
            }
            _logger.Information($"Created Table {tableName}", "DatabaseContext.CreateTableWithObject");
        }

        public long GetLastMessage()
        {
            long returnValue = 0;
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.GetLastMessage", "Reconnecting...");
                }
                string query = "SELECT \"Id\" FROM \"BirthdayEntries\" ORDER BY \"Id\" DESC LIMIT 1";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            returnValue = rdr.GetInt64(0);
                        }
                    }
                }
            }
            return returnValue;
        }

        public List<Birthday> GetBirthdays(DateTime date)
        {
            List<Birthday> returnValue = new List<Birthday>();
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.GetBirthdays", "Reconnecting...");
                }
                short todayShort = (short)(date.Month * 32 + date.Day);
                string query = "SELECT \"Name\", \"ChatId\" FROM \"Birthdays\" WHERE \"BirthDate\" = @BirthDate";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("BirthDate", todayShort));
                    using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while(rdr.Read())
                        {
                            returnValue.Add(new Birthday
                            {
                                Name = rdr.GetString(0),
                                ChatId = rdr.GetString(1)
                            });
                        }
                    }
                }
            }
            return returnValue;
        }

        public void UpsertMessage(BirthdayEntry tempBirthdayEntry)
        {
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.UpsertMessage", "Reconnecting...");
                }
                string query = "INSERT INTO \"BirthdayEntries\" (\"Id\", \"Message\", \"ChatId\") VALUES (@Id, @Message, @ChatId)";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("Id", tempBirthdayEntry.Id));
                    cmd.Parameters.Add(new NpgsqlParameter("Message", tempBirthdayEntry.Message));
                    cmd.Parameters.Add(new NpgsqlParameter("ChatId", tempBirthdayEntry.ChatId));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool UpsertBirthday(Birthday tempBirthday)
        {
            if (CheckIfEntryExists(tempBirthday) == false)
            {
                lock (_connectionLock)
                {
                    while (TestConnection() == false)
                    {
                        _logger.Warning("[{0}] [{1}]", "DatabaseContext.UpsertBirthday", "Reconnecting...");
                    }

                    string query = "INSERT INTO \"Birthdays\" (\"Name\", \"BirthDate\", \"ChatId\") VALUES (@Name, @BirthDate, @ChatId)";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                    {
                        cmd.Parameters.Add(new NpgsqlParameter("Name", tempBirthday.Name));
                        cmd.Parameters.Add(new NpgsqlParameter("BirthDate", tempBirthday.BirthDate));
                        cmd.Parameters.Add(new NpgsqlParameter("ChatId", tempBirthday.ChatId));
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            return false;
        }

        private bool CheckIfEntryExists(Birthday tempBirthday)
        {
            bool returnValue = true;
            lock (_connectionLock)
            {
                while (TestConnection() == false)
                {
                    _logger.Warning("[{0}] [{1}]", "DatabaseContext.UpsertBirthday", "Reconnecting...");
                }
                string query = "SELECT COUNT(*) FROM \"Birthdays\" WHERE \"Name\" = @Name AND \"BirthDate\" = @BirthDate AND \"ChatId\" = @ChatId";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, _con))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("Name", tempBirthday.Name));
                    cmd.Parameters.Add(new NpgsqlParameter("BirthDate", tempBirthday.BirthDate));
                    cmd.Parameters.Add(new NpgsqlParameter("ChatId", tempBirthday.ChatId));
                    long dbCount = (long)cmd.ExecuteScalar();
                    if (dbCount == 0)
                    {
                        returnValue = false;
                    }
                }
            }
            return returnValue;
        }
    }
}
