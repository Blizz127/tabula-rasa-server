using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Rasa.Test.Reconstruction
{
    /// <summary>
    /// Closed vocabularies of the reconstruction manifest
    /// (docs/evidence/reconstruction-manifest.schema.json) and its companion
    /// position and footage-event files. Tests keep these equal to the schema.
    /// </summary>
    public static class ManifestVocabulary
    {
        public const string ManifestSchemaId = "rasa-reconstruction-manifest/1";
        public const string PolicyAnchor = "AGENTS.md#evidence-bounded-reconstruction-user-decision-2026-09-13";
        public const string FootageEventsSchemaId = "rasa-reconstruction-footage-events/1";
        public const string PositionsSchemaId = "rasa-reconstruction-positions/1";

        /// <summary>An observed citation's t may differ from the footage event's t by at most this (seconds).</summary>
        public const double ObservedTimeTolerance = 0.07;

        /// <summary>Absorbs binary floating-point noise in the tolerance comparison only.</summary>
        public const double TimeComparisonEpsilon = 1e-6;

        public static readonly string[] RequiredTopLevel =
        {
            "schema", "policy", "segment", "sources", "scope", "rows", "omitted",
            "accepted_definition_gaps", "accepted_content_gaps", "gaps", "non_content_settings", "changes"
        };

        public static readonly string[] Tiers = { "original", "observed", "measured", "inferred", "analogue" };

        public static readonly string[] SourceKinds =
        {
            "client_table", "client_code", "client_map", "official_notes",
            "video", "screenshot", "measurement", "in_client_calibration"
        };

        public static readonly string[] Eras = { "final_live", "d11_to_shutdown", "pre_d11", "unknown" };

        // S1-S8: boot-camp build plan slices. W1: the Wilderness arrival (segment 3), recorded in the boot-camp manifest
        // because it closes the boot-camp exit (OD-36). W2: the class-gear missions 2010/2011, the next step of the
        // same segment (the tier-2 class choice answered by the class_selected rule).
        public static readonly string[] Slices = { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "W1", "W2" };

        public static readonly string[] StorageRoles = { "surrogate_key", "storage_reference", "scope", "comment" };

        public static readonly string[] DecisionStatuses = { "open", "approved", "rejected" };

        /// <summary>Footage events a citation may resolve to. Everything else (refuted) is excluded.</summary>
        public static readonly string[] CitableFootageStatuses = { "confirmed", "corrected" };

        public static readonly string[] PositionTiers = { "original", "measured", "inferred" };

        /// <summary>Source kinds whose citation (with a locator) can support an <c>original</c> field.</summary>
        public static readonly string[] OriginalSourceKinds = { "client_table", "client_code", "client_map", "official_notes" };

        /// <summary>Source kinds whose event citation can support an <c>observed</c> field.</summary>
        public static readonly string[] ObservedSourceKinds = { "video", "screenshot" };

        /// <summary>Source kinds whose citation can support a <c>measured</c> field.</summary>
        public static readonly string[] MeasuredSourceKinds = { "measurement", "in_client_calibration", "video" };

        public const string PreRebuildEra = "pre_d11";
    }

    /// <summary>One rule violation. <see cref="Rule"/> is a stable id from <see cref="ManifestRules"/>.</summary>
    public sealed class ValidationError
    {
        public ValidationError(string rule, string path, string message)
        {
            Rule = rule;
            Path = path;
            Message = message;
        }

        public string Rule { get; }
        public string Path { get; }
        public string Message { get; }

        public override string ToString() => $"[{Rule}] {Path}: {Message}";
    }

    public sealed class ValidationReport
    {
        private readonly List<ValidationError> _errors = new List<ValidationError>();

        public IReadOnlyList<ValidationError> Errors => _errors;
        public bool IsValid => _errors.Count == 0;

        public void Add(string rule, string path, string message) => _errors.Add(new ValidationError(rule, path, message));
        public void AddRange(ValidationReport other) => _errors.AddRange(other._errors);

        public override string ToString() => IsValid ? "valid" : string.Join(Environment.NewLine, _errors);
    }

    /// <summary>A <c>sources</c> entry of the manifest.</summary>
    public sealed class ManifestSource
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Era { get; set; }
        public string VideoId { get; set; }
        public string Sha256 { get; set; }
    }

    /// <summary>An <c>owner_decisions</c> entry of the manifest.</summary>
    public sealed class OwnerDecision
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public bool IsApproved => Status == "approved";
    }

    public sealed class FootageTranscript
    {
        public string Segment { get; set; }
        public string VideoId { get; set; }
        public string TranscriptSha256 { get; set; }
    }

    /// <summary>
    /// One extracted transcript event with verification corrections applied.
    /// <see cref="CorrectedText"/> is present only when the verifier replaced the
    /// transcript text; an observed string value must then equal it.
    /// </summary>
    public sealed class FootageEvent
    {
        public string Segment { get; set; }
        public string EventId { get; set; }
        public double T { get; set; }
        public string Category { get; set; }
        public string Text { get; set; }
        public string CorrectedText { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string TranscriptSha256 { get; set; }

        public bool IsCitable => ManifestVocabulary.CitableFootageStatuses.Contains(Status);
    }

    /// <summary>Resolvable view of a footage-events file (only well-formed entries).</summary>
    public sealed class FootageEventSet
    {
        public static readonly FootageEventSet Empty = new FootageEventSet();

        public Dictionary<string, FootageTranscript> TranscriptsBySegment { get; } = new Dictionary<string, FootageTranscript>(StringComparer.Ordinal);
        public Dictionary<string, FootageEvent> EventsById { get; } = new Dictionary<string, FootageEvent>(StringComparer.Ordinal);
        public HashSet<string> DroppedEventIds { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class PositionRecord
    {
        public string PositionKey { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double? Rotation { get; set; }
        public string Tier { get; set; }
    }

    /// <summary>Resolvable view of a positions file (only well-formed entries).</summary>
    public sealed class PositionSet
    {
        public static readonly PositionSet Empty = new PositionSet();

        public Dictionary<string, PositionRecord> ByKey { get; } = new Dictionary<string, PositionRecord>(StringComparer.Ordinal);
    }

    /// <summary>Tolerant accessors over System.Text.Json elements.</summary>
    public static class JsonAccess
    {
        public static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        };

        public static bool TryGet(JsonElement obj, string name, out JsonElement value)
        {
            value = default;
            return obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out value);
        }

        public static bool Has(JsonElement obj, string name) => TryGet(obj, name, out _);

        public static bool TryString(JsonElement obj, string name, out string value)
        {
            value = null;
            if (!TryGet(obj, name, out var element) || element.ValueKind != JsonValueKind.String)
                return false;
            value = element.GetString();
            return true;
        }

        /// <summary>A present, non-blank string member.</summary>
        public static bool TryText(JsonElement obj, string name, out string value)
            => TryString(obj, name, out value) && !string.IsNullOrWhiteSpace(value);

        public static bool TryNumber(JsonElement obj, string name, out double value)
        {
            value = 0;
            return TryGet(obj, name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value);
        }

        public static bool IsInteger(JsonElement element)
            => element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out _);

        public static bool TryInteger(JsonElement obj, string name, out long value)
        {
            value = 0;
            return TryGet(obj, name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out value);
        }

        public static bool TryKind(JsonElement obj, string name, JsonValueKind kind, out JsonElement value)
            => TryGet(obj, name, out value) && value.ValueKind == kind;

        /// <summary>A non-empty string, or a non-empty array of non-blank strings.</summary>
        public static bool IsTextOrList(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return !string.IsNullOrWhiteSpace(element.GetString());
            return element.ValueKind == JsonValueKind.Array
                && element.GetArrayLength() > 0
                && element.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(e.GetString()));
        }

        /// <summary>Order-independent canonical text of an object (for key identity).</summary>
        public static string Canonical(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return "{" + string.Join(",", element.EnumerateObject()
                        .OrderBy(p => p.Name, StringComparer.Ordinal)
                        .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical(p.Value))) + "}";
                case JsonValueKind.Array:
                    return "[" + string.Join(",", element.EnumerateArray().Select(Canonical)) + "]";
                case JsonValueKind.Number:
                    return element.TryGetInt64(out var integer)
                        ? integer.ToString(CultureInfo.InvariantCulture)
                        : element.GetDouble().ToString("R", CultureInfo.InvariantCulture);
                default:
                    return element.GetRawText();
            }
        }
    }
}
