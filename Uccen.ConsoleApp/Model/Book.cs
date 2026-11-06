using System.Text.Json.Serialization;

public class Book
{
    [JsonIgnore]
    public string? CoverTitle { get; set; }
    [JsonIgnore]
    public string? CoverSvgImage { get; set; }

    public List<Tale> Tales { get; set; } = new List<Tale>();
}
