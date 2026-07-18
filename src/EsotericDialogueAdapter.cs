using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

internal static class EsotericDialogueAdapter
{
    private static readonly object Gate = new();
    private static readonly List<LocalizedCapture> Captures = new();
    private static readonly HashSet<string> KnownKeys = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, List<string>> KeysByText = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> TextByKey = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> SpeakerByKey = new(StringComparer.Ordinal);
    private static Action<string> _log = _ => { };
    private static string _lastResolvedId = "";

    internal static int Load(string mapPath, Action<string> log)
    {
        lock (Gate)
        {
            _log = log;
            Captures.Clear();
            KnownKeys.Clear();
            KeysByText.Clear();
            TextByKey.Clear();
            SpeakerByKey.Clear();
            _lastResolvedId = "";
            if (!File.Exists(mapPath))
            {
                _log($"DIALOGUE_MAP_MISSING {mapPath}");
                return 0;
            }

            foreach (var line in File.ReadLines(mapPath))
            {
                var columns = line.Split(new[] { '\t' }, 3);
                if (columns.Length < 2) continue;
                var key = columns[0].Trim();
                var text = NormalizeText(columns[1]);
                var speaker = columns.Length >= 3 ? columns[2].Trim() : "";
                if (key.Length == 0) continue;
                KnownKeys.Add(key);
                TextByKey[key] = text;
                SpeakerByKey[key] = speaker;
                if (text.Length == 0) continue;
                if (!KeysByText.TryGetValue(text, out var keys))
                {
                    keys = new List<string>();
                    KeysByText[text] = keys;
                }
                if (!keys.Contains(key, StringComparer.Ordinal)) keys.Add(key);
            }
            _log($"DIALOGUE_MAP_LOADED keys={KnownKeys.Count} texts={KeysByText.Count} path={mapPath}");
            return KnownKeys.Count;
        }
    }

    internal static IEnumerable<KeyValuePair<string, string>> Entries()
    {
        lock (Gate) return TextByKey.ToArray();
    }

    internal static string SpeakerFor(string id)
    {
        lock (Gate) return SpeakerByKey.TryGetValue(id, out var speaker) ? speaker : "";
    }

    internal static void CaptureLocalization(string? id, string? localizedText)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        lock (Gate)
        {
            if (!KnownKeys.Contains(id)) return;
            var now = DateTime.UtcNow;
            Captures.RemoveAll(item => (now - item.CapturedUtc).TotalSeconds > 10);
            Captures.Add(new LocalizedCapture(id, NormalizeText(localizedText ?? ""), now));
            if (Captures.Count > 64) Captures.RemoveRange(0, Captures.Count - 64);
        }
    }

    internal static void CaptureCandidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var trimmed = value.Trim();
        lock (Gate)
        {
            if (KnownKeys.Contains(trimmed))
            {
                Captures.Add(new LocalizedCapture(trimmed, "", DateTime.UtcNow));
            }
        }
    }

    internal static string Resolve(string? runtimeText)
    {
        var raw = runtimeText ?? "";
        var normalized = NormalizeText(raw);
        var trimmed = raw.Trim();
        lock (Gate)
        {
            var now = DateTime.UtcNow;
            Captures.RemoveAll(item => (now - item.CapturedUtc).TotalSeconds > 10);
            if (KnownKeys.Contains(trimmed)) return Remember(trimmed);

            var exactCapture = Captures.LastOrDefault(
                item => item.LocalizedText.Length > 0 && item.LocalizedText == normalized
            );
            if (exactCapture != null)
            {
                Captures.Remove(exactCapture);
                return Remember(exactCapture.Id);
            }

            if (KeysByText.TryGetValue(normalized, out var exactKeys) && exactKeys.Count > 0)
            {
                return Remember(SelectMappedKey(exactKeys));
            }

            var unquoted = StripEnclosingDialogueQuotes(normalized);
            if (unquoted != normalized
                && KeysByText.TryGetValue(unquoted, out var unquotedKeys)
                && unquotedKeys.Count > 0)
            {
                _log($"RESOLVED_UNQUOTED {unquotedKeys.Count} candidate(s)");
                return Remember(SelectMappedKey(unquotedKeys));
            }

            for (var start = 0; start < normalized.Length; start++)
            {
                if (start > 0
                    && !char.IsWhiteSpace(normalized[start - 1])
                    && !IsAttachedPunctuationStart(normalized[start]))
                {
                    continue;
                }
                var suffix = normalized.Substring(start);
                var mappedSuffix = suffix;
                if (!KeysByText.TryGetValue(mappedSuffix, out var suffixKeys) || suffixKeys.Count == 0)
                {
                    mappedSuffix = StripEnclosingDialogueQuotes(suffix);
                    if (mappedSuffix == suffix
                        || !KeysByText.TryGetValue(mappedSuffix, out suffixKeys)
                        || suffixKeys.Count == 0)
                    {
                        continue;
                    }
                }
                var resolved = SelectMappedKey(suffixKeys);
                if (!IsSafeSuffixMatch(normalized, start, mappedSuffix))
                {
                    _log($"REJECTED_SUFFIX {resolved} prefixChars={start} suffix={LogText(mappedSuffix)}");
                    continue;
                }
                _log($"RESOLVED_SUFFIX {resolved} prefixChars={start} unquoted={mappedSuffix != suffix}");
                return Remember(resolved);
            }
            return "";
        }
    }

    internal static string NormalizeText(string value)
    {
        value = Regex.Replace(value, "<[^>]+>", " ");
        value = value.Replace('“', '"').Replace('”', '"').Replace('’', '\'');
        value = Regex.Replace(value, "\\s+", " ").Trim();
        value = Regex.Replace(value, @"(?<=\.\.\.)\s+", "");
        value = Regex.Replace(value, @"\s+(['\""])(?=\s*(?:[,.;:!?]|$))", "$1");
        value = Regex.Replace(value, @"\s+([\)\]\}])", "$1");
        value = Regex.Replace(value, @"\s+([,.;:!?])", "$1");
        value = Regex.Replace(value, @"([\(\[])\s+", "$1");
        return value;
    }

    private static string StripEnclosingDialogueQuotes(string value)
    {
        while (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
        {
            value = value.Substring(1, value.Length - 2).Trim();
        }
        return value;
    }

    private static string Remember(string id)
    {
        _lastResolvedId = id;
        return id;
    }

    private static bool IsAttachedPunctuationStart(char value)
    {
        return value is '.' or ',' or ';' or ':' or '!' or '?' or '"' or '\'' or '(' or '[' or '{';
    }

    private static bool IsSafeSuffixMatch(string normalized, int start, string suffix)
    {
        var prefix = normalized.Substring(0, start).Trim();
        if (LooksLikeRuntimeLabel(prefix)) return true;

        return suffix.Length >= 8 && suffix.Length * 2 >= normalized.Length;
    }

    private static bool LooksLikeRuntimeLabel(string prefix)
    {
        if (Regex.IsMatch(prefix, @"(?i)(?:^|\s)(?:DC|FC)\s*\d+\b|\b(?:success|failure)\b"))
        {
            return true;
        }

        var trimmed = prefix.Trim(' ', '.', ':', '-', '!', '?', '"', '\'');
        if (Regex.IsMatch(
            trimmed,
            @"(?i)^(?:intelligence|dexterity|charisma|strength|wisdom|constitution|system)(?:\s|$)"
        ))
        {
            return true;
        }

        return SpeakerByKey.Values.Any(
            speaker => speaker.Length > 0
                && trimmed.StartsWith(speaker, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static string LogText(string value)
    {
        value = value.Replace("\r", " ").Replace("\n", " ");
        return value.Length <= 80 ? value : value.Substring(0, 80) + "...";
    }

    private static string SelectMappedKey(List<string> candidates)
    {
        if (candidates.Count == 1) return candidates[0];
        if (TrySplitNumberedKey(_lastResolvedId, out var previousPrefix, out var previousNumber))
        {
            var next = candidates
                .Select(id => ParseCandidate(id))
                .Where(item => item.Parsed && item.Prefix == previousPrefix && item.Number > previousNumber)
                .OrderBy(item => item.Number)
                .FirstOrDefault();
            if (next != null) return next.Id;
        }
        return candidates.OrderBy(NaturalKey, StringComparer.Ordinal).First();
    }

    private static ParsedCandidate ParseCandidate(string id)
    {
        var parsed = TrySplitNumberedKey(id, out var prefix, out var number);
        return new ParsedCandidate(id, parsed, prefix, number);
    }

    private static bool TrySplitNumberedKey(string value, out string prefix, out int number)
    {
        var match = Regex.Match(value ?? "", @"^(.*_)(\d+)$");
        prefix = match.Success ? match.Groups[1].Value : "";
        number = 0;
        return match.Success && int.TryParse(match.Groups[2].Value, out number);
    }

    private static string NaturalKey(string value)
    {
        return Regex.Replace(value, @"\d+", match => match.Value.PadLeft(12, '0'));
    }

    private sealed class LocalizedCapture
    {
        internal LocalizedCapture(string id, string localizedText, DateTime capturedUtc)
        {
            Id = id;
            LocalizedText = localizedText;
            CapturedUtc = capturedUtc;
        }

        internal string Id { get; }
        internal string LocalizedText { get; }
        internal DateTime CapturedUtc { get; }
    }

    private sealed class ParsedCandidate
    {
        internal ParsedCandidate(string id, bool parsed, string prefix, int number)
        {
            Id = id;
            Parsed = parsed;
            Prefix = prefix;
            Number = number;
        }

        internal string Id { get; }
        internal bool Parsed { get; }
        internal string Prefix { get; }
        internal int Number { get; }
    }
}
