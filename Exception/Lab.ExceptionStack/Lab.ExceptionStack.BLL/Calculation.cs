using System;

namespace Lab.ExceptionStack.BLL
{
    public class Calculation
    {
        [ErrorDescription("加法錯誤")]
        public decimal Add(decimal firstNumber, decimal secondNumber)
        {
            throw new Exception("Fail");
        }

        [ErrorDescription("減法錯誤")]
        public decimal Sub(decimal firstNumber, decimal secondNumber)
        {
            throw new Exception("Fail");
        }
    }
}