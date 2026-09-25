using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// A small strict JSON reader.
    ///
    /// The mod targets net472 inside Unity, where System.Text.Json is not
    /// available and pulling a serializer in for one small config file is not
    /// worth the dependency. Values come back as
    /// <see cref="Dictionary{TKey,TValue}"/>, <see cref="List{T}"/>,
    /// <see cref="string"/>, <see cref="double"/>, <see cref="long"/>,
    /// <see cref="bool"/> or null. Malformed input throws
    /// <see cref="FormatException"/>, which the caller turns into "keep the
    /// stock layout" rather than letting it escape.
    /// </summary>
    internal static class MiniJson
    {
        public static object? Parse(string json)
        {
            if (json == null) throw new FormatException("порожній вміст");
            int i = 0;
            var value = ParseValue(json, ref i);
            SkipWhitespace(json, ref i);
            if (i != json.Length) throw new FormatException($"зайві символи на позиції {i}");
            return value;
        }

        private static object? ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) throw new FormatException("несподіваний кінець");

            switch (s[i])
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return ParseString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default: return ParseNumber(s, ref i);
            }
        }

        private static Dictionary<string, object?> ParseObject(string s, ref int i)
        {
            var result = new Dictionary<string, object?>();
            i++;                                  // '{'
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return result; }

            while (true)
            {
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new FormatException($"очікувався ключ на позиції {i}");
                var key = ParseString(s, ref i);

                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException($"очікувалось ':' на позиції {i}");
                i++;

                result[key] = ParseValue(s, ref i);

                SkipWhitespace(s, ref i);
                if (i >= s.Length) throw new FormatException("незакритий об'єкт");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return result; }
                throw new FormatException($"очікувалось ',' або '}}' на позиції {i}");
            }
        }

        private static List<object?> ParseArray(string s, ref int i)
        {
            var result = new List<object?>();
            i++;                                  // '['
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return result; }

            while (true)
            {
                result.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i >= s.Length) throw new FormatException("незакритий масив");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return result; }
                throw new FormatException($"очікувалось ',' або ']' на позиції {i}");
            }
        }

        private static string ParseString(string s, ref int i)
        {
            i++;                                  // opening quote
            var sb = new StringBuilder();
            while (true)
            {
                if (i >= s.Length) throw new FormatException("незакритий рядок");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }

                if (i >= s.Length) throw new FormatException("обірване екранування");
                char e = s[i++];
                switch (e)
                {
                    case '"':  sb.Append('"');  break;
                    case '\\': sb.Append('\\'); break;
                    case '/':  sb.Append('/');  break;
                    case 'b':  sb.Append('\b'); break;
                    case 'f':  sb.Append('\f'); break;
                    case 'n':  sb.Append('\n'); break;
                    case 'r':  sb.Append('\r'); break;
                    case 't':  sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("обірваний \\u");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw new FormatException($"невідоме екранування \\{e}");
                }
            }
        }

        private static object ParseNumber(string s, ref int i)
        {
            int start = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            bool isFloat = false;
            while (i < s.Length)
            {
                char c = s[i];
                if (c >= '0' && c <= '9') { i++; continue; }
                if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { isFloat = true; i++; continue; }
                break;
            }

            var text = s.Substring(start, i - start);
            if (text.Length == 0) throw new FormatException($"очікувалось число на позиції {start}");

            if (!isFloat && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                return l;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            throw new FormatException($"некоректне число '{text}'");
        }

        private static void Expect(string s, ref int i, string literal)
        {
            if (i + literal.Length > s.Length || string.CompareOrdinal(s, i, literal, 0, literal.Length) != 0)
                throw new FormatException($"очікувалось '{literal}' на позиції {i}");
            i += literal.Length;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
        }
    }
}
