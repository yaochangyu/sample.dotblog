using System;

namespace Server
{
    [AttributeUsage(AttributeTargets.All)]
    public class VersionRoute : Attribute
    {
        public VersionRoute(string name, int version)
        {
            this.Name = name;
            this.Version = version;
        }

        public string Name { get; set; }

        public int Version { get; set; }
    }
}