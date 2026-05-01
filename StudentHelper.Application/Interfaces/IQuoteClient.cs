using System.Threading.Tasks;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Interfaces;

public interface IQuoteClient
{
    /// <summary>
    /// Calls external API to get a random quote.
    /// </summary>
    Task<QuoteResponse?> GetRandomAsync();
}
