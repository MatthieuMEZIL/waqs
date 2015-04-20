using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VSLangProj;

namespace WAQS
{
    public static class DTEExtensions
    {

        public static IEnumerable<EnvDTE.Project> GetProjects(this DTE dte)
        {
            var items = new Stack<object>(dte.Solution.Projects.Cast<object>());
            while (items.Count != 0)
            {
                var item = items.Pop();
                var project = item as Project;
                if (project == null)
                {
                    var projectItem = item as ProjectItem;
                    if (projectItem == null)
                    {
                        throw new InvalidOperationException();
                    }
                    if (projectItem.SubProject != null)
                    {
                        items.Push(projectItem.SubProject);
                    }
                    if (projectItem.ProjectItems != null)
                    {
                        foreach (var pi in projectItem.ProjectItems)
                        {
                            items.Push(pi);
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(project.FullName))
                    {
                        if (!(project.Name == ".nuget" || project.Name == "Solution Items"))
                        {
                            if (project.ProjectItems != null)
                            {
                                foreach (var pi in project.ProjectItems)
                                {
                                    items.Push(pi);
                                }
                            }
                        }
                    }
                    else if (project.FullName.EndsWith(".csproj"))
                    {
                        yield return project;
                    }
                }
            }
        }

        public static IEnumerable<ProjectItem> GetAllProjectItems(this Project parent, Func<ProjectItem, bool> includett = null)
        {
            return GetAllProjectItems((object)parent, includett);
        }
        public static IEnumerable<ProjectItem> GetAllProjectItems(this ProjectItem parent, Func<ProjectItem, bool> includett = null)
        {
            return GetAllProjectItems((object)parent, includett);
        }

        public static IEnumerable<ProjectItem> GetAllProjectItems(object parent, Func<ProjectItem, bool> includett = null)
        {
            var items = new Stack<object>();
            items.Push(parent);
            while (items.Count != 0)
            {
                parent = items.Pop();
                var projectItem = parent as ProjectItem;
                ProjectItems projectItems = null;
                if (projectItem == null)
                {
                    var project = parent as Project;
                    if (project != null)
                    {
                        projectItems = project.ProjectItems;
                    }
                }
                else
                {
                    if (projectItem == null || projectItem.Name.EndsWith(".tt") && (includett == null || !includett(projectItem)))
                    {
                        continue;
                    }
                    yield return projectItem;
                    projectItems = projectItem.ProjectItems;
                    if (projectItem.SubProject != null)
                    {
                        items.Push(projectItem.SubProject);
                    }
                }
                foreach (var pi in projectItems)
                {
                    items.Push(pi);
                }
            }
        }

        public static string GetNetVersion(this Project project)
        {
            switch (Regex.Match((string)project.Properties.Cast<Property>().First(p => p.Name == "TargetFrameworkMoniker").Value, @"Version=v(\d.\d)").Groups[1].Value)
            {
                case "4.0":
                    return "NET40";
                case "4.5":
                    return "NET45";
                case "4.6":
                default:
                    return "NET46";
            }
        }

        public static IEnumerable<UIHierarchyItem> GetProjectsUIHierarchyItems(this DTE dte)
        {
            var uiHierarchyObject = dte.Windows.Cast<Window>().First(w => w.Type == vsWindowType.vsWindowTypeSolutionExplorer).Object;
            var uiHierarchy = uiHierarchyObject as UIHierarchy;
            if (uiHierarchy == null)
            {
                throw new InvalidOperationException();
            }
            var uiHierarchyItems = new Stack<UIHierarchyItem>(uiHierarchy.UIHierarchyItems.Cast<UIHierarchyItem>());
            while (uiHierarchyItems.Count != 0)
            {
                var uiHierarchyItem = uiHierarchyItems.Pop();
                if (((string)((dynamic)uiHierarchyItem.Object).FullName).EndsWith(".csproj"))
                    yield return uiHierarchyItem;
                else
                {
                    if (!(uiHierarchyItem.Name == ".nuget" || uiHierarchyItem.Name == "Solution Items" || uiHierarchyItem.UIHierarchyItems == null))
                        foreach (UIHierarchyItem uihi in uiHierarchyItem.UIHierarchyItems)
                            uiHierarchyItems.Push(uihi);
                }
            }
        }

        public static string GetFirstCsFilePath(this ProjectItem parent)
        {
            return parent.ProjectItems.Cast<ProjectItem>().FirstOrDefault(pi => pi.Name.EndsWith(".cs"))?.GetFilePath();
        }

        public static string GetFilePath(this ProjectItem projectItem)
        {
            var value = (string)projectItem.Properties.Cast<Property>().First(p => p.Name == "LocalPath").Value;
            if (value.EndsWith("\\"))
            {
                value = value.Substring(0, value.Length - 1);
            }
            return value;
        }

        public static string GetDisplayPath(this ProjectItem projectItem)
        {
            return GetDisplayPath((object)projectItem);
        }

        public static string GetDisplayPath(this Project project)
        {
            return GetDisplayPath((object)project);
        }

        private static string GetDisplayPath(object item)
        {
            var sb = new StringBuilder();
            while (item != null)
            {
                var itemAsProjectItem = item as ProjectItem;
                if (itemAsProjectItem == null)
                {
                    var itemAsProject = (Project)item;
                    sb.Insert(0, itemAsProject.Name);
                    item = itemAsProject.ParentProjectItem;
                    if (item != null)
                    {
                        sb.Insert(0, @"\");
                    }
                }
                else
                {
                    sb.Insert(0, itemAsProjectItem.Name);
                    sb.Insert(0, @"\");
                    item = itemAsProjectItem.Collection.Parent;
                }
            }
            return sb.ToString();
        }

        public static string GetDefaultNamespace(this Project project)
        {
            return (string)project.Properties.Cast<Property>().First(p => p.Name == "RootNamespace").Value;
        }

        public static string GetTargetFrameworkMoniker(this Project project)
        {
            return (string)project.Properties.Cast<Property>().First(p => p.Name == "TargetFrameworkMoniker").Value;
        }

        public static void RecursiveT4RunCustomTool(this ProjectItem item, Action<ProjectItem, Exception> error, Action<ProjectItem> generated)
        {
            if (item != null)
            {
                try
                {
                    ((VSProjectItem)item.Object).RunCustomTool();
                    generated(item);
                }
                catch (Exception e)
                {
                    error(item, e);
                }
            }
            foreach (var subItem in item.ProjectItems.Cast<ProjectItem>().Where(pi => pi.Name.EndsWith(".tt")))
            {
                RecursiveT4RunCustomTool(subItem, error, generated);
            }
        }
        public static async Task RecursiveT4RunCustomToolAsync(this ProjectItem item, Action<ProjectItem, Exception> error, Action<ProjectItem> generated)
        {
            if (item != null)
            {
                try
                {
                    await Task.Delay(1);
                    ((VSProjectItem)item.Object).RunCustomTool();
                    generated(item);
                }
                catch (Exception e)
                {
                    error(item, e);
                }
            }
            foreach (var subItem in item.ProjectItems.Cast<ProjectItem>().Where(pi => pi.Name.EndsWith(".tt")))
            {
                await RecursiveT4RunCustomToolAsync(subItem, error, generated);
            }
        }

        public static IEnumerable<FilePathes> GetSolutionEdmx(this DTE dte, Project defaultProject, bool skipWaqsAlreadyUsed = true)
        {
            return GetSolutionFiles(dte, defaultProject, ".edmx", skipWaqsAlreadyUsed);
        }

        public static IEnumerable<FilePathes> GetSolutionSvc(this DTE dte, Project defaultProject, bool skipWaqsAlreadyUsed = true)
        {
            return GetSolutionFiles(dte, defaultProject, ".svc", skipWaqsAlreadyUsed);
        }

        public static IEnumerable<FilePathes> GetSolutionXaml(this DTE dte, Project defaultProject, bool skipWaqsAlreadyUsed = true)
        {
            return GetSolutionFiles(dte, defaultProject, ".xaml", skipWaqsAlreadyUsed);
        }

        private static IEnumerable<FilePathes> GetSolutionFiles(this DTE dte, Project defaultProject, string extension, bool skipWaqsAlreadyUsed)
        {
            return (from f in dte.GetProjects().SelectMany(p => p.GetAllProjectItems()).Where(pi => pi.Name.EndsWith(extension))
                    let path = f.GetFilePath()
                    let projectDirectory = Path.GetDirectoryName(defaultProject.FullName)
                    where ! (skipWaqsAlreadyUsed && Directory.Exists(Path.Combine(projectDirectory, "WAQS." + Path.GetFileNameWithoutExtension(path))))
                    orderby path.StartsWith(projectDirectory + "\\") descending, path
                    select new FilePathes { FullPath = path, DisplayPath = f.GetDisplayPath() });
        }

        public class FilePathes
        {
            public string FullPath { get; set; }
            public string DisplayPath { get; set; }
        }
    }
}
