using System;
using System.Threading.Tasks;
using Grpc.Contracts;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GrpcServer.Core
{
    class RandomMarketDataService : MarketData.MarketDataBase
    {
        private readonly Random _rnd = new Random();

        private readonly ILogger<RandomMarketDataService> _logger;
        public RandomMarketDataService(ILogger<RandomMarketDataService> logger)
        {
            _logger = logger;
        }

        private Quote GetRandomQuote()
        {
            var bid = 15 - _rnd.NextDouble();
            var ask = 15 + _rnd.NextDouble();
            return new Quote { Bid = bid, Ask = ask };
        }

        // Server side handler of the GetQuote
        public override async Task<Quote> GetQuote(CurrencyPair ccyPair, ServerCallContext context)
        {
            await Task.Delay(50); // Gotta look busy

            var quote = GetRandomQuote();
            _logger.LogInformation($"{nameof(GetQuote)} - returning the quote for {ccyPair.From}/{ccyPair.To}: Bid: {quote.Bid}, Ask: {quote.Ask}");
            return quote;
        }

        public override async Task GetQuotes(CurrencyPair ccyPair, IServerStreamWriter<Quote> quotesStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500); // Gotta look busy

                    var quote = GetRandomQuote();

                    _logger.LogInformation($"{nameof(GetQuotes)} - returning the quote ticks for {ccyPair.From}/{ccyPair.To}: Bid: {quote.Bid}, Ask: {quote.Ask}");

                    await quotesStream.WriteAsync(quote);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{nameof(GetQuotes)} failed.");
            }
        }
    }
}
