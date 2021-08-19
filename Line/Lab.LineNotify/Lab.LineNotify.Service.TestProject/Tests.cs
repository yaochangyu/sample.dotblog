using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.LineNotify.Service.TestProject
{
    [TestClass]
    public class Tests
    {
        private readonly Dictionary<string, Func<string, int?, object>> _pool;

        public Tests()
        {
            this._pool = new Dictionary<string, Func<string, int?, object>>();
            this._pool.Add("info",   this.GetInfo);
            this._pool.Add("status", this.GetStatus);
        }

        [TestMethod]
        public void GetInfo()
        {
            var key      = "info";
            var response = this.Get<InfoResponse>(key, "yao", 18);
        }

        [TestMethod]
        public void GetStatus()
        {
            var key      = "status";
            var response = this.Get<StatusResponse>(key, "192.168.1.1", 1024);
        }

        private TResponse Get<TResponse>(string key, string p1, int? p2)
        {
            if (this._pool.ContainsKey(key) == false)
            {
                return default;
            }

            var func = this._pool[key];
            return (TResponse) func.Invoke(p1, p2);
        }

        private InfoResponse GetInfo(string p1, int? p2)
        {
            return new InfoResponse
            {
                Name = p1,
                Age  = p2
            };
        }
        private InfoResponse GetInfo1(string p1, int? p2)
        {
            return new InfoResponse
            {
                Name = p1,
            };
        }
        private StatusResponse GetStatus(string p1, int? p2)
        {
            return new StatusResponse
            {
                Code      = p2.Value,
                IpAddress = p1
            };
        }
    }

    internal class Content
    {
        public string TypeName { get; set; }
    }

    internal class StatusResponse
    {
        public int Code { get; set; }

        public string IpAddress { get; set; }
    }

    internal class StatusRequest
    {
        public int Code { get; set; }
    }

    internal class InfoRequest
    {
        public string Name { get; set; }
    }

    internal class InfoResponse
    {
        public string Name { get; set; }

        public int? Age { get; set; }
    }
}