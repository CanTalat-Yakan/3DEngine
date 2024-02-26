
![Logo](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/3DEngine_Logo.png)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/CanTalat-Yakan/3DEngine/blob/master/LICENSE) 
# [3D Engine - Get it in the Microsoft Store](https://www.microsoft.com/store/apps/9NFSX6JPV0PS)
## [Documentation](https://engine3d.gitbook.io/wiki/) 

# Windows App SDK

**Use the [WinAppSDK](https://github.com/microsoft/WindowsAppSDK) to create beautiful, modern apps for Windows 11 that are backwards compatible** to Windows 10 October 2018 Update (build 17763 / version 1809)!

* Use your current installer *(no requirement to use MSIX, but there are [reliability/security benefits to using MSIX](https://docs.microsoft.com/windows/msix/overview#key-features)!)*
* Additive platform APIs *(only add what you need, leave the rest of your app as-is)*
* Works with Win32, WPF, WinForms, and more apps

# Vortice.Windows

[Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) is a collection of Win32 and UWP libraries with bindings support for [DXGI](https://docs.microsoft.com/en-us/windows/desktop/direct3ddxgi/d3d10-graphics-programming-guide-dxgi), [WIC](https://docs.microsoft.com/en-us/windows/desktop/wic/-wic-lh), [DirectWrite](https://docs.microsoft.com/en-us/windows/desktop/directwrite/direct-write-portal), [Direct2D](https://docs.microsoft.com/en-us/windows/desktop/direct2d/direct2d-portal), [Direct3D9](https://docs.microsoft.com/en-us/windows/win32/direct3d9/dx9-graphics), [Direct3D11](https://docs.microsoft.com/en-us/windows/desktop/direct3d11/atoc-dx-graphics-direct3d-11), [Direct3D12](https://docs.microsoft.com/en-us/windows/desktop/direct3d12/directx-12-programming-guide), [XInput](https://docs.microsoft.com/en-us/windows/win32/xinput/getting-started-with-xinput), [XAudio2](https://docs.microsoft.com/en-us/windows/win32/xaudio2/xaudio2-introduction), [X3DAudio](https://docs.microsoft.com/it-it/windows/win32/xaudio2/x3daudio), [DirectInput](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416842(v=vs.85)), [DirectStorage](https://devblogs.microsoft.com/directx/landing-page/), [DirectML](https://docs.microsoft.com/en-us/windows/ai/directml/dml-intro), [UIAnimation](https://docs.microsoft.com/en-us/windows/win32/api/_uianimation) and [DirectSound](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/bb318665(v=vs.85)).

This library targets **.net7.0** and **.net8.0** and uses modern C# 12, see [CHANGELOG](https://github.com/amerkoleci/Vortice.Windows/blob/main/CHANGELOG.md) for list of changes between commits.

If you are looking for high-performance low level bindings, please visit [Vortice.Win32](https://github.com/amerkoleci/Vortice.Win32)

# Entity Component System (ECS)

The Entity Component System is an architectural pattern often used in video game development. It facilitates code reusability by separating the data from the behavior. In addition, ECS obeys the "composition over inheritance principle," providing improved flexibility and helping developers identify entities in a game's scene where all the objects are classified as entities. One reason ECS is faster than a game object component system is that you put all the data you need together in the same place in memory. Doing so avoids the performance overhead of having to search around to find it.

# NuGet Package
With the [NuGet](https://www.nuget.org/packages/3DEngine/) you can also only get the Engine with the NuGet Package Manager and create applications using the 3DEngine (w/o Editor) in a new project and implement all features via code.

If you want the Editor, this is the GitHub Repo for the complete [3DEngine](https://github.com/CanTalat-Yakan/3DEngine/tree/master).

```Batch
dotnet new console -n Project
cd Project
dotnet add package 3DEngine
dotnet add package Costura.Fody
./Project.csproj
```

Engine usage: 

```C#
sealed class Program
{
    [STAThread]
    private static void Main() =>
        new Engine.Program().Run();
}
```

Use the [Engine.Kernel](https://engine3d.gitbook.io/wiki/engine/core) to get to the Scene System.

Project Setup:

```XML
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<OutputType>WinExe</OutputType>
		<TargetFramework>net8.0-windows10.0.22000.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<SatelliteResourceLanguages>en</SatelliteResourceLanguages>
		<PlatformTarget>x64</PlatformTarget>
		<PublishAot>true</PublishAot>
	</PropertyGroup>

	<ItemGroup>
	  <None Remove="FodyWeavers.xml" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="3DEngine" Version="2.0.1" />
		<PackageReference Include="Costura.Fody" Version="5.7.0">
		  <PrivateAssets>all</PrivateAssets>
		</PackageReference>
	</ItemGroup>
</Project>
```

Also set the Files inside the Assets Folder to "CopyIfNewer" in the Properties Panel inside Visual Studio, so it is included in the Build Folder. This will be obsolete in the Future by the USD Format.

# Solution
The 3D Engine contains three Projects: 
* 3DEngine (Package)
* Editor
* Engine
 
You can compile the 3DEngine (Package) for the Editor and the Engine as a standalone. 

# Upcoming Features and Development Roadmap

* Materials,
* Render Textures,
* Compute Shader,
* Post Processing,
* Gizmos,
* USD,
* Asynchronous Reprojection,
* Lumen (Erebus, Aruna),
* Nanite (Nano Tech),
* Networking,
* Spatial Audio,
* PhsyX 5,
* Export to UWP for Xbox Platform.

As a Unity Developer, I've gathered a collection of top-notch assets from the Unity Asset Store. I plan to incorporate my favorite ones into this game engine when the time is right for implementing them in the creation of various games. Here's a list of some plugins that I'll be integrating into the engine:

* [KWS Water System](https://assetstore.unity.com/packages/tools/particles-effects/kws-water-system-hdrp-rendering-205007),
* [Acerola's Water System](https://github.com/GarrettGunnell/Water),
* [Expanse](https://assetstore.unity.com/packages/tools/particles-effects/expanse-volumetric-skies-clouds-and-atmospheres-in-hdrp-192456),
* [Atlas](https://assetstore.unity.com/packages/tools/terrain/atlas-terrain-editor-207568).

# Build
In order to compile, you need to install [Visual Studio 2022](https://visualstudio.microsoft.com/vs) with the following components:

*  [Windows 11 SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk)
*  [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
*  [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

# Screenshots
![3D Engine Layout](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Layout.png)
![1](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_1.png)
![2](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_2.png)
![3](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_3.png)
![4](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_4.png)
![5](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_5.png)
![6](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_6.png)
![7](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_7.png)
![8](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_8.png)
![9](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_9.png)
![10](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_10.png)
![13](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/Screenshots/Screenshot_Folder.png)
