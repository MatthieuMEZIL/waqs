using EnvDTE;
using Microsoft.Win32;
using System;

namespace WAQS
{
    public static class GenerationOptions
    {
        [Flags]
        public enum Kind
        {
            FrameworkOnly = 1,
            WithoutGlobalWithoutFramework = 2,
            GlobalOnly = 4,
            All = 7
        }
        private const string All = "All";
        public const string FrameworkOnly = "FrameworkOnly";
        private const string WithoutGlobalWithoutFramework = "WithoutFrameworkWithoutGlobal";
        private const string GlobalOnly = "GlobalOnly";

        public static KindViewModel GetViewModel(Kind kind)
        {
            switch (kind)
            {
                case Kind.All:
                    return new KindViewModel { Kind = kind, Key = All, Display = Resources.All };
                case Kind.FrameworkOnly:
                    return new KindViewModel { Kind = kind, Key = FrameworkOnly, Display = Resources.FrameworkOnly};
                case Kind.GlobalOnly:
                    return new KindViewModel { Kind = kind, Key = GlobalOnly, Display = Resources.GlobalOnly };
                case Kind.WithoutGlobalWithoutFramework:
                    return new KindViewModel { Kind = kind, Key = WithoutGlobalWithoutFramework, Display = Resources.WithoutGlobalWithoutFramework };
                default:
                    throw new NotImplementedException();
            }
        }

        public class KindViewModel
        {
            public Kind Kind { get; set; }
            public string Key { get; set; }
            public string Display { get; set; }

            public override string ToString()
            {
                return Display;
            }
        }

        public const string WPF = "WPF";
        public const string PCL = "PCL";

        public static bool CanBeRunnedWithNoCopy(DTE dte)
        {
            string keyName = @"Software\Microsoft\VisualStudio\" + dte.Version + @"_Config\TextTemplating\IncludeFolders\.tt";
            try
            {
                return !string.IsNullOrEmpty((string)Registry.CurrentUser.OpenSubKey(keyName, false).GetValue("Include18111981-0AEE-0AEE-0AEE-181119810AEE"));
            }
            catch
            {
                return false;
            }
        }
    }
}
