using System;

namespace Lab.NetRemoting.Core
{
    [Serializable]
    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public string Gender { get; set; }
    }
}