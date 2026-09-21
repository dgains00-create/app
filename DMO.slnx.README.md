# DMO.slnx

The solution file. `.slnx` is the current solution format produced by the .NET 10 SDK;
the request explicitly permits equivalent modern solution-file naming.

Build the whole solution:

```pwsh
dotnet build DMO.slnx
```

## Central configuration

| File | Purpose |
| --- | --- |
| `Directory.Build.props` | Shared build settings (`net10.0`, nullable, implicit usings). |
| `Directory.Packages.props` | Central package versions for the whole solution. |

Package versions are pinned in one place so the solution resolves a single consistent set of
EF Core / Microsoft.Extensions assemblies instead of each project drifting independently.
