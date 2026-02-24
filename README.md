# ReScene.NET

A cross-platform desktop application for inspecting and creating ReScene (SRR/SRS) files, built with Avalonia UI.

## Features

- **Inspector** — Open and explore the internal block structure of `.srr` and `.srs` files with a tree view, property grid, and hex viewer
- **SRR Creator** — Create `.srr` files from RAR archives (single or multi-volume) or SFV manifests, with support for stored files, OSO hashes, and app name tagging
- **SRS Creator** — Create `.srs` files from sample media files (AVI, MKV, MP4, WMV, FLAC, MP3, M2TS, and more)

## Requirements

- .NET 10.0

## Getting Started

Clone with submodules:

```bash
git clone --recurse-submodules https://github.com/prijkes/ReScene.NET.git
```

If already cloned without submodules:

```bash
git submodule update --init --recursive
```

## Building

```bash
dotnet build
```

## Dependencies

| Package | Version |
|---|---|
| [Avalonia](https://avaloniaui.net/) | 11.3.0 |
| [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) | 8.4.0 |
| [Rescene.Lib](https://github.com/prijkes/Rescene.Lib) | submodule |

## License

See [LICENSE](LICENSE) for details.
# ReScene.NET
# ReScene.NET
