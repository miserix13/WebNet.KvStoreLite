# Architecture

## Overview

`WebNet.KvStoreLite` is a single-project .NET class library that wraps `Microsoft.Data.Sqlite` behind a very small key/value API.

The library stores all data in one SQLite database file, and models each logical collection as its own SQLite table.

## Main types

### `KvStoreOptions`

`KvStoreOptions` controls where the database lives:

- `StoreName` becomes the database file name prefix
- `BaseDirectory` is the directory that holds the file

At runtime the final path is:

`<BaseDirectory>\<StoreName>.sqlitedb`

### `KvStore`

`KvStore` owns the database connection and all CRUD-style operations.

Key implementation details:

- A single `SqliteConnection` is created in the constructor and reused for the lifetime of the instance
- A mutable `SqliteCommand` field is also reused across operations
- `Dispose` disposes both the command and connection

## Collection model

Collections are plain SQLite tables created on demand with this schema:

```sql
CREATE TABLE IF NOT EXISTS <collectionName> (
    k TEXT NOT NULL,
    v TEXT NOT NULL
)
```

That means:

- keys and values are both stored as text
- there is no uniqueness constraint on keys
- there is no extra metadata per row

## Operation flow

### Create

`CreateCollection` initializes the shared command object and ensures the target table exists.

### Insert

`Add` calls `CreateCollection`, then inserts each provided pair with parameterized values for `k` and `v`.

### Read single value

`GetValue` runs:

```sql
SELECT v FROM <collectionName> WHERE k = $k
```

It collects all matching rows and returns the first value found.

### Read whole collection

`GetCollection` runs:

```sql
SELECT k, v FROM <collectionName>
```

It materializes the result into a `Dictionary<string, string>` and returns `AsReadOnly()`.

### Delete

`Remove` runs:

```sql
DELETE FROM <collectionName> WHERE k = $k
```

and returns the affected row count.

## Behavior that matters when modifying the code

- `GetCollection`, `GetValue`, and `Remove` all depend on the shared command object already being initialized
- `Add` is the only mutating operation that always initializes that command state internally
- missing or invalid reads return `string.Empty`
- failed or skipped write/delete operations return `0`
- collection names are interpolated directly into SQL statements, while keys and values are parameterized

## Important edge cases

- Because duplicate keys are allowed by the table schema, `GetValue` may hide later values for the same key by returning only the first match
- For the same reason, `GetCollection` can fail if the table contains duplicate keys and dictionary insertion encounters the same key more than once
