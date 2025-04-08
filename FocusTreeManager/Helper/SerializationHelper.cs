using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using FocusTreeManager.DataContract;
using NLog;

namespace FocusTreeManager.Helper;

public static class SerializationHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static Project? Deserialize(string filename)
    {
        //If we are loading a legacy version
        try
        {
            if (Path.GetExtension(filename) == ".xh4prj")
            {
                using var fs = new FileStream(filename, FileMode.Open);
                using var reader = XmlReader.Create(fs);
                var ser = new DataContractSerializer(typeof(Project));
                var project = (Project?)ser.ReadObject(reader, true);
                return project;
            }
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to deserialize project");
            return null;
        }
    }

    public static void Serialize(string filename, Project project)
    {
        if (filename == null || project == null)
        {
            return;
        }
        var serializer = new DataContractSerializer(typeof(Project));
        using Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        using var writer = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8);
        writer.WriteStartDocument();
        serializer.WriteObject(writer, project);
    }
}
