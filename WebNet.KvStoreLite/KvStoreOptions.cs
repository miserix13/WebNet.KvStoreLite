namespace WebNet.KvStoreLite
{
    /// <summary>
    /// Configures the SQLite file name and location used by <see cref="KvStore"/>.
    /// </summary>
    public class KvStoreOptions
    {
        /// <summary>
        /// Gets or sets the base file name for the SQLite database.
        /// The final file path is built as &lt;see cref="StoreName"/&gt;.sqlitedb under <see cref="BaseDirectory"/>.
        /// </summary>
        public string StoreName { get; set; } = "default_store";

        /// <summary>
        /// Gets or sets the directory that will contain the SQLite database file.
        /// </summary>
        public string BaseDirectory { get; set; } = Environment.CurrentDirectory;
    }
}
