# DotNetWorkQueue.Transport.Shared

Shared abstractions for [DotNetWorkQueue](https://github.com/blehnen/DotNetWorkQueue) transport implementations.

## Overview

This package provides the base interfaces and Command/Query pattern types used by all DotNetWorkQueue transports. It is not intended to be used directly by consumers -- it is referenced by transport implementations.

## Key Types

- `ICommandHandler<T>` / `ICommandHandlerWithOutput<T, TOutput>` - Command pattern interfaces
- `IQueryHandler<T, TResult>` / `IQueryHandlerAsync<T, TResult>` - Query pattern interfaces
- Dashboard query and command types for queue monitoring

## Installation

```
dotnet add package DotNetWorkQueue.Transport.Shared
```

> **Note:** You typically do not need to install this package directly. It is included as a dependency of the transport packages.

## Documentation

- [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki)
- [GitHub Repository](https://github.com/blehnen/DotNetWorkQueue)

## License

LGPL-2.1-or-later
