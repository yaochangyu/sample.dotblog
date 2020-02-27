using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hangfire.Dashboard.Management.Metadata;

namespace Lab.HangfireJob
{
    public class EncodingsInputDataList : IInputDataList
    {
        public Dictionary<string, string> GetData()
        {
            return Encoding.GetEncodings()
                           .GroupBy(f => f.Name, (f1, f2) => f2.FirstOrDefault())
                           .ToDictionary(f => f.Name, f => f.DisplayName);
        }

        public string GetDefaultValue()
        {
            return null;
        }
    }
}