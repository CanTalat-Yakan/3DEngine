[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/CanTalat-Yakan/3DEngine/blob/master/LICENSE)
# [3DEngine Wiki](https://3DEngine.Wiki)

# Windows App SDK

| Delight users with modern UI | Access new platform features | Backwards compatible |
|:--:|:--:|:--:|
| <img src="https://docs.microsoft.com/media/illustrations/biztalk-get-started-get-started.svg" width=250 alt="Modern navigation"/><br>Powerful WinUI 3 UI | <img src="https://docs.microsoft.com/media/illustrations/biztalk-get-started-scenarios.svg" width=250 alt="Platform logo"/><br>Localization + power status + more<br> | <img src="https://docs.microsoft.com/media/illustrations/biztalk-host-integration-install-configure.svg" width=250 alt="Down-level logo"/><br>Down to Windows 10 1809 |

...and more, **without having to rewrite your app!**

* Use your current installer *(no requirement to use MSIX, but there are [reliability/security benefits to using MSIX](https://docs.microsoft.com/windows/msix/overview#key-features)!)*
* Additive platform APIs *(only add what you need, leave the rest of your app as-is)*
* Works with Win32, WPF, WinForms, and more apps

Plus, **create beautiful, modern apps for Windows 11 that are backwards compatible** to Windows 10 October 2018 Update (build 17763 / version 1809)!


# Vortice.Windows


[Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) is a collection of Win32 and UWP libraries with bindings support for [DXGI](https://docs.microsoft.com/en-us/windows/desktop/direct3ddxgi/d3d10-graphics-programming-guide-dxgi), [WIC](https://docs.microsoft.com/en-us/windows/desktop/wic/-wic-lh), [DirectWrite](https://docs.microsoft.com/en-us/windows/desktop/directwrite/direct-write-portal), [Direct2D](https://docs.microsoft.com/en-us/windows/desktop/direct2d/direct2d-portal), [Direct3D9](https://docs.microsoft.com/en-us/windows/win32/direct3d9/dx9-graphics), [Direct3D11](https://docs.microsoft.com/en-us/windows/desktop/direct3d11/atoc-dx-graphics-direct3d-11), [Direct3D12](https://docs.microsoft.com/en-us/windows/desktop/direct3d12/directx-12-programming-guide), [XInput](https://docs.microsoft.com/en-us/windows/win32/xinput/getting-started-with-xinput), [XAudio2](https://docs.microsoft.com/en-us/windows/win32/xaudio2/xaudio2-introduction), [X3DAudio](https://docs.microsoft.com/it-it/windows/win32/xaudio2/x3daudio), [DirectInput](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ee416842(v=vs.85)), [DirectStorage](https://devblogs.microsoft.com/directx/landing-page/), [DirectML](https://docs.microsoft.com/en-us/windows/ai/directml/dml-intro) and [UIAnimation](https://docs.microsoft.com/en-us/windows/win32/api/_uianimation).

This library supports netstandard3.1 and net7.0 using modern .NET API, see [CHANGELOG](https://github.com/amerkoleci/Vortice.Windows/blob/main/CHANGELOG.md) for list of changes between commits.

# Entity Component System (ECS)

The Entity Component System is an architectural pattern often used in video game development. It facilitates code reusability by separating the data from the behavior. In addition, ECS obeys the "composition over inheritance principle," providing improved flexibility and helping developers identify entities in a game's scene where all the objects are classified as entities. One reason ECS is faster than a game object component system is that you put all the data you need together in the same place in memory. Doing so avoids the performance overhead of having to search around to find it.

# Build

In order to compile, make sure **no spaces** are present in the solution path otherwise SharpGen will fail to generate bindings.
Also, you need to install [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the following components:

- [x] [Windows 11 SDK (10.0.22621.0)](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk)
- [x] [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
- [x] [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

# Preview
![3D Engine](https://drive.google.com/uc?export=view&id=170eLySL90SpNPX44mh-mmovyXwpWQZVj)
![Home](https://drive.google.com/uc?export=view&id=1v-Z-WKouFL75Nlg85uCq9ffLggrpPKE0)
![Doc](https://drive.google.com/uc?export=view&id=1w2v9zC1jUfXvSp1aTyq6G3fTWTnz8koa)
![Settings](https://drive.google.com/uc?export=view&id=165NHfmJG2d2YmcXdJ1NcgH8Y4gTvOLZa)
![image 1](https://drive.google.com/uc?export=view&id=1HJbfFVhr8uwGUEskEbIUSIyG5WPamrAC)
![image 2](https://drive.google.com/uc?export=view&id=1OdxSjVUvRswUxk7QC7TjZmQz7slKjBqF)
![image 3](https://drive.google.com/uc?export=view&id=1Xo2Fm0iDfSitcbhXNZ2pMgiHWCtKJFaM)
![image 4](https://drive.google.com/uc?export=view&id=1W6ouLBRJ6bYkq-vIlke44YdiOtzMC9um)
![image 5](https://drive.google.com/uc?export=view&id=1Fv8Z-N4vnPL4fIL-E7sABV8V6dLRz-Yn)
![image 6](https://drive.google.com/uc?export=view&id=1Y0bj1AUBgcbweB0mU712mJj5xdUGJ34P)
![image 7](https://drive.google.com/uc?export=view&id=1gx5dcSIT0LH-FORZuRT7Zsk_KvO0mXxP)
