// Minimal WOFF2 reader (https://www.w3.org/TR/WOFF2/), enough to check a font's contents:
// which tables it has, and which characters its cmap maps to a glyph.
//
// A WOFF2 file is a table directory followed by all the font's tables concatenated and
// Brotli-compressed. Only glyf/loca may be transformed; cmap is stored exactly as in a TTF.

import { brotliDecompressSync } from "node:zlib";

export type Woff2Contents = {
  /** Table names, e.g. "cmap", "GPOS" (GPOS positions combining marks on their letters). */
  tables: string[];
  /** Code points the font maps to a real glyph. */
  characters: Set<number>;
};

export function readWoff2(font: Buffer): Woff2Contents {
  if (font.toString("latin1", 0, 4) !== "wOF2") throw new Error("Not a WOFF2 file");
  const numTables = font.readUInt16BE(12);
  const compressedLength = font.readUInt32BE(20);

  let offset = 48; // end of the fixed-size header
  const readUIntBase128 = (): number => {
    let value = 0;
    for (let i = 0; i < 5; i++) {
      const byte = font[offset++];
      value = value * 128 + (byte & 0x7f);
      if ((byte & 0x80) === 0) return value;
    }
    throw new Error("Invalid UIntBase128");
  };

  const entries: { tag: string; length: number }[] = [];
  for (let i = 0; i < numTables; i++) {
    const flags = font[offset++];
    let tag: string;
    if ((flags & 0x3f) === 0x3f) {
      tag = font.toString("latin1", offset, offset + 4);
      offset += 4;
    } else {
      tag = KNOWN_TAGS[flags & 0x3f];
    }
    const transformVersion = flags >> 6;
    const origLength = readUIntBase128();
    // glyf/loca: version 0 means transformed; every other table: any version but 0 means transformed.
    const transformed = tag === "glyf" || tag === "loca" ? transformVersion === 0 : transformVersion !== 0;
    entries.push({ tag, length: transformed ? readUIntBase128() : origLength });
  }

  const data = brotliDecompressSync(font.subarray(offset, offset + compressedLength));
  let position = 0;
  let cmap: Buffer | null = null;
  for (const entry of entries) {
    if (entry.tag === "cmap") cmap = data.subarray(position, position + entry.length);
    position += entry.length; // tables follow one another without padding
  }
  if (!cmap) throw new Error("No cmap table");

  return { tables: entries.map((entry) => entry.tag.trim()), characters: readCmap(cmap) };
}

/** Characters mapped to a real glyph (not glyph 0) by the cmap's format 12 or format 4 subtables. */
function readCmap(cmap: Buffer): Set<number> {
  const mapped = new Set<number>();
  const subtableOffsets = Array.from({ length: cmap.readUInt16BE(2) }, (_, i) => cmap.readUInt32BE(4 + i * 8 + 4));

  for (const start of subtableOffsets) {
    const format = cmap.readUInt16BE(start);
    if (format === 12) {
      const groups = cmap.readUInt32BE(start + 12);
      for (let g = 0; g < groups; g++) {
        const at = start + 16 + g * 12;
        const first = cmap.readUInt32BE(at);
        const last = cmap.readUInt32BE(at + 4);
        const firstGlyph = cmap.readUInt32BE(at + 8);
        for (let cp = first; cp <= last; cp++) if (firstGlyph + (cp - first) !== 0) mapped.add(cp);
      }
    } else if (format === 4) {
      const segments = cmap.readUInt16BE(start + 6) / 2;
      const endCodes = start + 14;
      const startCodes = endCodes + segments * 2 + 2;
      const idDeltas = startCodes + segments * 2;
      const idRangeOffsets = idDeltas + segments * 2;
      for (let s = 0; s < segments; s++) {
        const first = cmap.readUInt16BE(startCodes + s * 2);
        const last = cmap.readUInt16BE(endCodes + s * 2);
        const delta = cmap.readInt16BE(idDeltas + s * 2);
        const rangeOffset = cmap.readUInt16BE(idRangeOffsets + s * 2);
        for (let cp = first; cp <= last && cp !== 0xffff; cp++) {
          const glyph = rangeOffset === 0
            ? (cp + delta) & 0xffff
            : cmap.readUInt16BE(idRangeOffsets + s * 2 + rangeOffset + (cp - first) * 2);
          if (glyph !== 0) mapped.add(cp);
        }
      }
    }
  }
  return mapped;
}

/** In the WOFF2 table directory, a 6-bit index into this list replaces the 4-letter tag. */
const KNOWN_TAGS = [
  "cmap", "head", "hhea", "hmtx", "maxp", "name", "OS/2", "post", "cvt ", "fpgm", "glyf", "loca", "prep",
  "CFF ", "VORG", "EBDT", "EBLC", "gasp", "hdmx", "kern", "LTSH", "PCLT", "VDMX", "vhea", "vmtx", "BASE",
  "GDEF", "GPOS", "GSUB", "EBSC", "JSTF", "MATH", "CBDT", "CBLC", "COLR", "CPAL", "SVG ", "sbix", "acnt",
  "avar", "bdat", "bloc", "bsln", "cvar", "fdsc", "feat", "fmtx", "fvar", "gvar", "hsty", "just", "lcar",
  "mort", "morx", "opbd", "prop", "trak", "Zapf", "Silf", "Glat", "Gloc", "Feat", "Sill",
];
