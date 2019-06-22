using System;

namespace Lab.ExceptionStack.BLL
{
    public class Calculation
    {
        [ErrorDescription("加法")]
        public decimal Add(decimal firstNumber, decimal secondNumber)
        {
            return firstNumber + secondNumber;
        }
        [ErrorDescription("減法")]

        public decimal Sub(decimal firstNumber, decimal secondNumber)
        {
            throw new Exception("Fail");
            //return firstNumber - secondNumber;
        }
    }
}