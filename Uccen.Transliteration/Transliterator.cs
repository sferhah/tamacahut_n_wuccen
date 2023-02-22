using System.Text;

namespace Uccen.Transliteration;

public abstract partial class Transliterator
{
    abstract protected string Name { get; }

    private ScriptConfig? _Config;
    protected ScriptConfig Config => _Config ??= ConfigDeserializer.Parse(Name);

    public string Transliterate(string input)
    {
        if (Config.ScriptType == ScriptType.Abjad)
        {
            input = Abjadize(input);
            input = ReplaceHyphenWithKasheeda(input);
            input = ReplaceVowelsWithSemiVowels(input);
            input = ReplaceSchwaWithNullVowel(input);
        }

        return Map(input);
    }

    public virtual string Map(string result)
    {
        if (Config.Direction == "ltr")
        {
            result = new string(result.ToCharArray().Where(x => x != 0x200F).ToArray());
        }
        else
        {
            result = new string(result.ToCharArray().Where(x => x != 0x200E).ToArray());

            result = result.Replace("?", "⸮")
                .Replace(";", "؛")
                .Replace(",", "،");
        }

        var builder = new StringBuilder();

        for (int i = 0; i < result.Length; i++)
        {
            char c = result[i];            

            if (Config.Mapping.TryGetValue(c, out string? to_append))
            {
                builder.Append(to_append);
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}