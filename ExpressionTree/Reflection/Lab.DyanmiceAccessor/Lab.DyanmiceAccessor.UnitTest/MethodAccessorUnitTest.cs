using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DynamicAccessor.UnitTest
{
    [TestClass]
    public class MethodAccessorUnitTest
    {
        [TestMethod]
        public void 執行Sum方法()
        {
            var instance = new MyClass();
            var accessor = new MethodAccessor();
            var methodInfo = instance.GetType().GetMethod("Sum");
            var result   = accessor.Execute(instance, methodInfo, 1, 1);
        }

        private class MyClass
        {
            public int Sum(int p1, int p2)
            {
                return p1 + p2;
            }
        }
    }
}