using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace Engine.Helper;

public enum EDataType
{
    XML,
    JSON,
}

public static class Serialization
{
    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    public static void SaveFile<T>(T file, string path, EDataType eDataType = EDataType.XML)
    {
        switch (eDataType)
        {
            case EDataType.XML:
                SaveXml(file, path);
                break;
            case EDataType.JSON:
                SaveJSON(file, path);
                break;
            default:
                break;
        }
    }

    public static T LoadFile<T>(string path, EDataType eDataType = EDataType.XML)
    {
        object obj = null;

        switch (eDataType)
        {
            case EDataType.XML:
                obj = LoadXml(typeof(T), path);
                break;
            case EDataType.JSON:
                obj = LoadJSON(typeof(T), path);
                break;
            default:
                break;
        }

        return (T)obj;
    }

    public static void SaveXml<T>(T file, string path)
    {
        using (FileStream fs = new(path, File.Exists(path) ? FileMode.Create : FileMode.CreateNew))
        {
            XmlSerializer serializer = new(file.GetType());
            serializer.Serialize(fs, file);
        }
    }

    public static void SaveJSON<T>(T file, string path)
    {
        JsonSerializerOptions options = new();
        options.WriteIndented = true;

        string data = JsonSerializer.Serialize(file, typeof(T), options);
        File.WriteAllText(path, data);
    }

    public static object LoadXml(Type type, string path)
    {
        object obj = null;

        using (FileStream fs = new(path, FileMode.Open))
        {
            XmlSerializer serializer = new(type);
            obj = serializer.Deserialize(fs);
        }

        return obj;
    }

    public static object LoadJSON(Type type, string path) =>
        JsonSerializer.Deserialize(File.ReadAllText(path), type);
}
