# DavidHome.Optimizely.VirtualText

Virtual Text is an Optimizely CMS plugin to manage root-level text files (for example `robots.txt`, `ads.txt`, `security.txt`) from the CMS UI.

This repository also contains an optional RobotsTxt extension that adds indexing policy management and optional `robots.txt` manipulation behavior for non-production environments.

## Packages

Core plugin:

- `DavidHome.Optimizely.VirtualText`
- `DavidHome.Optimizely.VirtualText.Location.AzureTable`
- `DavidHome.Optimizely.VirtualText.Content.AzureBlob`

Optional RobotsTxt extension:

- `DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt`
- `DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Storage.AzureTable`

## Prerequisites

- .NET SDK 8+ (project builds with newer SDKs as well)
- Optimizely CMS solution
- Azure Storage connection string (for Blob + Table providers)

## Install In Your Optimizely Solution

Add package references to your web project (`.csproj`), for example:

```xml
<ItemGroup>
  <PackageReference Include="DavidHome.Optimizely.VirtualText" Version="*" />
  <PackageReference Include="DavidHome.Optimizely.VirtualText.Location.AzureTable" Version="*" />
  <PackageReference Include="DavidHome.Optimizely.VirtualText.Content.AzureBlob" Version="*" />

  <!-- Optional -->
  <PackageReference Include="DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt" Version="*" />
  <PackageReference Include="DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Storage.AzureTable" Version="*" />
</ItemGroup>
```

## Configure `appsettings.json`

All settings are optional and have defaults. The following example shows all available settings:

```json
{
  "ConnectionStrings": {
    "EPiServerAzureBlobs": "<your-azure-storage-connection-string>"
  },
  "DavidHome": {    
    "VirtualText": {
      "MaxFileLocationsPerPage": 50,
      "MaxFileContentsPerPage": 50,
      "RobotsTxt": {
        "DisableRobotsTxtManipulator": false,
        "DefaultManipulatorContent": "User-agent: *\nDisallow: /\n"
      }
    }
  }
}
```

## Register Services

In `Startup.ConfigureServices` (or equivalent):

```csharp
services
    .AddCms()
    .AddDavidHomeVirtualText(Configuration)
    .AddAzureTableLocation(Configuration.GetSection("ConnectionStrings:EPiServerAzureBlobs"))
    .AddAzureBlobContent(Configuration.GetSection("ConnectionStrings:EPiServerAzureBlobs"))

    // Optional RobotsTxt extension
    .AddRobotsTxtExtension(Configuration)
    .AddAzureTableRobotsTxtStorage(Configuration.GetSection("ConnectionStrings:EPiServerAzureBlobs"));
```

## Register Middleware / App Initialization

In `Startup.Configure`:

```csharp
app.UseDavidHomeVirtualText()
   .UseAzureTableFileLocation()
   .UseAzureBlobFileStorage()

   // Optional RobotsTxt extension
   .UseRobotsTxtExtension()
   .UseAzureTableRobotsTxtStorage();
```

## Permissions

Virtual Text uses a permission group:

- Group: `VirtualText-Settings`
- View: `View-Permissions`
- Edit: `Edit-Permissions`

Grant these to the appropriate editor/admin roles in Optimizely.

## Notes About RobotsTxt Extension

- Environment policies are used to emit robots directives (for example `X-Robots-Tag` / robots meta behavior).
- The optional `robots.txt` manipulator only applies in non-production environments.
- Manipulator behavior is configurable through:
  - `DavidHome:VirtualText:RobotsTxt:DisableRobotsTxtManipulator`
  - `DavidHome:VirtualText:RobotsTxt:DefaultManipulatorContent`

## Local Development (This Repository)

Build frontend assets:

```bash
cd src/DavidHome.Optimizely.VirtualText/Frontend
npm ci
npm run build

cd ../../DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt/Frontend
npm ci
npm run build
```

Build solution:

```bash
dotnet restore
dotnet build DavidHome.Optimizely.VirtualText.sln
```
