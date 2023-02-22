using System.Text.RegularExpressions;

namespace Uccen.Transliteration;

public class Cyrillizer : Transliterator { protected override string Name => "Cyrillic"; }
public class Hellenizer : Transliterator { protected override string Name => "Greek"; }
public class Tifinaghizer : Transliterator { protected override string Name => "Tifinagh"; }
public class Hebraizer : Transliterator { protected override string Name => "Hebrew"; }
public class Aramaicizer : Transliterator { protected override string Name => "Aramaic"; }
public class Phoenicianizer : Transliterator { protected override string Name => "Phoenician"; }
public class Musnadizer : Transliterator { protected override string Name => "Musnad"; }
public class Syriacizer : Transliterator { protected override string Name => "Syriac"; }
public class Arabizer2 : Arabizer { protected override string Name => "Arabic2"; }

public class Arabizer : Transliterator
{
    
    protected override string Name => "Arabic";
    protected override string ReplaceHyphenWithKasheeda(string result)
        => Regex.Replace(result, """(\b\w*)[-](\w*\b)""", "$1" + ArabizerExt.Kasheeda + " " + ArabizerExt.Kasheeda + "$2")
            .FixKasheeda("a")
            .FixKasheeda("r")
            .FixKasheeda("ṛ")
            .FixKasheeda("w")
            .FixKasheeda("d")
            .FixKasheeda("ḏ")
            .FixKasheeda("z")
            .FixKasheeda("ẓ")
        
            .FixKasheeda("r" + Config.Geminizer)
            .FixKasheeda("ṛ" + Config.Geminizer)
            .FixKasheeda("w" + Config.Geminizer)
            .FixKasheeda("d" + Config.Geminizer)
            .FixKasheeda("ḏ" + Config.Geminizer)
            .FixKasheeda("z" + Config.Geminizer)
            .FixKasheeda("ẓ" + Config.Geminizer);
}

public static class ArabizerExt
{
    //const string Kasheeda = "ـــ";
    public const string Kasheeda = "ـ";
    public static string FixKasheeda(this string result, string c) => result.Replace(c + Kasheeda, c);
}

