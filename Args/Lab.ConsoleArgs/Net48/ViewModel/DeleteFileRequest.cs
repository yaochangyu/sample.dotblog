using PowerArgs;

namespace Net48.ViewModel
{
    public class DeleteFileRequest
    {
        public static string MethodName = "Delete";

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("目標路徑")]
        [ArgPosition(1)]
        [ArgShortcut("-D")]
        public string DestinationFolder { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("搜尋模式")]
        [ArgPosition(2)]
        [ArgShortcut("-SP")]
        public string SearchPattern { get; set; }
    }
}