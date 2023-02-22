using System.Reflection;
using System.Xml;

namespace Uccen.Transliteration;

public class ConfigDeserializer
{
    public static ScriptConfig Parse(string name)
    {
        var doc = new XmlDocument();
        doc.LoadXml(GetResourceTextFile(name + ".xml"));

        var direction = doc.DocumentElement!.Attributes["direction"]?.Value ?? "ltr";
        var geminizer = doc.DocumentElement.Attributes["geminizer"]?.Value ?? "0x00B2"; // "²"
        var nullVowel = doc.DocumentElement.Attributes["nullVowel"]?.Value ?? "0x00B0"; // "°"


        Dictionary<char, string> mapping = new Dictionary<char, string>();
        foreach (var kv in doc.DocumentElement.ChildNodes.OfType<XmlElement>())
        {
            var key = kv.GetAttribute("key");
            var value = new string(kv.GetAttribute("value").ToCharArray().Where(x => x != 0x200E && x != 0x200F).ToArray());

            var keyAsChar = key[0];

            mapping.Add(keyAsChar, value);

            if (char.ToUpper(keyAsChar) != keyAsChar)
            {
                mapping.Add(char.ToUpper(keyAsChar), value.ToUpper());
            }
        }


        return new ScriptConfig
        {
            Direction = direction,
            Mapping = mapping,
            Geminizer = (char)Convert.ToInt32(geminizer, 16),
            NullVowel = (char)Convert.ToInt32(nullVowel, 16),
            ScriptType = (ScriptType)Enum.Parse(typeof(ScriptType), doc.DocumentElement!.Attributes["scriptType"]?.Value ?? "Alphabet")
        };
    }

    public static string GetResourceTextFile(string filename)
    {
        var assembly = typeof(ConfigDeserializer).GetTypeInfo().Assembly;
        var assembly_namespace = assembly.GetName().Name;

        using var stream = assembly.GetManifestResourceStream($"{assembly_namespace}.XmlRessources.{filename}")!;
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }
}