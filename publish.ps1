dotnet restore --runtime linux-x64

cd SharpCR.Features.LocalStorage
dotnet build -c Release -r linux-x64

cd ..
cd SharpCR.Registry
dotnet build -c Release -r linux-x64
dotnet publish -c Release --no-build -r linux-x64

docker build -t "jijiechen-docker.pkg.coding.net/sharpcr/apps/sharpcr-registry:1.0.5" .
cd ..