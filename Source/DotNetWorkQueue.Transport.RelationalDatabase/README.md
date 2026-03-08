# DotNetWorkQueue.Transport.RelationalDatabase

Shared abstractions for SQL-based [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue) transports.

## Overview

This package provides common SQL query handling, command preparation, and table management shared by the relational database transports (SQL Server, PostgreSQL, SQLite). It is not intended to be used directly by consumers -- it is referenced by relational transport implementations.

## Key Types

- Command handlers and prepare handlers for SQL operations
- `CommandStringCache` for SQL string management
- Dashboard query handlers for queue monitoring
- Shared table creation and schema management

## Installation

```
dotnet add package DotNetWorkQueue.Transport.RelationalDatabase
```

> **Note:** You typically do not need to install this package directly. It is included as a dependency of the relational transport packages.

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
