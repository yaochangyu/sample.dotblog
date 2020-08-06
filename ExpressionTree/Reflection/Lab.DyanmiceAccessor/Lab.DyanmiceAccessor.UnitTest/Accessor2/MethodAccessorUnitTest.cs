using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DynamicAccessor.UnitTest.Accessor2
{
    [TestClass]
    public class MethodAccessorUnitTest
    {
        [TestMethod]
        public void 執行Sum方法()
        {
            var instance = new MyClass();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var methodInfo = instance.GetType().GetMethod("Sum",flags);
            var accessor = new DynamicAccessor.Accessor2.MethodAccessor(methodInfo);
            var result   = accessor.Execute(instance,  1, 1);
            Assert.AreEqual(2, result);
        }

        private class MyClass
        {
            private int Sum(int p1, int p2)
            {
                return p1 + p2;
            }
        }
    }
}