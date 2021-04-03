$VER=$args[0] # || $1

if(-not $VER){
    $VER='1.0.0-dev'    
}

dotnet publish -c Release -r linux-x64

dotnet pack -c Release --runtime linux-x64 /p:VersionPrefix=$VER

dotnet nuget push --source integration --api-key api "bin\Release\SharpCR.$VER.nupkg"