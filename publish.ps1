cd SharpCR.Features.LocalStorage
dotnet build -c Release -r linux-x64

cd ..
cd SharpCR.Registry
dotnet build -c Release -r linux-x64
dotnet publish --no-build -c Release -r linux-x64

docker build -t "sharpcr-registry:1.0.0" .