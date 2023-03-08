using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SongCore.Data.ExtraSongData;

namespace MultiplayerCore.Beatmaps.Abstractions
{
    public class DifficultyColors : INetSerializable
    {
        public bool AnyAreNotNull => _colorLeft != null || _colorRight != null || _envColorLeft != null || _envColorRight != null || _envColorLeftBoost != null || _envColorRightBoost != null || _obstacleColor != null;

        public MapColor? _colorLeft;
        public MapColor? _colorRight;
        public MapColor? _envColorLeft;
        public MapColor? _envColorRight;
        public MapColor? _envColorLeftBoost;
        public MapColor? _envColorRightBoost;
        public MapColor? _obstacleColor;

        public DifficultyColors() { }

        public DifficultyColors(MapColor? colorLeft, MapColor? colorRight, MapColor? envColorLeft, MapColor? envColorRight, MapColor? envColorLeftBoost, MapColor? envColorRightBoost, MapColor? obstacleColor)
        {
            _colorLeft = colorLeft;
            _colorRight = colorRight;
            _envColorLeft = envColorLeft;
            _envColorRight = envColorRight;
            _envColorLeftBoost = envColorLeftBoost;
            _envColorRightBoost = envColorRightBoost;
        }

        public void Serialize(NetDataWriter writer)
        {
            byte colors = (byte)(_colorLeft != null ? 1 : 0);
            colors |= (byte)((_colorRight != null ? 1 : 0) << 1);
            colors |= (byte)((_envColorLeft != null ? 1 : 0) << 2);
            colors |= (byte)((_envColorRight != null ? 1 : 0) << 3);
            colors |= (byte)((_envColorLeftBoost != null ? 1 : 0) << 4);
            colors |= (byte)((_envColorRightBoost != null ? 1 : 0) << 5);
            colors |= (byte)((_obstacleColor != null ? 1 : 0) << 6);
            writer.Put(colors);

            if (_colorLeft != null)
                ((MapColorSerializable)_colorLeft).Serialize(writer);
            if (_colorRight != null)
                ((MapColorSerializable)_colorRight).Serialize(writer);
            if (_envColorLeft != null)
                ((MapColorSerializable)_envColorLeft).Serialize(writer);
            if (_envColorRight != null)
                ((MapColorSerializable)_envColorRight).Serialize(writer);
            if (_envColorLeftBoost != null)
                ((MapColorSerializable)_envColorLeftBoost).Serialize(writer);
            if (_envColorRightBoost != null)
                ((MapColorSerializable)_envColorRightBoost).Serialize(writer);
            if (_obstacleColor != null)
                ((MapColorSerializable)_obstacleColor).Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            var colors = reader.GetByte();
            if ((colors & 0x1) != 0)
                _colorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 1) & 0x1) != 0)
                _colorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 2) & 0x1) != 0)
                _colorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 3) & 0x1) != 0)
                _colorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 4) & 0x1) != 0)
                _colorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 5) & 0x1) != 0)
                _colorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            if (((colors >> 6) & 0x1) != 0)
                _colorLeft = new MapColor(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
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
