namespace Rasa.Packets.Mission.Server
{
    using Memory;
    using Structures;

    /// <summary>
    /// The missionInfo tuple of a mission offer, shared by the NPC dispense topic and the radio offer:
    /// conversationwindow.HandleShowMissionAvailable unpacks
    /// (level, rewardInfo, offerVOAudioSetId, itemsRequired, objectives, groupType).
    /// </summary>
    public static class MissionOfferWriter
    {
        public static void Write(PythonWriter pw, MissionInfo mission)
        {
            pw.WriteTuple(6);
            pw.WriteUInt(mission.MissionConstantData.Level);
            pw.WriteStruct(mission.MissionConstantData.RewardInfo);
            if (mission.AudioSetId > 0)
                pw.WriteInt(mission.AudioSetId);                  // offerVOAudioSetId, played when the offer comes from an NPC
            else
                pw.WriteNoneStruct();
            pw.WriteList(mission.ItemRequired.Count);             // itemsRequired
            foreach (var item in mission.ItemRequired)
                pw.WriteInt(item);                                // itemClassId
            pw.WriteList(mission.ObjectivesList.Count);           // objectives
            foreach (var objective in mission.ObjectivesList)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(objective.Ordinal);                  // ordinal, the client sorts the offer list by it
                pw.WriteUInt(objective.ObjectiveId);              // objectiveId
            }
            pw.WriteUInt(mission.MissionConstantData.GroupType);  // groupType
        }
    }
}
