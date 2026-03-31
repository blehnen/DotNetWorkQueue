FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install .NET 8 SDK (needed for Dashboard projects targeting net8.0)
# Pin to a specific version; update as needed
RUN dotnet_version=8.0.408 \
    && curl -fSL --output dotnet8.tar.gz \
       "https://dotnetcli.azureedge.net/dotnet/Sdk/${dotnet_version}/dotnet-sdk-${dotnet_version}-linux-x64.tar.gz" \
    && tar -oxzf dotnet8.tar.gz -C /usr/share/dotnet ./sdk ./shared \
    && rm dotnet8.tar.gz

# Install Java (required for Jenkins agent) and CI tools
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       openjdk-17-jre-headless \
       git \
       curl \
       openssh-client \
       procps \
    && rm -rf /var/lib/apt/lists/*

# Verify both SDKs are available
RUN dotnet --list-sdks

# Create Jenkins agent workspace directory
RUN mkdir -p /home/jenkins && chmod 777 /home/jenkins

WORKDIR /home/jenkins
