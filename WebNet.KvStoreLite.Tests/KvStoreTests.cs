using Microsoft.Data.Sqlite;

namespace WebNet.KvStoreLite.Tests;

public sealed class KvStoreTests
{
    [Fact]
    public void Add_ReturnsInsertedRowCount_ForSampleData()
    {
        using TestStoreContext context = new();

        int inserted = context.Store.Add(SampleData.UsersCollection, SampleData.Users);

        Assert.Equal(SampleData.Users.Length, inserted);
    }

    [Fact]
    public void GetValue_ReturnsValue_ForExistingSampleKey()
    {
        using TestStoreContext context = TestStoreContext.CreatePopulated();

        string value = context.Store.GetValue(SampleData.UsersCollection, "alice");

        Assert.Equal("admin", value);
    }

    [Fact]
    public void GetValue_ReturnsEmptyString_ForMissingKey()
    {
        using TestStoreContext context = TestStoreContext.CreatePopulated();

        string value = context.Store.GetValue(SampleData.UsersCollection, "nobody");

        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void GetCollection_ReturnsAllSampleRows()
    {
        using TestStoreContext context = TestStoreContext.CreatePopulated();

        var collection = context.Store.GetCollection(SampleData.UsersCollection);

        Assert.Equal(SampleData.Users.Length, collection.Count);

        foreach ((string key, string expectedValue) in SampleData.Users)
        {
            Assert.True(collection.ContainsKey(key));
            Assert.Equal(expectedValue, collection[key]);
        }
    }

    [Fact]
    public void Remove_DeletesExistingKey_AndReturnsAffectedRowCount()
    {
        using TestStoreContext context = TestStoreContext.CreatePopulated();

        int removed = context.Store.Remove(SampleData.UsersCollection, "bob");
        string value = context.Store.GetValue(SampleData.UsersCollection, "bob");
        var collection = context.Store.GetCollection(SampleData.UsersCollection);

        Assert.Equal(1, removed);
        Assert.Equal(string.Empty, value);
        Assert.DoesNotContain("bob", collection.Keys);
    }

    private sealed class TestStoreContext : IDisposable
    {
        private readonly string baseDirectory;

        public TestStoreContext()
        {
            this.baseDirectory = Path.Combine(Path.GetTempPath(), "WebNet.KvStoreLite.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.baseDirectory);

            this.Store = new KvStore(new KvStoreOptions
            {
                StoreName = "sample-store",
                BaseDirectory = this.baseDirectory
            });
        }

        public KvStore Store { get; }

        public static TestStoreContext CreatePopulated()
        {
            TestStoreContext context = new();
            context.Store.Add(SampleData.UsersCollection, SampleData.Users);
            return context;
        }

        public void Dispose()
        {
            this.Store.Dispose();
            SqliteConnection.ClearAllPools();
        }
    }
}
