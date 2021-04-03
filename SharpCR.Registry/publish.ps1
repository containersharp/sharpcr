$VER=$args[0] # || $1

if(-not $VER){
    $VER='dev'    
}

dotnet restore --runtime linux-x64

cd ..
cd SharpCR.Features.LocalStorage
dotnet build -c Release -r linux-x64

cd ..
cd SharpCR.Registry
dotnet build -c Release -r linux-x64
dotnet publish -c Release --no-build -r linux-x64

docker build -t "jijiechen-docker.pkg.coding.net/sharpcr/apps/sharpcr-registry:$VER" .
