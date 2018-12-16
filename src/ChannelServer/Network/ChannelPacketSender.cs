// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Melia.Channel.Network.Helpers;
using Melia.Channel.World;
using Melia.Shared.Const;
using Melia.Shared.Network;
using Melia.Shared.Network.Helpers;
using Melia.Shared.Util;
using Melia.Shared.World;
using Melia.Shared.World.ObjectProperties;

namespace Melia.Channel.Network
{
	public static class Send
	{
		/// <summary>
		/// Sends ZC_CONNECT_OK to connection, verifying the connection and
		/// giving information about the character.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		public static void ZC_CONNECT_OK(ChannelConnection conn, Character character)
		{
			var packet = new Packet(Op.ZC_CONNECT_OK);

			packet.PutByte(0); // gameMode 0 = NormalMode, 1 = SingleMode
			packet.PutInt(1292150020);
			packet.PutByte(3); // isGM (< 3)?
			packet.PutEmptyBin(10);

			// These bytes set the integrated and integrated dungeon server settings.
			packet.PutByte(0);
			packet.PutByte(0);

			packet.PutLpString(conn.SessionKey);
			packet.AddCommander(character);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_START_GAME to connection, which assumingly is the signal
		/// for the client to switch from load to map screen.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_START_GAME(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_START_GAME);

			packet.PutFloat(1); // Affects the speed of everything happening in the client o.o
			packet.PutFloat(1); // serverAppTimeOffset
			packet.PutFloat(1); // globalAppTimeOffset
			packet.PutLong(DateTime.Now.Add(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now)).ToFileTime());

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_START_INFO to connection.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_START_INFO(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_START_INFO);

			packet.PutInt(1); // count
			{
				packet.PutShort((short)conn.SelectedCharacter.JobId);
				packet.PutInt(0); // 1270153646, 2003304878
				packet.PutInt(0);
				packet.PutShort(1);
			}

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_MYPC_ENTER to character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_MYPC_ENTER(Character character)
		{
			var packet = new Packet(Op.ZC_MYPC_ENTER);

			packet.PutFloat(character.Position.X);
			packet.PutFloat(character.Position.Y);
			packet.PutFloat(character.Position.Z);
			packet.PutByte(0);
			packet.PutByte(0);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Makes character appear on connection's client, by sending ZC_ENTER_PC.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		public static void ZC_ENTER_PC(ChannelConnection conn, Character character)
		{
			var packet = new Packet(Op.ZC_ENTER_PC);

			packet.PutInt(character.Handle);
			packet.PutFloat(character.Position.X);
			packet.PutFloat(character.Position.Y);
			packet.PutFloat(character.Position.Z);
			packet.PutFloat(character.Direction.Cos);
			packet.PutFloat(character.Direction.Sin);
			packet.PutShort(0);
			packet.PutLong(character.Id + 1); // PCEtc GUID? socialInfoId
			packet.PutByte(0); // Pose
			packet.PutFloat(character.GetSpeed());
			packet.PutInt(0);
			packet.PutInt(character.Hp);
			packet.PutInt(character.MaxHp);
			packet.PutShort(character.Sp);
			packet.PutShort(character.MaxSp);
			packet.PutInt(0); // [i11025 (2016-02-26)]
			packet.PutInt(character.Stamina);
			packet.PutInt(character.MaxStamina);
			packet.PutByte(0);
			packet.PutShort(0);
			packet.PutInt(-1); // titleAchievmentId
			packet.PutInt(0);
			packet.PutByte(0);
			packet.AddAppearancePc(character);
			packet.PutInt(0);
			//packet.PutByte(0);

			conn.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_ENTER_MONSTER on monster's map, making it appear.
		/// </summary>
		/// <param name="monster"></param>
		public static void ZC_ENTER_MONSTER(Monster monster)
		{
			var packet = new Packet(Op.ZC_ENTER_MONSTER);
			packet.AddMonster(monster);

			monster.Map.Broadcast(packet);
		}

		/// <summary>
		/// Sends ZC_ENTER_MONSTER to connection, making it appear.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="monster"></param>
		public static void ZC_ENTER_MONSTER(ChannelConnection conn, Monster monster)
		{
			var packet = new Packet(Op.ZC_ENTER_MONSTER);
			packet.AddMonster(monster);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_QUICK_SLOT_LIST to connection, containing the
		/// list of hotkeys?
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_QUICK_SLOT_LIST(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_QUICK_SLOT_LIST);

			packet.PutInt(0);
			packet.PutShort(0);
			//...

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_SKILL_LIST to character, containing a list
		/// of all the character's skills.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_SKILL_LIST(Character character)
		{
			var skills = character.Skills.GetList();

			var packet = new Packet(Op.ZC_SKILL_LIST);
			packet.PutInt(character.Handle);
			packet.PutShort(skills.Count());
			packet.PutByte(0);

			packet.Zlib(false, zpacket =>
			{
				foreach (var skill in skills)
					zpacket.AddSkill(skill);
			});

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adds skill for character, by sending ZC_SKILL_ADD to its connection.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		public static void ZC_SKILL_ADD(Character character, Skill skill)
		{
			var packet = new Packet(Op.ZC_SKILL_ADD);

			packet.PutLong(character.Id);
			packet.PutByte(1); // REGISTER_QUICK_SKILL ?
			packet.PutByte(0); // SKILL_LIST_GET ?
			packet.PutLong(0); // ?
			packet.AddSkill(skill);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_SKILLMAP_LIST to character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_SKILLMAP_LIST(Character character)
		{
			var packet = new Packet(Op.ZC_SKILLMAP_LIST);

			packet.PutInt(0); // ?

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_OPTION_LIST to connection, containing the saved
		/// account options, like "Show Exp Aquired".
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_OPTION_LIST(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_OPTION_LIST);

			packet.PutString(conn.Account.Settings.ToString());

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_ACHIEVE_POINT_LIST to character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_ACHIEVE_POINT_LIST(Character character)
		{
			var packet = new Packet(Op.ZC_ACHIEVE_POINT_LIST);

			packet.PutInt(0); // ?

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends chat macros to the character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_CHAT_MACRO_LIST(Character character)
		{
			var packet = new Packet(Op.ZC_CHAT_MACRO_LIST);

			var macros = character.Connection.Account.GetChatMacros();

			packet.PutInt(macros.Count());
			foreach (var macro in macros)
			{
				packet.PutInt(macro.Index);
				packet.PutString(macro.Message, 128);
				packet.PutInt(macro.Pose);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_NPC_STATE_LIST to character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NPC_STATE_LIST(Character character)
		{
			var packet = new Packet(Op.ZC_NPC_STATE_LIST);

			packet.PutInt(0); // count
			packet.PutShort(0);

			// loop
			//   int mapId;
			//   int i1;
			//   int i2;

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_COOLDOWN_LIST to character, containing list of all
		/// cooldowns?
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_COOLDOWN_LIST(Character character)
		{
			var packet = new Packet(Op.ZC_COOLDOWN_LIST);

			packet.PutLong(0); // socialInfoId ?
			packet.PutInt(0); // ?

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_JOB_PTS to character, updating their job points.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="job"></param>
		public static void ZC_JOB_PTS(Character character, Job job)
		{
			var packet = new Packet(Op.ZC_JOB_PTS);

			packet.PutLong(character.Id);
			packet.PutShort((short)job.Id);
			packet.PutShort((short)job.SkillPoints);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_ABILITY_LIST to character, containing a list of all
		/// their abilities.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_ABILITY_LIST(Character character)
		{
			// The abilities need a unique object id to appear properly,
			// otherwise their tooltip is the same for all of them.

			//var abilities = new[] { 10001, 10007, 10009, 10012, 10013, 10014 };
			//var abilityNames = ChannelServer.Instance.Data.JobDb.Find(character.Job).DefaultAbilities;
			//var abilities = ChannelServer.Instance.Data.AbilityDb.Where(a => abilityNames.Contains(a.ClassName)).Select(a => a.Id);
			//var objectId = 0xE1A9001690B2;
			var abilities = character.Abilities.GetList();

			var packet = new Packet(Op.ZC_ABILITY_LIST);
			packet.PutInt(character.Handle);
			packet.PutShort(abilities.Length);

			packet.Zlib(false, zpacket =>
			{
				foreach (var ability in abilities)
				{
					var properties = ability.Properties.GetAll();
					var propertiesSize = ability.Properties.Size;

					zpacket.PutLong(ability.ObjectId);
					zpacket.PutInt(ability.Id);
					zpacket.PutShort((short)propertiesSize);
					zpacket.PutShort(0);
					zpacket.AddProperties(properties);
				}
			});

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_MOVE_SPEED in range of character, updating their move speed.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_MOVE_SPEED(Character character)
		{
			var packet = new Packet(Op.ZC_MOVE_SPEED);

			packet.PutInt(character.Handle);
			packet.PutFloat(character.GetSpeed());
			packet.PutFloat(0);

			// [i11257 (2016-03-25)]
			{
				packet.PutByte(0);
			}

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends ZC_ITEM_INVENTORY_LIST to character, containing a list of
		/// all items in their inventory.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_ITEM_INVENTORY_LIST(Character character)
		{
			var items = character.Inventory.GetItems();

			var packet = new Packet(Op.ZC_ITEM_INVENTORY_LIST);

			packet.PutInt(items.Count);
			packet.Zlib(false, zpacket =>
			{
				foreach (var item in items)
				{
					var properties = item.Value.Properties.GetAll();
					var propertiesSize = item.Value.Properties.Size;

					zpacket.PutInt(item.Value.Id);
					zpacket.PutShort(propertiesSize);
					zpacket.PutEmptyBin(2);
					zpacket.PutLong(item.Value.ObjectId);
					zpacket.PutInt(item.Value.Amount);
					zpacket.PutInt(item.Value.Price);
					zpacket.PutInt(item.Key);
					zpacket.PutInt(1);
					zpacket.AddProperties(properties);
				}
			});

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_ITEM_INVENTORY_DIVISION_LIST to character, containing a list of
		/// all items in their inventory.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_ITEM_INVENTORY_DIVISION_LIST(Character character)
		{
			var items = character.Inventory.GetItems();

			var packet = new Packet(Op.ZC_ITEM_INVENTORY_DIVISION_LIST);

			packet.PutInt(items.Count);
			packet.PutByte(1);
			packet.PutByte(1);
			packet.Zlib(false, zpacket =>
			{
				foreach (var item in items)
				{
					var properties = item.Value.Properties.GetAll();
					var propertiesSize = item.Value.Properties.Size;

					zpacket.PutInt(item.Value.Id);
					zpacket.PutShort(propertiesSize);
					zpacket.PutEmptyBin(2);
					zpacket.PutLong(item.Value.ObjectId);
					zpacket.PutInt(item.Value.Amount);
					zpacket.PutInt(item.Value.Price);
					zpacket.PutInt(item.Key);
					zpacket.PutInt(1);
					zpacket.AddProperties(properties);
				}
			});

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_ITEM_INVENTORY_LIST to character, containing a list of
		/// all their equipment.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_ITEM_EQUIP_LIST(Character character)
		{
			var equip = character.Inventory.GetEquip();
			if (equip.Count != Items.EquipSlotCount)
				throw new InvalidOperationException("Incorrect amount of equipment (" + equip.Count + ").");

			var packet = new Packet(Op.ZC_ITEM_EQUIP_LIST);

			foreach (var equipItem in equip)
			{
				var properties = equipItem.Value.Properties.GetAll();
				var propertiesSize = equipItem.Value.Properties.Size;

				packet.PutInt(equipItem.Value.Id);
				packet.PutShort(propertiesSize);
				packet.PutEmptyBin(2);
				packet.PutLong(equipItem.Value.ObjectId);
				packet.PutByte((byte)equipItem.Key);
				packet.PutEmptyBin(3);
				packet.PutInt(0);
				packet.AddProperties(properties);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Unequips an item and optionally shows a UI message.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="item"></param>
		/// <param name="message"></param>
		public static void ZC_EQUIP_ITEM_REMOVE(Character character, Item item, int message)
		{
			var packet = new Packet(Op.ZC_EQUIP_ITEM_REMOVE);
			packet.PutLong(item.ObjectId);

			// TODO: Make message an enumeration.
			packet.PutInt(message);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the durability of an item in an equipment slot.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="slot"></param>
		/// <param name="durability">Value in thousandths that the item has remaining.</param>
		public static void ZC_CHANGE_EQUIP_DURABILITY(Character character, EquipSlot slot, int durability)
		{
			var packet = new Packet(Op.ZC_CHANGE_EQUIP_DURABILITY);
			packet.PutByte((byte)slot);
			packet.PutInt(durability);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_CHAT in range of character.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void ZC_CHAT(Character character, string format, params object[] args)
		{
			ZC_CHAT(character, string.Format(format, args));
		}

		/// <summary>
		/// Broadcasts ZC_CHAT in range of character.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="message"></param>
		public static void ZC_CHAT(Character character, string message)
		{
			var packet = new Packet(Op.ZC_CHAT);

			packet.PutInt(character.Handle);
			packet.PutString(character.TeamName, 64);
			packet.PutString(character.Name, 65);
			packet.PutByte(0); // -11, -60, -1, -19, 1
			packet.PutShort((short)character.JobId);
			packet.PutInt((short)character.JobId);
			packet.PutInt(0); // 1, 10, 11
			packet.PutShort((byte)character.Gender);
			packet.PutShort((byte)character.Hair);
			packet.PutEmptyBin(2);
			packet.PutEmptyBin(14);
			packet.PutInt(0); // 628051

			// [i11257 (2016-03-25)] ?
			{
				packet.PutInt(1004);
			}

			packet.PutFloat(0); // Display time in seconds, min cap 5s
			packet.PutString(message);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Send ZC_SYSTEM_MSG to character.
		/// </summary>
		/// <param name="character">Character to send packet to.</param>
		/// <param name="clientMessage">Id of the message to use.</param>
		/// <param name="parameters">Optional list of message parameters.</param>
		public static void ZC_SYSTEM_MSG(Character character, int clientMessage, params MsgParameter[] parameters)
		{
			var packet = new Packet(Op.ZC_SYSTEM_MSG);

			packet.PutInt(clientMessage);
			packet.PutByte((byte)parameters.Length);
			packet.PutByte(1); // type? 0 = also show in red letters on the screen
			packet.PutLong(0); // ?

			foreach (var parameter in parameters)
			{
				packet.PutLpString(parameter.Key);
				packet.PutLpString(parameter.Value);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_JUMP in range of character, making them jump.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_JUMP(Character character)
		{
			var packet = new Packet(Op.ZC_JUMP);

			packet.PutInt(character.Handle);
			packet.PutFloat(character.GetJumpStrength());
			packet.PutInt(character.GetJumpType());
			packet.PutByte(1);  // 1 or 0

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Broadcasts ZC_REST_SIT in range of character, making them sit down.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_REST_SIT(Character character)
		{
			var packet = new Packet(Op.ZC_REST_SIT);

			packet.PutInt(character.Handle);
			packet.PutByte(0);

			// [i11257 (2016-03-25)]
			// If this is set incorrectly, the character "freezes" and
			// doesn't animate while running around anymore.
			{
				packet.PutByte(character.IsSitting);
			}

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends ZC_ITEM_REMOVE to character, which removes the given item
		/// or amount from the inventory.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="worldId"></param>
		/// <param name="amount"></param>
		/// <param name="msg"></param>
		/// <param name="invType"></param>
		public static void ZC_ITEM_REMOVE(Character character, long worldId, int amount, InventoryItemRemoveMsg msg, InventoryType invType)
		{
			var packet = new Packet(Op.ZC_ITEM_REMOVE);

			packet.PutLong(worldId);
			packet.PutInt(amount);
			packet.PutInt(0); // i1
			packet.PutByte((byte)msg);
			packet.PutByte((byte)invType);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_ITEM_INVENTORY_INDEX_LIST to character, containing a list
		/// of indices for all items in the inventory. This updates their order.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_ITEM_INVENTORY_INDEX_LIST(Character character)
		{
			ZC_ITEM_INVENTORY_INDEX_LIST(character, character.Inventory.GetIndices());
		}

		/// <summary>
		/// Sends ZC_ITEM_INVENTORY_INDEX_LIST to character, containing a list
		/// of indices for all items in the given category of the inventory.
		/// This updates their order.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="category"></param>
		public static void ZC_ITEM_INVENTORY_INDEX_LIST(Character character, InventoryCategory category)
		{
			ZC_ITEM_INVENTORY_INDEX_LIST(character, character.Inventory.GetIndices(category));
		}

		/// <summary>
		/// Sends ZC_ITEM_INVENTORY_INDEX_LIST to character, containing a list
		/// of indices for items in an inventory. This updates their order.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="indices"></param>
		public static void ZC_ITEM_INVENTORY_INDEX_LIST(Character character, IDictionary<int, long> indices)
		{
			var packet = new Packet(Op.ZC_ITEM_INVENTORY_INDEX_LIST);

			packet.PutInt(indices.Count);
			foreach (var index in indices)
			{
				packet.PutLong(index.Value);
				packet.PutInt(index.Key);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_UPDATED_PCAPPEARANCE in range of character, updating
		/// their appearance.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_UPDATED_PCAPPEARANCE(Character character)
		{
			var packet = new Packet(Op.ZC_UPDATED_PCAPPEARANCE);

			packet.PutInt(character.Handle);
			packet.AddAppearancePc(character);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends ZC_ITEM_ADD to character, adding the item to the inventory.
		/// </summary>
		/// <remarks>
		/// "Updating" stack by *adding* items to it is an ADD as well.
		/// </remarks>
		/// <param name="character">Character to send packet to.</param>
		/// <param name="item">Item added or updated.</param>
		/// <param name="index">Index of the item in the inventory.</param>
		/// <param name="amount">Amount to add.</param>
		/// <param name="addType">The way the add is displayed?</param>
		public static void ZC_ITEM_ADD(Character character, Item item, int index, int amount, InventoryAddType addType)
		{
			var packet = new Packet(Op.ZC_ITEM_ADD);

			packet.PutLong(item.ObjectId);
			packet.PutInt(amount);
			packet.PutInt(index);
			packet.PutInt(item.Id);
			packet.PutShort(0); // Size of the object at the end
			packet.PutByte((byte)addType);
			packet.PutFloat(0); // Notification delay
			packet.PutByte(0); // InvType
			packet.PutEmptyBin(2);
			//packet.PutEmptyBin(0); // properties

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_MOVE_BARRACK to connection, informing client that it's
		/// save to disconnect?
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_MOVE_BARRACK(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_MOVE_BARRACK);
			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_LOGOUT_OK to connection, informing client that it's
		/// save to disconnect?
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_LOGOUT_OK(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_LOGOUT_OK);
			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_CAMPINFO to connection.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_CAMPINFO(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_CAMPINFO); // Size: 18 (12)
			packet.PutEmptyBin(12);
			conn.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_SET_POS in range of character, updating its position.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_SET_POS(Character character)
		{
			var packet = new Packet(Op.ZC_SET_POS);

			packet.PutInt(character.Handle);
			packet.PutFloat(character.Position.X);
			packet.PutFloat(character.Position.Y);
			packet.PutFloat(character.Position.Z);
			packet.PutByte(0);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends ZC_MOVE_ZONE_OK to connection, telling the client where to
		/// connect to, and which map to load.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		/// <param name="mapId"></param>
		public static void ZC_MOVE_ZONE_OK(ChannelConnection conn, string ip, int port, int mapId)
		{
			var packet = new Packet(Op.ZC_MOVE_ZONE_OK);

			packet.PutInt(210004);
			packet.PutInt(IPAddress.Parse(ip).ToInt32());
			packet.PutInt(port);
			packet.PutInt(mapId);
			packet.PutFloat(38); // Camera X angle
			packet.PutFloat(45); // Camera Y angle
			packet.PutFloat(200);
			packet.PutFloat(2200);
			packet.PutFloat(1000);
			packet.PutInt(26);
			packet.PutInt(20);
			packet.PutInt(59);
			packet.PutShort(0);
			packet.PutByte(0);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_MOVE_ZONE to connection, telling client to prepare for
		/// a warp.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_MOVE_ZONE(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_MOVE_ZONE);
			packet.PutByte(0);

			conn.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_PC in range of character, updating certain information.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="updateType"></param>
		/// <param name="newValue"></param>
		public static void ZC_PC(Character character, PcUpdateType updateType, object newValue)
		{
			var packet = new Packet(Op.ZC_PC);
			packet.PutInt((int)character.Handle);
			packet.PutInt((int)updateType);

			if (updateType == PcUpdateType.Name)
			{
				packet.PutShort(0);
				packet.PutEmptyBin(2);
				packet.PutInt(0);
				packet.PutShort(65);
				packet.PutString((string)newValue, 65);
			}
			else if (updateType == PcUpdateType.Job)
			{
				packet.PutShort((short)newValue);
				packet.PutEmptyBin(2);
				packet.PutInt(0);
			}

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Updates all of character's  properties.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="properties"></param>
		public static void ZC_OBJECT_PROPERTY(Character character)
		{
			ZC_OBJECT_PROPERTY(character.Connection, character);
		}

		/// <summary>
		/// Updates character's given properties.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="properties"></param>
		public static void ZC_OBJECT_PROPERTY(Character character, params int[] properties)
		{
			ZC_OBJECT_PROPERTY(character.Connection, character, properties);
		}

		/// <summary>
		/// Updates object's given properties.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="obj"></param>
		public static void ZC_OBJECT_PROPERTY(ChannelConnection conn, IPropertyObject obj)
		{
			var properties = obj.Properties.GetAll();
			var propertiesSize = properties.Sum(a => a.Size);

			var packet = new Packet(Op.ZC_OBJECT_PROPERTY);

			packet.PutLong(obj.ObjectId);
			packet.PutInt(0); // isTrickPacket
			packet.AddProperties(properties);

			conn.Send(packet);
		}

		/// <summary>
		/// Updates object's given properties.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="obj"></param>
		/// <param name="propertyIds"></param>
		public static void ZC_OBJECT_PROPERTY(ChannelConnection conn, IPropertyObject obj, params int[] propertyIds)
		{
			var properties = obj.Properties.GetAll(propertyIds);
			var propertiesSize = properties.Sum(a => a.Size);

			var packet = new Packet(Op.ZC_OBJECT_PROPERTY);

			packet.PutLong(obj.ObjectId);
			packet.PutInt(0); // isTrickPacket
			packet.AddProperties(properties);

			conn.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_ROTATE in range of character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_ROTATE(Character character)
		{
			var packet = new Packet(Op.ZC_ROTATE);

			packet.PutInt(character.Handle);
			packet.PutFloat(character.Direction.Cos);
			packet.PutFloat(character.Direction.Sin);
			packet.PutByte(0);
			packet.PutByte(0);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Broadcasts ZC_HEAD_ROTATE in range of character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_HEAD_ROTATE(Character character)
		{
			var packet = new Packet(Op.ZC_HEAD_ROTATE);

			packet.PutInt(character.Handle);
			packet.PutFloat(character.HeadDirection.Cos);
			packet.PutFloat(character.HeadDirection.Sin);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends ZC_DIALOG_OK to connection, containing a dialog message.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="msg"></param>
		public static void ZC_DIALOG_OK(ChannelConnection conn, string msg)
		{
			var packet = new Packet(Op.ZC_DIALOG_OK);

			packet.PutInt(0); // handle?
			packet.PutString(msg);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_DIALOG_NEXT to connection, containing a dialog message.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="msg"></param>
		public static void ZC_DIALOG_NEXT(ChannelConnection conn, string msg)
		{
			var packet = new Packet(Op.ZC_DIALOG_OK);

			packet.PutInt(0); // handle?
			packet.PutString(msg);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_DIALOG_SELECT to connection, containing a dialog message
		/// and a list of selectable options.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="arguments"></param>
		public static void ZC_DIALOG_SELECT(ChannelConnection conn, params string[] arguments)
		{
			if (arguments == null || arguments.Length == 0)
				return;

			var packet = new Packet(Op.ZC_DIALOG_SELECT);

			packet.PutInt(0); // handle?
			packet.PutShort(arguments.Length);
			packet.PutByte(0); // [i171032] ?
			foreach (var arg in arguments)
				packet.PutLpString(arg);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_DIALOG_CLOSE to connection, which closes the currently
		/// open dialog.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_DIALOG_CLOSE(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_DIALOG_CLOSE);
			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_DIALOG_STRINGINPUT to connection, containing a dialog
		/// message, and requesting putting in a string.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="msg"></param>
		public static void ZC_DIALOG_STRINGINPUT(ChannelConnection conn, string msg)
		{
			var packet = new Packet(Op.ZC_DIALOG_STRINGINPUT);

			packet.PutInt(0); // handle?
			packet.PutString(msg);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_DIALOG_NUMBERRANGE over connection, containing a dialog
		/// message, and requesting putting in a number.
		/// </summary>
		/// <remarks>
		/// Due to number range using CZ_DIALOG_SELECT for its response,
		/// the max range is 0~255, since that packet only holds a
		/// byte. The dialog window for this packet seems a little
		/// unfinished, and I didn't see any packets for it yet,
		/// it can be assumed that this feature, albeit working,
		/// isn't 100% done yet.
		/// </remarks>
		/// <param name="conn"></param>
		/// <param name="msg"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static void ZC_DIALOG_NUMBERRANGE(ChannelConnection conn, string msg, int min = 0, int max = 255)
		{
			min = Math2.Clamp(0, 255, min);
			max = Math2.Clamp(0, 255, max);
			if (max < min)
				max = min;

			var packet = new Packet(Op.ZC_DIALOG_NUMBERRANGE);

			packet.PutInt(0); // handle?
			packet.PutString(msg, 128);
			packet.PutInt(min);
			packet.PutInt(max);

			conn.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_LEAVE on monster's map, making it disappear.
		/// </summary>
		/// <param name="entity"></param>
		public static void ZC_LEAVE(IEntity entity)
		{
			var packet = new Packet(Op.ZC_LEAVE);

			packet.PutInt(entity.Handle);
			packet.PutShort(1); // 0 shows a blue effect when the entity disappears

			entity.Map.Broadcast(packet);
		}

		/// <summary>
		/// Sends ZC_LEAVE to conn, making it disappear.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="entity"></param>
		public static void ZC_LEAVE(ChannelConnection conn, IEntity entity)
		{
			var packet = new Packet(Op.ZC_LEAVE);

			packet.PutInt(entity.Handle);
			packet.PutShort(1); // 0 shows a blue effect when the entity disappears

			conn.Send(packet);
		}

		/// <summary>
		/// Enables/disables control over character.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="enabled"></param>
		public static void ZC_ENABLE_CONTROL(ChannelConnection conn, bool enabled)
		{
			var packet = new Packet(Op.ZC_ENABLE_CONTROL);
			packet.PutInt(0);
			packet.PutByte(enabled);

			conn.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_DEAD in range of entity, which kills it visually.
		/// </summary>
		/// <param name="entity">Entity to kill visually.</param>
		public static void ZC_DEAD(IEntity entity)
		{
			var packet = new Packet(Op.ZC_DEAD);
			packet.PutInt(entity.Handle);
			packet.PutInt(0);

			entity.Map.Broadcast(packet, entity);
		}

		/// <summary>
		/// Informs players about a hit that occured, and about the target's
		/// new hp, after damage was applied.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="target"></param>
		/// <param name="damage"></param>
		public static void ZC_HIT_INFO(IEntity attacker, IEntity target, int damage)
		{
			var packet = new Packet(Op.ZC_HIT_INFO);
			packet.PutInt(target.Handle);
			packet.PutInt(attacker.Handle);
			packet.PutInt(100);
			packet.PutInt(damage);
			packet.PutInt(target.Hp);
			packet.PutInt(2);
			packet.PutBinFromHex("01 00 00 60 06 68 03 00   18 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");

			target.Map.Broadcast(packet);
		}

		/// <summary>
		/// Informs players about a hit that occured, and about the target's
		/// new hp, after damage was applied.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="target"></param>
		/// <param name="damage"></param>
		public static void ZC_SKILL_HIT_INFO(IEntity attacker, IEntity target, int damage)
		{
			var packet = new Packet(Op.ZC_SKILL_HIT_INFO);
			packet.PutInt(attacker.Handle);
			packet.PutByte(1); // Count?
			packet.PutShort(26057);
			packet.PutShort(5236);
			packet.PutInt(target.Handle);
			packet.PutInt(damage);
			packet.PutInt(target.Hp);
			packet.PutInt(2);
			packet.PutBinFromHex("00 00 01 E3 A0 D0 03 00 00 A0 60 FC 4A 01 00 00 64 00 02 01 00 00 01 5C 00 00 00 00 00 00 00 03 00 00 00 60");

			target.Map.Broadcast(packet);
		}

		/// <summary>
		/// Updates character's level.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_PC_LEVELUP(Character character)
		{
			var packet = new Packet(Op.ZC_PC_LEVELUP);
			packet.PutInt(character.Handle);
			packet.PutInt(character.Level);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the number of purchased character slots.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="count"></param>
		public static void ZC_NORMAL_BarrackSlotCount(ChannelConnection conn, int count)
		{
			var packet = new Packet(Op.BC_NORMAL);
			packet.PutInt(SubOp.Zone.BarrackSlotCount);
			packet.PutInt(count);

			conn.Send(packet);
		}

		/// <summary>
		/// Plays level up effect.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NORMAL_LevelUp(Character character)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.LevelUp);
			packet.PutInt(character.Handle);
			packet.PutShort(8351);
			packet.PutShort(39);
			packet.PutFloat(6); // Effect size
			packet.PutInt(2);
			packet.PutEmptyBin(4);
			packet.PutFloat(1);
			packet.PutEmptyBin(4);
			packet.PutEmptyBin(4);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Plays class level up effect.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NORMAL_ClassLevelUp(Character character)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.ClassLevelUp);
			packet.PutInt(character.Handle);
			packet.PutByte(1);
			packet.PutInt(2);
			packet.PutByte(1);
			packet.PutFloat(6); // Effect size
			packet.PutBinFromHex("9E 20 27 00 00 00 00 00"); // Necessary for it to play

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Adjusts the skill speed for a skill.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		/// <param name="value"></param>
		public static void ZC_NORMAL_SetSkillSpeed(Character character, int skillId, float value)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.SetSkillSpeed);
			packet.PutInt(skillId);
			packet.PutFloat(value);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adjusts the hit delay for a skill.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="skillId"></param>
		public static void ZC_NORMAL_SetHitDelay(Character character, int skillId, float value)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.SetHitDelay);
			packet.PutInt(skillId);
			packet.PutFloat(value);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends the session key to the client.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_NORMAL_SetSessionKey(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.SetSessionKey);
			packet.PutLpString(conn.SessionKey);
			conn.Send(packet);
		}

		/// <summary>
		/// Sets the state of whether a hat is visible or not.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NORMAL_HatVisibleState(Character character)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.HatVisibleState);

			packet.PutInt(character.Handle);
			packet.PutByte((character.VisibleHats & HatVisibleStates.Hat1) != 0);
			packet.PutByte((character.VisibleHats & HatVisibleStates.Hat2) != 0);
			packet.PutByte((character.VisibleHats & HatVisibleStates.Hat3) != 0);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Updates exp and max exp.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="exp"></param>
		public static void ZC_MAX_EXP_CHANGED(Character character, int exp)
		{
			var packet = new Packet(Op.ZC_MAX_EXP_CHANGED);
			packet.PutInt(exp);
			packet.PutInt(character.Exp);
			packet.PutInt(0);
			packet.PutInt(character.MaxExp);
			packet.PutInt(0);
			packet.PutInt(character.TotalExp);
			packet.PutInt(0);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Notification about acquired exp from killing a monster?
		/// </summary>
		/// <param name="character"></param>
		/// <param name="exp"></param>
		/// <param name="classExp"></param>
		/// <param name="monster"></param>
		public static void ZC_EXP_UP_BY_MONSTER(Character character, int exp, int classExp, Monster monster)
		{
			var packet = new Packet(Op.ZC_EXP_UP_BY_MONSTER);
			packet.PutInt(exp);
			packet.PutInt(0);
			packet.PutInt(classExp);
			packet.PutInt(0);
			packet.PutInt(monster.Handle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adds exp.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="exp"></param>
		/// <param name="classExp"></param>
		public static void ZC_EXP_UP(Character character, int exp, int classExp)
		{
			var packet = new Packet(Op.ZC_EXP_UP);
			packet.PutInt(exp);
			packet.PutInt(0);
			packet.PutInt(classExp);
			packet.PutInt(0);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Adds job exp.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="exp"></param>
		public static void ZC_JOB_EXP_UP(Character character, int exp)
		{
			var packet = new Packet(Op.ZC_JOB_EXP_UP);
			packet.PutLong(character.Id);
			packet.PutInt(exp);
			packet.PutInt(0);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Executes Lua addon function.
		/// </summary>
		/// <remarks>Strings are placed without terminating bytes.</remarks>
		/// <param name="character"></param>
		/// <param name="msg"></param>
		/// <param name="parameter"></param>
		public static void ZC_ADDON_MSG(Character character, string msg, string parameter = null)
		{
			ZC_ADDON_MSG(character, 0, msg, parameter);
		}

		/// <summary>
		/// Executes Lua addon function.
		/// </summary>
		/// <remarks>Strings are placed without terminating bytes.</remarks>
		/// <param name="character"></param>
		/// <param name="duration">Duration in seconds messages are displayed?</param>
		/// <param name="msg"></param>
		/// <param name="parameter"></param>
		public static void ZC_ADDON_MSG(Character character, int duration, string msg, string parameter = null)
		{
			var packet = new Packet(Op.ZC_ADDON_MSG);
			packet.PutByte((byte)(msg.Length));
			packet.PutInt(duration);
			packet.PutByte(0);
			packet.PutString(msg, msg.Length);

			if (parameter != null)
				packet.PutString(parameter, parameter.Length);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends ZC_PC_PROP_UPDATE to character, updating a property.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="property"></param>
		/// <param name="value"></param>
		public static void ZC_PC_PROP_UPDATE(Character character, short property, byte value)
		{
			var packet = new Packet(Op.ZC_PC_PROP_UPDATE);
			packet.PutShort(property);
			packet.PutByte(value); // ?

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Shows emoticon for entity.
		/// </summary>
		/// <remarks>
		/// Couldn't find a list in the client data yet.
		/// Known emoticons:
		/// - 60008: Explamation mark bubble
		/// - 60009: Hearts
		/// </remarks>
		/// <param name="entity"></param>
		/// <param name="emoticons"></param>
		/// <param name="duration"></param>
		public static void ZC_SHOW_EMOTICON(IEntity entity, int emoticons, int duration)
		{
			var packet = new Packet(Op.ZC_SHOW_EMOTICON);
			packet.PutInt(entity.Handle);
			packet.PutInt(emoticons);
			packet.PutInt(duration);

			entity.Map.Broadcast(packet, entity);
		}

		/// <summary>
		/// Sends ZC_LOGIN_TIME to connection.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="now"></param>
		public static void ZC_LOGIN_TIME(ChannelConnection conn, DateTime now)
		{
			var packet = new Packet(Op.ZC_LOGIN_TIME);
			packet.PutLong(now.ToUnixTimeSeconds() * 1000);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends the visible areas of a map to a character.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_MAP_REVEAL_LIST(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_MAP_REVEAL_LIST);

			var revealedMaps = conn.Account.GetRevealedMaps();

			packet.PutInt(revealedMaps.Count());
			foreach (var revealedMap in revealedMaps)
			{
				packet.PutInt(revealedMap.MapId);
				packet.PutBin(revealedMap.Explored);
			}
			packet.PutLong(0);
			packet.PutFloat(56.45161f);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_IES_MODIFY_LIST to connection, containing a list of
		/// modifications for IES files.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_IES_MODIFY_LIST(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_IES_MODIFY_LIST);

			packet.PutShort(1); // count
			{
				packet.PutLpString("SharedConst");
				packet.PutShort(2); // row count
				{
					packet.PutInt(251);
					packet.PutShort(1); // col count
					{
						packet.PutLpString("Value");
						packet.PutShort(5); // patch count
						{
							packet.PutInt(6);
							packet.PutLpString("0");
							packet.PutLpString("0.00");
							packet.PutLpString("YEJI");
							packet.PutLpString("2016-3-30 3:15");
							packet.PutLpString("Change By Tool");

							packet.PutInt(5);
							packet.PutLpString("0");
							packet.PutLpString("0.00");
							packet.PutLpString("YEJI");
							packet.PutLpString("2016-3-30 3:15");
							packet.PutLpString("Change By Tool");

							packet.PutInt(4);
							packet.PutLpString("0");
							packet.PutLpString("0.00");
							packet.PutLpString("YEJI");
							packet.PutLpString("2016-3-30 3:15");
							packet.PutLpString("Change By Tool");

							packet.PutInt(3);
							packet.PutLpString("0");
							packet.PutLpString("0.00");
							packet.PutLpString("YEJI");
							packet.PutLpString("2016-3-30 3:15");
							packet.PutLpString("Change By Tool");

							packet.PutInt(2);
							packet.PutLpString("0");
							packet.PutLpString("0.00");
							packet.PutLpString("YEJI");
							packet.PutLpString("2016-3-30 3:15");
							packet.PutLpString("Change By Tool");
						}
					}
				}
			}

			conn.Send(packet);
		}

		/// <summary>
		/// Broadcasts ZC_POSE in range of character, putting them into the
		/// given pose.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="pose"></param>
		public static void ZC_POSE(Character character, int pose)
		{
			var pos = character.Position;
			var dir = character.Direction;

			var packet = new Packet(Op.ZC_POSE);

			packet.PutInt(character.Handle);
			packet.PutInt(pose);
			packet.PutFloat(pos.X);
			packet.PutFloat(pos.Y);
			packet.PutFloat(pos.Z);
			packet.PutFloat(dir.Cos);
			packet.PutFloat(dir.Sin);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends an updated value for the shield to an entity.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="shield"></param>
		public static void ZC_UPDATE_SHIELD(Character character, IEntity entity, int shield)
		{
			var packet = new Packet(Op.ZC_UPDATE_SHIELD);
			packet.PutInt(entity.Handle);
			packet.PutInt(shield);

			character.Map.Broadcast(packet, entity);
		}

		/// <summary>
		/// Broadcasts ZC_MOVE_DIR in range of character, informing other
		/// characters about the movement.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		/// <param name="unkFloat"></param>
		public static void ZC_MOVE_DIR(Character character, float x, float y, float z, float dx, float dy, float unkFloat)
		{
			var packet = new Packet(Op.ZC_MOVE_DIR);

			packet.PutInt(character.Handle);
			packet.PutFloat(x);
			packet.PutFloat(y);
			packet.PutFloat(z);
			packet.PutFloat(dx);
			packet.PutFloat(dy);
			packet.PutByte(1); // 0 = reduced movement speed... walk mode?
			packet.PutFloat(character.GetSpeed());
			packet.PutByte(1);
			packet.PutFloat(unkFloat);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Broadcasts ZC_MOVE_STOP in range of character, informing other
		/// characters about the movement stop.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public static void ZC_MOVE_STOP(Character character, float x, float y, float z)
		{
			var packet = new Packet(Op.ZC_MOVE_STOP);

			packet.PutInt(character.Handle);
			packet.PutFloat(x);
			packet.PutFloat(y);
			packet.PutFloat(z);
			packet.PutByte(0);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Broadcasts ZC_PC_MOVE_STOP in range of character, informing other
		/// characters about the movement stop.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		public static void ZC_PC_MOVE_STOP(Character character, Position position, Direction direction)
		{
			var packet = new Packet(Op.ZC_PC_MOVE_STOP);

			packet.PutInt(character.Handle);
			packet.PutFloat(position.X);
			packet.PutFloat(position.Y);
			packet.PutFloat(position.Z);
			packet.PutByte(1);
			packet.PutFloat(direction.Cos);
			packet.PutFloat(direction.Sin);
			packet.PutFloat(4238.274f); // ?

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends ZC_DIALOG_TRADE to connection, containing the name of the
		/// shop to open.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="shopName"></param>
		public static void ZC_DIALOG_TRADE(ChannelConnection conn, string shopName)
		{
			var packet = new Packet(Op.ZC_DIALOG_TRADE);

			packet.PutString(shopName, 33);

			conn.Send(packet);
		}

		/// Inform client that the skill is ready
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="id"></param>
		/// <param name="position1"></param>
		/// <param name="position2"></param>
		public static void ZC_SKILL_READY(Character character, int id, Position position1, Position position2)
		{
			var packet = new Packet(Op.ZC_SKILL_READY);
			packet.PutInt(character.Handle);
			packet.PutInt(id);
			packet.PutFloat(1);
			packet.PutBinFromHex("01 39 EC C0");
			packet.PutFloat(1);
			packet.PutFloat(position1.X);
			packet.PutFloat(position1.Y);
			packet.PutFloat(position1.Z);
			packet.PutFloat(position2.X);
			packet.PutFloat(position2.Y);
			packet.PutFloat(position2.Z);


			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Adjusts the time speed of the client.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="timeFactor"></param>
		public static void ZC_TIME_FACTOR(ChannelConnection conn, float timeFactor = 1)
		{
			var packet = new Packet(Op.ZC_TIME_FACTOR);
			packet.PutFloat(timeFactor);

			conn.Send(packet);
		}

		/// <summary>
		/// Sets the team ID for an entity.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="entity"></param>
		/// <param name="team">The team ID which is a value of either '0', '1', or '2'.</param>
		public static void ZC_TEAMID(ChannelConnection conn, IEntity entity, byte team)
		{
			var packet = new Packet(Op.ZC_TEAMID);
			packet.PutInt(entity.Handle);
			packet.PutByte(team);

			conn.Send(packet);
		}

		/// <summary>
		/// Sets the entity's owner to the specified character handle.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="monster"></param>
		public static void ZC_OWNER(Character character, IEntity entity)
		{
			var packet = new Packet(Op.ZC_OWNER);
			packet.PutInt(entity.Handle);
			packet.PutInt(character.Handle);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Creates a skill in client
		/// </summary>
		/// <param name="character"></param>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="direction"></param>
		/// <param name="create"></param>
		public static void ZC_NORMAL_Skill(Character character, int id, Position position, Direction direction, bool create)
		{
			var actorId = 1234; // ActorId (entity unique id for this skill)
			var distance = 20.0f; // Distance to caster? Not sure. Observed values (20, 40, 80)

			var skillState = 0; // skillState seems to be an ENUM of animation states (0 = initial animation, 1 = touched animation)
			if (!create)
				skillState = 1;

			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.Skill);
			packet.PutInt(character.Handle);
			packet.PutBinFromHex("11 18 27 00"); // Heal skill effect
			packet.PutInt(id); // SkillId
			packet.PutInt(1); // Skill Level ?
			packet.PutFloat(position.X);
			packet.PutFloat(position.Y);
			packet.PutFloat(position.Z);
			packet.PutFloat(direction.Cos); // Direction (commented out for now)
			packet.PutFloat(direction.Sin); // Direction (commented out for now)
			packet.PutInt(0);
			packet.PutFloat(distance);
			packet.PutInt(actorId);
			packet.PutByte(create);
			packet.PutInt(skillState);
			packet.PutInt(0);
			packet.PutInt(0);
			packet.PutInt(0);
			packet.PutInt(0);
			packet.PutInt(0);
			packet.PutInt(0);
			packet.PutInt(0);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Creates a particle effect (or set desired animation)
		/// </summary>
		/// <param name="character"></param>
		/// <param name="actorId"></param>
		/// <param name="enable"></param>
		public static void ZC_NORMAL_ParticleEffect(Character character, int actorId, int enable)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.ParticleEffect);
			packet.PutInt(actorId);
			packet.PutInt(enable);

			character.Map.Broadcast(packet);
		}

		/// <summary>
		/// Unkown purpose yet. It could be a "target" packet. (this actor is targeting "id" actor
		/// </summary>
		/// <param name="character"></param>
		/// <param name="handle"></param>
		/// <param name="position"></param>
		/// <param name="direction"></param>
		public static void ZC_NORMAL_Unkown_1c(Character character, int handle, Position position, Direction direction)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.Unkown_1D);
			packet.PutInt(character.Handle);
			packet.PutBinFromHex("00 D9 DB 30 09"); // This is not a fixed value, check more packets
			packet.PutInt(handle); // Target ActorId (seems to be)
			packet.PutFloat(position.X);
			packet.PutFloat(position.Y);
			packet.PutFloat(position.Z);
			packet.PutFloat(direction.Cos);
			packet.PutFloat(direction.Sin);
			packet.PutFloat(0);
			packet.PutFloat(0);
			packet.PutFloat(0);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends account properties.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NORMAL_AccountUpdate(Character character)
		{
			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.AccountUpdate);
			packet.PutLong(character.AccountId);
			packet.AddAccountProperties(character.Connection.Account);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Updates the skill UI with character job data.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_NORMAL_UpdateSkillUI(Character character)
		{
			// While the client will apparently gladly accept any combination
			// of jobs, the skill UI will only appear correctly if job
			// data for the character's current "display job" is sent.
			// For example, if the display job is Archer, data for *that*
			// job must be sent. Other base classes or higher jobs in the
			// same class do not work. Same thing for when the display
			// job is a higher job.

			var jobs = character.Jobs.GetList();

			var packet = new Packet(Op.ZC_NORMAL);
			packet.PutInt(SubOp.Zone.UpdateSkillUI);
			packet.PutLong(character.Id);

			packet.PutInt(jobs.Length);
			foreach (var job in jobs)
			{
				packet.PutShort((short)job.Id);
				packet.PutShort(177); // 174
				packet.PutInt(0);
				packet.PutInt(job.TotalExp);
				packet.PutInt(0);
				packet.PutShort(job.SkillPoints);
				packet.PutShort((short)job.Circle);
				packet.PutEmptyBin(4);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Set a Range type "FAN" shape in a given position
		/// </summary>
		/// <param name="character"></param>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="direction"></param>
		public static void ZC_SKILL_RANGE_FAN(Character character, int id, Position position, Direction direction)
		{
			var packet = new Packet(Op.ZC_SKILL_RANGE_FAN);
			packet.PutInt(character.Handle);
			packet.PutByte(1);
			packet.PutByte(1);
			packet.PutFloat(position.X);
			packet.PutFloat(position.Y);
			packet.PutFloat(position.Z);
			packet.PutFloat(direction.Cos);
			packet.PutFloat(direction.Sin);
			packet.PutFloat(0); // Height min ?
			packet.PutFloat(0.174533f); // Height max ?

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Enables/disables creature attack state
		/// </summary>
		/// <param name="character"></param>
		/// <param name="attacking"></param>
		public static void ZC_PC_ATKSTATE(Character character, bool attacking)
		{
			var packet = new Packet(Op.ZC_PC_ATKSTATE);
			packet.PutInt(character.Handle);
			packet.PutByte(attacking);

			character.Map.Broadcast(packet);
		}

		/// <summary>
		/// Updates creature's SP
		/// </summary>
		/// <param name="character"></param>
		/// <param name="spValue"></param>
		public static void ZC_UPDATE_SP(Character character, short spValue)
		{
			var packet = new Packet(Op.ZC_UPDATE_SP);
			packet.PutInt(character.Handle);
			packet.PutShort(spValue);
			packet.PutShort(0);
			packet.PutByte(0);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Updates a characters HP for damage and healing.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="amount"></param>
		/// <param name="isDamage"></param>
		/// <param name="currentHp"></param>
		/// <param name="priority"></param>
		public static void ZC_ADD_HP(Character character, int amount, bool isDamage, int currentHp, int priority)
		{
			// For some reason this is '1' for damage.
			var healing = (isDamage ? 1 : amount);

			var packet = new Packet(Op.ZC_ADD_HP);
			packet.PutInt(character.Handle);
			packet.PutInt(healing);
			packet.PutInt(currentHp);
			packet.PutInt(priority);

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Update creature basic stats (hp, mp)
		/// </summary>
		/// <param name="character"></param>
		/// <param name="currentHP"></param>
		/// <param name="maxHP"></param>
		/// <param name="currentSP"></param>
		/// <param name="maxSP"></param>
		public static void ZC_UPDATE_ALL_STATUS(Character character, int currentHP, int maxHP, int currentSP, int maxSP)
		{
			var packet = new Packet(Op.ZC_UPDATE_ALL_STATUS);
			packet.PutInt(character.Handle);
			packet.PutInt(currentHP);
			packet.PutInt(maxHP);
			packet.PutInt(currentSP);
			packet.PutInt(maxSP);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Increases player experience by killing monsters
		/// </summary>
		/// <param name="character"></param>
		/// <param name="experience"></param>
		/// <param name="jobExperience"></param>
		/// <param name="monsterActorId"></param>
		public static void ZC_EXP_UP_BY_MONSTER(Character character, int experience, int jobExperience, int monsterActorId)
		{
			var packet = new Packet(Op.ZC_EXP_UP_BY_MONSTER);
			packet.PutInt(experience);
			packet.PutInt(jobExperience);
			packet.PutInt(monsterActorId);

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Heals creature's HP
		/// </summary>
		/// <param name="character"></param>
		/// <param name="amountHealed"></param>
		/// <param name="maxHP"></param>
		public static void ZC_HEAL_INFO(Character character, int amountHealed, int maxHP)
		{
			var packet = new Packet(Op.ZC_HEAL_INFO);
			packet.PutInt(character.Handle);
			packet.PutInt(amountHealed);
			packet.PutInt(maxHP);
			packet.PutInt(1);
			packet.PutInt(0);
			packet.PutInt(0);

			character.Map.Broadcast(packet);
		}

		/// <summary>
		/// Updates the stance of a character.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_STANCE_CHANGE(Character character)
		{
			var packet = new Packet(Op.ZC_STANCE_CHANGE);
			packet.PutInt(character.Handle);
			packet.PutInt(character.Stance);

			character.Map.Broadcast(packet);
		}

		/// <summary>
		/// Reply packet when skill melee ground is placed
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_SKILL_MELEE_GROUND(Character character, int id, Position position, Direction direction)
		{
			var packet = new Packet(Op.ZC_SKILL_MELEE_GROUND);
			packet.PutInt(id);
			packet.PutInt(character.Handle);
			packet.PutFloat(direction.Cos);
			packet.PutFloat(direction.Sin);
			packet.PutInt(1);
			packet.PutFloat(600);
			packet.PutFloat(1);
			packet.PutFloat(-1);
			packet.PutInt(0);
			packet.PutInt(0);
			packet.PutFloat(position.X);
			packet.PutFloat(position.Y);
			packet.PutFloat(position.Z);
			packet.PutShort(0); // Some sort of Size for something else. Since this is a "variable size" packet.

			character.Map.Broadcast(packet, character);
		}

		/// <summary>
		/// Sends the faction for the handle.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="monster"></param>
		public static void ZC_FACTION(ChannelConnection conn, Monster monster, FactionType faction)
		{
			var packet = new Packet(Op.ZC_FACTION);
			packet.PutInt(monster.Handle);
			packet.PutInt((int)faction);

			conn.Send(packet);
		}

		/// <summary>
		/// Sends a list of help topics to the client.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_HELP_LIST(Character character)
		{
			// Get only the basic help topics for now. We probably need
			// a character or account based list of help topics the
			// player can see, potentially incl. the information of
			// whether they've read a specific topic yet.

			var defaultList = ChannelServer.Instance.Data.HelpDb.Entries.Values.Where(a => a.BasicHelp);

			var packet = new Packet(Op.ZC_HELP_LIST);
			packet.PutInt(defaultList.Count());
			foreach (var data in defaultList)
			{
				packet.PutInt(data.Id);
				packet.PutByte(0); // Unknown, maybe "has seen" toggle?
			}

			character.Connection.Send(packet);
		}

		/// Request the client to send information that needs to be saved before exiting?
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_SAVE_INFO(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_SAVE_INFO);
			conn.Send(packet);
		}

		/// <summary>
		/// Acknowledges the client that the loading screen has completed.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_LOAD_COMPLETE(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_LOAD_COMPLETE);
			conn.Send(packet);
		}

		/// <summary>
		/// Sends session objects to character's client.
		/// </summary>
		/// <param name="character"></param>
		public static void ZC_SESSION_OBJECTS(Character character)
		{
			var sessionObjects = character.SessionObjects.GetList();

			var packet = new Packet(Op.ZC_SESSION_OBJECTS);

			packet.PutShort(sessionObjects.Length);
			packet.PutByte(0);
			foreach (var obj in sessionObjects)
			{
				var properties = obj.Properties.GetAll();
				var propertiesSize = obj.Properties.Size;

				packet.PutInt(obj.Id);
				packet.PutInt(-926557701);
				packet.PutLong(obj.ObjectId);
				packet.PutInt(0);

				packet.PutShort(propertiesSize);
				packet.PutShort(0);
				packet.AddProperties(properties);
			}

			character.Connection.Send(packet);
		}

		/// <summary>
		/// Sends premium state properties to client.
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_SEND_CASH_VALUE(ChannelConnection conn)
		{
			var packet = new Packet(Op.ZC_SEND_CASH_VALUE);

			// Premium state 0?
			packet.PutInt(4); // count?
			{
				packet.PutLpString("speedUp");
				packet.PutFloat(0);

				packet.PutLpString("marketUpMax");
				packet.PutFloat(1);

				packet.PutLpString("marketSellCom");
				packet.PutFloat(30);

				packet.PutLpString("abilityMax");
				packet.PutFloat(1);
			}

			// Premium state 1?
			packet.PutInt(4);
			{
				packet.PutLpString("speedUp");
				packet.PutFloat(3);

				packet.PutLpString("marketUpMax");
				packet.PutFloat(5);

				packet.PutLpString("marketSellCom");
				packet.PutFloat(10);

				packet.PutLpString("abilityMax");
				packet.PutFloat(3);
			}

			// Premium state 2?
			packet.PutInt(4);
			{
				packet.PutLpString("speedUp");
				packet.PutFloat(3);

				packet.PutLpString("marketUpMax");
				packet.PutFloat(10);

				packet.PutLpString("marketSellCom");
				packet.PutFloat(10);

				packet.PutLpString("abilityMax");
				packet.PutFloat(2);
			}

			// ?
			packet.PutInt(4);
			{
				packet.PutInt(7);
				packet.PutFloat(2.5f);

				packet.PutInt(5);
				packet.PutFloat(2);

				packet.PutInt(3);
				packet.PutFloat(1.5f);

				packet.PutInt(1);
				packet.PutFloat(1);
			}

			conn.Send(packet);
		}

		/// <summary>
		/// Sends ZC_RESPONSE_GUILD_INDEX to client (dummy).
		/// </summary>
		/// <param name="conn"></param>
		public static void ZC_RESPONSE_GUILD_INDEX(Character character)
		{
			var packet = new Packet(Op.ZC_RESPONSE_GUILD_INDEX);
			packet.PutInt(character.Handle);
			packet.PutInt(0);
			packet.PutInt(0);
			packet.PutShort(1003);

			character.Connection.Send(packet);
		}

		public static void DUMMY(ChannelConnection conn)
		{
		}
	}

	public class MsgParameter
	{
		public string Key { get; protected set; }
		public string Value { get; protected set; }

		public MsgParameter(string key, string value)
		{
			this.Key = key;
			this.Value = value;
		}
	}

	public enum FactionType : int
	{
		Player = 1,
		Monster = 2,
		Npc = 3,
	}
}
