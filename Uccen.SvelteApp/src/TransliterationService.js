const LATIN_CHARS = [
  'a', 'i', 'u', 'o', 'e', 'b', 'g', 'ǧ', 'd', 'h', 'w', 'ḥ', 'y', 'k', 'l', 'm', 'n', 's', 'č', 'f', 'p', 'q', 'r', 't', 'c', 'z', 'ț', 'ɛ', 'ṭ', 'v', 'j', 'x', 'ḏ', 'ṯ', 'ṣ', 'ɣ', 'ẓ', 'ḍ', 'ṛ'
];

export class TransliterationService {

  static preNormalize(input) {
    if (!input) return "";
    return input
      .replace(/’/g, "'")
      .replace(/γ/g, "ɣ")
      .replace(/ε/g, "ɣ")
      .replace(/ğ/g, "ǧ")
      .replace(/ţ/g, "tt");
  }

  static transliterate(input, config) {
    let result = this.preNormalize(input);

    if (config.scriptType === "Abjad") {
      result = this.abjadize(result, config.geminizer);
      result = this.replaceHyphenWithKasheeda(result, config.name, config.geminizer);
      result = this.replaceVowelsWithSemiVowels(result, config.geminizer);
      result = this.replaceSchwaWithNullVowel(
        result,
        config.nullVowel,
        config.geminizer,
        config.mapping.e !== undefined
      );
    }

    return this.mapCharacters(result, config);
  }

  static abjadize(input, geminizer) {
    let res = input.toLowerCase()
      .replace(/à/g, "a").replace(/ā/g, "a")
      .replace(/ī/g, "i").replace(/í/g, "i")
      .replace(/ū/g, "u").replace(/ú/g, "u")
      .replace(/ò/g, "o")
      .replace(/ie/g, "a")
      .replace(/ea/g, "ia")
      .replace(/é/g, "a").replace(/è/g, "a")
      .replace(/ç/g, "s").replace(/ț/g, "s");
    // Escape characters with special meaning inside a [...] class
    const escapeForCharClass = (s) => s.replace(/[\]\\^-]/g, "\\$&");

    // Unicode-aware equivalents of .NET's \w and \b (JS's are ASCII-only)
    const W = String.raw`[\p{L}\p{Mn}\p{Nd}\p{Pc}]`;
    

    // 1. Doubled letters -> letter + Geminizer
    const cls = LATIN_CHARS.map(escapeForCharClass).join(","); // use .join("") if the comma is unintended
    res = res.replace(new RegExp(`([${cls}])\\1`, "gu"), (_, c) => c + geminizer);

    // 2. After a hyphen/apostrophe inside a word: i -> y, u -> w, a -> â
    
    // old code:
    // const WORD_START = String.raw`(?<!${W})`;
    // const afterSeparator = new RegExp(`(${WORD_START}${W}*[-\u2011'])`, "u").source;
    
    const afterSeparator = new RegExp(`(${W}[-\u2011'])`, "u").source;

    res = res.replace(new RegExp(`${afterSeparator}i`, "gu"), "$1y");
    res = res.replace(new RegExp(`${afterSeparator}u`, "gu"), "$1w");
    res = res.replace(new RegExp(`${afterSeparator}a`, "gu"), "$1â");


    return res;
  }

  static replaceHyphenWithKasheeda(input, scriptName, geminizer) {
    if (scriptName !== "Arabic") return input;

    const kasheeda = "ـ";
    const W = String.raw`[\p{L}\p{Mn}\p{Nd}\p{Pc}]`;

    // left side: a word start, or empty when directly preceded by a word char (a-b-c case)
    const pattern = new RegExp(`((?<!${W})${W}+|(?<=${W}))[-\u2011'](${W}+)`, "gu");

    let out = input.replace(pattern, (_, left, right) => `${left}${kasheeda}\u00A0${kasheeda}${right}`);

    const fixKasheeda = (s, c) => s.replaceAll(c + kasheeda, c);
    const nonJoining = ["a", "r", "ṛ", "w", "d", "ḏ", "z", "ẓ"];

    for (const c of nonJoining) out = fixKasheeda(out, c);
    for (const c of nonJoining.slice(1)) out = fixKasheeda(out, c + geminizer);

    return out;
  }


  static replaceVowelsWithSemiVowels(input, geminizer = "²") {
    const vowels = { 'i': "y", 'u': "w", 'a': "â" };
    let builder = "";
    let previousChar = null;

    for (let i = 0; i < input.length; i++) {
      const c = input[i];

      const isWordCharacter = LATIN_CHARS.includes(previousChar) || previousChar === geminizer;

      const isFirst = previousChar === null || !isWordCharacter;

      if (vowels[c] && !isFirst) {
        builder += vowels[c];
      } else {
        builder += c;
      }
      previousChar = c;
    }
    return builder;
  }


  static replaceSchwaWithNullVowel(input, nullVowel, geminizer, preserveSchwa = false) {
    const result = preserveSchwa ? input : input
      .split("ew").join("w" + nullVowel).split("w" + nullVowel + geminizer).join("w" + geminizer)
      .split("ey").join("y" + nullVowel).split("y" + nullVowel + geminizer).join("y" + geminizer);

    return preserveSchwa ? result : result.split("e").join("");
  }

  static mapCharacters(input, config) {
    let res = input;

    if (config.direction === "ltr") {
      res = res.replace(/\u200F/g, "");
    } else {
      res = res.replace(/\u200E/g, "")
        .replace(/\?/g, "⸮")
        .replace(/;/g, "؛")
        .replace(/,/g, "،");
    }

    if (config.initialSchwa !== undefined) {
      res = res.replace(/(?<![\p{L}\p{Mn}\p{Nd}\p{Pc}])e/gu, config.initialSchwa);
    }

    let output = "";
    for (let i = 0; i < res.length; i++) {
      const c = res[i];

      if (config.mapping[c] !== undefined) {
        output += config.mapping[c];
      } else if (config.mapping[c.toLowerCase()] !== undefined) {
        output += config.mapping[c.toLowerCase()].toUpperCase();
      } else {
        output += c;
      }
    }
    return output;
  }

  static getTransliterations(loadedConfigs) {
    const chars = [
      'a', 'i', 'u', 'o', 'e', 'b', 'g', 'ǧ', 'd', 'h', 'w', 'ḥ', 'y', 'k', 'l', 'm', 'n', 's', 'č', 'f', 'p', 'q', 'r', 't', 'c', 'z', 'ț', 'ɛ', 'ṭ', 'v', 'j', 'x', 'ḏ', 'ṯ', 'ṣ', 'ɣ', 'ẓ', 'ḍ', 'ṛ',
    ];

    const rows = [];

    // 1. columns
    const header = ['Latin'];
    Object.keys(loadedConfigs).forEach(scriptName => {
      header.push(scriptName);
    });
    rows.push(header);

    // 2. rows
    chars.forEach(c => {
      const row = [c];
      Object.keys(loadedConfigs).forEach(scriptName => {
        const config = loadedConfigs[scriptName];
        row.push(this.transliterate(c, config));
      });
      rows.push(row);
    });

    return rows;
  }

}
