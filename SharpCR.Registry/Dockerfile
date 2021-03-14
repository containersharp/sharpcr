FROM centos:7

WORKDIR /app
COPY bin/Release/netcoreapp3.1/linux-x64/publish ./

EXPOSE 5000/tcp
ENTRYPOINT ["/app/SharpCR.Registry"]