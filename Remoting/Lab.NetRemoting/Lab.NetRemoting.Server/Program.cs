using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Lab.NetRemoting.Implement;

namespace Lab.NetRemoting.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var tcpChannel = new TcpChannel(9527);
                ChannelServices.RegisterChannel(tcpChannel, false);
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(TrMessage)
                                                                 , "RemotingTest", WellKnownObjectMode.Singleton);
                Console.WriteLine("按任意鍵離開!");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}