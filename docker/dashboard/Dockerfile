# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install .NET 8 SDK (needed for projects targeting net8.0 during restore)
# Pin to a specific version; update as needed
RUN dotnet_version=8.0.408 \
    && curl -fSL --output dotnet8.tar.gz \
       "https://dotnetcli.azureedge.net/dotnet/Sdk/${dotnet_version}/dotnet-sdk-${dotnet_version}-linux-x64.tar.gz" \
    && tar -oxzf dotnet8.tar.gz -C /usr/share/dotnet ./sdk ./shared \
    && rm dotnet8.tar.gz

WORKDIR /src

# Copy project files individually for layer caching
COPY Source/DotNetWorkQueue/DotNetWorkQueue.csproj Source/DotNetWorkQueue/
COPY Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj Source/DotNetWorkQueue.Transport.Shared/
COPY Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj Source/DotNetWorkQueue.Transport.RelationalDatabase/
COPY Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj Source/DotNetWorkQueue.Transport.SqlServer/
COPY Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj Source/DotNetWorkQueue.Transport.PostgreSQL/
COPY Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj Source/DotNetWorkQueue.Transport.Redis/
COPY Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj Source/DotNetWorkQueue.Transport.SQLite/
COPY Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj Source/DotNetWorkQueue.Transport.LiteDB/
COPY Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj Source/DotNetWorkQueue.Dashboard.Api/
COPY Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj Source/DotNetWorkQueue.Dashboard.Ui/

# Copy build infrastructure (Central Package Management)
COPY Lib/ Lib/
COPY Source/Directory.Build.props Source/
COPY Source/Directory.Packages.props Source/

RUN dotnet restore "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"

# Copy all source and publish
COPY Source/ Source/
RUN dotnet publish "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj" \
    -c Release -f net10.0 -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install SQLite native library (required by the SQLite transport)
RUN apt-get update \
    && apt-get install -y --no-install-recommends libsqlite3-0 \
    && rm -rf /var/lib/apt/lists/*

# System.Data.SQLite native loader expects libdl.so, but on glibc 2.34+
# dlopen is in libc directly. Create the symlink it needs.
RUN if [ ! -e /usr/lib/x86_64-linux-gnu/libdl.so ]; then \
        ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so 2>/dev/null || \
        ln -s /lib/x86_64-linux-gnu/libc.so.6 /usr/lib/x86_64-linux-gnu/libdl.so; \
    fi

WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DotNetWorkQueue.Dashboard.Ui.dll"]
