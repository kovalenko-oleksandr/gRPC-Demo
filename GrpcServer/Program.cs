using System;
using System.IO;
using System.Threading.Tasks;
using Grpc.Contracts;
using Grpc.Core;
using Grpc.Core.Logging;

namespace GrpcService.ServerApp
{
    class RandomMarketDataService: MarketData.MarketDataBase
    {
        private readonly Random _rnd = new Random();

        private Quote GetRandomQuote()
        {
            var bid = 15 - _rnd.NextDouble();
            var ask = 15 + _rnd.NextDouble();
            return new Quote { Bid = bid, Ask = ask };
        }

        // Server side handler of the GetQuote
        public override async Task<Quote> GetQuote(CurrencyPair ccyPair, ServerCallContext context)
        {
            const string nof = nameof(GetQuote);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken); // Gotta look busy

                var quote = GetRandomQuote();
                Console.WriteLine($"{nof} - returning the quote for {ccyPair.From}/{ccyPair.To}: Bid: {quote.Bid}, Ask: {quote.Ask}");
                return quote;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"{nof} - task cancelled.");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{nof} failed. {e}");
                throw;
            }
        }

        #region Stream RPC

        public override async Task GetQuotes(CurrencyPair ccyPair, IServerStreamWriter<Quote> quotesStream, ServerCallContext context)
        {
            const string nof = nameof(GetQuotes);
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500, context.CancellationToken); // Gotta look busy

                    var quote = GetRandomQuote();

                    Console.WriteLine($"{nof} - returning the quote ticks for {ccyPair.From}/{ccyPair.To}: Bid: {quote.Bid}, Ask: {quote.Ask}");

                    await quotesStream.WriteAsync(quote);
                }

                Console.WriteLine($"{nof} - cancelled by client.");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"{nof} - task cancelled.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{nof} failed. {e}");
            }
        }

        #endregion
    }

    class Program
    {
        const int Port = 30051;

        public static void Main(string[] args)
        {
            //Environment.SetEnvironmentVariable("GRPC_TRACE", "all");
            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "debug");
            GrpcEnvironment.SetLogger(new ConsoleLogger());

            //https://stackoverflow.com/questions/37714558/how-to-enable-server-side-ssl-for-grpc
            var cacert = File.ReadAllText(@"C:\Users\ABOK078\Desktop\gRPC presentation\Grpc-Demo\cert\ca.crt");
            var servercert = File.ReadAllText(@"C:\Users\ABOK078\Desktop\gRPC presentation\Grpc-Demo\cert\server.crt");
            var serverkey = File.ReadAllText(@"C:\Users\ABOK078\Desktop\gRPC presentation\Grpc-Demo\cert\server.key");
            var keypair = new KeyCertificatePair(servercert, serverkey);

            var credentials = new SslServerCredentials(new[] { keypair }, cacert, true);

            Server server = new Server
            {
                Services = { MarketData.BindService(new RandomMarketDataService()) },
                //Ports = { new ServerPort("0.0.0.0", Port, ServerCredentials.Insecure) }
                Ports = { new ServerPort("0.0.0.0", Port, credentials) }
            };
            server.Start();

            Console.WriteLine("MarketData server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }


    }
}
