using LiteNetLib.Utils;
using MultiplayerCore.Networking.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerCore.Networking.Attributes;

namespace MultiplayerCore.Players.Packets
{
	internal class GetMpPerPlayerPacket : MpPacket
	{
		public override void Deserialize(NetDataReader reader) { }

		public override void Serialize(NetDataWriter writer) { }
	}
}
