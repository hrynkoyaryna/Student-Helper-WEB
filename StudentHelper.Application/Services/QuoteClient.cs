using System.Net.Http.Json;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;

namespace StudentHelper.Application.Services;

public class QuoteClient : IQuoteClient
{
    private readonly HttpClient _http;

    public QuoteClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<QuoteResponse?> GetRandomAsync()
    {
        try
        {
            // Using quotable.io public API
            var resp = await _http.GetFromJsonAsync<QuotableDto>("/random");
            if (resp == null) return null;
            return new QuoteResponse { Content = resp.Content, Author = resp.Author };
        }
        catch (HttpRequestException)
        {
            // Network issue or DNS failure - return null to let caller handle gracefully
            return null;
        }
        catch (TaskCanceledException)
        {
            // Timeout
            return null;
        }
        catch
        {
            // Any other error - do not throw to the UI
            return null;
        }
    }

    private class QuotableDto
    {
        public string? Content { get; set; }
        public string? Author { get; set; }
    }
}
