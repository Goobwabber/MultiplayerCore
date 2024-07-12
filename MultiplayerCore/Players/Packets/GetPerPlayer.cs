using LiteNetLib.Utils;
using MultiplayerCore.Networking.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerCore.Players.Packets
{
	internal class GetPerPlayer : MpPacket
	{
		public override void Deserialize(NetDataReader reader) { }

		public override void Serialize(NetDataWriter writer) { }
	}
}
