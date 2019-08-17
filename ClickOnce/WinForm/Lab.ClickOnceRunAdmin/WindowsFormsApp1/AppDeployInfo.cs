using System;

namespace WindowsFormsApp1
{
    public class AppDeployInfo
    {
        public string PublisherName { get; set; }

        public string ProductName { get; set; }

        public string DesktopShortcutPath { get; set; }

        public string StartMenuShortcutPath { get; set; }

        public Version UpdateVersion { get; set; }

        public string Name { get; set; }

        public string Language { get; set; }

        public string ActivationLocation { get; set; }

        public string Arguments { get; set; }

        public Version CurrentVersion { get; set; }

        public bool HasUpdateVersion { get; set; }

        public string UpdatedApplicationFullName { get; set; }

        public Uri UpdateLocation { get; set; }
    }
}