# WebNet.KvStoreLite

A small .NET class library that stores string key/value pairs in a SQLite database file.

## Requirements

- .NET 10 SDK

## Build

From the repository root:

```powershell
dotnet build .\WebNet.KvStoreLite.slnx -nologo
```

Run the full test suite with:

```powershell
dotnet test .\WebNet.KvStoreLite.slnx -nologo
```

Run a single test with:

```powershell
dotnet test .\WebNet.KvStoreLite.Tests\WebNet.KvStoreLite.Tests.csproj -nologo --filter "FullyQualifiedName~GetValue_ReturnsValue_ForExistingSampleKey"
```

## Package layout

The solution contains two projects:

- `WebNet.KvStoreLite` - the SQLite-backed key/value store implementation
- `WebNet.KvStoreLite.Tests` - xUnit tests covering the current public API behavior with reusable sample data

## Core API

- `KvStoreOptions` configures the database file name and base directory
- `KvStore` provides collection creation, inserts, reads, and deletes

Each collection is stored as a SQLite table with two text columns:

- `k` - key
- `v` - value

The backing database file is created at:

`<BaseDirectory>\<StoreName>.sqlitedb`

## Usage

```csharp
using WebNet.KvStoreLite;

KvStoreOptions options = new()
{
    StoreName = "app-data",
    BaseDirectory = AppContext.BaseDirectory
};

using KvStore store = new(options);

store.CreateCollection("users");

store.Add(
    "users",
    new("alice", "admin"),
    new("bob", "reader"));

string aliceRole = store.GetValue("users", "alice");
int removed = store.Remove("users", "bob");
var snapshot = store.GetCollection("users");
```

## Behavior to know before changing the code

- `Add` creates the collection automatically if it does not already exist.
- `GetCollection`, `GetValue`, and `Remove` rely on the internal command object having already been initialized. In practice, call `CreateCollection` or `Add` before using those methods on a fresh `KvStore` instance.
- Missing values return `string.Empty`.
- `Add` and `Remove` return the number of affected rows, or `0` when the operation does not run.
- Keys and values are stored as strings only.
- Collection names are inserted directly into SQL statements and are therefore expected to come from trusted inputs.

## Data model caveats

- Tables do not enforce unique keys.
- `GetValue` returns the first matching row for a key.
- `GetCollection` materializes rows into a `ReadOnlyDictionary<string, string>`, so duplicate keys in a collection can cause dictionary population to fail.

## Documentation

- API comments live in `WebNet.KvStoreLite\KvStore.cs` and `WebNet.KvStoreLite\KvStoreOptions.cs`
- Architecture notes live in `docs\architecture.md`

## Tests

The test project uses a small shared sample dataset:

- collection: `users`
- keys: `alice`, `bob`, `charlie`
- values: `admin`, `reader`, `editor`

Each test creates its own temporary SQLite database directory so runs stay isolated from one another.
