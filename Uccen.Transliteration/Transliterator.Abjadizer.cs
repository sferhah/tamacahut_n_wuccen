using System.Text;
using System.Text.RegularExpressions;

namespace Uccen.Transliteration;

public partial class Transliterator
{
    private List<char>? _LatinChars;
    protected List<char> LatinChars => _LatinChars ??= new List<char>()
    {
        'a','i','u','o','e','b','g', 'ǧ' ,'d','h','w' ,'ḥ' ,'y' ,'k','l','m','n','s','č','f','p','q','r','t','c','z','ț','ɛ','ṭ','v','j','x','ḏ','ṯ','ṣ', 'ɣ', 'ẓ', 'ḍ', 'ṛ',
        Config.Geminizer
    };

    protected string Abjadize(string input)
    {
        input = input.ToLower()
            .Replace("à", "a")
            .Replace("ā", "a")
            .Replace("ī", "i")
            .Replace("í", "i")
            .Replace("ū", "u")
            .Replace("ū", "u")
            .Replace("ú", "u")
            .Replace("ò", "o")

            .Replace("ie", "a")
            .Replace("ea", "ia")

            .Replace("é", "a")
            .Replace("è", "a")

            .Replace("ç", "s")
            .Replace("ț", "s");

        input = new Regex("([" + string.Join(",", LatinChars) + "])\\1").Replace(input, "$1" + Config.Geminizer);

        input = Regex.Replace(input, """(\b\w*[-'])(i)""", "$1y");
        input = Regex.Replace(input, """(\b\w*[-'])(u)""", "$1w");
        input = Regex.Replace(input, """(\b\w*[-'])(a)""", "$1â");

        return input;
    }

    virtual protected string ReplaceHyphenWithKasheeda(string input)
    {
        return input;
    }

    protected string ReplaceVowelsWithSemiVowels(string result)
    {
        Dictionary<char, string> vowels = new Dictionary<char, string>
        {
            { 'i', "y"},
            { 'u', "w"},
            { 'a', "â"},
        };

        StringBuilder builder = new StringBuilder();
        char? previous_c = null;

        for (int i = 0; i < result.Length; i++)
        {
            char c = result[i];

            char? next_c = (i >= result.Length - 1) ? null : result[i + 1];
            bool is_first = previous_c is null || !LatinChars.Contains(previous_c.Value);            

            if (vowels.ContainsKey(c) && !is_first)
            {
                builder.Append(vowels[c]);
            }
            else
            {
                builder.Append(c);
            }

            previous_c = c;
        }

        return builder.ToString();
    }

    protected string ReplaceSchwaWithNullVowel(string input) 
        => input.Replace("ew", "w" + Config.NullVowel).Replace("w" + Config.NullVowel + Config.Geminizer, "w" + Config.Geminizer)
            .Replace("ey", "y" + Config.NullVowel).Replace("y" + Config.NullVowel + Config.Geminizer, "y" + Config.Geminizer)
            .Replace("e", "");
}