namespace Tfs.WebHook
{

    public class TeamsRootObject
    {
        public string context { get; set; }
        public string type { get; set; }
        public string themeColor { get; set; }
        public string title { get; set; }
        public string text { get; set; }
        public Potentialaction[] potentialAction { get; set; }
    }

    public class Potentialaction
    {
        public string type { get; set; }
        public string name { get; set; }
        public Input[] inputs { get; set; }
        public Action[] actions { get; set; }
        public Target[] targets { get; set; }
    }

    public class Input
    {
        public string type { get; set; }
        public string id { get; set; }
        public bool isMultiline { get; set; }
        public string title { get; set; }
    }

    public class Action
    {
        public string type { get; set; }
        public string name { get; set; }
        public bool isPrimary { get; set; }
        public string target { get; set; }
    }

    public class Target
    {
        public string os { get; set; }
        public string uri { get; set; }
    }

}