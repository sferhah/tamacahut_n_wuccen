namespace Uccen.Transliteration;

public class TransliterationService
{
    public static readonly Dictionary<string, Type> transliterators = new Dictionary<string, Type>
    {
        { "Tifinagh", typeof(Tifinaghizer) },
        { "Arabic", typeof(Arabizer) },
        { "Arabic2", typeof(Arabizer2) },
        { "Cyrillic", typeof(Cyrillizer) },
        { "Greek", typeof(Hellenizer) },
        { "Hebrew", typeof(Hebraizer) },
        { "Syriac", typeof(Syriacizer) },
        { "Musnad", typeof(Musnadizer) },
        { "Phoenician", typeof(Phoenicianizer) },
        { "Aramaic", typeof(Aramaicizer) },
     };

    public static List<List<string>> GetTransliterations()
    {
        List<char> chars = new List<char>()
        {
            'a','i','u','o','e','b','g', 'ǧ' ,'d','h','w' ,'ḥ' ,'y' ,'k','l','m','n','s','č','f','p','q','r','t','c','z','ț','ɛ','ṭ','v','j','x','ḏ','ṯ','ṣ', 'ɣ', 'ẓ', 'ḍ', 'ṛ',
        };

        var transliteratorInstances = transliterators.ToDictionary(x => x.Key, x => (Transliterator)Activator.CreateInstance(x.Value)!);    

        List<List<string>> rows = new List<List<string>>();

        var columns = new List<string>
        {
            "Latin",
        };

        foreach (var kvp in transliteratorInstances)
        {
            columns.Add(kvp.Key);
        }

        rows.Add(columns);

        foreach (var c in chars)
        {
            var row = new List<string>
            {
                c.ToString()
            };

            foreach (var kvp in transliteratorInstances)
            {
                row.Add(kvp.Value.Transliterate(c.ToString()));             
            }

            rows.Add(row);
        }

        return rows;
    }

    public static string Transliterate(string input, string target)
    {
        input = input
          .Replace("’", "'")
          .Replace("γ", "ɣ")
          .Replace("ε", "ɣ")
          .Replace("ğ", "ǧ")
          .Replace("ţ", "tt");

        return ((Transliterator)Activator.CreateInstance(transliterators[target])!).Transliterate(input);
    }
}
