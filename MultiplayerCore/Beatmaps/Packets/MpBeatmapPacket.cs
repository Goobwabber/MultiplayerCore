using System.Collections.Generic;
using LiteNetLib.Utils;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Serializable;
using MultiplayerCore.Networking.Abstractions;
using static SongCore.Data.SongData;

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

        public string characteristicName = null!;
        public BeatmapDifficulty difficulty;

        public Dictionary<BeatmapDifficulty, string[]> requirements = new();
        public Dictionary<BeatmapDifficulty, DifficultyColors> mapColors = new();
        public Contributor[] contributors = null!;

        public MpBeatmapPacket() { }

        public MpBeatmapPacket(MpBeatmap beatmap, BeatmapKey beatmapKey)
        {
            levelHash = Utilities.HashForLevelID(beatmap.LevelID) ?? "";
            songName = beatmap.SongName;
            songSubName = beatmap.SongSubName;
            songAuthorName = beatmap.SongAuthorName;
            levelAuthorName = beatmap.LevelAuthorName;
            beatsPerMinute = beatmap.BeatsPerMinute;
            songDuration = beatmap.SongDuration;
            characteristicName = beatmapKey.beatmapCharacteristic.serializedName;
            difficulty = beatmapKey.difficulty;
            if (beatmap.Requirements.TryGetValue(characteristicName, out var requirementSet))
                requirements = requirementSet;
            contributors = beatmap.Contributors!;
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

            writer.Put(characteristicName);
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

            characteristicName = reader.GetString();
            difficulty = (BeatmapDifficulty)reader.GetUInt();

            try
            {
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
            catch
            {
                Plugin.Logger.Warn($"Player using old version of MultiplayerCore, not all info may be available.");
            }
        }
    }
}
