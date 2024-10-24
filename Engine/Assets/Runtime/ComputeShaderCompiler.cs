using System.IO;

namespace Engine.Runtime;

public sealed class ComputeShaderEntry
{
    public FileInfo FileInfo;
}

public sealed class ComputeShaderCompiler
{
    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public void Compile(string assetsPath = null)
    {
        if (assetsPath is null)
            return;

        string computeShadersFolderPath = Path.Combine(assetsPath, "ComputeShaders");
        if (!Directory.Exists(computeShadersFolderPath))
            return;

        foreach (var computeShaderFilePath in Directory.GetFiles(computeShadersFolderPath, "*", SearchOption.AllDirectories))
            CheckComputeShaderEntry(computeShaderFilePath);
    }

    private void CheckComputeShaderEntry(string path)
    {
        FileInfo fileInfo = new(path);

        if (fileInfo.Extension != ".hlsl")
            return;

        if (!Assets.ComputeShaderEntries.TryGetValue(fileInfo.Name.RemoveExtension(), out var computeShaderEntry))
        {
            computeShaderEntry = new() { FileInfo = fileInfo };
            Assets.ComputeShaderEntries.Add(fileInfo.Name.RemoveExtension(), computeShaderEntry);

            Context.CreateComputeShader(false, fileInfo.FullName);

            Output.Log("Read new Compute Shader");
        }
        else if (fileInfo.LastWriteTime > computeShaderEntry.FileInfo.LastWriteTime)
        {
            computeShaderEntry.FileInfo = fileInfo;

            Context.CreateComputeShader(false, fileInfo.FullName);

            Output.Log("Updated Compute Shader");
        }
    }
}
