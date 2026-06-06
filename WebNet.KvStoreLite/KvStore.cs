using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;

namespace WebNet.KvStoreLite
{
    /// <summary>
    /// Provides a lightweight SQLite-backed key/value store where each collection is stored as its own table.
    /// </summary>
    public class KvStore : IDisposable
    {
        private readonly KvStoreOptions options;
        private readonly SqliteConnection connection;
        private SqliteCommand cmd;

        private const string fileExt = "sqlitedb";

        /// <summary>
        /// Releases the command and SQLite connection owned by this store.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (this.cmd is not null)
            {
                this.cmd.Dispose();
            }
            this.connection.Dispose();
        }

        /// <summary>
        /// Initializes a new store instance for the provided options.
        /// </summary>
        /// <param name="storeOptions">The file name and base directory for the SQLite database.</param>
        public KvStore(KvStoreOptions storeOptions) :
            base()
        {
            this.options = storeOptions;
            string fn = $"{this.options.StoreName}.{fileExt}";
            this.connection = new($"DataSource={Path.Combine(this.options.BaseDirectory, fn)}");
        }

        /// <summary>
        /// Initializes a new store instance using the default store options.
        /// </summary>
        public KvStore() : this(new()) { }

        /// <summary>
        /// Creates a collection table when it does not already exist and initializes the shared command object.
        /// </summary>
        /// <param name="collectionName">The SQLite table name to create.</param>
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

        /// <summary>
        /// Returns all rows in a collection as a read-only dictionary.
        /// </summary>
        /// <param name="collectionName">The SQLite table name to read.</param>
        /// <returns>
        /// A read-only snapshot of the collection contents. Call <see cref="CreateCollection"/> or <see cref="Add"/>
        /// first on a fresh store instance so the internal command has been initialized.
        /// </returns>
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

        /// <summary>
        /// Gets the first value stored for a key in the specified collection.
        /// </summary>
        /// <param name="collectionName">The SQLite table name to query.</param>
        /// <param name="key">The key to look up.</param>
        /// <returns>
        /// The first matching value, or <see cref="string.Empty"/> when the lookup does not run or finds no rows.
        /// Call <see cref="CreateCollection"/> or <see cref="Add"/> first on a fresh store instance so the internal
        /// command has been initialized.
        /// </returns>
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

        /// <summary>
        /// Removes all rows that match the provided key from a collection.
        /// </summary>
        /// <param name="collectionName">The SQLite table name to modify.</param>
        /// <param name="key">The key to delete.</param>
        /// <returns>
        /// The number of deleted rows, or <c>0</c> when the delete does not run.
        /// Call <see cref="CreateCollection"/> or <see cref="Add"/> first on a fresh store instance so the internal
        /// command has been initialized.
        /// </returns>
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

        /// <summary>
        /// Inserts one or more key/value pairs into a collection.
        /// </summary>
        /// <param name="collectionName">The SQLite table name to modify.</param>
        /// <param name="keyValues">The key/value pairs to insert.</param>
        /// <returns>The number of inserted rows, or <c>0</c> when nothing is written.</returns>
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
