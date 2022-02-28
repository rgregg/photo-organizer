dotnet publish --output release\win-x64 --self-contained --runtime win-x64 -c release
dotnet publish --output release\linux-x64 --self-contained --runtime linux-x64 -c release
dotnet publish --output release\osx-x64 --self-contained --runtime osx-x64 -c release
