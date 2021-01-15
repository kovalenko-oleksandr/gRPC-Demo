using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Contracts;
using Grpc.Core;

namespace GrpcClient.NetCore
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            using var channel = GrpcChannel.ForAddress("http://localhost:30051");
            var client = new MarketData.MarketDataClient(channel);
            try
            {
                for (int i = 0; i < 10; ++i)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    //var quote = client.GetQuote(new CurrencyPair { From = "USD", To = "ZAR" });
                    var quote = await client.GetQuoteAsync(new CurrencyPair { From = "USD", To = "ZAR" });
                    stopwatch.Stop();
                    Console.WriteLine($"{DateTime.Now} - Bid: {quote.Bid}, Ask: {quote.Ask}. Request time (ms): {stopwatch.ElapsedMilliseconds}");

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var qoutesStreamingCall = client.GetQuotes(new CurrencyPair { From = "USD", To = "ZAR" }, cancellationToken: cts.Token);
                await foreach(var quote in qoutesStreamingCall.ResponseStream.ReadAllAsync(cancellationToken: cts.Token))
                {
                    Console.WriteLine($"{DateTime.Now} - Bid: {quote.Bid}, Ask: {quote.Ask}.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
