﻿namespace Engine.Data;

public class Config
{
    public CameraProjection CameraProjection;
    public RenderMode RenderMode;

    public bool VSync;
    public bool SuperSample;

    public void SetVSync(bool b) =>
        VSync = b;

    public void SetSuperSample(bool b) =>
        SuperSample = b;
}