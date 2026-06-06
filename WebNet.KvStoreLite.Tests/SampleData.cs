namespace WebNet.KvStoreLite.Tests;

internal static class SampleData
{
    public const string UsersCollection = "users";

    public static readonly KeyValuePair<string, string>[] Users =
    [
        new("alice", "admin"),
        new("bob", "reader"),
        new("charlie", "editor")
    ];
}
