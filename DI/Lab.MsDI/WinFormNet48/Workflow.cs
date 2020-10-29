using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormNet48.Operations;

namespace WinFormNet48
{
    public class Workflow
    {
        public Workflow(IMessager operation)
        {
           Console.WriteLine(operation.OperationId); 
        }
    }
}
