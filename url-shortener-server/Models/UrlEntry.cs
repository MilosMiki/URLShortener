using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace UrlShortener.Models;

[Table("url_entries")]
public class UrlEntry : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string? Id { get; set; }

    [Column("full_url")]
    public string? FullUrl { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }    
    
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("access_count")]
    public int AccessCount { get; set; }
}