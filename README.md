![Logo](https://raw.githubusercontent.com/CanTalat-Yakan/3DEngine/master/3DEngine_Logo.png)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/CanTalat-Yakan/3DEngine/blob/master/LICENSE) 

# 3D Engine

[Get it on the Microsoft Store](https://www.microsoft.com/store/apps/9NFSX6JPV0PS)  
[Documentation](https://engine3d.gitbook.io/wiki/)

## Overview

The **3D Engine** is a high-performance, feature-rich game engine designed for modern game development. It leverages cutting-edge technology and APIs to provide a robust platform for creating immersive 3D experiences.

## Key Technologies

### Windows App SDK

- **Use the [WinAppSDK](https://github.com/microsoft/WindowsAppSDK) to create beautiful, modern apps for Windows 11, with backward compatibility to Windows 10 October 2018 Update (build 17763 / version 1809).**
  - Continue using your existing installer with optional MSIX for enhanced reliability and security.
  - Integrate with Win32, WPF, WinForms, and more.

### Vortice.Windows

- **[Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows)** provides bindings for key Windows libraries including:
  - DXGI, WIC, DirectWrite, Direct2D, Direct3D9, Direct3D11, Direct3D12, XInput, XAudio2, X3DAudio, DirectInput, DirectStorage, DirectML, UIAnimation, and DirectSound.
- Targets **.NET 7.0** and **.NET 8.0** with modern C# 12. [CHANGELOG](https://github.com/amerkoleci/Vortice.Windows/blob/main/CHANGELOG.md) for updates.

### Entity Component System (ECS)

- **ECS** is a design pattern for high-performance and flexible game development. It emphasizes separation of data from behavior and supports the "composition over inheritance" principle, improving performance and code reusability.

## NuGet Package

- **[3DEngine NuGet Package](https://www.nuget.org/packages/3DEngine/)**: Install the package via NuGet Package Manager for integration into your project.

  ```bash
  dotnet new console -n Project
  cd Project
  dotnet add package 3DEngine
  dotnet add package Costura.Fody
  ./Project.csproj
  ```

  Example usage:

  ```csharp
  sealed class Program
  {
      [STAThread]
      private static void Main() =>
          new Engine.Program().Run();
  }
  ```

  Setup in your project:

  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
          <OutputType>WinExe</OutputType>
          <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
          <ImplicitUsings>enable</ImplicitUsings>
          <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
          <PlatformTarget>x64</PlatformTarget>
          <PublishAot>true</PublishAot>
      </PropertyGroup>

      <ItemGroup>
        <None Remove="FodyWeavers.xml" />
      </ItemGroup>

      <ItemGroup>
          <PackageReference Include="3DEngine" Version="3.0.3" />
          <PackageReference Include="Costura.Fody" Version="5.7.0">
            <PrivateAssets>all</PrivateAssets>
          </PackageReference>
      </ItemGroup>

      <ItemGroup>
          <Content Update="$(NuGetPackageRoot)\3dengine\3.0.3\contentFiles\any\net8.0-windows10.0.22000\Assets\Resources\**\*">
              <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
          </Content>
      </ItemGroup>
  </Project>
  ```

  Ensure "CopyIfNewer" is set for files in the Assets folder in Visual Studio.

## Solution Structure

The 3D Engine repository includes:
- **3DEngine (Package)**
- **Editor**
- **Engine**

You can build the package for both the Editor and Engine as standalone components.

## Upcoming Features and Development Roadmap

- USD Integration
- Materials
- Render Textures
- Compute Shaders
- Post Processing
- Gizmos
- Asynchronous Reprojection
- Radiance Cascade (Alexander Sannikov)
- Virtualized Geometry (Nano Tech, Chris K)
- Networking
- Spatial Audio
- PhysX 5
- UWP Export for Xbox Platform

## Build Instructions

To compile the 3D Engine, ensure you have [Visual Studio 2022](https://visualstudio.microsoft.com/vs) with the following components:

- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
- [Visual Studio Installer](https://visualstudio.microsoft.com/vs) with:
  - [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
  - [Windows 11 SDK (10.0.22621.0)](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk)

## Screenshots

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