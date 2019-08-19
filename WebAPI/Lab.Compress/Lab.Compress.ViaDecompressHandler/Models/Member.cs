using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lab.Compress.ViaDecompressHandler.Models
{
    public class Member
    {
        public Guid Id { get; set; }

        public int Age { get; set; }

        public string Name { get; set; }

        public DateTime Birthday { get; set; }

        public string Address { get; set; }
    }
}