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
        object obj;

        switch (eDataType)
        {
            case EDataType.XML:
                obj = LoadXml(typeof(T), path);
                break;
            case EDataType.JSON:
                obj = LoadJSON(typeof(T), path);
                break;
            default:
                obj = null;
                break;
        }

        return (T)obj;
    }

    public static void SaveXml<T>(T file, string path)
    {
        try
        {
            using MemoryStream tempStream = new MemoryStream();

            new XmlSerializer(typeof(T)).Serialize(tempStream, file);
            File.WriteAllBytes(path, tempStream.ToArray());
        }

        catch (Exception ex) { throw new Exception(ex.Message); }
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
        object obj;

        try
        {
            using FileStream fs = new(path, FileMode.Open);

            obj = new XmlSerializer(type).Deserialize(fs);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }

        return obj;
    }

    public static object LoadJSON(Type type, string path)
    {
        object obj;

        try
        {
            obj = JsonSerializer.Deserialize(File.ReadAllText(path), type);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }

        return obj;
    }
}
