using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rasa.Test.Reconstruction
{
    /// <summary>
    /// Stable rule ids. Every message the validator emits names one of them, and every
    /// rule has a rejecting fixture under Reconstruction/Fixtures (enforced by tests).
    /// </summary>
    public static class ManifestRules
    {
        public const string DocumentParse = "document.parse";

        // Schema-level structure (docs/evidence/reconstruction-manifest.schema.json).
        public const string SchemaRequired = "schema.required";
        public const string SchemaConst = "schema.const";
        public const string SegmentRequired = "segment.required";
        public const string SourceKind = "source.kind";
        public const string SourceEra = "source.era";
        public const string ScopeRequired = "scope.required";
        public const string ScopeKeyRange = "scope.key-range";
        public const string RowRequired = "row.required";
        public const string RowSlice = "row.slice";
        public const string RowUnique = "row.unique";
        public const string RowScopeRange = "row.scope-range";
        public const string StorageRequired = "storage.required";
        public const string FieldRequired = "field.required";
        public const string FieldTier = "field.tier";
        public const string FieldCitations = "field.citations";
        public const string CitationRequired = "citation.required";
        public const string CitationSourceExists = "citation.source-exists";
        public const string CitationEventT = "citation.event-t";
        public const string UncertaintyRequired = "uncertainty.required";
        public const string OmittedRequired = "omitted.required";
        public const string OmittedGapExists = "omitted.gap-exists";
        public const string AcceptedGapRequired = "accepted-gap.required";
        public const string GapRequired = "gap.required";
        public const string GapUnique = "gap.unique";
        public const string SettingRequired = "setting.required";
        public const string ChangeRequired = "change.required";
        public const string DecisionRequired = "decision.required";

        // Tier rules (build plan section 1.7, "Tier rules").
        public const string OriginalLocator = "original.locator";
        public const string OriginalSourceKind = "original.source-kind";
        public const string ObservedEvent = "observed.event";
        public const string ObservedSourceKind = "observed.source-kind";
        public const string ObservedEventMissing = "observed.event-missing";
        public const string ObservedEventExcluded = "observed.event-excluded";
        public const string ObservedTime = "observed.time";
        public const string ObservedVideo = "observed.video";
        public const string ObservedCorrectedText = "observed.corrected-text";
        public const string MeasuredUncertainty = "measured.uncertainty";
        public const string MeasuredReference = "measured.reference";
        public const string MeasuredSourceKind = "measured.source-kind";
        public const string MeasuredPositionKey = "measured.position-key";
        public const string InferredReasoning = "inferred.reasoning";
        public const string AnalogueCounterpart = "analogue.counterpart";
        public const string AnalogueRequiredBecause = "analogue.required-because";
        public const string AnalogueDecision = "analogue.decision";
        public const string AnalogueDecisionApproved = "analogue.decision-approved";
        public const string AnaloguePreD11 = "analogue.pre-d11";
        public const string AnalogueOptionalColumn = "analogue.optional-column";
        public const string AnalogueUnregisteredColumn = "analogue.unregistered-column";

        // Footage-events file contract.
        public const string FootageSchema = "footage.schema";
        public const string FootageRequired = "footage.required";
        public const string FootageStatus = "footage.status";
        public const string FootageUnique = "footage.unique";
        public const string FootageTranscript = "footage.transcript";
        public const string FootageCorrectedText = "footage.corrected-text";
        public const string FootageDropped = "footage.dropped";

        // Positions file contract.
        public const string PositionsSchema = "positions.schema";
        public const string PositionsRequired = "positions.required";
        public const string PositionsUnique = "positions.unique";
        public const string PositionsTier = "positions.tier";

        public static IReadOnlyList<string> All { get; } = typeof(ManifestRules)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue())
            .ToArray();

        public static IReadOnlyList<string> TierRules { get; } = All
            .Where(r => ManifestVocabulary.Tiers.Any(t => r.StartsWith(t + ".", StringComparison.Ordinal)))
            .ToArray();
    }

    /// <summary>
    /// Enforces the reconstruction manifest contract: the schema-level required members
    /// and every tier rule of build plan section 1.7, which .NET 5 cannot check with a
    /// JSON Schema validator. Also validates the companion footage-event and position files.
    /// </summary>
    public sealed class ManifestValidator
    {
        private static readonly Regex Sha256Pattern = new Regex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant);
        private static readonly Regex DatePattern = new Regex("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", RegexOptions.CultureInvariant);

        private readonly ProvenanceRegistry _registry;

        public ManifestValidator() : this(ProvenanceRegistry.Default)
        {
        }

        public ManifestValidator(ProvenanceRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>Validates a manifest together with its footage-event and position files.</summary>
        public ValidationReport ValidateBundle(string manifestJson, string footageEventsJson, string positionsJson)
        {
            var report = new ValidationReport();
            report.AddRange(ValidateFootageEvents(footageEventsJson, out var footage));
            report.AddRange(ValidatePositions(positionsJson, out var positions));
            report.AddRange(ValidateManifest(manifestJson, footage, positions));
            return report;
        }

        public ValidationReport ValidateManifest(string json, FootageEventSet footage, PositionSet positions)
        {
            var report = new ValidationReport();
            if (!TryParse(json, "manifest", report, out var document))
                return report;
            using (document)
            {
                new ManifestRun(document.RootElement, footage ?? FootageEventSet.Empty, positions ?? PositionSet.Empty, _registry, report).Validate();
            }
            return report;
        }

        private static bool TryParse(string json, string what, ValidationReport report, out JsonDocument document)
        {
            document = null;
            try
            {
                document = JsonDocument.Parse(json ?? string.Empty, JsonAccess.DocumentOptions);
            }
            catch (JsonException e)
            {
                report.Add(ManifestRules.DocumentParse, "$", $"{what} is not valid JSON: {e.Message}");
                return false;
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return true;
            report.Add(ManifestRules.DocumentParse, "$", $"{what} root must be a JSON object");
            document.Dispose();
            document = null;
            return false;
        }

        private static bool IsSha256(JsonElement obj, string name)
            => JsonAccess.TryString(obj, name, out var value) && Sha256Pattern.IsMatch(value);

        private static bool IsDate(JsonElement obj, string name)
            => JsonAccess.TryString(obj, name, out var value) && DatePattern.IsMatch(value);

        #region Footage events

        public static ValidationReport ValidateFootageEvents(string json, out FootageEventSet set)
        {
            set = new FootageEventSet();
            var report = new ValidationReport();
            if (!TryParse(json, "footage-events file", report, out var document))
                return report;
            using (document)
            {
                var root = document.RootElement;
                if (!JsonAccess.TryString(root, "schema", out var schema) || schema != ManifestVocabulary.FootageEventsSchemaId)
                    report.Add(ManifestRules.FootageSchema, "schema", $"must be \"{ManifestVocabulary.FootageEventsSchemaId}\"");

                if (JsonAccess.TryKind(root, "transcripts", JsonValueKind.Array, out var transcripts))
                    ReadTranscripts(transcripts, set, report);
                else
                    report.Add(ManifestRules.FootageRequired, "transcripts", "missing required array");

                if (JsonAccess.TryKind(root, "events", JsonValueKind.Array, out var events))
                    ReadEvents(events, set, report);
                else
                    report.Add(ManifestRules.FootageRequired, "events", "missing required array");

                if (JsonAccess.TryGet(root, "dropped", out var dropped))
                {
                    if (dropped.ValueKind == JsonValueKind.Array)
                        ReadDropped(dropped, set, report);
                    else
                        report.Add(ManifestRules.FootageRequired, "dropped", "must be an array when present");
                }
            }
            return report;
        }

        private static void ReadTranscripts(JsonElement transcripts, FootageEventSet set, ValidationReport report)
        {
            var index = 0;
            foreach (var entry in transcripts.EnumerateArray())
            {
                var path = $"transcripts[{index++}]";
                var missing = new List<string>();
                if (!JsonAccess.TryText(entry, "segment", out var segment)) missing.Add("segment");
                if (!JsonAccess.TryText(entry, "video_id", out var videoId)) missing.Add("video_id");
                if (!IsSha256(entry, "transcript_sha256")) missing.Add("transcript_sha256 (64 lowercase hex)");
                if (!IsSha256(entry, "verification_sha256")) missing.Add("verification_sha256 (64 lowercase hex)");
                if (missing.Count > 0)
                {
                    report.Add(ManifestRules.FootageRequired, path, "missing or malformed: " + string.Join(", ", missing));
                    continue;
                }

                if (set.TranscriptsBySegment.ContainsKey(segment))
                {
                    report.Add(ManifestRules.FootageUnique, path, $"segment {segment} is listed twice");
                    continue;
                }

                JsonAccess.TryString(entry, "transcript_sha256", out var sha);
                set.TranscriptsBySegment[segment] = new FootageTranscript { Segment = segment, VideoId = videoId, TranscriptSha256 = sha };
            }
        }

        private static void ReadEvents(JsonElement events, FootageEventSet set, ValidationReport report)
        {
            var index = 0;
            foreach (var entry in events.EnumerateArray())
            {
                var path = $"events[{index++}]";
                var missing = new List<string>();
                if (!JsonAccess.TryText(entry, "segment", out var segment)) missing.Add("segment");
                if (!JsonAccess.TryText(entry, "event_id", out var eventId)) missing.Add("event_id");
                if (!JsonAccess.TryNumber(entry, "t", out var t) || t < 0) missing.Add("t (non-negative seconds)");
                if (!JsonAccess.TryText(entry, "category", out var category)) missing.Add("category");
                string text = null;
                if (!JsonAccess.TryGet(entry, "text", out var textElement)
                    || (textElement.ValueKind != JsonValueKind.String && textElement.ValueKind != JsonValueKind.Null))
                    missing.Add("text (string or null)");
                else if (textElement.ValueKind == JsonValueKind.String)
                    text = textElement.GetString();
                if (!JsonAccess.TryText(entry, "description", out var description)) missing.Add("description");
                if (!JsonAccess.TryText(entry, "status", out var status)) missing.Add("status");
                if (!IsSha256(entry, "transcript_sha256")) missing.Add("transcript_sha256 (64 lowercase hex)");
                if (missing.Count > 0)
                {
                    report.Add(ManifestRules.FootageRequired, path, "missing or malformed: " + string.Join(", ", missing));
                    continue;
                }

                path = $"{path}({eventId})";
                JsonAccess.TryString(entry, "transcript_sha256", out var sha);
                string correctedText = null;
                if (JsonAccess.TryGet(entry, "corrected_text", out var corrected))
                {
                    if (corrected.ValueKind != JsonValueKind.String)
                        report.Add(ManifestRules.FootageCorrectedText, path, "corrected_text must be a string when present");
                    else
                    {
                        correctedText = corrected.GetString();
                        if (status != "corrected")
                            report.Add(ManifestRules.FootageCorrectedText, path, $"corrected_text requires status \"corrected\", found \"{status}\"");
                        if (text != correctedText)
                            report.Add(ManifestRules.FootageCorrectedText, path, "text must carry the corrected_text once a correction is applied");
                    }
                }

                if (!ManifestVocabulary.CitableFootageStatuses.Contains(status))
                    report.Add(ManifestRules.FootageStatus, path,
                        $"status \"{status}\" is not confirmed or corrected; refuted events belong in dropped, never in events");

                if (!set.TranscriptsBySegment.TryGetValue(segment, out var transcript))
                    report.Add(ManifestRules.FootageTranscript, path, $"segment {segment} has no transcripts entry");
                else if (transcript.TranscriptSha256 != sha)
                    report.Add(ManifestRules.FootageTranscript, path, $"transcript_sha256 differs from the transcripts entry for segment {segment}");

                if (set.EventsById.ContainsKey(eventId))
                {
                    report.Add(ManifestRules.FootageUnique, path, $"event {eventId} is listed twice");
                    continue;
                }

                set.EventsById[eventId] = new FootageEvent
                {
                    Segment = segment,
                    EventId = eventId,
                    T = t,
                    Category = category,
                    Text = text,
                    CorrectedText = correctedText,
                    Description = description,
                    Status = status,
                    TranscriptSha256 = sha
                };
            }
        }

        private static void ReadDropped(JsonElement dropped, FootageEventSet set, ValidationReport report)
        {
            var index = 0;
            foreach (var entry in dropped.EnumerateArray())
            {
                var path = $"dropped[{index++}]";
                if (!JsonAccess.TryText(entry, "segment", out _)
                    || !JsonAccess.TryText(entry, "event_id", out var eventId)
                    || !JsonAccess.TryString(entry, "status", out var status) || status != "refuted"
                    || !IsSha256(entry, "transcript_sha256"))
                {
                    report.Add(ManifestRules.FootageDropped, path, "needs segment, event_id, status \"refuted\" and transcript_sha256");
                    continue;
                }

                if (set.EventsById.ContainsKey(eventId))
                    report.Add(ManifestRules.FootageDropped, path, $"event {eventId} is both dropped and listed in events");
                set.DroppedEventIds.Add(eventId);
            }
        }

        #endregion

        #region Positions

        public static ValidationReport ValidatePositions(string json, out PositionSet set)
        {
            set = new PositionSet();
            var report = new ValidationReport();
            if (!TryParse(json, "positions file", report, out var document))
                return report;
            using (document)
            {
                var root = document.RootElement;
                if (!JsonAccess.TryString(root, "schema", out var schema) || schema != ManifestVocabulary.PositionsSchemaId)
                    report.Add(ManifestRules.PositionsSchema, "schema", $"must be \"{ManifestVocabulary.PositionsSchemaId}\"");

                if (!JsonAccess.TryKind(root, "positions", JsonValueKind.Array, out var positions))
                {
                    report.Add(ManifestRules.PositionsRequired, "positions", "missing required array");
                    return report;
                }

                var index = 0;
                foreach (var entry in positions.EnumerateArray())
                {
                    var path = $"positions[{index++}]";
                    var missing = new List<string>();
                    if (!JsonAccess.TryText(entry, "position_key", out var key)) missing.Add("position_key");
                    if (!JsonAccess.TryNumber(entry, "x", out var x)) missing.Add("x");
                    if (!JsonAccess.TryNumber(entry, "y", out var y)) missing.Add("y");
                    if (!JsonAccess.TryNumber(entry, "z", out var z)) missing.Add("z");
                    double? rotation = null;
                    if (JsonAccess.TryGet(entry, "rotation", out _))
                    {
                        if (JsonAccess.TryNumber(entry, "rotation", out var r)) rotation = r;
                        else missing.Add("rotation (number when present)");
                    }
                    if (!JsonAccess.TryText(entry, "tier", out var tier)) missing.Add("tier");
                    if (missing.Count > 0)
                    {
                        report.Add(ManifestRules.PositionsRequired, path, "missing or malformed: " + string.Join(", ", missing));
                        continue;
                    }

                    path = $"{path}({key})";
                    ValidatePositionTier(entry, tier, path, report);
                    if (set.ByKey.ContainsKey(key))
                    {
                        report.Add(ManifestRules.PositionsUnique, path, $"position_key {key} is listed twice");
                        continue;
                    }
                    set.ByKey[key] = new PositionRecord { PositionKey = key, X = x, Y = y, Z = z, Rotation = rotation, Tier = tier };
                }
            }
            return report;
        }

        private static void ValidatePositionTier(JsonElement entry, string tier, string path, ValidationReport report)
        {
            switch (tier)
            {
                case "measured":
                    if (!JsonAccess.TryKind(entry, "uncertainty", JsonValueKind.Object, out var uncertainty)
                        || !JsonAccess.TryNumber(uncertainty, "horizontal_m", out var horizontal) || horizontal < 0
                        || !JsonAccess.TryNumber(uncertainty, "vertical_m", out var vertical) || vertical < 0)
                        report.Add(ManifestRules.PositionsTier, path, "a measured position needs uncertainty {horizontal_m, vertical_m} (non-negative metres)");
                    if (!JsonAccess.TryText(entry, "method", out _))
                        report.Add(ManifestRules.PositionsTier, path, "a measured position needs its method");
                    break;
                case "inferred":
                    if (!JsonAccess.TryText(entry, "reasoning", out _))
                        report.Add(ManifestRules.PositionsTier, path, "an inferred position needs reasoning");
                    break;
                case "original":
                    if (!JsonAccess.TryKind(entry, "anchors", JsonValueKind.Array, out var anchors) || anchors.GetArrayLength() == 0)
                        report.Add(ManifestRules.PositionsTier, path, "an original position needs anchors (map entity index and file offset)");
                    break;
                default:
                    report.Add(ManifestRules.PositionsTier, path,
                        $"tier \"{tier}\" is not one of {string.Join(", ", ManifestVocabulary.PositionTiers)}");
                    break;
            }
        }

        #endregion

        private sealed class CitationInfo
        {
            public string Path;
            public string SourceId;
            public ManifestSource Source;
            public bool SourceUnresolved;
            public string Event;
            public double? T;
            public string Locator;
            public string PositionKey;
        }

        private sealed class ScopeEntry
        {
            public string Table;
            public List<(string Column, long Min, long Max)> Ranges = new List<(string, long, long)>();
        }

        /// <summary>One validation pass over a parsed manifest.</summary>
        private sealed class ManifestRun
        {
            private readonly JsonElement _root;
            private readonly FootageEventSet _footage;
            private readonly PositionSet _positions;
            private readonly ProvenanceRegistry _registry;
            private readonly ValidationReport _report;

            private readonly Dictionary<string, ManifestSource> _sources = new Dictionary<string, ManifestSource>(StringComparer.Ordinal);
            private readonly Dictionary<string, OwnerDecision> _decisions = new Dictionary<string, OwnerDecision>(StringComparer.Ordinal);
            private readonly HashSet<string> _gapIds = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<ScopeEntry> _scope = new List<ScopeEntry>();
            private bool _sourcesKnown;
            private bool _gapsKnown;

            public ManifestRun(JsonElement root, FootageEventSet footage, PositionSet positions, ProvenanceRegistry registry, ValidationReport report)
            {
                _root = root;
                _footage = footage;
                _positions = positions;
                _registry = registry;
                _report = report;
            }

            private void Error(string rule, string path, string message) => _report.Add(rule, path, message);

            public void Validate()
            {
                ValidateTopLevel();
                ValidateSegment();
                ReadSources();
                ReadOwnerDecisions();
                ReadGaps();
                ReadScope();
                ValidateRows();
                ValidateOmitted();
                ValidateAcceptedGaps();
                ValidateSettings();
                ValidateChanges();
            }

            private static readonly Dictionary<string, JsonValueKind> TopLevelKinds = new Dictionary<string, JsonValueKind>
            {
                ["schema"] = JsonValueKind.String,
                ["policy"] = JsonValueKind.String,
                ["segment"] = JsonValueKind.Object,
                ["sources"] = JsonValueKind.Object,
                ["scope"] = JsonValueKind.Array,
                ["rows"] = JsonValueKind.Array,
                ["omitted"] = JsonValueKind.Array,
                ["accepted_definition_gaps"] = JsonValueKind.Array,
                ["accepted_content_gaps"] = JsonValueKind.Array,
                ["gaps"] = JsonValueKind.Array,
                ["non_content_settings"] = JsonValueKind.Array,
                ["changes"] = JsonValueKind.Array
            };

            private void ValidateTopLevel()
            {
                foreach (var name in ManifestVocabulary.RequiredTopLevel)
                {
                    if (!JsonAccess.TryGet(_root, name, out var element))
                        Error(ManifestRules.SchemaRequired, name, "missing required member");
                    else if (element.ValueKind != TopLevelKinds[name])
                        Error(ManifestRules.SchemaRequired, name, $"must be a JSON {TopLevelKinds[name].ToString().ToLowerInvariant()}");
                }

                if (JsonAccess.TryString(_root, "schema", out var schema) && schema != ManifestVocabulary.ManifestSchemaId)
                    Error(ManifestRules.SchemaConst, "schema", $"must be \"{ManifestVocabulary.ManifestSchemaId}\", found \"{schema}\"");
                if (JsonAccess.TryString(_root, "policy", out var policy) && policy != ManifestVocabulary.PolicyAnchor)
                    Error(ManifestRules.SchemaConst, "policy", $"must be \"{ManifestVocabulary.PolicyAnchor}\", found \"{policy}\"");
            }

            private static bool IsIntegerArray(JsonElement obj, string name)
                => JsonAccess.TryKind(obj, name, JsonValueKind.Array, out var array) && array.EnumerateArray().All(JsonAccess.IsInteger);

            private void ValidateSegment()
            {
                if (!JsonAccess.TryKind(_root, "segment", JsonValueKind.Object, out var segment))
                    return;
                if (!JsonAccess.TryText(segment, "id", out _))
                    Error(ManifestRules.SegmentRequired, "segment.id", "must be a non-empty string");
                if (!IsIntegerArray(segment, "map_context_ids"))
                    Error(ManifestRules.SegmentRequired, "segment.map_context_ids", "must be an array of integers");
                if (!JsonAccess.TryInteger(segment, "map_version", out _))
                    Error(ManifestRules.SegmentRequired, "segment.map_version", "must be an integer");
                if (!IsIntegerArray(segment, "missions"))
                    Error(ManifestRules.SegmentRequired, "segment.missions", "must be an array of integers");
                if (!JsonAccess.TryText(segment, "client_version", out _))
                    Error(ManifestRules.SegmentRequired, "segment.client_version", "must be a non-empty string");
            }

            private void ReadSources()
            {
                if (!JsonAccess.TryKind(_root, "sources", JsonValueKind.Object, out var sources))
                    return;
                _sourcesKnown = true;
                foreach (var property in sources.EnumerateObject())
                {
                    var path = $"sources.{property.Name}";
                    var entry = property.Value;
                    JsonAccess.TryString(entry, "kind", out var kind);
                    JsonAccess.TryString(entry, "era", out var era);
                    JsonAccess.TryString(entry, "video_id", out var videoId);
                    JsonAccess.TryString(entry, "sha256", out var sha);
                    if (entry.ValueKind != JsonValueKind.Object || !ManifestVocabulary.SourceKinds.Contains(kind))
                        Error(ManifestRules.SourceKind, path,
                            $"kind \"{kind}\" is not one of {string.Join(", ", ManifestVocabulary.SourceKinds)}");
                    if (entry.ValueKind != JsonValueKind.Object || !ManifestVocabulary.Eras.Contains(era))
                        Error(ManifestRules.SourceEra, path,
                            $"era \"{era}\" is not one of {string.Join(", ", ManifestVocabulary.Eras)}");
                    _sources[property.Name] = new ManifestSource { Id = property.Name, Kind = kind, Era = era, VideoId = videoId, Sha256 = sha };
                }
            }

            private void ReadOwnerDecisions()
            {
                if (!JsonAccess.TryGet(_root, "owner_decisions", out var decisions))
                    return;
                if (decisions.ValueKind != JsonValueKind.Array)
                {
                    Error(ManifestRules.DecisionRequired, "owner_decisions", "must be an array when present");
                    return;
                }

                var index = 0;
                foreach (var entry in decisions.EnumerateArray())
                {
                    var path = $"owner_decisions[{index++}]";
                    var missing = new List<string>();
                    if (!JsonAccess.TryText(entry, "id", out var id)) missing.Add("id");
                    if (!JsonAccess.TryText(entry, "decision", out _)) missing.Add("decision");
                    if (!JsonAccess.TryString(entry, "status", out var status) || !ManifestVocabulary.DecisionStatuses.Contains(status))
                        missing.Add("status (open, approved or rejected)");
                    if (status == "approved")
                    {
                        if (!JsonAccess.TryText(entry, "choice", out _)) missing.Add("choice");
                        if (!IsDate(entry, "approved_on")) missing.Add("approved_on (yyyy-mm-dd)");
                        if (!JsonAccess.TryText(entry, "record", out _)) missing.Add("record");
                    }
                    if (missing.Count > 0)
                        Error(ManifestRules.DecisionRequired, path, "missing or malformed: " + string.Join(", ", missing));
                    if (id != null && !_decisions.ContainsKey(id))
                        _decisions[id] = new OwnerDecision { Id = id, Status = status };
                }
            }

            private void ReadGaps()
            {
                if (!JsonAccess.TryKind(_root, "gaps", JsonValueKind.Array, out var gaps))
                    return;
                _gapsKnown = true;
                var index = 0;
                foreach (var entry in gaps.EnumerateArray())
                {
                    var path = $"gaps[{index++}]";
                    var missing = new List<string>();
                    if (!JsonAccess.TryText(entry, "id", out var id) || !id.StartsWith("GAP-", StringComparison.Ordinal)) missing.Add("id (GAP-...)");
                    if (!JsonAccess.TryText(entry, "summary", out _)) missing.Add("summary");
                    if (!JsonAccess.TryGet(entry, "evidence", out var evidence) || !JsonAccess.IsTextOrList(evidence)) missing.Add("evidence");
                    if (!JsonAccess.TryText(entry, "closes_with", out _)) missing.Add("closes_with");
                    if (!JsonAccess.TryGet(entry, "surfaced_by", out var surfaced) || !JsonAccess.IsTextOrList(surfaced)) missing.Add("surfaced_by");
                    if (missing.Count > 0)
                        Error(ManifestRules.GapRequired, path, "missing or malformed: " + string.Join(", ", missing));
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    if (!_gapIds.Add(id))
                        Error(ManifestRules.GapUnique, path, $"gap id {id} is listed twice");
                }
            }

            private void ReadScope()
            {
                if (!JsonAccess.TryKind(_root, "scope", JsonValueKind.Array, out var scope))
                    return;
                var index = 0;
                foreach (var entry in scope.EnumerateArray())
                {
                    var path = $"scope[{index++}]";
                    if (!JsonAccess.TryText(entry, "table", out var table) || !JsonAccess.TryKind(entry, "key_ranges", JsonValueKind.Object, out var ranges))
                    {
                        Error(ManifestRules.ScopeRequired, path, "needs table and a key_ranges object");
                        continue;
                    }

                    var scopeEntry = new ScopeEntry { Table = table };
                    var valid = true;
                    foreach (var range in ranges.EnumerateObject())
                    {
                        if (!JsonAccess.TryInteger(range.Value, "min", out var min) || !JsonAccess.TryInteger(range.Value, "max", out var max))
                        {
                            Error(ManifestRules.ScopeKeyRange, $"{path}.key_ranges.{range.Name}", "must be {min, max} integers");
                            valid = false;
                            continue;
                        }
                        if (min > max)
                        {
                            Error(ManifestRules.ScopeKeyRange, $"{path}.key_ranges.{range.Name}", $"min {min} is greater than max {max}");
                            valid = false;
                            continue;
                        }
                        scopeEntry.Ranges.Add((range.Name, min, max));
                    }
                    if (scopeEntry.Ranges.Count == 0 && valid)
                    {
                        Error(ManifestRules.ScopeKeyRange, $"{path}.key_ranges", "declares no key range");
                        valid = false;
                    }
                    if (valid)
                        _scope.Add(scopeEntry);
                }
            }

            private void ValidateRows()
            {
                if (!JsonAccess.TryKind(_root, "rows", JsonValueKind.Array, out var rows))
                    return;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var index = 0;
                foreach (var row in rows.EnumerateArray())
                {
                    var rowIndex = index++;
                    var hasTable = JsonAccess.TryText(row, "table", out var table);
                    var hasKey = JsonAccess.TryKind(row, "key", JsonValueKind.Object, out var key);
                    var label = $"rows[{rowIndex}]" + (hasTable && hasKey ? $"({table} {JsonAccess.Canonical(key)})" : string.Empty);

                    var missing = new List<string>();
                    if (!hasTable) missing.Add("table");
                    if (!hasKey) missing.Add("key (object)");
                    if (!JsonAccess.TryGet(row, "slice", out var slice) || slice.ValueKind != JsonValueKind.String) missing.Add("slice");
                    if (!JsonAccess.TryText(row, "migration", out _)) missing.Add("migration");
                    var hasFields = JsonAccess.TryKind(row, "fields", JsonValueKind.Object, out var fields);
                    if (!hasFields) missing.Add("fields (object)");
                    if (missing.Count > 0)
                        Error(ManifestRules.RowRequired, label, "missing or malformed: " + string.Join(", ", missing));

                    if (slice.ValueKind == JsonValueKind.String && !ManifestVocabulary.Slices.Contains(slice.GetString()))
                        Error(ManifestRules.RowSlice, label, $"slice \"{slice.GetString()}\" is not one of {string.Join(", ", ManifestVocabulary.Slices)}");

                    if (hasTable && hasKey)
                    {
                        if (!seen.Add(table + " " + JsonAccess.Canonical(key)))
                            Error(ManifestRules.RowUnique, label, "the same table and key appear in more than one row");
                        CheckScope(label, table, key);
                    }

                    if (JsonAccess.TryGet(row, "storage_fields", out var storage))
                        ValidateStorageFields(label, storage);

                    if (hasFields)
                        foreach (var field in fields.EnumerateObject())
                            ValidateField($"{label}.fields.{field.Name}", hasTable ? table : null, field.Name, field.Value);
                }
            }

            private void CheckScope(string label, string table, JsonElement key)
            {
                var entries = _scope.Where(s => s.Table == table).ToList();
                if (entries.Count == 0)
                    return;
                foreach (var entry in entries)
                {
                    if (entry.Ranges.All(r => JsonAccess.TryInteger(key, r.Column, out var value) && value >= r.Min && value <= r.Max))
                        return;
                }
                var ranges = string.Join(" or ", entries.Select(e => string.Join(", ", e.Ranges.Select(r => $"{r.Column} {r.Min}-{r.Max}"))));
                Error(ManifestRules.RowScopeRange, label, $"key lies outside the reserved scope for {table} ({ranges})");
            }

            private void ValidateStorageFields(string label, JsonElement storage)
            {
                if (storage.ValueKind != JsonValueKind.Object)
                {
                    Error(ManifestRules.StorageRequired, $"{label}.storage_fields", "must be an object when present");
                    return;
                }
                foreach (var property in storage.EnumerateObject())
                {
                    var path = $"{label}.storage_fields.{property.Name}";
                    if (!JsonAccess.Has(property.Value, "value"))
                        Error(ManifestRules.StorageRequired, path, "missing value");
                    if (!JsonAccess.TryString(property.Value, "role", out var role) || !ManifestVocabulary.StorageRoles.Contains(role))
                        Error(ManifestRules.StorageRequired, path,
                            $"role \"{role}\" is not one of {string.Join(", ", ManifestVocabulary.StorageRoles)}");
                }
            }

            private void ValidateField(string path, string table, string column, JsonElement field)
            {
                if (field.ValueKind != JsonValueKind.Object)
                {
                    Error(ManifestRules.FieldRequired, path, "a field must be an object with value, tier and citations");
                    return;
                }

                var hasValue = JsonAccess.TryGet(field, "value", out var value);
                if (!hasValue)
                    Error(ManifestRules.FieldRequired, path, "missing value");

                string tier = null;
                if (!JsonAccess.TryString(field, "tier", out var tierText))
                    Error(ManifestRules.FieldRequired, path, "missing tier");
                else if (!ManifestVocabulary.Tiers.Contains(tierText))
                    Error(ManifestRules.FieldTier, path, $"tier \"{tierText}\" is not one of {string.Join(", ", ManifestVocabulary.Tiers)}");
                else
                    tier = tierText;

                var citations = new List<CitationInfo>();
                if (!JsonAccess.TryKind(field, "citations", JsonValueKind.Array, out var citationArray))
                    Error(ManifestRules.FieldRequired, path, "missing citations array");
                else if (citationArray.GetArrayLength() == 0)
                    Error(ManifestRules.FieldCitations, path, "needs at least one citation");
                else
                    citations = ReadCitations(path, citationArray);

                var hasUncertainty = JsonAccess.TryGet(field, "uncertainty", out var uncertainty);
                if (hasUncertainty)
                    ValidateUncertainty($"{path}.uncertainty", uncertainty);

                if (tier == null)
                    return;

                var resolved = citations.Where(c => c.Source != null).ToList();
                var anyUnresolved = citations.Any(c => c.SourceUnresolved);
                var label = $"{path} ({tier})";

                switch (tier)
                {
                    case "original":
                        if (!resolved.Any(c => ManifestVocabulary.OriginalSourceKinds.Contains(c.Source.Kind) && !string.IsNullOrWhiteSpace(c.Locator)) && !anyUnresolved)
                        {
                            if (citations.Any(c => !string.IsNullOrWhiteSpace(c.Locator)))
                                Error(ManifestRules.OriginalSourceKind, label,
                                    "an original value needs a located citation from client_table, client_code, client_map or official_notes; video or screenshot support alone is rejected");
                            else
                                Error(ManifestRules.OriginalLocator, label,
                                    "an original value needs at least one citation with a locator (member + store offset, map entity index + file offset, or notes capture + section)");
                        }
                        break;

                    case "observed":
                        var eventCitations = citations.Where(c => c.Event != null && c.T.HasValue).ToList();
                        if (eventCitations.Count == 0)
                        {
                            Error(ManifestRules.ObservedEvent, label, "an observed value needs at least one citation with a footage event and t");
                            break;
                        }
                        if (!eventCitations.Any(c => c.Source != null && ManifestVocabulary.ObservedSourceKinds.Contains(c.Source.Kind)) && !anyUnresolved)
                            Error(ManifestRules.ObservedSourceKind, label, "an observed value's event citation must come from a video or screenshot source");
                        if (hasValue && value.ValueKind == JsonValueKind.String)
                        {
                            foreach (var citation in eventCitations)
                            {
                                if (_footage.EventsById.TryGetValue(citation.Event, out var footageEvent)
                                    && footageEvent.IsCitable
                                    && footageEvent.CorrectedText != null
                                    && value.GetString() != footageEvent.CorrectedText)
                                    Error(ManifestRules.ObservedCorrectedText, label,
                                        $"value \"{value.GetString()}\" differs from the corrected text of event {citation.Event}: \"{footageEvent.CorrectedText}\"");
                            }
                        }
                        break;

                    case "measured":
                        if (!hasUncertainty)
                            Error(ManifestRules.MeasuredUncertainty, label, "a measured value needs an uncertainty");
                        var measuredSupport = resolved.Any(c =>
                            ManifestVocabulary.MeasuredSourceKinds.Contains(c.Source.Kind)
                            && (c.PositionKey != null || c.Source.Kind == "measurement" || c.Source.Kind == "in_client_calibration" || c.T.HasValue));
                        if (!measuredSupport && !anyUnresolved)
                        {
                            if (citations.Any(c => c.PositionKey != null || c.T.HasValue))
                                Error(ManifestRules.MeasuredSourceKind, label,
                                    "a measured value's position key or frames must come from a measurement, in_client_calibration or video source");
                            else
                                Error(ManifestRules.MeasuredReference, label,
                                    "a measured value needs a citation to a position_key, a measurement file or video frames");
                        }
                        break;

                    case "inferred":
                        if (!JsonAccess.TryText(field, "reasoning", out _))
                            Error(ManifestRules.InferredReasoning, label, "an inferred value needs reasoning");
                        break;

                    case "analogue":
                        ValidateAnalogue(label, table, column, field, resolved);
                        break;
                }
            }

            private void ValidateAnalogue(string label, string table, string column, JsonElement field, List<CitationInfo> resolved)
            {
                if (!JsonAccess.TryText(field, "counterpart", out _))
                    Error(ManifestRules.AnalogueCounterpart, label, "an analogue needs its original counterpart");
                if (!JsonAccess.TryText(field, "required_because", out _))
                    Error(ManifestRules.AnalogueRequiredBecause, label, "an analogue needs required_because: why the content cannot function without a value");
                if (!JsonAccess.TryText(field, "decision", out var decision))
                    Error(ManifestRules.AnalogueDecision, label, "an analogue needs a decision reference to an approved owner decision");
                else if (!_decisions.TryGetValue(decision, out var ownerDecision) || !ownerDecision.IsApproved)
                    Error(ManifestRules.AnalogueDecisionApproved, label,
                        ownerDecision == null
                            ? $"decision {decision} is not listed in owner_decisions"
                            : $"decision {decision} has status \"{ownerDecision.Status}\", not approved");

                foreach (var citation in resolved.Where(c => c.Source.Era == ManifestVocabulary.PreRebuildEra))
                    Error(ManifestRules.AnaloguePreD11, $"{label} {citation.Path}",
                        $"source {citation.SourceId} is pre_d11; obsolete pre-rebuild content is never an analogue for rebuilt content");

                switch (_registry.RoleOf(table, column))
                {
                    case ColumnRole.Required:
                        break;
                    case ColumnRole.Key:
                        // A key column documented in fields is a deliberate reference choice
                        // (e.g. content_item_set.item_template_id), so its analogue is legitimate.
                        break;
                    case ColumnRole.Optional:
                        Error(ManifestRules.AnalogueOptionalColumn, label,
                            $"{table}.{column} is an optional column in ProvenanceRegistry; leave it out instead of using an analogue");
                        break;
                    default:
                        Error(ManifestRules.AnalogueUnregisteredColumn, label,
                            $"{table}.{column} is not a registered required column in ProvenanceRegistry; an analogue is allowed only where a value is required");
                        break;
                }
            }

            private List<CitationInfo> ReadCitations(string path, JsonElement array)
            {
                var result = new List<CitationInfo>();
                var index = 0;
                foreach (var element in array.EnumerateArray())
                {
                    var info = ReadCitation($"{path}.citations[{index++}]", element);
                    if (info != null)
                        result.Add(info);
                }
                return result;
            }

            private CitationInfo ReadCitation(string path, JsonElement citation)
            {
                if (citation.ValueKind != JsonValueKind.Object)
                {
                    Error(ManifestRules.CitationRequired, path, "a citation must be an object");
                    return null;
                }

                var info = new CitationInfo { Path = path };
                var malformed = new List<string>();
                if (!JsonAccess.TryText(citation, "source", out var sourceId))
                {
                    malformed.Add("source");
                    info.SourceUnresolved = true;
                    sourceId = null;
                }
                if (JsonAccess.TryGet(citation, "event", out var eventElement))
                {
                    if (eventElement.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(eventElement.GetString())) info.Event = eventElement.GetString();
                    else malformed.Add("event (string)");
                }
                if (JsonAccess.TryGet(citation, "t", out _))
                {
                    if (JsonAccess.TryNumber(citation, "t", out var t)) info.T = t;
                    else malformed.Add("t (number)");
                }
                if (JsonAccess.TryGet(citation, "locator", out var locator))
                {
                    if (locator.ValueKind == JsonValueKind.String) info.Locator = locator.GetString();
                    else malformed.Add("locator (string)");
                }
                if (JsonAccess.TryGet(citation, "position_key", out var positionKey))
                {
                    if (positionKey.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(positionKey.GetString())) info.PositionKey = positionKey.GetString();
                    else malformed.Add("position_key (string)");
                }
                if (malformed.Count > 0)
                    Error(ManifestRules.CitationRequired, path, "missing or malformed: " + string.Join(", ", malformed));

                if (sourceId != null)
                {
                    info.SourceId = sourceId;
                    if (_sources.TryGetValue(sourceId, out var source))
                        info.Source = source;
                    else
                    {
                        info.SourceUnresolved = true;
                        if (_sourcesKnown)
                            Error(ManifestRules.CitationSourceExists, path, $"source {sourceId} is not declared in sources");
                    }
                }

                if (info.Event != null && !info.T.HasValue)
                    Error(ManifestRules.CitationEventT, path, $"event {info.Event} is cited without t");

                if (info.Event != null && info.T.HasValue)
                    ResolveFootageEvent(path, info);

                if (info.PositionKey != null && !_positions.ByKey.ContainsKey(info.PositionKey))
                    Error(ManifestRules.MeasuredPositionKey, path, $"position_key {info.PositionKey} has no record in the positions file");

                return info;
            }

            private void ResolveFootageEvent(string path, CitationInfo citation)
            {
                var known = _footage.EventsById.TryGetValue(citation.Event, out var footageEvent);
                if (_footage.DroppedEventIds.Contains(citation.Event) || (known && !footageEvent.IsCitable))
                {
                    Error(ManifestRules.ObservedEventExcluded, path,
                        $"event {citation.Event} was refuted and excluded from the footage events; it cannot support a value");
                    return;
                }
                if (!known)
                {
                    Error(ManifestRules.ObservedEventMissing, path, $"event {citation.Event} is not in the footage events file");
                    return;
                }

                var delta = Math.Abs(citation.T.Value - footageEvent.T);
                if (delta > ManifestVocabulary.ObservedTimeTolerance + ManifestVocabulary.TimeComparisonEpsilon)
                    Error(ManifestRules.ObservedTime, path, string.Format(CultureInfo.InvariantCulture,
                        "t {0} differs from event {1} t {2} by {3:0.###} s (limit {4} s)",
                        citation.T.Value, citation.Event, footageEvent.T, delta, ManifestVocabulary.ObservedTimeTolerance));

                if (citation.Source?.Kind == "video"
                    && citation.Source.VideoId != null
                    && _footage.TranscriptsBySegment.TryGetValue(footageEvent.Segment, out var transcript)
                    && transcript.VideoId != citation.Source.VideoId)
                    Error(ManifestRules.ObservedVideo, path,
                        $"event {citation.Event} belongs to video {transcript.VideoId}, but source {citation.SourceId} is video {citation.Source.VideoId}");
            }

            private void ValidateUncertainty(string path, JsonElement uncertainty)
            {
                if (uncertainty.ValueKind != JsonValueKind.Object)
                {
                    Error(ManifestRules.UncertaintyRequired, path, "must be an object");
                    return;
                }
                var problems = new List<string>();
                if (!JsonAccess.TryText(uncertainty, "unit", out _))
                    problems.Add("unit");
                var hasPlusMinus = JsonAccess.TryNumber(uncertainty, "plus_minus", out var plusMinus);
                var hasHorizontal = JsonAccess.TryNumber(uncertainty, "horizontal_m", out var horizontal);
                var hasVertical = JsonAccess.TryNumber(uncertainty, "vertical_m", out var vertical);
                var hasMin = JsonAccess.TryNumber(uncertainty, "min", out var min);
                var hasMax = JsonAccess.TryNumber(uncertainty, "max", out var max);
                if (!hasPlusMinus && !hasHorizontal && !hasVertical && !(hasMin && hasMax))
                    problems.Add("a magnitude (plus_minus, horizontal_m, vertical_m, or min and max)");
                if ((hasPlusMinus && plusMinus < 0) || (hasHorizontal && horizontal < 0) || (hasVertical && vertical < 0))
                    problems.Add("non-negative magnitudes");
                if (hasMin && hasMax && min > max)
                    problems.Add("min not greater than max");
                if (problems.Count > 0)
                    Error(ManifestRules.UncertaintyRequired, path, "needs " + string.Join(", ", problems));
            }

            private void ValidateOmitted()
            {
                if (!JsonAccess.TryKind(_root, "omitted", JsonValueKind.Array, out var omitted))
                    return;
                var index = 0;
                foreach (var entry in omitted.EnumerateArray())
                {
                    var path = $"omitted[{index++}]";
                    var missing = new List<string>();
                    if (!JsonAccess.TryText(entry, "table", out _)) missing.Add("table");
                    if (!JsonAccess.TryText(entry, "what", out _)) missing.Add("what");
                    if (!JsonAccess.TryText(entry, "reason", out _)) missing.Add("reason");
                    if (!JsonAccess.TryText(entry, "gap", out var gap)) missing.Add("gap");
                    if (missing.Count > 0)
                        Error(ManifestRules.OmittedRequired, path, "missing or malformed: " + string.Join(", ", missing));
                    if (gap != null && _gapsKnown && !_gapIds.Contains(gap))
                        Error(ManifestRules.OmittedGapExists, path, $"gap {gap} is not in the gaps register");
                }
            }

            private static bool IsStringArray(JsonElement obj, string name)
                => JsonAccess.TryKind(obj, name, JsonValueKind.Array, out var array)
                   && array.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String);

            private void ValidateAcceptedGaps()
            {
                if (JsonAccess.TryKind(_root, "accepted_definition_gaps", JsonValueKind.Array, out var definitionGaps))
                {
                    var index = 0;
                    foreach (var entry in definitionGaps.EnumerateArray())
                    {
                        var missing = new List<string>();
                        if (!JsonAccess.TryInteger(entry, "mission_id", out _)) missing.Add("mission_id (integer)");
                        if (!IsStringArray(entry, "gaps")) missing.Add("gaps (array of strings)");
                        if (!JsonAccess.TryText(entry, "reason", out _)) missing.Add("reason");
                        if (!JsonAccess.TryString(entry, "slice", out var slice) || !ManifestVocabulary.Slices.Contains(slice)) missing.Add("slice (S1-S8)");
                        if (missing.Count > 0)
                            Error(ManifestRules.AcceptedGapRequired, $"accepted_definition_gaps[{index}]", "missing or malformed: " + string.Join(", ", missing));
                        index++;
                    }
                }

                if (JsonAccess.TryKind(_root, "accepted_content_gaps", JsonValueKind.Array, out var contentGaps))
                {
                    var index = 0;
                    foreach (var entry in contentGaps.EnumerateArray())
                    {
                        var missing = new List<string>();
                        if (!JsonAccess.TryText(entry, "table", out _)) missing.Add("table");
                        if (!JsonAccess.TryKind(entry, "key", JsonValueKind.Object, out _)) missing.Add("key (object)");
                        if (!IsStringArray(entry, "gaps")) missing.Add("gaps (array of strings)");
                        if (!JsonAccess.TryText(entry, "reason", out _)) missing.Add("reason");
                        if (missing.Count > 0)
                            Error(ManifestRules.AcceptedGapRequired, $"accepted_content_gaps[{index}]", "missing or malformed: " + string.Join(", ", missing));
                        index++;
                    }
                }
            }

            private void ValidateSettings()
            {
                if (!JsonAccess.TryKind(_root, "non_content_settings", JsonValueKind.Array, out var settings))
                    return;
                var index = 0;
                foreach (var entry in settings.EnumerateArray())
                {
                    if (!JsonAccess.TryText(entry, "key", out _) || !JsonAccess.TryText(entry, "note", out _))
                        Error(ManifestRules.SettingRequired, $"non_content_settings[{index}]", "needs key and note");
                    index++;
                }
            }

            private void ValidateChanges()
            {
                if (!JsonAccess.TryKind(_root, "changes", JsonValueKind.Array, out var changes))
                    return;
                var index = 0;
                foreach (var entry in changes.EnumerateArray())
                {
                    var path = $"changes[{index++}]";
                    var missing = new List<string>();
                    if (!IsDate(entry, "date")) missing.Add("date (yyyy-mm-dd)");
                    if (!JsonAccess.TryText(entry, "migration", out _)) missing.Add("migration");
                    if (!JsonAccess.TryText(entry, "table", out _)) missing.Add("table");
                    if (!JsonAccess.TryKind(entry, "key", JsonValueKind.Object, out _)) missing.Add("key (object)");
                    if (!JsonAccess.TryText(entry, "field", out _)) missing.Add("field");
                    if (!JsonAccess.Has(entry, "old")) missing.Add("old");
                    if (!JsonAccess.Has(entry, "new")) missing.Add("new");
                    if (!JsonAccess.TryText(entry, "reason", out _)) missing.Add("reason");
                    var hasCitations = JsonAccess.TryKind(entry, "citations", JsonValueKind.Array, out var citations) && citations.GetArrayLength() > 0;
                    if (!hasCitations) missing.Add("citations (at least one)");
                    if (missing.Count > 0)
                        Error(ManifestRules.ChangeRequired, path, "missing or malformed: " + string.Join(", ", missing));
                    if (hasCitations)
                        ReadCitations(path, citations);
                }
            }
        }
    }
}
