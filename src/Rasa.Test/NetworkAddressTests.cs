using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Networking;

namespace Rasa.Test
{
    /// <summary>
    /// The deployment configuration's Communicator address was a literal container IP, which changes when the compose
    /// network is recreated (the 2026-09-15 deployment lost the auth link that way). A host name may be configured
    /// instead; literal addresses must keep behaving exactly as before.
    /// </summary>
    [TestClass]
    public class NetworkAddressTests
    {
        [TestMethod]
        public void ALiteralAddressIsUsedUnchanged()
        {
            Assert.AreEqual(IPAddress.Parse("192.168.16.3"), NetworkAddress.Resolve("192.168.16.3"));
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), NetworkAddress.Resolve("127.0.0.1"));
            Assert.AreEqual(IPAddress.Parse("::1"), NetworkAddress.Resolve("::1"));
        }

        [TestMethod]
        public void AHostNameResolvesToAnAddress()
        {
            var resolved = NetworkAddress.Resolve("localhost");

            Assert.IsTrue(IPAddress.IsLoopback(resolved), resolved.ToString());
        }

        [TestMethod]
        public void TheEndPointCarriesTheConfiguredPort()
        {
            var endPoint = NetworkAddress.ResolveEndPoint("192.168.16.3", 2107);

            Assert.AreEqual(IPAddress.Parse("192.168.16.3"), endPoint.Address);
            Assert.AreEqual(2107, endPoint.Port);
        }

        [TestMethod]
        public void AnEmptyAddressIsRejected()
        {
            Assert.ThrowsException<ArgumentException>(() => NetworkAddress.Resolve(null));
            Assert.ThrowsException<ArgumentException>(() => NetworkAddress.Resolve("   "));
        }

        [TestMethod]
        public void AnUnresolvableHostNameFails()
        {
            // .invalid is reserved and never resolves (RFC 2606). The runtime throws ExtendedSocketException, a
            // SocketException subclass, so the base type is asserted rather than the exact runtime type.
            try
            {
                NetworkAddress.Resolve("no-such-host.invalid");
                Assert.Fail("an unresolvable host name must not produce an address");
            }
            catch (System.Net.Sockets.SocketException)
            {
            }
        }

        [TestMethod]
        public void AnIpv4AddressWinsWhenAHostResolvesToBothFamilies()
        {
            // localhost commonly resolves to ::1 and 127.0.0.1; the communicator sockets take the IPv4 one.
            var addresses = Dns.GetHostAddresses("localhost");
            if (!addresses.Any(entry => entry.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                Assert.Inconclusive("localhost has no IPv4 address in this environment");

            Assert.AreEqual(System.Net.Sockets.AddressFamily.InterNetwork,
                NetworkAddress.Resolve("localhost").AddressFamily);
        }
    }
}
