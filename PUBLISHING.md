# Publishing SpringRabbit.NET to NuGet

## Package Created Successfully ✅

**Package Location:** `./nupkg/SpringRabbit.NET.1.0.0.nupkg`  
**Symbols Package:** `./nupkg/SpringRabbit.NET.1.0.0.snupkg`  
**Package Size:** 31 KB

## Package Metadata

- **Package ID:** SpringRabbit.NET
- **Version:** 1.0.0
- **Authors:** Rexford Asiedu
- **Company:** Poseidon
- **License:** MIT
- **Repository:** https://github.com/lionboy634/SpringRabbit.NET

## Publishing Steps

### 1. Get Your NuGet API Key

1. Go to https://www.nuget.org/
2. Sign in (or create an account)
3. Click on your profile → **API Keys**
4. Create a new API Key with:
   - **Key Name:** SpringRabbit.NET Publishing
   - **Expiration:** Choose appropriate duration
   - **Select Scopes:** Select "Push new packages and package versions"
5. Copy the API key (you'll only see it once!)

### 2. Publish to NuGet.org

```powershell
# Navigate to the project directory
cd C:\Users\PC\Desktop\SpringRabbit.NET

# Publish the package
dotnet nuget push ./nupkg/SpringRabbit.NET.1.0.0.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY_HERE

# Publish symbols package (optional but recommended)
dotnet nuget push ./nupkg/SpringRabbit.NET.1.0.0.snupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY_HERE
```

### 3. Alternative: Store API Key Securely

```powershell
# Store API key (Windows Credential Manager)
dotnet nuget update source "nuget.org" --username YOUR_NUGET_USERNAME --password YOUR_API_KEY --store-password-in-clear-text

# Then push without specifying API key
dotnet nuget push ./nupkg/SpringRabbit.NET.1.0.0.nupkg --source nuget.org
```

### 4. Verify Publication

After publishing, check:
- https://www.nuget.org/packages/SpringRabbit.NET
- Package should appear within a few minutes

## Installing the Package

Once published, users can install it with:

```bash
dotnet add package SpringRabbit.NET
```

Or via Package Manager Console:
```
Install-Package SpringRabbit.NET
```

## Updating the Package

To publish a new version:

1. Update the version in `SpringRabbit.NET.csproj`:
   ```xml
   <Version>1.0.1</Version>
   ```

2. Rebuild and repack:
   ```powershell
   dotnet pack SpringRabbit.NET/SpringRabbit.NET.csproj --configuration Release --output ./nupkg
   ```

3. Push the new version:
   ```powershell
   dotnet nuget push ./nupkg/SpringRabbit.NET.1.0.1.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY
   ```

## Package Contents

The package includes:
- ✅ All core library files
- ✅ XML documentation (for IntelliSense)
- ✅ Symbol package (for debugging)
- ✅ README.md
- ✅ All dependencies properly referenced

## Notes

- The package targets .NET 8.0
- Dependencies are automatically resolved by NuGet
- XML documentation warnings are non-blocking (package still works)
- Consider adding XML comments in future versions for better IntelliSense

