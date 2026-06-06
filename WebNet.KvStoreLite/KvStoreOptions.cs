namespace WebNet.KvStoreLite
{
    public class KvStoreOptions
    {
        public string StoreName { get; set; } = "default_store";
        public string BaseDirectory { get; set; } = Environment.CurrentDirectory;
    }
}
