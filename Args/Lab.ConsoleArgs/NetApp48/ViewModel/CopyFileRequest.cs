using PowerArgs;

namespace NetApp48.ViewModel
{
    public class CopyFileRequest
    {
        public static string MethodName = "Copy";

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("來源路徑")]
        [ArgShortcut("-S")]
        [ArgPosition(1)]
        public string SourceFolder { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("目標路徑")]
        [ArgPosition(2)]
        [ArgShortcut("-D")]
        public string DestinationFolder { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("搜尋模式")]
        [ArgPosition(3)]
        [ArgShortcut("-SP")]
        public string SearchPattern { get; set; }
    }
}