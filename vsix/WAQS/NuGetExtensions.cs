using EnvDTE;
using NuGet;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WAQS.NuGetOData;

namespace WAQS
{
    public static class NuGetExtensions
    {
        public static void InstallPackage(this IVsPackageInstaller packageInstaller, Project project, string packageName, IVsPackageInstallerServices packageInstallerServices, string version = null)
        {
            if (version == null)
            {
                version = new V2FeedContext(new Uri("http://www.nuget.org/api/v2/")).Execute<V2FeedPackage>(new Uri("http://www.nuget.org/api/v2/Packages?$filter=IsAbsoluteLatestVersion and Id eq '" + packageName + "'&$skip=0&$top=1&$select=Id,Version&targetFramework=&includePrerelease=true")).Single().Version;
            }
            packageInstaller.InstallPackage("http://packages.nuget.org", project, packageName, version, false);
            //workaround to the bug on Package installer that does not install package depencences
            var dependencies = new LocalPackageRepository(packageInstallerServices.GetPackageLocation(packageName)).FindPackage(packageName).DependencySets.SelectMany(ds => ds.Dependencies);
            foreach (var dependency in dependencies)
            {
                InstallPackage(packageInstaller, project, dependency.Id, packageInstallerServices, dependency.VersionSpec.ToString());
            }
        }
        public static string GetPackageLocation(this IVsPackageInstallerServices packageInstallerServices, string packageName)
        {
            return packageInstallerServices.GetInstalledPackages().Where(p => p.Id == packageName).OrderByDescending(p => p.VersionString).First().InstallPath;
        }
    }
}
