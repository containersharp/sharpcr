FROM mcr.microsoft.com/dotnet/runtime:3.1-alpine

WORKDIR /app
COPY bin/Release/netcoreapp3.1/linux-x64/publish ./

ENTRYPOINT ["/app/SharpCR.Registry"]