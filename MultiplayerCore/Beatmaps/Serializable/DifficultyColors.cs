using LiteNetLib.Utils;
using static SongCore.Data.ExtraSongData;

namespace MultiplayerCore.Beatmaps.Serializable
{
    public class DifficultyColors : INetSerializable
    {
        public MapColor? ColorLeft;
        public MapColor? ColorRight;
        public MapColor? EnvColorLeft;
        public MapColor? EnvColorRight;
        public MapColor? EnvColorLeftBoost;
        public MapColor? EnvColorRightBoost;
        public MapColor? ObstacleColor;
        
        public bool AnyAreNotNull => ColorLeft != null || ColorRight != null || EnvColorLeft != null ||
                                     EnvColorRight != null || EnvColorLeftBoost != null || EnvColorRightBoost != null ||
                                     ObstacleColor != null;

        public DifficultyColors()
        {
        }

        public DifficultyColors(MapColor? colorLeft, MapColor? colorRight, MapColor? envColorLeft, MapColor? envColorRight, MapColor? envColorLeftBoost, MapColor? envColorRightBoost, MapColor? obstacleColor)
        {
            ColorLeft = colorLeft;
            ColorRight = colorRight;
            EnvColorLeft = envColorLeft;
            EnvColorRight = envColorRight;
            EnvColorLeftBoost = envColorLeftBoost;
            EnvColorRightBoost = envColorRightBoost;
        }

        public void Serialize(NetDataWriter writer)
        {
            byte colors = (byte)(ColorLeft != null ? 1 : 0);
            colors |= (byte)((ColorRight != null ? 1 : 0) << 1);
            colors |= (byte)((EnvColorLeft != null ? 1 : 0) << 2);
            colors |= (byte)((EnvColorRight != null ? 1 : 0) << 3);
            colors |= (byte)((EnvColorLeftBoost != null ? 1 : 0) << 4);
            colors |= (byte)((EnvColorRightBoost != null ? 1 : 0) << 5);
            colors |= (byte)((ObstacleColor != null ? 1 : 0) << 6);
            writer.Put(colors);

            if (ColorLeft != null)
                ((MapColorSerializable)ColorLeft).Serialize(writer);
            if (ColorRight != null)
                ((MapColorSerializable)ColorRight).Serialize(writer);
            if (EnvColorLeft != null)
                ((MapColorSerializable)EnvColorLeft).Serialize(writer);
            if (EnvColorRight != null)
                ((MapColorSerializable)EnvColorRight).Serialize(writer);
            if (EnvColorLeftBoost != null)
                ((MapColorSerializable)EnvColorLeftBoost).Serialize(writer);
            if (EnvColorRightBoost != null)
                ((MapColorSerializable)EnvColorRightBoost).Serialize(writer);
            if (ObstacleColor != null)
                ((MapColorSerializable)ObstacleColor).Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            var colors = reader.GetByte();
            if ((colors & 0x1) != 0)
                ColorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 1) & 0x1) != 0)
                ColorRight = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 2) & 0x1) != 0)
                EnvColorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 3) & 0x1) != 0)
                EnvColorRight = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 4) & 0x1) != 0)
                EnvColorLeftBoost = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 5) & 0x1) != 0)
                EnvColorRightBoost = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 6) & 0x1) != 0)
                ObstacleColor = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public class MapColorSerializable : INetSerializable
        {
            public float r;
            public float g;
            public float b;

            public MapColorSerializable(float red, float green, float blue)
            {
                r = red;
                g = green;
                b = blue;
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(r);
                writer.Put(g);
                writer.Put(b);
            }

            public void Deserialize(NetDataReader reader)
            {
                r = reader.GetFloat();
                g = reader.GetFloat();
                b = reader.GetFloat();
            }

            public static implicit operator MapColor(MapColorSerializable c) => new MapColor(c.r, c.g, c.b);
            public static explicit operator MapColorSerializable(MapColor c) => new MapColorSerializable(c.r, c.g, c.b);
        }
    }
}
