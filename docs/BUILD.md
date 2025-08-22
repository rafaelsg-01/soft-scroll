# Build

Requirements:
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 (or `dotnet` CLI)

## Build & Run

- `dotnet build`
- `dotnet run`

## Publish single-file

```
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

Output will be in:
`bin/Release/net8.0-windows/win-x64/publish/SoftScroll.exe`
