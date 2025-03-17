using Supabase;

namespace UrlShortener.Services;

public class SupabaseService
{
    public Client Client { get; }

    public SupabaseService(Client client)
    {
        Client = client;
    }
}