using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAQS
{
    public static class PackageInstallerServicesExtensions
    {
        public static string GetPackageLocation(this IVsPackageInstallerServices packageInstallerServices, string packageName)
        {
            return packageInstallerServices.GetInstalledPackages().Where(p => p.Id == packageName).OrderByDescending(p => p.VersionString).First().InstallPath;
        }
    }
}
