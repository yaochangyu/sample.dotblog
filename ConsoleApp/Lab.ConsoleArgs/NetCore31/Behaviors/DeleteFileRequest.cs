using PowerArgs;

namespace NetCore31.Behaviors
{
    public class DeleteFileRequest
    {
        public static string MethodName = "DeleteAll";

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("目標路徑")]
        [ArgPosition(1)]
        [ArgShortcut("-T")]
        public string TargetFolder { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("搜尋模式")]
        [ArgPosition(2)]
        [ArgShortcut("-SP")]
        public string SearchPattern { get; set; }
    }
}