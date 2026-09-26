using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// Static client collision volumes used for a bounded cover reconstruction. The original
    /// client has a cover damage lookup and a cover UI, but the final-live server's sample points
    /// are lost. The nine samples below are inferred from a contemporary player's later account;
    /// their dimensions are estimates. See docs/evidence/bootcamp-cover-mechanics-gap.json.
    /// </summary>
    public sealed class CoverGeometryManager
    {
        private static readonly CoverGeometryManager InstanceValue = new CoverGeometryManager();
        private static readonly bool TraceCover = Environment.GetEnvironmentVariable("RASA_TRACE_COVER") == "1";
        private readonly Dictionary<string, CoverGeometry> _maps = new Dictionary<string, CoverGeometry>(StringComparer.OrdinalIgnoreCase);

        public static CoverGeometryManager Instance => InstanceValue;

        private CoverGeometryManager() { }

        public int ScaleRangedWeaponDamage(MapChannel map, Vector3 source, Vector3 target, int damage)
            => ScaleRangedWeaponDamage(map, source, target, damage, out _);

        public int ScaleRangedWeaponDamage(MapChannel map, Vector3 source, Vector3 target, int damage,
            out float coverModifier)
        {
            // No cover calculation has run yet. Preserve the client's original
            // integer-zero DamageInfo field when this map has no cover geometry.
            coverModifier = 0f;
            if (damage <= 0 || map?.MapInfo?.MapName == null)
                return damage;

            var name = map.MapInfo.MapName;
            if (!_maps.TryGetValue(name, out var geometry))
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Content", "Cover", name + ".cover.json");
                geometry = null;
                if (File.Exists(path))
                    try { geometry = CoverGeometry.Load(path); }
                    catch (Exception e) { Logger.WriteLog(LogType.Error, $"Could not load cover geometry {path}: {e.Message}"); }
                else if (string.Equals(name, "adv_bootcamp", StringComparison.OrdinalIgnoreCase))
                    Logger.WriteLog(LogType.Error, $"Expected boot-camp cover geometry is missing: {path}");
                _maps[name] = geometry;
                if (geometry != null)
                    Logger.WriteLog(LogType.Initialize, $"Loaded {geometry.TriangleCount} cover triangles for {name}");
            }

            if (geometry == null)
                return damage;
            var scaled = geometry.ScaleDamage(source, target, damage, out var blocked, out coverModifier);
            if (TraceCover)
                Logger.WriteLog(LogType.Debug,
                    $"Cover trace: map={name}, source={source}, target={target}, blocked={blocked}/9, damage={damage}->{scaled}");
            return scaled;
        }
    }

    public sealed class CoverGeometry
    {
        private readonly List<Triangle> _triangles = new List<Triangle>();
        private static readonly float[] CoverFractions = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };
        private static readonly float[] DamageFactors = { 1f, 1f, 0.7f, 0.5f, 0.35f, 0.25f };
        private static readonly float[] SampleHeights = { 0.4f, 1.1f, 1.8f };
        private static readonly float[] SampleSides = { -0.3f, 0f, 0.3f };

        public int TriangleCount => _triangles.Count;

        private CoverGeometry() { }

        public static CoverGeometry Load(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.GetProperty("schema_version").GetInt32() != 1)
                throw new InvalidDataException($"Unsupported cover geometry schema: {path}");
            var geometry = new CoverGeometry();
            foreach (var obj in document.RootElement.GetProperty("objects").EnumerateArray())
                foreach (var row in obj.GetProperty("triangles").EnumerateArray())
                {
                    var v = new float[9];
                    var index = 0;
                    foreach (var number in row.EnumerateArray())
                    {
                        if (index == 9)
                            throw new InvalidDataException($"Cover triangle has more than nine coordinates: {path}");
                        v[index++] = number.GetSingle();
                    }
                    if (index != 9 || Array.Exists(v, value => float.IsNaN(value) || float.IsInfinity(value)))
                        throw new InvalidDataException($"Invalid cover triangle in {path}");
                    geometry._triangles.Add(new Triangle(new Vector3(v[0], v[1], v[2]),
                        new Vector3(v[3], v[4], v[5]), new Vector3(v[6], v[7], v[8])));
                }
            return geometry;
        }

        /// <summary>
        /// Sample a 3x3 target grid perpendicular to the shot. Horizontal half-width 0.3 m,
        /// target heights 0.4/1.1/1.8 m and source height 1.2 m are inferred human-sized
        /// values, not original server constants. Returns a count for diagnostics and tests.
        /// </summary>
        public int OccludedSamples(Vector3 source, Vector3 target)
        {
            var direction = target - source;
            direction.Y = 0;
            if (direction.LengthSquared() < 0.0001f)
                return 0;
            direction = Vector3.Normalize(direction);
            var across = new Vector3(-direction.Z, 0, direction.X);
            var start = source + new Vector3(0, 1.2f, 0);
            var blocked = 0;
            foreach (var height in SampleHeights)
                foreach (var side in SampleSides)
                {
                    var end = target + new Vector3(0, height, 0) + across * side;
                    foreach (var triangle in _triangles)
                        if (triangle.Intersects(start, end))
                        {
                            blocked++;
                            break;
                        }
                }
            return blocked;
        }

        public int ScaleDamage(Vector3 source, Vector3 target, int damage)
            => ScaleDamage(source, target, damage, out _);

        public int ScaleDamage(Vector3 source, Vector3 target, int damage, out int blocked)
            => ScaleDamage(source, target, damage, out blocked, out _);

        public int ScaleDamage(Vector3 source, Vector3 target, int damage, out int blocked,
            out float coverModifier)
        {
            blocked = 0;
            coverModifier = 1f;
            if (damage <= 0)
                return damage;
            blocked = OccludedSamples(source, target);
            var fraction = blocked / 9f;
            coverModifier = DamageFactor(fraction);
            return Math.Max(1, (int)Math.Round(damage * coverModifier, MidpointRounding.AwayFromZero));
        }

        /// <summary>Linear interpolation of the original client lookup; interpolation is inferred.</summary>
        public static float DamageFactor(float coveredFraction)
        {
            var fraction = Math.Max(0f, Math.Min(1f, coveredFraction));
            for (var i = 1; i < CoverFractions.Length; i++)
                if (fraction <= CoverFractions[i])
                {
                    var t = (fraction - CoverFractions[i - 1]) / (CoverFractions[i] - CoverFractions[i - 1]);
                    return DamageFactors[i - 1] + t * (DamageFactors[i] - DamageFactors[i - 1]);
                }
            return DamageFactors[DamageFactors.Length - 1];
        }

        private readonly struct Triangle
        {
            private readonly Vector3 _a, _b, _c;

            public Triangle(Vector3 a, Vector3 b, Vector3 c) { _a = a; _b = b; _c = c; }

            public bool Intersects(Vector3 start, Vector3 end)
            {
                var ray = end - start;
                var edge1 = _b - _a;
                var edge2 = _c - _a;
                var p = Vector3.Cross(ray, edge2);
                var determinant = Vector3.Dot(edge1, p);
                if (Math.Abs(determinant) < 0.000001f)
                    return false;
                var inverse = 1f / determinant;
                var t = start - _a;
                var u = Vector3.Dot(t, p) * inverse;
                if (u < 0 || u > 1)
                    return false;
                var q = Vector3.Cross(t, edge1);
                var v = Vector3.Dot(ray, q) * inverse;
                if (v < 0 || u + v > 1)
                    return false;
                var distance = Vector3.Dot(edge2, q) * inverse;
                return distance >= 0 && distance <= 1;
            }
        }
    }
}
