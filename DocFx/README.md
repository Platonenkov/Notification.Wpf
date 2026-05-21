# DocFX Documentation

## Prerequisites

```bash
dotnet tool install -g docfx
```

## Build

```bash
# From repository root
dotnet restore Notification.Wpf.sln
docfx DocFx/docfx.json
```

Output will be in `docs/` folder.

## Serve locally

```bash
docfx DocFx/docfx.json --serve
```

Opens at http://localhost:8080

## Clean and rebuild

```powershell
Remove-Item -Recurse -Force docs -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force DocFx/reference -ErrorAction SilentlyContinue
docfx DocFx/docfx.json --serve
```

## Deployment

Documentation is automatically deployed to GitHub Pages on push to `master` via `.github/workflows/docs.yml`.
