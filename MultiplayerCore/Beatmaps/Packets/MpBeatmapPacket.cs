using LiteNetLib.Utils;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Networking.Abstractions;
using System.Collections.Generic;
using static SongCore.Data.ExtraSongData;

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

        public Dictionary<BeatmapDifficulty, string[]> requirements = new();
        public Dictionary<BeatmapDifficulty, DifficultyColors> mapColors = new();
        public Contributor[] contributors = null!;

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

            if (beatmap.beatmapLevel is MpBeatmapLevel mpBeatmapLevel)
            {
                if (mpBeatmapLevel.requirements.ContainsKey(beatmap.beatmapCharacteristic.name))
                    requirements = mpBeatmapLevel.requirements[beatmap.beatmapCharacteristic.name];
                if (mpBeatmapLevel.requirements.ContainsKey(beatmap.beatmapCharacteristic.serializedName))
                    requirements = mpBeatmapLevel.requirements[beatmap.beatmapCharacteristic.serializedName];
                contributors = mpBeatmapLevel.contributors!;
            }
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

            writer.Put((byte)requirements.Count);
            foreach (var difficulty in requirements)
            {
                writer.Put((byte)difficulty.Key);
                writer.Put((byte)difficulty.Value.Length);
                foreach (var requirement in difficulty.Value)
                    writer.Put(requirement);
            }

            if (contributors != null)
            {
                writer.Put((byte)contributors.Length);
                foreach (var contributor in contributors)
                {
                    writer.Put(contributor._role);
                    writer.Put(contributor._name);
                    writer.Put(contributor._iconPath);
                }
            }
            else
                writer.Put((byte)0);

            writer.Put((byte)mapColors.Count);
            foreach (var difficulty in mapColors)
            {
                writer.Put((byte)difficulty.Key);
                difficulty.Value.Serialize(writer);
            }
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

            var difficultyCount = reader.GetByte();
            for (int i = 0; i < difficultyCount; i++)
            {
                var difficulty = (BeatmapDifficulty)reader.GetByte();
                var requirementCount = reader.GetByte();
                string[] reqsForDifficulty = new string[requirementCount];
                for (int j = 0; j < requirementCount; j++)
                    reqsForDifficulty[j] = reader.GetString();
                requirements[difficulty] = reqsForDifficulty;
            }

            var contributorCount = reader.GetByte();
            contributors = new Contributor[contributorCount];
            for (int i = 0; i < contributorCount; i++)
                contributors[i] = new Contributor
                {
                    _role = reader.GetString(),
                    _name = reader.GetString(),
                    _iconPath = reader.GetString()
                };

            var colorCount = reader.GetByte();
            for (int i = 0; i < colorCount; i++)
            {
                var difficulty = (BeatmapDifficulty)reader.GetByte();
                var colors = new DifficultyColors();
                colors.Deserialize(reader);
                mapColors[difficulty] = colors;
            }
        }
    }
}
