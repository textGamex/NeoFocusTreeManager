using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using FocusTreeManager.Model;
using FocusTreeManager.Parsers;

namespace FocusTreeManager.DataContract;

[KnownType(typeof(FociGridContainer))]
[KnownType(typeof(LocalisationContainer))]
[KnownType(typeof(EventContainer))]
[KnownType(typeof(ScriptContainer))]
[DataContract(Name = "project")]
public sealed class Project
{
    private static readonly string FocusTreePath = Path.Combine("common", "national_focus");

    private const string LOCALISATION_PATH = @"\localisation\";

    private const string EVENTS_PATH = @"\events\";

    public string filename { get; set; }

    [DataMember(Name = "foci_containers", Order = 0)]
    public List<FociGridContainer> fociContainerList { get; set; }

    [DataMember(Name = "locale_containers", Order = 1)]
    public List<LocalisationContainer> localisationList { get; set; }

    [DataMember(Name = "event_containers", Order = 2)]
    public List<EventContainer> eventList { get; set; }

    [DataMember(Name = "script_containers", Order = 3)]
    public List<ScriptContainer> scriptList { get; set; }

    [DataMember(Name = "mod_folder_list", Order = 4)]
    public List<string>? ModFolderList { get; set; }

    [DataMember(Name = "pre_load_content", Order = 5)]
    public bool preloadGameContent { get; set; }

    [DataMember(Name = "default_locale", Order = 6)]
    public LocalisationContainer defaultLocale { get; set; }

    public Project()
    {
        ModFolderList = [];
        fociContainerList = [];
        localisationList = [];
        eventList = [];
        scriptList = [];
    }

    public void ExportProject(string paramFilename)
    {
        string path = Path.Combine(paramFilename, FocusTreePath);
        Directory.CreateDirectory(path);
        //For each parsed focus trees
        foreach (var item in FocusTreeParser.ParseAllTrees(fociContainerList))
        {
            using var tw = new StreamWriter(Path.Combine(path, $"{item.Key}.txt"));
            tw.Write(item.Value);
        }
        path = paramFilename + LOCALISATION_PATH;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        //For each parsed localisation files
        foreach (var item in LocalisationParser.ParseEverything(localisationList))
        {
            using Stream stream = File.OpenWrite($"{path}{item.Key}.yml");
            using var tw = new StreamWriter(stream, Encoding.UTF8);
            tw.Write(item.Value);
        }
        path = paramFilename + EVENTS_PATH;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        //For each parsed event file
        foreach (var item in EventParser.ParseAllEvents(eventList))
        {
            using var tw = new StreamWriter($"{path}{item.Key}.txt");
            tw.Write(item.Value);
        }
        //For each parsed script file
        foreach (var item in ScriptParser.ParseEverything(scriptList))
        {
            using var tw = new StreamWriter($"{paramFilename}\\{item.Key}.txt");
            tw.Write(item.Value);
        }
    }

    public void UpdateDataContract(ProjectModel model)
    {
        filename = model.Filename;
        ModFolderList = model.ListModFolders.ToList();
        preloadGameContent = model.PreloadGameContent;
        //Build foci list
        fociContainerList.Clear();
        foreach (var item in model.fociList)
        {
            fociContainerList.Add(new FociGridContainer(item));
        }
        //Build localization list
        localisationList.Clear();
        foreach (var item in model.localisationList)
        {
            localisationList.Add(new LocalisationContainer(item));
        }
        //Build events list
        eventList.Clear();
        foreach (var item in model.eventList)
        {
            eventList.Add(new EventContainer(item));
        }
        //Build scripts list
        scriptList.Clear();
        foreach (var item in model.scriptList)
        {
            scriptList.Add(
                new ScriptContainer()
                {
                    IdentifierID = item.UniqueID,
                    ContainerID = item.VisibleName,
                    InternalScript = item.InternalScript,
                    FileInfo = item.FileInfo
                }
            );
        }
        if (localisationList.Any() && model.DefaultLocale != null)
        {
            defaultLocale = localisationList.FirstOrDefault(l =>
                l.IdentifierID == model.DefaultLocale.UniqueID
            );
        }
    }
}
