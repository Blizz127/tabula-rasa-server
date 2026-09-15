using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Networking
{
    /// <summary>
    /// Turns a configured address into an <see cref="IPAddress"/>. The deployment configuration historically carried a
    /// literal container address (for example the auth container's 192.168.16.2), which changes whenever the compose
    /// network is recreated; a host name such as the compose service name <c>auth</c> may be configured instead and is
    /// resolved here. Literal addresses keep working unchanged, so no existing deployment file is invalidated.
    /// </summary>
    public static class NetworkAddress
    {
        /// <summary>
        /// Parses a literal IPv4 or IPv6 address, or resolves a host name. An IPv4 address is preferred when a host
        /// name resolves to both families, matching the addresses the communicator sockets expect.
        /// </summary>
        /// <exception cref="ArgumentException">The address is null, empty or only whitespace.</exception>
        /// <exception cref="SocketException">The host name cannot be resolved.</exception>
        public static IPAddress Resolve(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("An address or host name is required.", nameof(address));

            if (IPAddress.TryParse(address, out var literal))
                return literal;

            var addresses = Dns.GetHostAddresses(address);
            return addresses.FirstOrDefault(entry => entry.AddressFamily == AddressFamily.InterNetwork)
                   ?? addresses.FirstOrDefault()
                   ?? throw new SocketException((int)SocketError.HostNotFound);
        }

        /// <summary>The <see cref="IPEndPoint"/> for a configured address and port, as the communicator sockets need it.</summary>
        public static IPEndPoint ResolveEndPoint(string address, int port)
            => new IPEndPoint(Resolve(address), port);
    }
}
