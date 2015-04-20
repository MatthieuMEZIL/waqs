using EnvDTE;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WAQS
{
    public static class TemplatesCopying
    {
        public static void CopyTemplates(DTE dte, string templatesFolderName, string netVersion, string toolsPath, string vsVersion, out string templatesFolder, out HashSet<string> existingTTIncludes, out ProjectItems templatesProjectItems)
        {
            var slnFolder = Path.GetDirectoryName(dte.Solution.FullName);
            templatesFolder = Path.Combine(slnFolder, templatesFolderName);
            if (!Directory.Exists(templatesFolder))
            {
                Directory.CreateDirectory(templatesFolder);
            }

            const string solutionItemsFolderName = "Solution Items";
            Project solutionItems = dte.Solution.Projects.OfType<Project>().FirstOrDefault(pi => pi.Name == solutionItemsFolderName);
            UIHierarchyItem solutionItemsUIHierarchyItem = null;
            Action setSolutionItemsUIHierarchyItem = () => solutionItemsUIHierarchyItem = (dte.Windows.Cast<Window>().First(w => w.Type == vsWindowType.vsWindowTypeSolutionExplorer).Object as UIHierarchy)?.UIHierarchyItems.Cast<UIHierarchyItem>().First().UIHierarchyItems.Cast<UIHierarchyItem>().First(uihi => uihi.Name == solutionItemsFolderName);
            bool solutionItemsExpanded;

            Project templates = null;
            UIHierarchyItem templatesUIHierarchyItem = null;
            Action setTemplatesUIHierarchyItem = () => templatesUIHierarchyItem = solutionItemsUIHierarchyItem.UIHierarchyItems.Cast<UIHierarchyItem>().First(uihi => uihi.Name == templatesFolderName);
            bool templatesItemsExpanded = false;
            Action addTemplatesUIHierarchyItem = () =>
            {
                templates = ((EnvDTE80.SolutionFolder)solutionItems.Object).AddSolutionFolder(templatesFolderName);
                setTemplatesUIHierarchyItem();
                templatesItemsExpanded = false;
            };

            if (solutionItems == null)
            {
                solutionItems = ((EnvDTE80.Solution2)dte.Solution).AddSolutionFolder(solutionItemsFolderName);
                setSolutionItemsUIHierarchyItem();
                solutionItemsExpanded = false;
                addTemplatesUIHierarchyItem();
            }
            else
            {
                setSolutionItemsUIHierarchyItem();
                solutionItemsExpanded = solutionItemsUIHierarchyItem.UIHierarchyItems.Expanded;

                templates = ((IEnumerable)solutionItems.ProjectItems).Cast<ProjectItem>().FirstOrDefault(pi => pi.Name == templatesFolderName).SubProject;
                if (templates == null)
                {
                    addTemplatesUIHierarchyItem();
                }
                else
                {
                    setTemplatesUIHierarchyItem();
                    templatesItemsExpanded = templatesUIHierarchyItem.UIHierarchyItems.Expanded;
                }
            }

            var ttincludesFolder = Path.Combine(toolsPath, "ttincludes");
            templatesProjectItems = (ProjectItems)templates.ProjectItems;
            existingTTIncludes = new HashSet<string>(templatesProjectItems.Cast<ProjectItem>().Select(pi => pi.Name));
            string ttIncludeName = null;
            foreach (var ttInclude in Directory.GetFiles(ttincludesFolder).Where(f => (ttIncludeName = Path.GetFileName(f)).StartsWith("WAQS.")))
            {
                AddItem(ttInclude, vsVersion, netVersion, ttIncludeName, templatesFolder, existingTTIncludes, templatesProjectItems);
            }
            var ttIncludesFolderVS = Path.Combine(ttincludesFolder, vsVersion);
            foreach (var ttInclude in Directory.GetFiles(ttIncludesFolderVS).Where(f => (ttIncludeName = Path.GetFileName(f)).StartsWith("WAQS.")))
            {
                AddItem(ttInclude, vsVersion, netVersion, ttIncludeName, templatesFolder, existingTTIncludes, templatesProjectItems);
            }
            const string mergeTTIncludeFileName = "MergeT4Files.ttinclude";
            File.Copy(Path.Combine(ttincludesFolder, mergeTTIncludeFileName), Path.Combine(templatesFolder, mergeTTIncludeFileName), true);
            var specialMergeFolder = Path.Combine(ttincludesFolder, "SpecialMerge");
            foreach (var specialMerge in Directory.GetFiles(specialMergeFolder))
            {
                var ttSpecialMergeFileName = Path.GetFileName(specialMerge);
                var specialMergeFile = Path.Combine(specialMergeFolder, ttSpecialMergeFileName);
                var ttSpecialMergeFileCopy = Path.Combine(templatesFolder, ttSpecialMergeFileName);
                File.Copy(specialMergeFile, ttSpecialMergeFileCopy, true);
                if (!existingTTIncludes.Contains(ttSpecialMergeFileName))
                {
                    templatesProjectItems.AddFromFile(ttSpecialMergeFileCopy);
                }
            }
            try
            {
                templatesUIHierarchyItem.UIHierarchyItems.Expanded = templatesItemsExpanded;
                solutionItemsUIHierarchyItem.UIHierarchyItems.Expanded = solutionItemsExpanded;
            }
            catch
            {
            }
            MergeTTIncludes(dte, templates, templatesFolder);
        }

        public static void AddItem(string ttInclude, string vsVersion, string netVersion, string ttIncludeName, string templatesFolder, HashSet<string> existingTTIncludes, ProjectItems templatesProjectItems)
        {
            var m = Regex.Match(ttInclude, @".(NET\d+).");
            if (!m.Success || m.Groups[1].Value == netVersion)
            {
                const string x64Key = @"Software\Wow6432Node\Microsoft\Microsoft SDKs\Windows";
                if (ttIncludeName.EndsWith(".x64"))
                {
                    try
                    {
                        if (Registry.LocalMachine.OpenSubKey(x64Key) == null)
                        {
                            return;
                        }
                        ttIncludeName = ttIncludeName.Substring(0, ttIncludeName.Length - 4);
                    }
                    catch
                    {
                        return;
                    }
                }
                else if (ttIncludeName.EndsWith(".x86"))
                {
                    try
                    {
                        if (Registry.LocalMachine.OpenSubKey(x64Key) != null)
                        {
                            return;
                        }
                    }
                    catch
                    {
                    }
                    ttIncludeName = ttIncludeName.Substring(0, ttIncludeName.Length - 4);
                }
                var ttIncludeCopy = Path.Combine(templatesFolder, ttIncludeName);
                if (!existingTTIncludes.Contains(ttIncludeName))
                {
                    templatesProjectItems.AddFromFile(ttIncludeCopy);
                }
                if (ttIncludeName.Contains("." + vsVersion + "." + netVersion + "."))
                {
                    ttIncludeCopy = ttIncludeCopy.Substring(0, ttIncludeCopy.Length - 10) + ".merge.tt";
                    ttIncludeName = Path.GetFileName(ttIncludeCopy);
                    if (!existingTTIncludes.Contains(ttIncludeName))
                    {
                        templatesProjectItems.AddFromFile(ttIncludeCopy);
                    }
                }
            }
        }

        private static void MergeTTIncludes(DTE dte, Project templates, string templatesFolder)
        {
            string keyName = "VisualStudio.TextTemplating." + dte.Version + @"\DefaultIcon";
            var defaultIconKey = (string)Registry.ClassesRoot.OpenSubKey(keyName, false).GetValue(null);
            var transformTemplatesExePath = Path.Combine(Path.GetDirectoryName(defaultIconKey), "TextTransform.exe");
            var processes = new List<System.Diagnostics.Process>();
            foreach (var tt in templates.ProjectItems.Cast<ProjectItem>().Where(t => t.Name.EndsWith(".merge.tt")))
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = transformTemplatesExePath;
                process.StartInfo.Arguments = "\"" + Path.Combine(templatesFolder, tt.Name) + "\"";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                processes.Add(process);
            }
            foreach (var process in processes)
            {
                process.WaitForExit();
            }
        }
    }
}
