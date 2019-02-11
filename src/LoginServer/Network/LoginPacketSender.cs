// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System.Linq;
using System.Net;
using Melia.Login.Database;
using Melia.Login.Network.Helpers;
using Melia.Login.World;
using Melia.Shared.Const;
using Melia.Shared.Network;
using Melia.Shared.Util;
using Melia.Shared.World;
using Melia.Shared.Network.Helpers;

namespace Melia.Login.Network
{
	public static class Send
	{
		/// <summary>
		/// Sends notification that login packet was received.
		/// </summary>
		/// <param name="conn"></param>
		public static void BC_LOGIN_PACKET_RECEIVED(LoginConnection conn)
		{
			var packet = new Packet(Op.BC_LOGIN_PACKET_RECEIVED);
			conn.Send(packet);
		}

		/// <summary>
		/// Sends positive response to login attempt.
		/// </summary>
		/// <param name="conn"></param>
		public static void BC_LOGINOK(LoginConnection conn)
		{
			var packet = new Packet(Op.BC_LOGINOK);
			packet.PutShort(0);
			packet.PutLong(conn.Account.Id);
			packet.PutString(conn.Account.Name, 56);
			packet.PutInt(3); // accountPrivileges? <= 3 enables a kind of debug context menu
			packet.PutString(conn.SessionKey, 64);
			packet.PutInt(conn.IntegritySeed);
			packet.PutLong(0);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends positive response to logout attempt.
		/// </summary>
		/// <param name="conn"></param>
		public static void BC_LOGOUTOK(LoginConnection conn)
		{
			var packet = new Packet(Op.BC_LOGOUTOK);
			conn.Send(packet);
		}

		/// <summary>
		/// Sends a list of characters to the client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="layer">This is the number on the left side of the character list in the client.</param>
		public static void BC_COMMANDER_LIST(LoginConnection conn, int layer = 1)
		{
			var characters = conn.Account.GetCharacters().Where(x => x.BarrackLayer == layer);

			var packet = new Packet(Op.BC_COMMANDER_LIST);
			packet.PutLong(conn.Account.Id);
			packet.PutByte(0);
			packet.PutByte((byte)characters.Count());
			packet.PutString(conn.Account.TeamName, 64);
			packet.AddAccountProperties(conn.Account);

			foreach (var character in characters)
				packet.AddBarrackPc(character);

			// Null terminated list of some kind?
			// Example of != 0: 02 00 | 0B 00 00 00 01 00, 0C 00 00 00 00 00
			packet.PutShort(0); // count?

			packet.PutShort(0);
			packet.PutInt(0);
			packet.PutShort(0);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends the new character's index.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		public static void BC_COMMANDER_CREATE_SLOTID(LoginConnection conn, Character character)
		{
			var characterCount = conn.Account.CharacterCount;

			var packet = new Packet(Op.BC_COMMANDER_CREATE_SLOTID);
			packet.PutByte((byte)characterCount);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends information about newly created character.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		public static void BC_COMMANDER_CREATE(LoginConnection conn, Character character)
		{
			var packet = new Packet(Op.BC_COMMANDER_CREATE);
			packet.AddAppearanceBarrackPc(character);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends response to team name change request.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="result"></param>
		public static void BC_BARRACKNAME_CHANGE(LoginConnection conn, TeamNameChangeResult result)
		{
			var response = new Packet(Op.BC_BARRACKNAME_CHANGE);
			response.PutByte(result == TeamNameChangeResult.Okay);
			response.PutInt((int)result);
			response.PutString(conn.Account.TeamName, 64);

			conn.Send(response);
		}

		/// <summary>
		/// Sends pre-defined message to client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="msgType"></param>
		public static void BC_MESSAGE(LoginConnection conn, MsgType msgType)
		{
			var packet = new Packet(Op.BC_MESSAGE);
			packet.PutByte((byte)msgType);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends custom message to client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="msg"></param>
		public static void BC_MESSAGE(LoginConnection conn, string msg)
		{
			var packet = new Packet(Op.BC_MESSAGE);
			packet.PutByte((byte)MsgType.Text);
			packet.PutEmptyBin(40);
			packet.PutString(msg);

			conn.Send(packet);
		}

		/// <summary>
		/// Removes character from barrack.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="index"></param>
		public static void BC_COMMANDER_DESTROY(LoginConnection conn, byte index)
		{
			var packet = new Packet(Op.BC_COMMANDER_DESTROY);
			packet.PutByte(index);

			conn.Send(packet);
		}

		/// <summary>
		/// Sets the barrack, but not working for some reason.
		/// Use the account property to do this.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="id"></param>
		public static void BC_NORMAL_SetBarrack(LoginConnection conn, int id)
		{
			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.SetBarrack);
			packet.PutLong(conn.Account.Id);
			packet.PutInt(id);
			packet.PutByte(0);
			packet.PutInt(0);
			conn.Send(packet);
		}

		/// <summary>
		/// Moves a character in the barrack.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="index"></param>
		/// <param name="position"></param>
		public static void BC_NORMAL_SetPosition(LoginConnection conn, byte index, Position position)
		{
			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.SetPosition);
			packet.PutLong(conn.Account.Id);
			packet.PutByte(index);
			packet.PutFloat(position.X);
			packet.PutFloat(position.Y);
			packet.PutFloat(position.Z);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends information related to the team to be displayed in the barrack.
		/// </summary>
		/// <param name="conn"></param>
		public static void BC_NORMAL_TeamUI(LoginConnection conn)
		{
			var characters = conn.Account.GetCharacters();

			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.TeamUI);
			packet.PutLong(conn.Account.Id);
			packet.PutShort(0); // Number of additional character slots
			packet.PutInt(0); // Team experience. Displayed under "Team Info"
			packet.PutShort(characters.Count()); // Sum of characters and pets.

			conn.Send(packet);
		}

		/// <summary>
		/// Sends zone traffic list.
		/// </summary>
		/// <param name="conn"></param>
		public static void BC_NORMAL_ZoneTraffic(LoginConnection conn)
		{
			var characters = conn.Account.GetCharacters();
			var mapAvailableCount = characters.Length;
			var zoneServerCount = 1;
			var zoneMaxPcCount = 150;

			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.ZoneTraffic);

			packet.Zlib(true, zpacket =>
			{
				zpacket.PutShort(zoneMaxPcCount);
				zpacket.PutShort(mapAvailableCount);
				for (var i = 0; i < mapAvailableCount; ++i)
				{
					zpacket.PutShort(characters[i].MapId);
					zpacket.PutShort(zoneServerCount);
					for (var zone = 0; zone < zoneServerCount; ++zone)
					{
						zpacket.PutShort(zone);
						zpacket.PutShort(1); // currentPlayersCount
					}
				}
			});

			conn.Send(packet);
		}

		/// <summary>
		/// Sends positive response to game start request.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		public static void BC_START_GAMEOK(LoginConnection conn, Character character, string ip, int port)
		{
			var packet = new Packet(Op.BC_START_GAMEOK);

			packet.PutInt(0); // Zone ID.
			packet.PutInt(IPAddress.Parse(ip).ToInt32());
			packet.PutInt(port);
			packet.PutInt(character.MapId);
			packet.PutByte(1); // Channel ID.
			packet.PutLong(character.Id);
			packet.PutByte(0); // Only connects if 0
			packet.PutByte(1); // Passed to a function if ^ is 0

			conn.Send(packet);
		}

		/// <summary>
		/// Sends social server connection information?
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="ip1"></param>
		/// <param name="port1"></param>
		/// <param name="ip2"></param>
		/// <param name="port2"></param>
		public static void BC_SERVER_ENTRY(LoginConnection conn, string ip1, int port1, string ip2, int port2)
		{
			var packet = new Packet(Op.BC_SERVER_ENTRY);

			packet.PutInt(IPAddress.Parse(ip1).ToInt32());
			packet.PutInt(IPAddress.Parse(ip2).ToInt32());
			packet.PutShort(port1);
			packet.PutShort(port2);

			conn.Send(packet);
		}

		/// <summary>
		/// Updates account's properties on the client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="account"></param>
		public static void BC_ACCOUNT_PROP(LoginConnection conn, Account account)
		{
			var packet = new Packet(Op.BC_ACCOUNT_PROP);

			packet.PutLong(account.Id);
			packet.AddAccountProperties(account);

			conn.Send(packet);
		}

		/// <summary>
		/// Updates the state of a postbox message.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="messageId"></param>
		/// <param name="state"></param>
		public static void BC_NORMAL_UpdatePostBoxState(LoginConnection conn, long messageId, PostBoxMessageState state)
		{
			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.PostBoxState);
			packet.PutLong(messageId);
			packet.PutByte((byte)state);

			conn.Send(packet);
		}

		/// <summary>
		/// Invokes a lua function.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="str"></param>
		public static void BC_NORMAL_Run(LoginConnection conn, string str)
		{
			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.Run);
			packet.PutLpString(str);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends the session key to the client.
		/// </summary>
		/// <param name="conn"></param>
		public static void BC_NORMAL_SetSessionKey(LoginConnection conn)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Barrack.SetSessionKey);
			packet.PutLpString(conn.SessionKey);
			conn.Send(packet);
		}

		/// <summary>
		/// Notifies the client that it has failed the integrity check of its files.
		/// </summary>
		/// <param name="conn"></param>
		public static void BC_NORMAL_ClientIntegrityFailure(LoginConnection conn)
		{
			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.ClientIntegrityFailure);

			conn.Send(packet);
		}

		/// <summary>
		/// Updates the number of purchased character slots.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="count"></param>
		public static void BC_NORMAL_BarrackSlotCount(LoginConnection conn, int count)
		{
			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Barrack.BarrackSlotCount);
			packet.PutInt(count);

			conn.Send(packet);
		}
	}
}
