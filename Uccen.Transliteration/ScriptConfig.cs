namespace Uccen.Transliteration;
public class ScriptConfig
{
    public string? Direction { get; set; }
    public char Geminizer { get; set; }
    public char NullVowel { get; set; }
    public Dictionary<char, string> Mapping { get; set; } = new Dictionary<char, string>();    
    public ScriptType ScriptType { get; set; }
}

public enum ScriptType
{
    Alphabet,
    Abjad,
}