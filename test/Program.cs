using System;
using Ibt.Ortc.Api.Extensibility;
using System.Threading;

namespace test
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.Write("Enter your app key: ");
            var appKey = Console.ReadLine();
            Console.WriteLine();
            Console.WriteLine($"Using app key {appKey}");

            var api = new Ibt.Ortc.Api.Ortc();
            IOrtcFactory factory = api.LoadOrtcFactory("");
            var client = factory.CreateClient();
            client.ClusterUrl = "http://ortc-developers.realtime.co/server/2.1/";
            client.ConnectionMetadata = "this can be anything?";
            client.OnConnected += (object sender) => {
                Console.WriteLine("Connected to realtime.co");
                Console.WriteLine("Subscribing to testChannel");
                client.Subscribe(
                    "testChannel", 
                    true, 
                    (s, channel, message) => {
                    Console.WriteLine($"message recieved on testChannel: {message}");
                });
            };
            client.OnDisconnected += (object sender) => {
                Console.WriteLine("Disconnected from realtime.co");
            };

            client.OnException += (object sender, Exception ex) => {
                Console.WriteLine(ex);
            };

            client.Connect(appKey, "myToken");


            var waitHandle = new ManualResetEvent(false);
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => {
                waitHandle.Set();
            };
            waitHandle.WaitOne();
            Console.WriteLine("Done");
        }
    }
}
