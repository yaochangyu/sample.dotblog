using System;
using System.Runtime.Serialization;

namespace Lab.LineBot.SDK
{
    [Serializable]
    public class LineNotifyProviderException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public LineNotifyProviderException()
        {
        }

        public LineNotifyProviderException(string message) : base(message)
        {
        }

        public LineNotifyProviderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected LineNotifyProviderException(
            SerializationInfo info,
            StreamingContext  context) : base(info, context)
        {
        }
    }
}