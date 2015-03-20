// Guids.cs
// MUST match guids.h
using System;

namespace WAQS
{
    static class GuidList
    {
        public const string guidWAQSPkgString = "5ad58966-3cff-4af7-9009-15e13cfb963a";
        public const string guidWAQSCmdSetString = "9ac3e3e3-c740-4fdb-8619-1965158e2ee6";

        public static readonly Guid guidWAQSCmdSet = new Guid(guidWAQSCmdSetString);
    };
}