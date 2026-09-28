using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class PerCharacterInstanceNavMeshTests
    {
        private const uint Camp = 1985;

        // Reported by the DIT boot camp pilot (2026-09-28): a per-character boot camp instance was created
        // without the context's navmesh, so FindPath returned null for every creature in it.
        [TestMethod]
        public void APrivateBootcampInstanceGetsTheContextNavMesh()
        {
            var mesh = new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", "adv_bootcamp.nav")));
            var maps = new MapChannelManager(null, () => 0)
            {
                IsPerCharacterContext = contextId => contextId == Camp,
                PopulateInstance = _ => { }
            };
            maps.MapChannelArray.Add(Camp, new MapChannel
            {
                MapInfo = new MapInfo(Camp, "adv_bootcamp", 1, 4),
                NavMesh = mesh,
                ClientList = new List<Client>()
            });

            var first = maps.ChannelForEntry(1, Camp);
            var second = maps.ChannelForEntry(2, Camp);

            Assert.AreNotSame(maps.MapChannelArray[Camp], first);
            Assert.AreNotSame(first, second);
            Assert.AreSame(mesh, first.NavMesh);
            Assert.AreSame(mesh, second.NavMesh);
        }

        private static string RepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Rasa.NET.sln")))
                dir = dir.Parent;
            Assert.IsNotNull(dir, "repository root not found");
            return dir.FullName;
        }
    }
}
