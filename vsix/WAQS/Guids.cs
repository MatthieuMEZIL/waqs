// Guids.cs
// MUST match guids.h
using System;

namespace WAQS
{
    static class GuidList
    {
        public const string guidWAQSPkgString = "5ad58966-3cff-4af7-9009-15e13cfb963a";
        public const string guidWAQSProjectCmdSetString = "9ac3e3e3-c740-4fdb-8619-1965158e2ee6";
        public const string guidWAQSFileCmdSetString = "9ac3e3e3-c740-4fdb-8619-1965158e2ee7";

        public static readonly Guid guidWAQSProjectCmdSet = new Guid(guidWAQSProjectCmdSetString);
        public static readonly Guid guidWAQSFileCmdSet = new Guid(guidWAQSFileCmdSetString);
    };
}