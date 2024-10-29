using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace Engine.Helpers;

public enum DataType
{
    XML,
    JSON
}

public static partial class Serialization
{
    public static void SaveFile<T>(T file, string path, DataType dataType = DataType.XML)
    {
        switch (dataType)
        {
            case DataType.XML:
                SaveXml(file, path);
                break;
            case DataType.JSON:
                SaveJSON(file, path);
                break;
            default:
                break;
        }
    }

    public static T LoadFile<T>(string path, DataType dataType = DataType.XML)
    {
        object result;

        switch (dataType)
        {
            case DataType.XML:
                result = LoadXml(typeof(T), path);
                break;
            case DataType.JSON:
                result = LoadJSON(typeof(T), path);
                break;
            default:
                result = null;
                break;
        }

        return (T)result;
    }
}

public static partial class Serialization
{
    private static void SaveXml<T>(T file, string path)
    {
        try
        {
            using MemoryStream temporaryMemoryStream = new MemoryStream();

            new XmlSerializer(typeof(T)).Serialize(temporaryMemoryStream, file);
            File.WriteAllBytes(path, temporaryMemoryStream.ToArray());
        }
        catch (Exception ex) { Output.Log(ex.Message); }
    }

    private static void SaveJSON<T>(T file, string path)
    {
        JsonSerializerOptions options = new();
        options.WriteIndented = true;

        string data = JsonSerializer.Serialize(file, typeof(T), options);
        File.WriteAllText(path, data);
    }
}

public static partial class Serialization
{
    private static object LoadXml(Type type, string path)
    {
        object result = null;

        try
        {
            using FileStream fileStream = new(path, FileMode.Open);

            result = new XmlSerializer(type).Deserialize(fileStream);
        }
        catch (Exception ex) { Output.Log(ex.Message); }

        return result;
    }

    private static object LoadJSON(Type type, string path)
    {
        object result = null;

        try
        {
            result = JsonSerializer.Deserialize(File.ReadAllText(path), type);
        }
        catch (Exception ex) { Output.Log(ex.Message); }

        return result;
    }
}