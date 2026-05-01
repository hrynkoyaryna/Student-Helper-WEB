using Microsoft.AspNetCore.Mvc;
using StudentHelper.Application.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StudentHelper.Application.Models;

namespace StudentHelper.Web.Controllers
{
    public class QuoteController : Controller
    {
        private readonly IQuoteClient _quoteClient;
        private readonly ILogger<QuoteController> _logger;

        public QuoteController(IQuoteClient quoteClient, ILogger<QuoteController> logger)
        {
            _quoteClient = quoteClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var quote = await _quoteClient.GetRandomAsync();
                return View(quote);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to load quote");
                return View((QuoteResponse?)null);
            }
        }
    }
}
