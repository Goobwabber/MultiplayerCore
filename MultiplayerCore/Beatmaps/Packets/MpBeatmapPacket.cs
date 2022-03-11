using LiteNetLib.Utils;
using MultiplayerCore.Networking.Abstractions;

namespace MultiplayerCore.Beatmaps.Packets
{
    public class MpBeatmapPacket : MpPacket
    {
        public string levelHash = null!;
        public string songName = null!;
        public string songSubName = null!;
        public string songAuthorName = null!;
        public string levelAuthorName = null!;
        public float beatsPerMinute;
        public float songDuration;

        public string characteristic = null!;
        public BeatmapDifficulty difficulty;

        public MpBeatmapPacket() { }

        public MpBeatmapPacket(PreviewDifficultyBeatmap beatmap)
        {
            levelHash = Utilities.HashForLevelID(beatmap.beatmapLevel.levelID);
            songName = beatmap.beatmapLevel.songName;
            songSubName = beatmap.beatmapLevel.songSubName;
            songAuthorName = beatmap.beatmapLevel.songAuthorName;
            levelAuthorName = beatmap.beatmapLevel.levelAuthorName;
            beatsPerMinute = beatmap.beatmapLevel.beatsPerMinute;
            songDuration = beatmap.beatmapLevel.songDuration;

            characteristic = beatmap.beatmapCharacteristic.serializedName;
            difficulty = beatmap.beatmapDifficulty;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(levelHash);
            writer.Put(songName);
            writer.Put(songSubName);
            writer.Put(songAuthorName);
            writer.Put(levelAuthorName);
            writer.Put(beatsPerMinute);
            writer.Put(songDuration);

            writer.Put(characteristic);
            writer.Put((uint)difficulty);
        }

        public override void Deserialize(NetDataReader reader)
        {
            levelHash = reader.GetString();
            songName = reader.GetString();
            songSubName = reader.GetString();
            songAuthorName = reader.GetString();
            levelAuthorName = reader.GetString();
            beatsPerMinute = reader.GetFloat();
            songDuration = reader.GetFloat();

            characteristic = reader.GetString();
            difficulty = (BeatmapDifficulty)reader.GetUInt();
        }
    }
}
