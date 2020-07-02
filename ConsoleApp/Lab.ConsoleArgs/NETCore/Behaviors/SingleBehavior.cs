using System;
using PowerArgs;

namespace NETCore.Behaviors
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class SingleBehavior
    {
        [HelpHook]
        [ArgShortcut("-?")]
        [ArgDescription("Shows this help")]
        [ArgExample("example text", "Example description")]
        public bool Help { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("來源路徑")]
        [ArgShortcut("-S")]
        [ArgPosition(1)]
        [ArgExample("example text", "Example description")]
        public string SourceFolder { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("目標路徑")]
        [ArgPosition(2)]
        [ArgShortcut("-T")]
        public string TargetFolder { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("搜尋模式")]
        [ArgPosition(3)]
        [ArgShortcut("-SP")]
        public string SearchPattern { get; set; }

        public void Main()
        {
            Console.WriteLine($"SourceFolder: '{this.SourceFolder}' and TargetFolder '{this.TargetFolder}'");
        }
    }
}