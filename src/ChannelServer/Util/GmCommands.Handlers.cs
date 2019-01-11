// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Globalization;
using System.Linq;
using Melia.Channel.Network;
using Melia.Channel.World;
using Melia.Shared.Const;
using Melia.Shared.Data.Database;
using Melia.Shared.Util;
using Melia.Shared.Util.Commands;
using Melia.Shared.World;

namespace Melia.Channel.Util
{
	/// <summary>
	/// GM command manager.
	/// </summary>
	public partial class GmCommands : CommandManager<GmCommand, GmCommandFunc>
	{
		/// <summary>
		/// Creates new manager and initializes it.
		/// </summary>
		public GmCommands()
		{
			// The required authority levels for commands can be specified
			// in the configuration file "system/conf/commands.conf".

			// Official
			Add("requpdateequip", "", this.HandleReqUpdateEquip);
			Add("buyabilpoint", "<amount>", this.HandleBuyAbilPoint);
			Add("learnpcabil", "<ability class name>", this.HandleLearnPcAbil);

			// Normal
			Add("where", "", this.HandleWhere);
			Add("name", "<new name>", this.HandleName);

			// GMs
			Add("jump", "<x> <y> <z>", this.HandleJump);
			Add("warp", "<map id> <x> <y> <z>", this.HandleWarp);
			Add("item", "<item id> [amount]", this.HandleItem);
			Add("silver", "<modifier>", this.HandleSilver);
			Add("spawn", "<monster id> [amount=1]", this.HandleSpawn);
			Add("madhatter", "", this.HandleGetAllHats);
			Add("levelup", "<levels>", this.HandleLevelUp);
			Add("speed", "<speed>", this.HandleSpeed);
			Add("iteminfo", "<name>", this.HandleItemInfo);
			Add("monsterinfo", "<name>", this.HandleMonsterInfo);
			Add("go", "<destination>", this.HandleGo);
			Add("goto", "<team name>", this.HandleGoTo);
			Add("recall", "<team name>", this.HandleRecall);
			Add("recallmap", "[map id/name]", this.HandleRecallMap);
			Add("recallall", "", this.HandleRecallAll);
			Add("clearinv", "", this.HandleClearInventory);
			Add("addjob", "<job id> [circle]", this.HandleAddJob);
			Add("removejob", "<job id>", this.HandleRemoveJob);
			Add("skillpoints", "<job id> <modifier>", this.HandleSkillPoints);
			Add("statpoints", "<amount>", this.HandleStatPoints);

			// Dev
			Add("test", "", this.HandleTest);
			Add("reloadscripts", "", this.HandleReloadScripts);
			Add("reloadconf", "", this.HandleReloadConf);

			// Aliases
			AddAlias("iteminfo", "ii");
			AddAlias("monsterinfo", "mi");
			AddAlias("reloadscripts", "rs");
		}

		/// <summary>
		/// Test command, modify to quickly test something, but never
		/// commit the changes to it.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleTest(ChannelConnection conn, Character character, Character target, string command, string[] args)
		{
			Log.Debug("test!!");

			return CommandResult.Okay;
		}

		/// <summary>
		/// Tells the sender where the target currently is.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleWhere(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (sender == target)
				sender.ServerMessage("You are here: {0} ({1}), {2}", target.Map.Name, target.Map.Id, target.Position);
			else
				sender.ServerMessage("{3} is here: {0} ({1}), {2}", target.Map.Name, target.Map.Id, target.Position, target.TeamName);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Warps target to given position on their current map.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleJump(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 4)
				return CommandResult.InvalidArgument;

			if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) || !float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) || !float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
				return CommandResult.InvalidArgument;

			target.Position = new Position(x, y, z);
			Send.ZC_SET_POS(target);

			if (sender == target)
			{
				sender.ServerMessage("You were warped to {0}.", target.Position);
			}
			else
			{
				target.ServerMessage("You were warped to {0} by {1}.", target.Position, sender.TeamName);
				sender.ServerMessage("Target was warped.");
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Warps target to given location.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleWarp(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			float x = 0, y = 0, z = 0;

			if (!int.TryParse(args[1], out var mapId))
			{
				var data = ChannelServer.Instance.Data.MapDb.Find(args[1]);
				if (data == null)
				{
					sender.ServerMessage("Map not found.");
					return CommandResult.Okay;
				}

				mapId = data.Id;
			}

			if (args.Length >= 5)
			{
				if (!float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out x) || !float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out y) || !float.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
					return CommandResult.InvalidArgument;
			}

			try
			{
				target.Warp(mapId, x, y, z);

				if (sender == target)
				{
					sender.ServerMessage("You were warped to {0}.", target.GetLocation());
				}
				else
				{
					target.ServerMessage("You were warped to {0} by {1}.", target.GetLocation(), sender.TeamName);
					sender.ServerMessage("Target was warped.");
				}
			}
			catch (ArgumentException)
			{
				sender.ServerMessage("Map not found.");
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Spawns item in target's inventory.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleItem(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			var amount = 1;

			// Get and check id
			if (!int.TryParse(args[1], out var itemId))
				return CommandResult.InvalidArgument;

			if (!ChannelServer.Instance.Data.ItemDb.Exists(itemId))
			{
				sender.ServerMessage("Item not found.");
				return CommandResult.Okay;
			}

			// Get amount
			if (args.Length > 2)
			{
				if (!int.TryParse(args[2], out amount) || amount < 1)
					return CommandResult.InvalidArgument;
			}

			// Create and add item
			var item = new Item(itemId, amount);
			target.Inventory.Add(item, InventoryAddType.PickUp);

			sender.ServerMessage("Item created.");
			if (sender != target)
				target.ServerMessage("An item was added to your inventory by {0}.", sender.TeamName);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Adds or removes silver from target's inventory.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleSilver(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			if (!int.TryParse(args[1], out var modifier) || modifier == 0)
				return CommandResult.InvalidArgument;

			// Create and add silver item
			if (modifier > 0)
			{
				var item = new Item(ItemId.Silver, modifier);
				target.Inventory.Add(item, InventoryAddType.PickUp);

				if (sender == target)
				{
					sender.ServerMessage("{0:n0} silver were added to your inventory.", modifier);
				}
				else
				{
					sender.ServerMessage("{0:n0} silver were added to target's inventory.", modifier);
					target.ServerMessage("{0} added {1:n0} silver to your inventory.", sender.TeamName, modifier);
				}
			}
			// Remove silver items
			else
			{
				modifier = -modifier;

				target.Inventory.Remove(ItemId.Silver, modifier, InventoryItemRemoveMsg.Destroyed);

				if (sender == target)
				{
					sender.ServerMessage("{0:n0} silver were removed from your inventory.", modifier);
				}
				else
				{
					sender.ServerMessage("{0:n0} silver were removed from target's inventory.", modifier);
					target.ServerMessage("{0} removed {1:n0} silver from your inventory.", sender.TeamName, modifier);
				}
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Spawns monsters at target's location.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleSpawn(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			if (!int.TryParse(args[1], out var id))
				return CommandResult.InvalidArgument;

			var amount = 1;
			if (args.Length > 2 && !int.TryParse(args[2], out amount))
				return CommandResult.InvalidArgument;

			amount = Math2.Clamp(1, 100, amount);

			var monsterData = ChannelServer.Instance.Data.MonsterDb.Find(id);
			if (monsterData == null)
			{
				sender.ServerMessage("Monster not found.");
				return CommandResult.Okay;
			}

			var rnd = new Random(Environment.TickCount);
			for (var i = 0; i < amount; ++i)
			{
				var monster = new Monster(id, NpcType.Monster);

				Position pos;
				Direction dir;
				if (amount == 1)
				{
					pos = target.Position;
					dir = target.Direction;
				}
				else
				{
					pos = target.Position.GetRandomInRange2D(amount * 4, rnd);
					dir = new Direction(rnd.Next(0, 360));
				}

				monster.Position = pos;
				monster.Direction = dir;

				target.Map.AddMonster(monster);
			}

			sender.ServerMessage("Monsters were spawned.");
			if (sender != target)
				target.ServerMessage("Monsters were spawned at your location by {0}.", sender.TeamName);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Adds all available hats to target's inventory.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleGetAllHats(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			var addedCount = 0;
			for (var itemId = 628001; itemId <= 629503; ++itemId)
			{
				if (!ChannelServer.Instance.Data.ItemDb.Exists(itemId))
					continue;

				if (!sender.Inventory.HasItem(itemId))
				{
					sender.Inventory.Add(new Item(itemId), InventoryAddType.PickUp);
					addedCount++;
				}
			}

			if (sender == target)
			{
				sender.ServerMessage("Added {0} hats to your inventory.", addedCount);
			}
			else
			{
				target.ServerMessage("{1} added {0} hats to your inventory.", addedCount, sender.TeamName);
				sender.ServerMessage("Added {0} hats to target's inventory.", addedCount);
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Changes target's name (not team name).
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleName(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			var newName = args[1];
			if (newName == sender.Name)
				return CommandResult.Okay;

			// TODO: Can you rename any time, without cooldown?

			// TODO: Keep a list of all account characters after all?
			if (ChannelServer.Instance.Database.CharacterExists(conn.Account.Id, newName))
			{
				sender.ServerMessage("Name already exists.");
				return CommandResult.Okay;
			}

			target.Name = newName;
			Send.ZC_PC(target, PcUpdateType.Name, newName);

			sender.ServerMessage("Name changed.", target.Position);
			if (sender != target)
				target.ServerMessage("Your name was changed by {0}.", sender.TeamName);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Reloads all scripts.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleReloadScripts(ChannelConnection conn, Character character, Character target, string command, string[] args)
		{
			character.ServerMessage("Reloading scripts...");

			ChannelServer.Instance.ScriptManager.Reload();

			character.ServerMessage("Done.");

			return CommandResult.Okay;
		}

		/// <summary>
		/// Reloads all conf files.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="character"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleReloadConf(ChannelConnection conn, Character character, Character target, string command, string[] args)
		{
			character.ServerMessage("Reloading configuration...");

			ChannelServer.Instance.Conf.LoadAll();

			character.ServerMessage("Done.");

			return CommandResult.Okay;
		}

		/// <summary>
		/// Levels up target.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleLevelUp(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			var levels = 1;
			if (args.Length > 1 && (!int.TryParse(args[1], out levels) || levels < 1))
				return CommandResult.InvalidArgument;

			// Set exp to 0, ZC_MAX_EXP_CHANGED apparently doesn't update the
			// exp bar if the exp didn't change.
			target.Exp = 0;
			target.TotalExp = ChannelServer.Instance.Data.ExpDb.GetTotalExp(target.Level + levels);
			target.LevelUp(levels);

			if (sender == target)
			{
				sender.ServerMessage("Your level was changed.");
			}
			else
			{
				target.ServerMessage("Your level was changed by {0}.", sender.TeamName);
				sender.ServerMessage("The target's level was changed.");
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Changes target's speed.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleSpeed(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			if (!float.TryParse(args[1], out var speed))
				return CommandResult.InvalidArgument;

			target.Speed = speed;
			Send.ZC_MOVE_SPEED(target);
			Send.ZC_OBJECT_PROPERTY(target, PropertyId.PC.MSPD);

			if (sender == target)
			{
				sender.ServerMessage("Your speed was changed.");
			}
			else
			{
				target.ServerMessage("Your speed was changed by {0}.", sender.TeamName);
				sender.ServerMessage("Target's speed was changed.");
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Searches item database for given string.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleItemInfo(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			var search = command.Substring(command.IndexOf(" ")).Trim();
			var items = ChannelServer.Instance.Data.ItemDb.FindAll(search);
			if (items.Count == 0)
			{
				sender.ServerMessage("No items found for '{0}'.", search);
				return CommandResult.Okay;
			}

			var eItems = items.OrderBy(a => a.Name.LevenshteinDistance(search)).ThenBy(a => a.Id).GetEnumerator();
			var max = 20;
			for (var i = 0; eItems.MoveNext() && i < max; ++i)
			{
				var item = eItems.Current;
				sender.ServerMessage("{0}: {1}, Category: {2}", item.Id, item.Name, item.Category);
			}

			sender.ServerMessage("Results: {0} (Max. {1} shown)", items.Count, max);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Searches monster database for given string.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleMonsterInfo(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			var search = command.Substring(command.IndexOf(" ")).Trim();
			var monsters = ChannelServer.Instance.Data.MonsterDb.FindAll(search);
			if (monsters.Count == 0)
			{
				sender.ServerMessage("No monsters found for '{0}'.", search);
				return CommandResult.Okay;
			}

			var entries = monsters.OrderBy(a => a.Name.LevenshteinDistance(search)).ThenBy(a => a.Id).GetEnumerator();
			var max = 20;
			for (var i = 0; entries.MoveNext() && i < max; ++i)
			{
				var current = entries.Current;
				sender.ServerMessage("{0}: {1}", current.Id, current.Name);
			}

			sender.ServerMessage("Results: {0} (Max. {1} shown)", monsters.Count, max);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Warps target to a pre-defined location.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleGo(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
			{
				sender.ServerMessage("Destinations: klaipeda, orsha");
				return CommandResult.InvalidArgument;
			}

			if (args[1].StartsWith("klaip"))
				target.Warp("c_Klaipe", -75, 148, -24);
			else if (args[1].StartsWith("ors"))
				target.Warp("c_orsha", 271, 176, 292);
			else
			{
				sender.ServerMessage("Unknown destination.");
				return CommandResult.Okay;
			}

			if (sender == target)
			{
				sender.ServerMessage("You were warped to {0}.", target.GetLocation());
			}
			else
			{
				target.ServerMessage("You were warped to {0} by {1}.", target.GetLocation(), sender.TeamName);
				sender.ServerMessage("Target was warped.");
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Warps target to a specific character's location.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleGoTo(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			// TODO: Once we have support for more than one map server,
			//   we have to search for characters across all of them.

			var teamName = args[1];
			var character = ChannelServer.Instance.World.GetCharacterByTeamName(teamName);
			if (character == null)
			{
				sender.ServerMessage("Character not found.");
				return CommandResult.Okay;
			}

			target.Warp(character.GetLocation());

			if (sender == target)
			{
				sender.ServerMessage("You've been warped to {0}'s location.", teamName);
			}
			else
			{
				sender.ServerMessage("Target was warped.");
				target.ServerMessage("You've been warped to {0}'s location by {1}.", teamName, sender.TeamName);
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Warps specific character to target.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleRecall(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			// TODO: Once we have support for more than one map server,
			//   we have to search for characters across all of them.

			var teamName = args[1];
			var character = ChannelServer.Instance.World.GetCharacterByTeamName(teamName);
			if (character == null)
			{
				sender.ServerMessage("Character not found.");
				return CommandResult.Okay;
			}

			character.Warp(target.GetLocation());

			character.ServerMessage("You've been warped to {0}'s location.", target.TeamName);
			sender.ServerMessage("Character was warped.");
			if (sender != target)
				target.ServerMessage("{0} was warped to your location by {1}.", character.TeamName, sender.TeamName);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Warps all players on the map to target's location.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleRecallMap(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length > 2)
				return CommandResult.InvalidArgument;

			var map = target.Map;

			// TODO: Once we have support for channels and map servers,
			//   add warp from other servers and restrict recall to
			//   channel's max player count.
			if (args.Length > 1)
			{
				// Search for map by name and id
				if (int.TryParse(args[1], out var mapId))
					map = ChannelServer.Instance.World.GetMap(mapId);
				else
					map = ChannelServer.Instance.World.GetMap(args[1]);

				// Check map
				if (map == null)
				{
					sender.ServerMessage("Unknown map.");
					return CommandResult.Okay;
				}
			}

			var characters = map.GetCharacters(a => a != target);

			// Check for characters
			if (!characters.Any())
			{
				sender.ServerMessage("No players found.");
				return CommandResult.Okay;
			}

			RecallCharacters(sender, target, characters);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Warps all players on the server to target's location.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleRecallAll(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length > 1)
				return CommandResult.InvalidArgument;

			// TODO: Once we have support for channels and map servers,
			//   add warp from other servers and restrict recall to
			//   channel's max player count.

			// Check for characters
			var characters = ChannelServer.Instance.World.GetCharacters(a => a != target);
			if (!characters.Any())
			{
				sender.ServerMessage("No players found.");
				return CommandResult.Okay;
			}

			RecallCharacters(sender, target, characters);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Removes all items from target's inventory.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleClearInventory(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			target.Inventory.Clear();

			sender.ServerMessage("Inventory cleared.");
			if (sender != target)
				target.ServerMessage("Your inventory was cleared by {0}.", sender.TeamName);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Official slash command, purpose unknown.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleReqUpdateEquip(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			// Command is sent when the inventory is opened, purpose unknown,
			// officials don't seem to send anything back.

			// Comment in the client's Lua files:
			//   내구도 회복 유료템 때문에 정확한 값을 지금 알아야 함.
			//   (Durability recovery Due to the paid system, you need to know the correct value now.)

			return CommandResult.Okay;
		}

		/// <summary>
		/// Official slash command, exchanges silver for ability points.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleBuyAbilPoint(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			// Since this command is sent via UI interactions, we'll not
			// use any automated command result messages, but we'll leave
			// debug messages for now, in case of unexpected values.

			if (args.Length < 0)
			{
				Log.Debug("HandleBuyAbilPoint: No amount given by user '{0}'.", conn.Account.Name);
				return CommandResult.Okay;
			}

			if (!int.TryParse(args[1], out var amount))
			{
				Log.Debug("HandleBuyAbilPoint: Invalid amount '{0}' by user '{1}'.", amount, conn.Account.Name);
				return CommandResult.Okay;
			}

			var cost = (amount * 1000);
			var silver = sender.Inventory.CountItem(ItemId.Silver);
			if (silver < cost)
			{
				Log.Debug("HandleBuyAbilPoint: User '{0}' didn't have enough money.", conn.Account.Name);
				return CommandResult.Okay;
			}

			sender.Inventory.Remove(ItemId.Silver, cost, InventoryItemRemoveMsg.Given);
			sender.ModifyAbilityPoints(amount);
			Send.ZC_ADDON_MSG(sender, AddonMessage.SUCCESS_BUY_ABILITY_POINT, "BLANK");

			return CommandResult.Okay;
		}

		/// <summary>
		/// Official slash command to learn abilities.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleLearnPcAbil(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			// Since this command is sent via UI interactions, we'll not
			// use any automated command result messages, but we'll leave
			// debug messages for now, in case of unexpected values.

			if (args.Length != 3 || !int.TryParse(args[2], out var levels) || levels < 1)
			{
				Log.Debug("HandleLearnPcAbil: Invalid call by user '{0}': {1}", conn.Account.Name, command);
				return CommandResult.Okay;
			}

			var className = args[1];

			var abilityData = ChannelServer.Instance.Data.AbilityDb.Find(className);
			if (abilityData == null)
			{
				Log.Debug("HandleLearnPcAbil: User '{0}' tried to learn non-existent ability '{1}'.", conn.Account.Name, className);
				return CommandResult.Okay;
			}

			// All we get here is the ability name, but whether it can
			// be learned or not potentially depends on any of the job's
			// ability tree's entries. We have to check whether the ability
			// can be learned by any of the character's jobs.

			var abilityId = abilityData.Id;
			var canLearn = false;
			var jobs = sender.Jobs.GetList();

			AbilityTreeData abilityTreeData = null;
			foreach (var job in jobs)
			{
				// An ability can be learned by a job if there's an entry
				// for it in the tree and an unlock condition is given.
				var jobAbilityTreeData = ChannelServer.Instance.Data.AbilityTreeDb.Find(job.Id, abilityId);
				if (jobAbilityTreeData != null && jobAbilityTreeData.HasUnlock)
				{
					var unlocked = AbilityUnlock.IsUnlocked(sender, abilityData, jobAbilityTreeData);
					if (unlocked)
					{
						canLearn = true;
						abilityTreeData = jobAbilityTreeData;
						break;
					}
				}
			}

			if (!canLearn)
			{
				Log.Debug("HandleLearnPcAbil: User '{0}' tried to learn ability '{1}', which they can't learn (yet).", conn.Account.Name, className);
				return CommandResult.Okay;
			}

			var ability = sender.Abilities.Get(abilityId);
			var currentLevel = (ability == null ? 0 : ability.Level);
			var newLevel = (currentLevel + levels);
			var maxLevel = abilityTreeData.MaxLevel;

			if (newLevel > maxLevel)
			{
				Log.Debug("HandleLearnPcAbil: User '{0}' tried to increase ability '{1}'s level past the max level of {2}.", conn.Account.Name, className, maxLevel);
				return CommandResult.Okay;
			}

			// Price and time can come either from the actual values,
			// or from functions that return both.

			var price = abilityTreeData.Price;
			var time = abilityTreeData.Time;

			if (abilityTreeData.HasPriceTime)
			{
				price = 0;

				for (var i = currentLevel + 1; i <= newLevel; ++i)
				{
					AbilityPriceTime.Get(sender, abilityData, abilityTreeData, i, out var addPrice, out time);
					price += addPrice;
				}
			}

			var points = sender.AbilityPoints;
			if (points < price)
			{
				Log.Debug("HandleLearnPcAbil: User '{0}' didn't have enough points.", conn.Account.Name);
				return CommandResult.Okay;
			}

			//Log.Debug("Learn: {0}", abilityData.EngName);
			//Log.Debug("- From: {0}", currentLevel);
			//Log.Debug("- To: {0}", newLevel);
			//Log.Debug("- Price: {0}", price);
			//Log.Debug("- Time: {0}", time);

			// Add ability if character doesn't have it yet
			if (ability == null)
			{
				ability = new Ability(abilityId, 0);
				sender.Abilities.Add(ability);
			}

			// Update ability
			ability.Level += levels;
			Send.ZC_OBJECT_PROPERTY(sender.Connection, ability);

			sender.ModifyAbilityPoints(-price);
			Send.ZC_ADDON_MSG(sender, AddonMessage.RESET_ABILITY_UP, "Ability_" + abilityTreeData.Category);

			return CommandResult.Okay;
		}

		/// <summary>
		/// Adds job to target.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleAddJob(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			if (!int.TryParse(args[1], out var iJobId))
				return CommandResult.InvalidArgument;

			var jobId = (JobId)iJobId;
			if (!ChannelServer.Instance.Data.JobDb.Exists(jobId))
			{
				sender.ServerMessage("Job data for '{0}' not found.", jobId);
				return CommandResult.Okay;
			}

			var circle = Circle.First;

			if (args.Length > 2)
			{
				if (!short.TryParse(args[2], out var iCircle) || iCircle < (short)Circle.First || !Enum.IsDefined(typeof(Circle), iCircle))
					return CommandResult.InvalidArgument;

				circle = (Circle)iCircle;
			}

			var job = target.Jobs.Get(jobId);
			if (job != null && job.Circle >= circle)
			{
				sender.ServerMessage("The job exists already, at an equal or higher circle.");
				return CommandResult.Okay;
			}

			if (job == null)
				target.Jobs.Add(new Job(target, jobId, circle));
			else
				target.Jobs.ChangeCircle(jobId, circle);

			sender.ServerMessage("Job '{0}' was added at circle '{1}'.", jobId, (int)circle);
			if (sender != target)
				target.ServerMessage("Job '{0}' was added to your character at circle '{1}' by {2}.", jobId, (int)circle, sender.TeamName);

			return CommandResult.Okay;
		}

		private CommandResult HandleRemoveJob(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			if (!int.TryParse(args[1], out var iJobId))
				return CommandResult.InvalidArgument;

			var jobId = (JobId)iJobId;

			if (!target.Jobs.Remove(jobId))
			{
				sender.ServerMessage("The job doesn't exist.");
				return CommandResult.Okay;
			}

			if (sender == target)
			{
				sender.ServerMessage("Job '{0}' was removed. Login again to see the change.", jobId);
			}
			else
			{
				target.ServerMessage("Job '{0}' was removed by {1}. Login again to see the change.", jobId, sender.TeamName);
				sender.ServerMessage("Job '{0}' was removed from target.", jobId);
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Modifies target's skill points for the given job.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleSkillPoints(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 3)
				return CommandResult.InvalidArgument;

			if (!int.TryParse(args[1], out var iJobId))
				return CommandResult.InvalidArgument;

			if (!int.TryParse(args[2], out var modifier))
				return CommandResult.InvalidArgument;

			var jobId = (JobId)iJobId;

			if (!target.Jobs.ModifySkillPoints(jobId, modifier))
			{
				sender.ServerMessage("The job doesn't exist.");
				return CommandResult.Okay;
			}

			if (sender == target)
			{
				sender.ServerMessage("Modified {0}'s skill points by {1:+0;-0;0}.", jobId, modifier);
			}
			else
			{
				sender.ServerMessage("Modified target {0}'s skill points by {1:+0;-0;0}.", jobId, modifier);
				target.ServerMessage("Your {0}'s skill points were modified by {1}.", jobId, sender.TeamName);
			}

			return CommandResult.Okay;
		}

		/// <summary>
		/// Adds stat points to target character.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="sender"></param>
		/// <param name="target"></param>
		/// <param name="command"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private CommandResult HandleStatPoints(ChannelConnection conn, Character sender, Character target, string command, string[] args)
		{
			if (args.Length < 2)
				return CommandResult.InvalidArgument;

			if (!int.TryParse(args[1], out var amount) || amount < 1)
				return CommandResult.InvalidArgument;

			// Modification for stat points is a little tricky, because ToS
			// has 3 stat points properties:
			// - Stat points gained by leveling
			// - Stat points gained in another way
			// - Used stat points
			// When increasing stats, "Used" is increased and the others are
			// left alone. I'll make this adding-only for now, until I feel
			// like untangling modifying them.

			target.AddStatPoints(amount);

			sender.ServerMessage("Added {0} stat points.", amount);
			if (sender != target)
				sender.ServerMessage("{1} added {0} stat points to your character.", amount, sender.TeamName);

			return CommandResult.Okay;
		}
	}
}
