using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;

namespace WebNet.KvStoreLite
{
    public class KvStore : IDisposable
    {
        private readonly KvStoreOptions options;
        private readonly SqliteConnection connection;
        private SqliteCommand cmd;

        private const string fileExt = "sqlitedb";

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (this.cmd is not null)
            {
                this.cmd.Dispose();
            }
            this.connection.Dispose();
        }

        public KvStore(KvStoreOptions storeOptions) :
            base()
        {
            this.options = storeOptions;
            string fn = $"{this.options.StoreName}.{fileExt}";
            this.connection = new($"DataSource={Path.Combine(this.options.BaseDirectory, fn)}");
        }

        public KvStore() : this(new()) { }

        public void CreateCollection(string collectionName)
        {
            if (!string.IsNullOrEmpty(collectionName))
            {
                this.cmd = this.connection.CreateCommand();
                this.cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {collectionName} (k TEXT NOT NULL, v TEXT NOT NULL)";

                this.connection.Open();
                this.cmd.ExecuteNonQuery();
                this.connection.Close();
            }
        }

        public ReadOnlyDictionary<string, string> GetCollection(string collectionName)
        {
            Dictionary<string, string> pairs = [];

            if (this.cmd is not null && !string.IsNullOrEmpty(collectionName))
            {
                this.CreateCollection(collectionName);
                this.cmd.CommandText = $"SELECT k, v FROM {collectionName}";

                this.connection.Open();

                using SqliteDataReader reader = this.cmd.ExecuteReader();

                while (reader.Read())
                {
                    pairs.Add(reader.GetString(0), reader.GetString(1));
                }

                reader.Close();
                this.connection.Close();
            }

            return pairs.AsReadOnly();
        }

        public string GetValue(string collectionName, string key)
        {
            if (this.cmd is not null && !string.IsNullOrEmpty(collectionName) && !string.IsNullOrEmpty(key))
            {
                this.cmd.Parameters.Clear();
                this.cmd.CommandText = $"SELECT v FROM {collectionName} WHERE k = $k";
                this.cmd.Parameters.AddWithValue("$k", key);

                this.connection.Open();

                List<string> values = [];
                using SqliteDataReader reader = this.cmd.ExecuteReader();

                while (reader.Read())
                {
                    values.Add(reader.GetString(0));
                }

                reader.Close();
                this.cmd.Parameters.Clear();
                this.connection.Close();

                if (values.Count > 0)
                {
                    return values[0];
                }

                return string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        public int Remove(string collectionName, string key)
        {
            if (this.cmd is not null && !string.IsNullOrEmpty(collectionName) && !string.IsNullOrEmpty(key))
            {
                this.cmd.Parameters.Clear();
                this.cmd.CommandText = $"DELETE FROM {collectionName} WHERE k = $k";
                this.cmd.Parameters.AddWithValue("$k", key);

                this.connection.Open();
                int r = this.cmd.ExecuteNonQuery();
                this.cmd.Parameters.Clear();
                this.connection.Close();

                return r;
            }

            return 0;
        }

        public int Add(string collectionName, params KeyValuePair<string, string>[] keyValues)
        {
            if (!string.IsNullOrEmpty(collectionName) && keyValues is not null && keyValues.Length > 0)
            {
                this.CreateCollection(collectionName);
                this.cmd.CommandText = $"INSERT INTO {collectionName} VALUES ($k, $v)";

                this.connection.Open();

                int j = 0;
                for (int i = 0; i < keyValues.Length; i++)
                {
                    this.cmd.Parameters.AddWithValue("$k", keyValues[i].Key);
                    this.cmd.Parameters.AddWithValue("$v", keyValues[i].Value);
                    j += this.cmd.ExecuteNonQuery();
                    this.cmd.Parameters.Clear();
                }

                this.connection.Close();

                return j;
            }

            return 0;
        }
    }
}
