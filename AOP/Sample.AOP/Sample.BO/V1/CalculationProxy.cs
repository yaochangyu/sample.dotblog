using System;
using System.Threading;

namespace Sample.BO.V1
{
    public class CalculationProxy : ICalculation
    {
        private readonly ICalculation _calculation;

        public CalculationProxy()
        {
            if (this._calculation == null)
            {
                this._calculation = new Calculation();
            }
        }

        public int Execute(int first, int second)
        {
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
                return this._calculation.Execute(first, second);
            }
            catch (Exception e)
            {
                this.CachException(e);
                throw;
            }
        }

        public int Execute(int first, int second, int third)
        {
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
                return this._calculation.Execute(first, second);
            }
            catch (Exception e)
            {
                this.CachException(e);
                throw;
            }
        }

        private void CachException(Exception e)
        {
            var msg = $"{e.Message},{e.StackTrace}";

            //寫例外log
            Console.WriteLine(msg);
        }
    }
}