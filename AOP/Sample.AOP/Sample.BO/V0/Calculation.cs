using System;
using System.Threading;

namespace Sample.BO.V0
{
    public class Calculation
    {
        public int Execute(int first, int second)
        {
            var result = 0;
            var principal = Thread.CurrentPrincipal;
            if (!principal.Identity.IsAuthenticated)
            {
                throw new Exception("沒有通過驗證");
            }

            if (!principal.IsInRole("Admin"))
            {
                throw new Exception("沒有在Admin群裡");
            }

            try
            {
                throw new Exception("喔喔，出錯了");
            }
            catch (Exception e)
            {
                //寫Log
                Console.WriteLine($"{e.Message},{e.StackTrace}");
                throw;
            }
        }
    }
}