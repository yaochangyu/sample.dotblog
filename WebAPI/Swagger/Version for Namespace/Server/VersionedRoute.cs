using System;

namespace Server
{
    [AttributeUsage(AttributeTargets.All)]
    public class VersionedRoute : Attribute
    {
        public VersionedRoute(string name, int version)
        {
            this.Name = name;
            this.Version = version;
        }

        public string Name { get; set; }

        public int Version { get; set; }
    }
}