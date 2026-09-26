using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// The map template of each loaded game context, as the final client holds it: game/generated/client/gamecontext.pyo
    /// lookup[contextId][3] (client 1.16.5.0; e.g. 1220 -> 1378, 2375 -> 2378). The client derives a context from a
    /// template (gamemap.GetContextIdForMapTemplateId), and ChooseInstanceList names each copy by its template
    /// (waypointwindow.ShowInstances line 472), so the server has to send the template, not the context. Tier: original.
    /// The one loaded context the client has no row for (1991, a test map) is absent.
    /// </summary>
    public static class ClientMapTemplates
    {
        public static readonly IReadOnlyDictionary<uint, uint> ByContext = new Dictionary<uint, uint>
        {
            { 1115u, 1271u },
            { 1148u, 1306u },
            { 1220u, 1378u },
            { 1244u, 1402u },
            { 1304u, 1461u },
            { 1347u, 1504u },
            { 1348u, 1505u },
            { 1349u, 1506u },
            { 1384u, 1541u },
            { 1394u, 1551u },
            { 1397u, 1554u },
            { 1416u, 1573u },
            { 1429u, 1586u },
            { 1430u, 1587u },
            { 1451u, 1608u },
            { 1454u, 1611u },
            { 1465u, 1622u },
            { 1497u, 1654u },
            { 1502u, 1659u },
            { 1506u, 1663u },
            { 1694u, 1694u },
            { 1700u, 1700u },
            { 1721u, 1721u },
            { 1734u, 1734u },
            { 1737u, 1737u },
            { 1743u, 1743u },
            { 1759u, 1759u },
            { 1761u, 1761u },
            { 1763u, 1763u },
            { 1764u, 1764u },
            { 1773u, 1773u },
            { 1803u, 1803u },
            { 1806u, 1806u },
            { 1823u, 1823u },
            { 1830u, 1830u },
            { 1865u, 1865u },
            { 1911u, 1911u },
            { 1977u, 1977u },
            { 1985u, 1985u },
            { 1988u, 1988u },
            { 1993u, 1993u },
            { 2028u, 2028u },
            { 2029u, 2029u },
            { 2034u, 2034u },
            { 2047u, 2047u },
            { 2051u, 2051u },
            { 2055u, 2055u },
            { 2084u, 2084u },
            { 2085u, 2085u },
            { 2093u, 2093u },
            { 2103u, 2103u },
            { 2105u, 2105u },
            { 2107u, 2107u },
            { 2110u, 2110u },
            { 2111u, 2111u },
            { 2112u, 2112u },
            { 2115u, 2115u },
            { 2125u, 2125u },
            { 2136u, 2136u },
            { 2138u, 2138u },
            { 2141u, 2141u },
            { 2146u, 2146u },
            { 2155u, 2155u },
            { 2156u, 2156u },
            { 2162u, 2162u },
            { 2163u, 2163u },
            { 2190u, 2190u },
            { 2203u, 2203u },
            { 2233u, 2237u },
            { 2259u, 2263u },
            { 2278u, 2282u },
            { 2327u, 2331u },
            { 2361u, 2365u },
            { 2368u, 2371u },
            { 2374u, 2377u },
            { 2375u, 2378u },
            { 20000009u, 2232u },
        };

        /// <summary>The context's template, or the context id itself when the client has no row (templates from 1694 on usually equal their context).</summary>
        public static uint For(uint contextId) => ByContext.TryGetValue(contextId, out var template) ? template : contextId;
    }
}
