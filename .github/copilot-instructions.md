# Copilot Instructions

## Build and verification

- Build from the repository root with `dotnet build .\WebNet.KvStoreLite.slnx -nologo`.
- `dotnet test .\WebNet.KvStoreLite.slnx -nologo` currently restores and builds the solution, but there is no test project checked in yet.

## High-level architecture

- This repository is a single .NET 10 class library project, `WebNet.KvStoreLite`, packaged as a lightweight SQLite-backed key-value store.
- The public surface area is centered on two types: `KvStoreOptions`, which supplies `StoreName` and `BaseDirectory`, and `KvStore`, which turns those options into a database path named `<StoreName>.sqlitedb`.
- Each logical collection is mapped directly to a SQLite table. `CreateCollection` creates tables on demand with a fixed schema of `k TEXT NOT NULL, v TEXT NOT NULL`.
- `KvStore` keeps one `SqliteConnection` for the lifetime of the instance and reuses a mutable `SqliteCommand` field across operations instead of creating a fresh command per call.
- The root `README.md` and `LICENSE.txt` are packed into the NuGet package through the project file, so packaging-related changes often span both the root files and `WebNet.KvStoreLite.csproj`.

## Key conventions

- The store API is string-only. Keys, values, and collection snapshots all use `string`, and `GetCollection` returns a `ReadOnlyDictionary<string, string>`.
- Missing data and invalid inputs use sentinel return values rather than exceptions in the current API shape: reads return `string.Empty`, and write/delete methods return `0` when nothing is written or removed.
- `GetValue` and `Remove` depend on the shared `cmd` field already existing, so changes to initialization order or command lifecycle can alter behavior in subtle ways.
- SQL parameters are used for keys and values, but collection names are interpolated directly into SQL statements. Treat collection names as trusted inputs unless you are deliberately redesigning that contract.
- Nullable reference types are disabled for the project, and warning level is explicitly set to `0` in both Debug and Release configurations.
