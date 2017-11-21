using System;

namespace Sample.BO.V0
{
    public class Calculation
    {
        public int Execute(int first, int second)
        {
            var result = 0;

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