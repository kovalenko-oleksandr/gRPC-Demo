using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Contracts;
using Grpc.Core.Logging;

namespace GrpcClient
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            //Environment.SetEnvironmentVariable("GRPC_TRACE", "all");
            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "debug");
            GrpcEnvironment.SetLogger(new ConsoleLogger());

            //TODO: Configure channel:
            //https://github.com/grpc/grpc/blob/master/doc/keepalive.md
            //Channel channel = new Channel("localhost", 30051, ChannelCredentials.Insecure);
            //Channel channel = new Channel("<UAT SERVER HERE>", 30051, ChannelCredentials.Insecure);
            //Works with http port on AspNetCore grpc server
            //var channel = new Channel("localhost", 5001, ChannelCredentials.Insecure);

            //https://stackoverflow.com/questions/37714558/how-to-enable-server-side-ssl-for-grpc
            var cacert = File.ReadAllText(@"C:\Users\ABOK078\Desktop\gRPC presentation\Grpc-Demo\cert\ca.crt");
            var clientcert = File.ReadAllText(@"C:\Users\ABOK078\Desktop\gRPC presentation\Grpc-Demo\cert\client.crt");
            var clientkey = File.ReadAllText(@"C:\Users\ABOK078\Desktop\gRPC presentation\Grpc-Demo\cert\client.key");
            var credentials = new SslCredentials(cacert, new KeyCertificatePair(clientcert, clientkey));
            Channel channel = new Channel("localhost", 30051, credentials);
            var client = new MarketData.MarketDataClient(channel);

            await SimpleCall(client, 10);
            //wait SimpleCallWithDeadline(client);

            //var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            //await StreamCallWithCts(client, cts.Token);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            channel.ShutdownAsync().Wait();
        }

        #region Demo 1 - Simple call

        private static async Task SimpleCall(MarketData.MarketDataClient client, int n)
        {
            try
            {
                for (int i = 0; i < n; ++i)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    //var quote = client.GetQuote(new CurrencyPair { From = "USD", To = "ZAR" });
                    var quote = await client.GetQuoteAsync(new CurrencyPair { From = "USD", To = "ZAR" });
                    stopwatch.Stop();
                    Console.WriteLine($"{DateTime.Now} - Bid: {quote.Bid}, Ask: {quote.Ask}. Request time (ms): {stopwatch.ElapsedMilliseconds}");

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Simple call failed: {e}");
            }
        }

        #endregion

        #region Demo 2 - Deadline

        private static async Task SimpleCallWithDeadline(MarketData.MarketDataClient client)
        {
            try
            {
                var quote = await client.GetQuoteAsync(new CurrencyPair {From = "USD", To = "ZAR"}, deadline: DateTime.UtcNow.AddMilliseconds(500));
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                Console.WriteLine("Greeting timeout.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SimpleCall failed: {ex}");
            }
        }

        #endregion

        #region Demo 3 - CancellationToken

        private static async Task StreamCallWithCts(MarketData.MarketDataClient client, CancellationToken ct)
        {
            try
            {
                var qoutesStreamingCall = client.GetQuotes(new CurrencyPair { From = "USD", To = "ZAR" }, cancellationToken: ct);

                while (await qoutesStreamingCall.ResponseStream.MoveNext(ct))
                {
                    var quote = qoutesStreamingCall.ResponseStream.Current;
                    Console.WriteLine($"{DateTime.Now} - Bid: {quote.Bid}, Ask: {quote.Ask}.");
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("StreamCall was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StreamCall failed: {ex}");
            }
        }

        #endregion

    }
}
