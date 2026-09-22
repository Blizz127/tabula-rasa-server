using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Migrations.WildernessData;
    using Rasa.Structures;

    /// <summary>
    /// The first spoken line: what the client's own table says, and the two rules the client's own code puts on
    /// the server. Nothing here invents a cadence - a bark is sent when something asks for one.
    /// </summary>
    [TestClass]
    public class BarkTests
    {
        /// <summary>bark.pyo row 852: boot_camp_major_mcallister_bark.ogg, ten seconds of bubble.</summary>
        private const uint McAllisterBark = 852u;

        [TestMethod]
        public void TheClientsBarkTableIsCarriedWhole()
        {
            // generated/client/bark.pyo, sha256 37c160bf...c856: 859 rows, every one of them with a duration.
            Assert.AreEqual(859, BarkTable.Count);

            Assert.IsTrue(BarkTable.TryGetDuration(McAllisterBark, out var mcAllister));
            Assert.AreEqual(10000, mcAllister);

            // The ends of the table, so a truncated or re-ordered import shows up here: bark 110 puts no bubble
            // up at all, bark 39 ("Testing!") shows for two milliseconds and bark 192 for thirty seconds.
            Assert.IsTrue(BarkTable.TryGetDuration(110u, out var none));
            Assert.AreEqual(0, none);
            Assert.IsTrue(BarkTable.TryGetDuration(39u, out var shortest));
            Assert.AreEqual(2, shortest);
            Assert.IsTrue(BarkTable.TryGetDuration(192u, out var longest));
            Assert.AreEqual(30000, longest);
        }

        [TestMethod]
        public void AnIdTheClientCannotResolveIsNeverSent()
        {
            // The client looks the id up in its own table and does nothing at all with a row it does not have,
            // so an id off the table is a packet with no effect; the server does not send one.
            Assert.IsFalse(BarkTable.TryGetDuration(0u, out _));
            Assert.IsFalse(BarkTable.TryGetDuration(9999u, out _));

            Assert.IsFalse(BarkManager.Instance.Say(new Creature(), 9999u));
            Assert.IsFalse(BarkManager.Instance.Say(null, McAllisterBark));
        }

        [TestMethod]
        public void NobodyHearsABarkPastTheClientsTwentyMetres()
        {
            // creature.pyo Recv_Bark reads the clip's own falloff and then overwrites it with (2.0, 20.0), and
            // overheadwindow.pyo culls the bubble at kOverheadDisplayMaxDistance = 20.
            Assert.AreEqual(20.0f, BarkManager.AudibleRange);

            var speaker = new Vector3(387.2f, 125.57f, 53.3f);

            Assert.IsTrue(BarkManager.InEarshot(speaker, speaker));
            Assert.IsTrue(BarkManager.InEarshot(speaker, speaker + new Vector3(19.9f, 0, 0)));
            Assert.IsTrue(BarkManager.InEarshot(speaker, speaker + new Vector3(20.0f, 0, 0)));
            Assert.IsFalse(BarkManager.InEarshot(speaker, speaker + new Vector3(20.1f, 0, 0)));

            // Height counts: the client's falloff is a 3D distance, so a recruit on the deck above is out of it.
            Assert.IsFalse(BarkManager.InEarshot(speaker, speaker + new Vector3(0, 25.0f, 0)));
        }

        [TestMethod]
        public void McAllisterCannotBeHeardFromTheHologramHeIsTalkingAbout()
        {
            // Why bark 852 fires on the recruit's return and not on the objective: hologram 2 (content_area
            // 198601, BootcampS1InitiationRows) is 48.3 m from McAllister's placement 198650, so a bark sent the
            // instant objective 1990/2 completed would be a packet the client renders as nothing.
            var hologram = new Vector3(387.83f, 112.75f, 6.71f);
            var mcAllister = new Vector3(387.2f, 125.57f, 53.3f);

            Assert.AreEqual(48.3f, Vector3.Distance(hologram, mcAllister), 0.05f);
            Assert.IsFalse(BarkManager.InEarshot(mcAllister, hologram));

            // The seeded trigger area is the client's earshot and nothing else, so entering it and being able to
            // hear him are the same event.
            Assert.AreEqual(BarkManager.AudibleRange, (float)ContentRuleBarkRows.EarshotRadius);
        }

        [TestMethod]
        public void ASpeakerDoesNotTalkOverItself()
        {
            // The client has no rate limit: a second bark inside the first one's duration replaces the bubble and
            // plays a second clip over the first, so the speaker is held for the bubble's own life plus a beat.
            var mcAllister = new Creature();

            Assert.IsTrue(BarkManager.Instance.TryTakeTheFloor(mcAllister, 10000, 1000));
            Assert.IsFalse(BarkManager.Instance.TryTakeTheFloor(mcAllister, 10000, 1001));
            Assert.IsFalse(BarkManager.Instance.TryTakeTheFloor(mcAllister, 10000, 11000));
            Assert.IsFalse(BarkManager.Instance.TryTakeTheFloor(mcAllister, 10000, 11499));
            Assert.IsTrue(BarkManager.Instance.TryTakeTheFloor(mcAllister, 10000, 11500));

            // A line nobody was in range to hear must not leave the speaker silent afterwards: Say takes the
            // floor only once it has an audience.
            var quiet = new Creature();
            Assert.IsFalse(BarkManager.Instance.Say(quiet, McAllisterBark));
            Assert.AreEqual(0L, quiet.BarkSpeakingUntil);
        }
    }
}
