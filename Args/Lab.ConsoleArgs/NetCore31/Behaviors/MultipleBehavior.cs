using System;
using NetCore31.ViewModel;
using Newtonsoft.Json;
using PowerArgs;

namespace NetCore31.Behaviors
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class MultipleBehavior
    {
        [HelpHook]
        [ArgShortcut("-?")]
        [ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgActionMethod]
        [ArgDescription("複製")]
        [ArgExample(@"Net48 Copy D:\Source D:\Target *.txt", "省略參數，需按照順序")]
        [ArgExample(@"Net48 Copy /SourceFolder:D:\Source /TargetFolder:D:\Target /SearchPattern:*.txt",
                    "完整參數或是縮寫參數，不需要按照順序")]
        public void Copy(CopyFileRequest args)
        {
            Console.WriteLine($"Copy - {JsonConvert.SerializeObject(args)}");
        }

        [ArgActionMethod]
        [ArgDescription("刪除")]
        [ArgExample(@"Net48 Delete D:\Target *.txt", "省略參數，需按照順序")]
        [ArgExample(@"Net48 Delete /TargetFolder:D:\Target /SearchPattern:*.txt",
                    "完整參數或是縮寫參數，不需要按照順序")]
        public void Delete(DeleteFileRequest args)
        {
            Console.WriteLine($"Delete - {JsonConvert.SerializeObject(args)}");
        }
    }
}