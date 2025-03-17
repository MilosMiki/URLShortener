using Supabase;
using UrlShortener.Models;

namespace UrlShortener.Utilities;

public class CodeGenerator
{
    private readonly Client _supabaseClient;

    public CodeGenerator(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task<string> GenerateShortCode()
    {
        string code;
        do
        {
            code = Guid.NewGuid().ToString("N").Substring(0, 6);
        } while (await CheckIfExistsInDB(code));

        return code;
    }

    private async Task<bool> CheckIfExistsInDB(string code)
    {
        var existingEntry = await _supabaseClient
            .From<UrlEntry>()
            .Where(x => x.Id == code) // 'Id' stores the shortcode
            .Single();

        return existingEntry != null;
    }
}
