// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Melia.Channel.Scripting;
using Melia.Channel.World;
using Melia.Shared.Const;
using Melia.Shared.Database;
using Melia.Shared.Util;
using Melia.Shared.World;
using Melia.Shared.World.ObjectProperties;
using MySql.Data.MySqlClient;

namespace Melia.Channel.Database
{
	public class ChannelDb : MeliaDb
	{
		/// <summary>
		/// Saves account.
		/// </summary>
		/// <param name="account"></param>
		/// <returns></returns>
		public bool SaveAccount(Account account)
		{
			if (account == null)
				throw new ArgumentNullException("account");

			using (var conn = this.GetConnection())
			using (var cmd = new UpdateCommand("UPDATE `accounts` SET {0} WHERE `accountId` = @accountId", conn))
			{
				cmd.AddParameter("@accountId", account.Id);
				cmd.Set("settings", account.Settings.ToString());

				if (cmd.Execute() == 0)
					return false;
			}

			this.SaveVariables("account:" + account.Id, account.Variables.Perm);
			this.SaveChatMacros(account);
			this.SaveRevealedMaps(account);

			return true;
		}

		/// <summary>
		/// Returns account with given name, or null if it doesn't exist.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Account GetAccount(string name)
		{
			var account = new Account();

			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `accounts` WHERE `name` = @name", conn))
			{
				mc.Parameters.AddWithValue("@name", name);

				using (var reader = mc.ExecuteReader())
				{
					if (!reader.Read())
						return null;

					account.Id = reader.GetInt64("accountId");
					account.Name = reader.GetStringSafe("name");
					account.TeamName = reader.GetStringSafe("teamName");
					account.Authority = reader.GetInt32("authority");
					account.Settings.Parse(reader.GetStringSafe("settings"));
					account.Medals = reader.GetInt32("medals");
					account.GiftMedals = reader.GetInt32("giftMedals");
					account.PremiumMedals = reader.GetInt32("premiumMedals");

				}
			}

			this.LoadVars("account:" + account.Id, account.Variables.Perm);
			this.LoadChatMacros(account);
			this.LoadRevealedMaps(account);
			return account;
		}

		/// <summary>
		/// Returns given character, or null if it doesn't exist.
		/// </summary>
		/// <param name="characterId"></param>
		/// <returns></returns>
		public Character GetCharacter(long accountId, long characterId)
		{
			var character = new Character();

			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `characters` WHERE `accountId` = @accountId AND `characterId` = @characterId", conn))
			{
				mc.Parameters.AddWithValue("@accountId", accountId);
				mc.Parameters.AddWithValue("@characterId", characterId);

				using (var reader = mc.ExecuteReader())
				{
					if (!reader.Read())
						return null;

					character.Id = reader.GetInt64("characterId");
					character.AccountId = accountId;
					character.Name = reader.GetStringSafe("name");
					character.TeamName = reader.GetStringSafe("teamName");
					character.JobId = (JobId)reader.GetInt16("job");
					character.Gender = (Gender)reader.GetByte("gender");
					character.Hair = reader.GetByte("hair");
					character.Level = reader.GetInt32("level");
					character.MapId = reader.GetInt32("zone");
					character.Exp = reader.GetInt32("exp");
					character.MaxExp = reader.GetInt32("maxExp");
					character.TotalExp = reader.GetInt32("totalExp");
					character.StaminaByJob = reader.GetInt32("staminaByJob");
					character.Stamina = reader.GetInt32("stamina");
					character.StrInvested = reader.GetInt32("str");
					character.StrByJob = reader.GetInt32("strByJob");
					character.ConInvested = reader.GetInt32("con");
					character.ConByJob = reader.GetInt32("conByJob");
					character.IntInvested = reader.GetInt32("int");
					character.IntByJob = reader.GetInt32("intByJob");
					character.SprInvested = reader.GetInt32("spr");
					character.SprByJob = reader.GetInt32("sprByJob");
					character.DexInvested = reader.GetInt32("dex");
					character.DexByJob = reader.GetInt32("dexByJob");
					character.HpRateByJob = reader.GetFloat("hpRate");
					character.SpRateByJob = reader.GetFloat("spRate");
					character.Hp = reader.GetInt32("hp");
					character.Sp = reader.GetInt32("sp");
					character.StatByLevel = reader.GetInt32("statByLevel");
					character.StatByBonus = reader.GetInt32("statByBonus");
					character.UsedStat = reader.GetInt32("usedStat");
					character.AbilityPoints = reader.GetInt32("abilityPoints");

					var x = reader.GetFloat("x");
					var y = reader.GetFloat("y");
					var z = reader.GetFloat("z");
					character.Position = new Position(x, y, z);
					character.Direction = new Direction(0);
					
				}
			}

			this.LoadCharacterItems(character);
			this.LoadVars("character:" + character.Id, character.Variables.Perm);
			this.LoadSessionObjects(character);
			this.LoadJobs(character);
			this.LoadSkills(character);
			this.LoadAbilities(character);

			// Update stance, in case no equip was added, which would've
			// triggered this call.
			character.UpdateStance();

			return character;
		}

		/// <summary>
		/// Loads character's skills.
		/// </summary>
		/// <param name="character"></param>
		private void LoadSkills(Character character)
		{
			using (var conn = this.GetConnection())
			{
				using (var mc = new MySqlCommand("SELECT * FROM `skills` WHERE `characterId` = @characterId", conn))
				{
					mc.Parameters.AddWithValue("@characterId", character.Id);

					using (var reader = mc.ExecuteReader())
					{
						while (reader.Read())
						{
							var skillId = reader.GetInt32("id");
							var level = reader.GetInt32("level");

							var skill = new Skill(character, skillId, level);

							character.Skills.AddSilent(skill);
						}
					}
				}

				// Properties?
			}

			// Load default skills of all jobs
			// Interestingly, it seems like officials don't actually save
			// those skills, they add them via ZC_SKILL_ADD on every login,
			// the skill list only containing the skills from the jobs'
			// skill trees. Abilities on the other hand are all sent in
			// their normal list. Should we mimic that? Could there be a
			// reason for it, aside from saving space in the db?
			if (character.Skills.Count == 0)
			{
				foreach (var job in character.Jobs.GetList())
				{
					foreach (var skillName in job.Data.DefaultSkills)
					{
						var skillData = ChannelServer.Instance.Data.SkillDb.Find(skillName);
						if (skillData == null)
						{
							Log.Warning("ChannelDb.LoadSkills: Skill '{0}' not found.", skillName);
							continue;
						}

						if (character.Skills.Has(skillData.Id))
							continue;

						var skill = new Skill(character, skillData.Id, 1);
						character.Skills.AddSilent(skill);
					}
				}
			}
		}

		/// <summary>
		/// Loads character's abilities.
		/// </summary>
		/// <param name="character"></param>
		private void LoadAbilities(Character character)
		{
			using (var conn = this.GetConnection())
			{
				using (var mc = new MySqlCommand("SELECT * FROM `abilities` WHERE `characterId` = @characterId", conn))
				{
					mc.Parameters.AddWithValue("@characterId", character.Id);

					using (var reader = mc.ExecuteReader())
					{
						while (reader.Read())
						{
							var abilityId = reader.GetInt32("id");
							var level = reader.GetInt32("level");

							var ability = new Ability(abilityId, level);

							character.Abilities.AddSilent(ability);
						}
					}
				}

				// Properties?
			}

			// Load default abilities of all jobs
			if (character.Abilities.Count == 0)
			{
				foreach (var job in character.Jobs.GetList())
				{
					foreach (var abilityName in job.Data.DefaultAbilities)
					{
						var data = ChannelServer.Instance.Data.AbilityDb.Find(abilityName);
						if (data == null)
						{
							Log.Warning("ChannelDb.LoadAbilitys: Ability '{0}' not found.", abilityName);
							continue;
						}

						if (character.Abilities.Has(data.Id))
							continue;

						var ability = new Ability(data.Id, 1);
						character.Abilities.AddSilent(ability);
					}
				}
			}
		}

		/// <summary>
		/// Loads character's jobs from the database.
		/// </summary>
		/// <param name="character"></param>
		private void LoadJobs(Character character)
		{
			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `jobs` WHERE `characterId` = @characterId", conn))
			{
				mc.Parameters.AddWithValue("@characterId", character.Id);

				using (var reader = mc.ExecuteReader())
				{
					while (reader.Read())
					{
						var jobId = (JobId)reader.GetInt32("jobId");
						var circle = (Circle)reader.GetInt32("circle");
						var skillPoints = reader.GetInt32("skillPoints");
						var totalExp = reader.GetInt32("totalExp");

						var job = new Job(character, jobId, totalExp, circle, skillPoints);

						character.Jobs.AddSilent(job);
					}
				}
			}

			// Fallback if the character doesn't have a job for some reason
			if (character.Jobs.Count == 0)
				character.Jobs.AddSilent(new Job(character, character.JobId));
		}

		/// <summary>
		/// Loads character's session objects.
		/// </summary>
		/// <param name="character"></param>
		private void LoadSessionObjects(Character character)
		{
			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `session_objects_properties` WHERE `characterId` = @characterId", conn))
			{
				mc.Parameters.AddWithValue("@characterId", character.Id);

				using (var reader = mc.ExecuteReader())
				{
					while (reader.Read())
					{
						var sessionObjectId = reader.GetInt32("sessionObjectId");
						var propertyId = reader.GetInt32("propertyId");
						var value = reader.GetString("value");

						var sessionObject = character.SessionObjects.GetOrCreate(sessionObjectId);
						switch (Properties.GetPropertyType(propertyId))
						{
							case PropertyType.Float:
								var floatValue = Convert.ToSingle(value, CultureInfo.InvariantCulture);
								sessionObject.Properties.Set(propertyId, floatValue);
								break;

							case PropertyType.String:
								sessionObject.Properties.Set(propertyId, value);
								break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Saves character's session objects.
		/// </summary>
		/// <param name="character"></param>
		private void SaveSessionObjects(Character character)
		{
			var sessionObjects = character.SessionObjects.GetList();

			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				foreach (var sessionObject in sessionObjects)
				{
					using (var cmd = new MySqlCommand("DELETE FROM `session_objects_properties` WHERE `characterId` = @characterId AND `sessionObjectId` = @sessionObjectId", conn, trans))
					{
						cmd.Parameters.AddWithValue("@characterId", character.Id);
						cmd.Parameters.AddWithValue("@sessionObjectId", sessionObject.Id);
						cmd.ExecuteNonQuery();
					}

					var properties = sessionObject.Properties.GetAll();
					foreach (var property in properties)
					{
						using (var cmd = new InsertCommand("INSERT INTO `session_objects_properties` {0}", conn, trans))
						{
							cmd.Set("characterId", character.Id);
							cmd.Set("sessionObjectId", sessionObject.Id);
							cmd.Set("propertyId", property.Id);
							switch (property)
							{
								case FloatProperty floatProperty: cmd.Set("value", floatProperty.Value.ToString(CultureInfo.InvariantCulture)); break;
								case StringProperty stringProperty: cmd.Set("value", stringProperty.Value); break;
							}

							cmd.Execute();
						}
					}
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Saves character information.
		/// </summary>
		/// <param name="character"></param>
		/// <returns></returns>
		public bool SaveCharacter(Character character)
		{
			using (var conn = this.GetConnection())
			using (var cmd = new UpdateCommand("UPDATE `characters` SET {0} WHERE `characterId` = @characterId", conn))
			{
				cmd.AddParameter("@characterId", character.Id);
				cmd.Set("name", character.Name);
				cmd.Set("job", (short)character.JobId);
				cmd.Set("gender", (byte)character.Gender);
				cmd.Set("hair", character.Hair);
				cmd.Set("level", character.Level);
				cmd.Set("zone", character.MapId);
				cmd.Set("x", character.Position.X);
				cmd.Set("y", character.Position.Y);
				cmd.Set("z", character.Position.Z);
				cmd.Set("exp", character.Exp);
				cmd.Set("maxExp", character.MaxExp);
				cmd.Set("totalExp", character.TotalExp);
				cmd.Set("hp", character.Hp);
				cmd.Set("hpRate", character.HpRateByJob);
				cmd.Set("sp", character.Sp);
				cmd.Set("spRate", character.SpRateByJob);
				cmd.Set("stamina", character.Stamina);
				cmd.Set("staminaByJob", character.StaminaByJob);
				cmd.Set("str", character.StrInvested);
				cmd.Set("strByJob", character.StrByJob);
				cmd.Set("con", character.ConInvested);
				cmd.Set("conByJob", character.ConByJob);
				cmd.Set("int", character.IntInvested);
				cmd.Set("intByJob", character.IntByJob);
				cmd.Set("spr", character.SprInvested);
				cmd.Set("sprByJob", character.SprByJob);
				cmd.Set("dex", character.DexInvested);
				cmd.Set("dexByJob", character.DexByJob);
				cmd.Set("statByLevel", character.StatByLevel);
				cmd.Set("statByBonus", character.StatByBonus);
				cmd.Set("usedStat", character.UsedStat);
				cmd.Set("abilityPoints", character.AbilityPoints);

				cmd.Execute();
			}

			this.SaveCharacterItems(character);
			this.SaveVariables("character:" + character.Id, character.Variables.Perm);
			this.SaveSessionObjects(character);
			this.SaveJobs(character);
			this.SaveSkills(character);
			this.SaveAbilities(character);

			return false;
		}

		/// <summary>
		/// Saves character's skills.
		/// </summary>
		/// <param name="character"></param>
		private void SaveSkills(Character character)
		{
			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var cmd = new MySqlCommand("DELETE FROM `skills` WHERE `characterId` = @characterId", conn, trans))
				{
					cmd.Parameters.AddWithValue("@characterId", character.Id);
					cmd.ExecuteNonQuery();
				}

				var skills = character.Skills.GetList();
				foreach (var skill in skills)
				{
					using (var cmd = new InsertCommand("INSERT INTO `skills` {0}", conn, trans))
					{
						cmd.Set("characterId", character.Id);
						cmd.Set("id", skill.Id);
						cmd.Set("level", skill.Level);
						cmd.Execute();
					}

					// Properties?
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Saves character's abilities.
		/// </summary>
		/// <param name="character"></param>
		private void SaveAbilities(Character character)
		{
			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var cmd = new MySqlCommand("DELETE FROM `abilities` WHERE `characterId` = @characterId", conn, trans))
				{
					cmd.Parameters.AddWithValue("@characterId", character.Id);
					cmd.ExecuteNonQuery();
				}

				var abilities = character.Abilities.GetList();
				foreach (var ability in abilities)
				{
					using (var cmd = new InsertCommand("INSERT INTO `abilities` {0}", conn, trans))
					{
						cmd.Set("characterId", character.Id);
						cmd.Set("id", ability.Id);
						cmd.Set("level", ability.Level);
						cmd.Execute();
					}

					// Properties?
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Saves character's jobs to the database.
		/// </summary>
		/// <param name="character"></param>
		private void SaveJobs(Character character)
		{
			var jobs = character.Jobs.GetList();

			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var cmd = new MySqlCommand("DELETE FROM `jobs` WHERE `characterId` = @characterId", conn, trans))
				{
					cmd.Parameters.AddWithValue("@characterId", character.Id);
					cmd.ExecuteNonQuery();
				}

				foreach (var job in jobs)
				{
					using (var cmd = new InsertCommand("INSERT INTO `jobs` {0}", conn, trans))
					{
						cmd.Set("characterId", character.Id);
						cmd.Set("jobId", job.Id);
						cmd.Set("circle", job.Circle);
						cmd.Set("skillPoints", job.SkillPoints);
						cmd.Set("totalExp", job.TotalExp);

						cmd.Execute();
					}
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Load character's items.
		/// </summary>
		/// <param name="characterId"></param>
		/// <returns></returns>
		private void LoadCharacterItems(Character character)
		{
			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `items` WHERE `characterId` = @characterId ORDER BY `sort` ASC", conn))
			{
				mc.Parameters.AddWithValue("@characterId", character.Id);

				using (var reader = mc.ExecuteReader())
				{
					while (reader.Read())
					{
						var itemId = reader.GetInt32("itemId");
						var amount = reader.GetInt32("amount");
						var equipSlot = (EquipSlot)reader.GetByte("equipSlot");

						// Check item, in case its data was removed
						if (!ChannelServer.Instance.Data.ItemDb.Exists(itemId))
						{
							Log.Warning("ChannelDb.LoadCharacterItems: Item '{0}' not found, removing it from inventory.", itemId);
							continue;
						}

						var item = new Item(itemId, amount);

						if (!Enum.IsDefined(typeof(EquipSlot), equipSlot))
							character.Inventory.AddSilent(item);
						else
							character.Inventory.SetEquipSilent(equipSlot, item);
					}
				}
			}
		}

		/// <summary>
		/// Returns items for given character.
		/// </summary>
		/// <param name="characterId"></param>
		/// <returns></returns>
		public void SaveCharacterItems(Character character)
		{
			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var mc = new MySqlCommand("DELETE FROM `items` WHERE `characterId` = @characterId", conn, trans))
				{
					mc.Parameters.AddWithValue("@characterId", character.Id);
					mc.ExecuteNonQuery();
				}

				var i = 0;
				foreach (var item in character.Inventory.GetItems().OrderBy(a => a.Key))
				{
					using (var cmd = new InsertCommand("INSERT INTO `items` {0}", conn))
					{
						cmd.Set("characterId", character.Id);
						cmd.Set("itemId", item.Value.Id);
						cmd.Set("amount", item.Value.Amount);
						cmd.Set("sort", i++);
						cmd.Set("equipSlot", 0x7F);

						cmd.Execute();
					}
				}

				// Save only non-dummy equip to the database, and make sure
				// that dummy equip that was loaded into the character as a
				// normal item wrongfully isn't saved again.
				foreach (var item in character.Inventory.GetEquip().Where(a => !(a.Value is DummyEquipItem) && !Items.DefaultItems.Contains(a.Value.Id)))
				{
					using (var cmd = new InsertCommand("INSERT INTO `items` {0}", conn))
					{
						cmd.Set("characterId", character.Id);
						cmd.Set("itemId", item.Value.Id);
						cmd.Set("amount", item.Value.Amount);
						cmd.Set("sort", 0);
						cmd.Set("equipSlot", (byte)item.Key);

						cmd.Execute();
					}
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Saves owner's variables in database.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="vars"></param>
		public void SaveVariables(string owner, VariableManager vars)
		{
			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var mc = new MySqlCommand("DELETE FROM `vars` WHERE `owner` = @owner", conn, trans))
				{
					mc.Parameters.AddWithValue("@owner", owner);
					mc.ExecuteNonQuery();
				}

				foreach (var var in vars.GetList())
				{
					if (var.Value == null)
						continue;

					// Get type
					string type;
					if (var.Value is byte) type = "1u";
					else if (var.Value is ushort) type = "2u";
					else if (var.Value is uint) type = "4u";
					else if (var.Value is ulong) type = "8u";
					else if (var.Value is sbyte) type = "1";
					else if (var.Value is short) type = "2";
					else if (var.Value is int) type = "4";
					else if (var.Value is long) type = "8";
					else if (var.Value is float) type = "f";
					else if (var.Value is double) type = "d";
					else if (var.Value is bool) type = "b";
					else if (var.Value is string) type = "s";
					else
					{
						Log.Warning("SaveVars: Skipping variable '{0}', unsupported type '{1}'.", var.Key, var.Value.GetType().Name);
						continue;
					}

					// Get value
					var val = "";
					switch (type)
					{
						case "f": val = ((float)var.Value).ToString(CultureInfo.InvariantCulture); break;
						case "d": val = ((double)var.Value).ToString(CultureInfo.InvariantCulture); break;
						default: val = var.Value.ToString(); break;
					}

					// Make sure value isn't too big for the mediumtext field
					// (unlikely as it may be). Size: 16,777,215
					if (val.Length > (1 << 24) - 1)
					{
						Log.Warning("SaveVars: Skipping variable '{0}', it's too big.", var.Key);
						continue;
					}

					// Save
					using (var cmd = new InsertCommand("INSERT INTO `vars` {0}", conn, trans))
					{
						cmd.Set("owner", owner);
						cmd.Set("name", var.Key);
						cmd.Set("type", type);
						cmd.Set("value", val);

						cmd.Execute();
					}
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Loads owner's variables into the variable manager.
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public void LoadVars(string owner, VariableManager vars)
		{
			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `vars` WHERE `owner` = @owner", conn))
			{
				mc.Parameters.AddWithValue("@owner", owner);

				using (var reader = mc.ExecuteReader())
				{
					while (reader.Read())
					{
						var name = reader.GetString("name");
						var type = reader.GetString("type");
						var val = reader.GetStringSafe("value");

						if (val == null)
							continue;

						try
						{
							switch (type)
							{
								case "1u": vars[name] = byte.Parse(val); break;
								case "2u": vars[name] = ushort.Parse(val); break;
								case "4u": vars[name] = uint.Parse(val); break;
								case "8u": vars[name] = ulong.Parse(val); break;
								case "1": vars[name] = sbyte.Parse(val); break;
								case "2": vars[name] = short.Parse(val); break;
								case "4": vars[name] = int.Parse(val); break;
								case "8": vars[name] = long.Parse(val); break;
								case "f": vars[name] = float.Parse(val, CultureInfo.InvariantCulture); break;
								case "d": vars[name] = double.Parse(val, CultureInfo.InvariantCulture); break;
								case "b": vars[name] = bool.Parse(val); break;
								case "s": vars[name] = val; break;

								default:
									Log.Warning("LoadVars: Unknown variable type '{0}'.", type);
									continue;
							}
						}
						catch (FormatException)
						{
							Log.Warning("LoadVars: Variable '{0}' could not be parsed as type '{1}'. Value: '{2}', Owner: '{3}'", name, type, val, owner);
							continue;
						}
						catch (OverflowException)
						{
							Log.Warning("LoadVars: Value '{2}' of variable '{0}' doesn't fit into type '{1}'. Owner: '{3}'", name, type, val, owner);
							continue;
						}
					}
				}
			}
		}

		/// <summary>
		/// Loads chat macros from the database.
		/// </summary>
		/// <param name="account"></param>
		private void LoadChatMacros(Account account)
		{
			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `chatMacros` WHERE `accountId` = @accountId ORDER BY `index` DESC", conn))
			{
				mc.Parameters.AddWithValue("@accountId", account.Id);

				using (var reader = mc.ExecuteReader())
				{
					while (reader.Read())
					{
						var index = reader.GetInt32("index");
						var message = reader.GetString("message");
						var pose = reader.GetInt32("pose");

						var macro = new ChatMacro(index, message, pose);
						account.AddChatMacro(macro);
					}
				}
			}
		}

		/// <summary>
		/// Persists the account's chat macros to the database.
		/// </summary>
		/// <param name="account"></param>
		private void SaveChatMacros(Account account)
		{
			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var mc = new MySqlCommand("DELETE FROM `chatMacros` WHERE `accountId` = @accountId", conn, trans))
				{
					mc.Parameters.AddWithValue("@accountId", account.Id);
					mc.ExecuteNonQuery();
				}

				foreach (var macro in account.GetChatMacros().OrderBy(x => x.Index))
				{
					using (var cmd = new InsertCommand("INSERT INTO `chatMacros` {0}", conn))
					{
						cmd.Set("accountId", account.Id);
						cmd.Set("index", macro.Index);
						cmd.Set("message", macro.Message);
						cmd.Set("pose", macro.Pose);

						cmd.Execute();
					}
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Loads revealed maps for the account.
		/// </summary>
		/// <param name="account"></param>
		private void LoadRevealedMaps(Account account)
		{
			using (var conn = this.GetConnection())
			using (var mc = new MySqlCommand("SELECT * FROM `revealedMaps` WHERE `accountId` = @accountId", conn))
			{
				mc.Parameters.AddWithValue("@accountId", account.Id);

				using (var reader = mc.ExecuteReader())
				{
					while (reader.Read())
					{
						var map = reader.GetInt32("map");
						var explored = (byte[])reader["explored"];
						var percentage = reader.GetFloat("percentage");

						var revealedMap = new RevealedMap(map, explored, percentage);
						account.AddRevealedMap(revealedMap);
					}
				}
			}
		}

		/// <summary>
		/// Persists the account's revealed maps to the database.
		/// </summary>
		/// <param name="account"></param>
		private void SaveRevealedMaps(Account account)
		{
			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var mc = new MySqlCommand("DELETE FROM `revealedMaps` WHERE `accountId` = @accountId", conn, trans))
				{
					mc.Parameters.AddWithValue("@accountId", account.Id);
					mc.ExecuteNonQuery();
				}

				foreach (var revealedMap in account.GetRevealedMaps())
				{
					using (var cmd = new InsertCommand("INSERT INTO `revealedMaps` {0}", conn, trans))
					{
						cmd.Set("accountId", account.Id);
						cmd.Set("map", revealedMap.MapId);
						cmd.Set("explored", revealedMap.Explored);
						cmd.Set("percentage", revealedMap.Percentage);

						cmd.Execute();
					}
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Executes query and returns list of returned rows. Also returns
		/// the amount of affected rows and the last insert id via out.
		/// </summary>
		/// <param name="sqlQuery"></param>
		/// <param name="affectedRows"></param>
		/// <param name="lastInsertId"></param>
		/// <returns></returns>
		public List<Dictionary<string, object>> GetQueryResult(string sqlQuery, out int affectedRows, out long lastInsertId)
		{
			var result = new List<Dictionary<string, object>>();
			affectedRows = 0;
			lastInsertId = 0;

			using (var conn = this.GetConnection())
			using (var cmd = new MySqlCommand(sqlQuery, conn))
			{
				using (var reader = cmd.ExecuteReader())
				{
					affectedRows = reader.RecordsAffected;
					lastInsertId = cmd.LastInsertedId;

					while (reader.Read())
					{
						var row = new Dictionary<string, object>();
						for (var i = 0; i < reader.FieldCount; ++i)
						{
							var columnName = reader.GetName(i);
							var value = reader.GetValue(i);

							row[columnName] = value;
						}

						result.Add(row);
					}
				}
			}

			return result;
		}
	}
}
