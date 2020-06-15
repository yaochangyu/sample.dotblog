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
                var port = 9527;
               
                var tcpChannel = new TcpChannel(port);
                ChannelServices.RegisterChannel(tcpChannel, false);
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(TrMessage)
                                                                 , "RemotingTest", WellKnownObjectMode.Singleton);

                RemotingConfiguration.ApplicationName = "RemotingTest";
                RemotingConfiguration.RegisterActivatedServiceType(typeof(TrMessage));

                Console.WriteLine($"{DateTime.Now}, Server 已啟動");
                Console.WriteLine("按任意鍵離開!");
                Console.ReadLine();
                CloseChannel();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void CloseChannel()
        {
            var channels = ChannelServices.RegisteredChannels;

            foreach (var channel in channels)
            {
                if (channel.ChannelName == "tcp")
                {
                    var tcpChannel = (TcpChannel) channel;
                    tcpChannel.StopListening(null);
                    ChannelServices.UnregisterChannel(tcpChannel);
                }
            }
        }
    }
}