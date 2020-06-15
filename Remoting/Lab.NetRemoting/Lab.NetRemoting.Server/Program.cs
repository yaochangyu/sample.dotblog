using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Lab.NetRemoting.Core;
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

                ////RemotingConfiguration.RegisterWellKnownServiceType + WellKnownObjectMode
                ////=================================================================================
                //var tcpChannel = new TcpChannel(port);
                //ChannelServices.RegisterChannel(tcpChannel, false);

                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(TrMessage),
                //                                                   "RemotingTest",
                //                                                   WellKnownObjectMode.Singleton);
                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(TrMessageFactory),
                //                                                   "RemotingTest",
                //                                                   WellKnownObjectMode.Singleton);
                ////RemotingConfiguration.ApplicationName = "RemotingTest";
                ////RemotingConfiguration.RegisterActivatedServiceType(typeof(TrMessage));
                //Console.WriteLine($"{DateTime.Now}, Server 已啟動");
                //Console.WriteLine("按任意鍵離開!");
                //Console.ReadLine();
                ////=================================================================================
                
                

                //註冊多個通道
                //=================================================================================
                IDictionary channelDefines = new Hashtable()
                {
                    {"name","tcp9527"},
                    {"port",9527},
                };
                channelDefines["name"] = "tcp9527";
                channelDefines["port"] = 9527;
                var channel = new TcpChannel(channelDefines,
                                             new BinaryClientFormatterSinkProvider(),
                                             new BinaryServerFormatterSinkProvider());
                ChannelServices.RegisterChannel(channel);

                RemotingConfiguration.RegisterWellKnownServiceType(typeof(TrMessageFactory),
                                                                   "RemotingTest",
                                                                   WellKnownObjectMode.Singleton);

                Console.WriteLine($"{DateTime.Now}, Server 已啟動");
                Console.WriteLine("按任意鍵離開!");
                Console.ReadLine();
                //=================================================================================


                ////RemotingServices.Marshal，同等 Singleton 模式
                ////=================================================================================
                //var tcpChannel = new TcpChannel(port);
                //ChannelServices.RegisterChannel(tcpChannel, false);
                //var message = new TrMessageFactory();
                //var objRef  = RemotingServices.Marshal(message, "RemotingTest");

                //Console.WriteLine($"{DateTime.Now}, Server 已啟動");
                //Console.WriteLine("按任意鍵離開!");
                //Console.ReadLine();

                //RemotingServices.Disconnect(message);

                //CloseChannel();
                ////=================================================================================

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