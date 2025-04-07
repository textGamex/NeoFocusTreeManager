using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FocusTreeManager.CodeStructures;
using FocusTreeManager.DataContract;
using FocusTreeManager.Model;
using FocusTreeManager.Model.TabModels;
using FocusTreeManager.ViewModel;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;

namespace FocusTreeManager.Parsers;

public static class FocusTreeParser
{
    private static readonly string[] ALL_PARSED_ELEMENTS =
    [
        "id",
        "x",
        "y",
        "icon",
        "text",
        "prerequisite",
        "relative_position_id",
        "cost",
        "mutually_exclusive"
    ];

    public static string ParseTreeForCompare(FocusGridModel model)
    {
        var container = new FociGridContainer(model);
        string focusTreeId = container.ContainerID.Replace(" ", "_");
        return Parse(container.FociList.ToList(), focusTreeId, container.TAG, container.AdditionnalMods);
    }

    public static string ParseTreeScriptForCompare(string filename)
    {
        var script = new Script();
        script.Analyse(File.ReadAllText(filename));
        return !File.Exists(filename) ? "" : ParseTreeForCompare(CreateTreeFromScript(script));
    }

    public static Dictionary<string, string> ParseAllTrees(List<FociGridContainer> containers)
    {
        var fileList = new Dictionary<string, string>();
        foreach (var container in containers)
        {
            string id = container.ContainerID.Replace(' ', '_');
            fileList[id] = Parse(container.FociList, id, container.TAG, container.AdditionnalMods);
        }
        return fileList;
    }

    private static string Parse(List<Focus> focusList, string focusTreeId, string tag, string additionnalMods)
    {
        focusList = PrepareParse(focusList);
        var focusTreeNode = new Node("focus_tree");
        var countryNode = CreateCountryNode(tag);
        var childrenList = new List<Child>(8)
        {
            ChildHelper.LeafString("id", focusTreeId),
            Child.Create(countryNode),
            !string.IsNullOrEmpty(tag)
                ? ChildHelper.Leaf("default", false)
                : ChildHelper.Leaf("default", true)
        };

        foreach (var focus in focusList)
        {
            var focusNode = new Node("focus");
            var focusChildren = new List<Child>(8)
            {
                ChildHelper.LeafString("id", focus.UniqueName),
                ChildHelper.LeafString("icon", $"GFX_{focus.Image}"),
                ChildHelper.Leaf("cost", (int)focus.Cost),
                ChildHelper.Leaf("x", focus.X),
                ChildHelper.Leaf("y", focus.Y)
            };

            if (focus.Prerequisite.Count != 0)
            {
                foreach (var prerequisiteSet in focus.Prerequisite)
                {
                    var prerequisiteNode = new Node("prerequisite");
                    var prerequisiteChildren = new List<Child>(4);
                    foreach (var prerequisiteFocus in prerequisiteSet.FociList)
                    {
                        prerequisiteChildren.Add(
                            ChildHelper.LeafString("focus", prerequisiteFocus.UniqueName)
                        );
                    }
                    prerequisiteNode.AllArray = prerequisiteChildren.ToArray();
                    focusChildren.Add(Child.Create(prerequisiteNode));
                }
            }

            if (focus.MutualyExclusive.Count != 0)
            {
                foreach (var set in focus.MutualyExclusive)
                {
                    var mutuallyExclusiveNode = new Node("mutually_exclusive");
                    mutuallyExclusiveNode.AddChild(
                        set.Focus1.UniqueName == focus.UniqueName
                            ? ChildHelper.LeafString("focus", set.Focus2.UniqueName)
                            : ChildHelper.LeafString("focus", set.Focus1.UniqueName)
                    );
                    focusChildren.Add(Child.Create(mutuallyExclusiveNode));
                }
            }

            if (focus.RelativeTo is not null)
            {
                focusChildren.Add(
                    ChildHelper.LeafString("relative_position_id", focus.RelativeTo.UniqueName)
                );
            }

            focusNode.AllArray = focusChildren.ToArray();
            childrenList.Add(Child.Create(focusNode));
        }

        focusTreeNode.AllArray = childrenList.ToArray();

        // TODO: 重新实现内部脚本解析
        // foreach (var focus in focusList)
        // {
        //     text.AppendLine(focus.InternalScript.Parse(null, 3));
        // }

        return CKPrinter.PrettyPrintStatement(focusTreeNode.ToRaw);
    }

    private static Node CreateCountryNode(string countryTag)
    {
        var countryNode = Node.Create("country");
        countryNode.AddChild(ChildHelper.Leaf("factor", 0));

        if (!string.IsNullOrEmpty(countryTag))
        {
            var modifierNode = new Node("modifier")
            {
                AllArray = [ChildHelper.Leaf("add", 10), ChildHelper.LeafString("tag", countryTag)]
            };
            countryNode.AddChild(modifierNode);
        }

        return countryNode;
    }

    private static List<Focus> PrepareParse(List<Focus> listFoci)
    {
        if (listFoci.Count == 0)
        {
            return [];
        }

        var sortedList = new List<Focus>();
        var holdedList = new List<Focus>();
        //Add the roots, the nodes without any perequisites. Helps with performance
        sortedList.AddRange(listFoci.Where(f => f.Prerequisite.Count == 0));
        int maxY = listFoci.Max(i => i.Y);
        int maxX = listFoci.Max(i => i.X);
        //For each X in the grid
        for (int i = 0; i < maxX; i++)
        {
            //For each Y in the grid
            for (int j = 0; j < maxY; j++)
            {
                //If there is a focus with the current X and Y
                var focus = listFoci.FirstOrDefault(f => f.X == i && f.Y == j);
                if (focus == null)
                {
                    continue;
                }
                //If the prerequisites are not present
                if (!CheckPrerequisite(focus, sortedList))
                {
                    foreach (var set in focus.Prerequisite)
                    {
                        //check if any of the prerequisite can be added immediatly
                        foreach (var setFocus in set.FociList)
                        {
                            //If that focus has no prerequisites or the prerequisites are present
                            if (
                                (CheckPrerequisite(setFocus, sortedList) || !setFocus.Prerequisite.Any())
                                && !sortedList.Contains(setFocus)
                            )
                            {
                                //Add it if it is not there already
                                sortedList.Add(setFocus);
                            }
                        }
                    }
                    //Recheck prerequisite again
                    if (CheckPrerequisite(focus, sortedList) && !sortedList.Contains(focus))
                    {
                        //Add the focus to the sorted list
                        sortedList.Add(focus);
                    }
                    else
                    {
                        //Add the current focus to the holded list
                        holdedList.Add(focus);
                        break;
                    }
                }
                else if (!sortedList.Contains(focus))
                {
                    //Otherwise, add it to the list
                    sortedList.Add(focus);
                }
                //Check if we can add some of the holded focus
                AddBackwardsPrerequisite(holdedList, sortedList);
            }
            //Check if we can add some of the holded focus
            AddBackwardsPrerequisite(holdedList, sortedList);
        }
        //Check if we can add some of the holded focus
        AddBackwardsPrerequisite(holdedList, sortedList);
        //Just to be sure, add any prerequisite that are not in the list, but in the original
        sortedList.AddRange(listFoci.Except(sortedList));
        return sortedList;
    }

    private static bool CheckPrerequisite(Focus focus, ICollection<Focus> SortedList)
    {
        return focus.Prerequisite.All(set => set.FociList.All(SortedList.Contains));
    }

    private static void AddBackwardsPrerequisite(ICollection<Focus> HoldedList, ICollection<Focus> SortedList)
    {
        bool wasAdded = false;
        var TempoList = HoldedList.ToList();
        //Run through all focus holded
        foreach (var focus in TempoList)
        {
            if (CheckPrerequisite(focus, SortedList) && !SortedList.Contains(focus))
            {
                //If all prerequisites were okay, add it to the list
                SortedList.Add(focus);
                HoldedList.Remove(focus);
                wasAdded = true;
            }
            //If one focus was added
            if (wasAdded)
            {
                //Recheck prerequisites
                AddBackwardsPrerequisite(HoldedList, SortedList);
            }
        }
    }

    public static FocusGridModel CreateTreeFromScript(Script script)
    {
        if (script.Logger.hasErrors())
        {
            return null;
        }
        var container = new FocusGridModel(script.TryParse(script, "id"));
        //Get content of Modifier block
        var modifier = script.FindAssignation("modifier");
        container.TAG = script.TryParse(modifier, "tag", null, false);
        //Run through all foci
        foreach (var codeStruct in script.FindAllValuesOfType<CodeBlock>("focus"))
        {
            var block = (CodeBlock)codeStruct;
            try
            {
                //Create the focus
                var newFocus = new FocusModel
                {
                    UniqueName = script.TryParse(block, "id"),
                    Text = script.TryParse(block, "text", null, false),
                    Image = script.TryParse(block, "icon").Replace("GFX_", ""),
                    X = int.Parse(script.TryParse(block, "x")),
                    Y = int.Parse(script.TryParse(block, "y")),
                    Cost = GetDouble(script.TryParse(block, "cost"), 10)
                };
                //Get all core scripting elements
                var InternalFocusScript = block.GetContentAsScript(
                    ALL_PARSED_ELEMENTS.ToArray(),
                    script.Comments
                );
                newFocus.InternalScript = InternalFocusScript;
                if (script.Logger.hasErrors())
                {
                    new ViewModelLocator().ErrorDawg.AddError(string.Join("\n", script.Logger.getErrors()));
                    continue;
                }
                container.FociList.Add(newFocus);
            }
            catch (Exception)
            {
                //TODO: Add language support
                new ViewModelLocator().ErrorDawg.AddError(script.Logger.ErrorsToString());
                new ViewModelLocator().ErrorDawg.AddError(
                    "Invalid syntax for focus "
                        + script.TryParse(block, "id")
                        + ", please double-check the syntax."
                );
            }
        }
        //Run through all foci again for mutually exclusives and prerequisites
        foreach (var codeStruct in script.FindAllValuesOfType<CodeBlock>("focus"))
        {
            var block = (CodeBlock)codeStruct;
            string id = block.FindValue("id") != null ? block.FindValue("id").Parse() : "";
            var newFocus = container.FociList.FirstOrDefault(f => f.UniqueName == id);
            if (newFocus == null)
            {
                //Check if we removed this focus because of a syntax error
                continue;
            }
            //Try to find its relative to
            string relativeToId = script.TryParse(block, "relative_position_id", null, false);
            if (!string.IsNullOrEmpty(relativeToId))
            {
                newFocus.CoordinatesRelativeTo = container.FociList.FirstOrDefault(f =>
                    f.UniqueName == relativeToId
                );
            }
            //Recreate its mutually exclusives
            foreach (var exclusives in block.FindAllValuesOfType<ICodeStruct>("mutually_exclusive"))
            {
                foreach (var focuses in exclusives.FindAllValuesOfType<ICodeStruct>("focus"))
                {
                    //Check if focus exists in list
                    var found = container.FociList.FirstOrDefault(f => f.UniqueName == focuses.Parse());
                    //If we have found something
                    if (found == null)
                        continue;
                    var set = new MutuallyExclusiveSetModel(newFocus, found);
                    //Check if the set already exists in this focus
                    if (newFocus.MutualyExclusive.Contains(set) || found.MutualyExclusive.Contains(set))
                    {
                        continue;
                    }
                    newFocus.MutualyExclusive.Add(set);
                    found.MutualyExclusive.Add(set);
                }
            }
            //Recreate its prerequisites
            foreach (var prerequistes in block.FindAllValuesOfType<ICodeStruct>("prerequisite"))
            {
                if (!prerequistes.FindAllValuesOfType<ICodeStruct>("focus").Any())
                {
                    break;
                }
                var set = new PrerequisitesSetModel(newFocus);
                foreach (var focuses in prerequistes.FindAllValuesOfType<ICodeStruct>("focus"))
                {
                    //Add the focus as a prerequisites in the current existing focuses
                    var search = container.FociList.FirstOrDefault((f) => f.UniqueName == focuses.Parse());
                    if (search != null)
                    {
                        set.FociList.Add(search);
                    }
                }
                //If any prerequisite was added (Poland has empty prerequisite blocks...)
                if (set.FociList.Any())
                {
                    newFocus.Prerequisite.Add(set);
                }
            }
        }
        return container;
    }

    private static double GetDouble(string value, double defaultValue)
    {
        double result;
        //Try parsing in the current culture
        if (
            !double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
            &&
            //Then try in US english
            !double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
            &&
            //Then in neutral language
            !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
        )
        {
            result = defaultValue;
        }
        return result;
    }
}
