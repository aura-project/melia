// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.IO;
using Melia.Shared.Data;
using Melia.Shared.Database;
using Melia.Shared.Util;
using Melia.Shared.Util.Configuration;

namespace Melia.Shared
{
	/// <summary>
	/// Base class for server applications.
	/// </summary>
	public abstract class Server
	{
		private bool _running;

		/// <summary>
		/// File databases.
		/// </summary>
		public MeliaData Data { get; private set; }

		/// <summary>
		/// Initializes class.
		/// </summary>
		public Server()
		{
			this.Data = new MeliaData();
		}

		/// <summary>
		/// Starts the server.
		/// </summary>
		public virtual void Run()
		{
			if (_running)
				throw new Exception("Server is already running.");
			_running = true;

			this.NavigateToRoot();
		}

		/// <summary>
		/// Initializes database connection with data from Conf.
		/// </summary>
		protected void InitDatabase(MeliaDb db, Conf conf)
		{
			try
			{
				Log.Info("Initializing database...");
				db.Init(conf.Database.Host, conf.Database.User, conf.Database.Pass, conf.Database.Db);
			}
			catch (Exception ex)
			{
				Log.Error("Failed to initialize database: {0}", ex.Message);
				CliUtil.Exit(1, true);
			}
		}

		/// <summary>
		/// Loads data from files.
		/// </summary>
		protected void LoadData(DataToLoad toLoad, bool reload)
		{
			Log.Info("Loading data...");

			try
			{
				if ((toLoad & DataToLoad.Items) != 0)
				{
					this.LoadDb(this.Data.ItemDb, "db/items.txt", reload);
				}

				if ((toLoad & DataToLoad.Jobs) != 0)
				{
					this.LoadDb(this.Data.JobDb, "db/jobs.txt", reload);
					this.LoadDb(this.Data.StanceConditionDb, "db/stanceconditions.txt", reload);
				}

				if ((toLoad & DataToLoad.Maps) != 0)
				{
					this.LoadDb(this.Data.MapDb, "db/maps.txt", reload);
				}

				if ((toLoad & DataToLoad.Monsters) != 0)
				{
					this.LoadDb(this.Data.MonsterDb, "db/monsters.txt", reload);
				}

				if ((toLoad & DataToLoad.Servers) != 0)
				{
					this.LoadDb(this.Data.ServerDb, "db/servers.txt", reload);
				}

				if ((toLoad & DataToLoad.Barracks) != 0)
				{
					this.LoadDb(this.Data.BarrackDb, "db/barracks.txt", reload);
				}

				if ((toLoad & DataToLoad.Shops) != 0)
				{
					this.LoadDb(this.Data.ShopDb, "db/shops.txt", reload);
				}

				if ((toLoad & DataToLoad.Skills) != 0)
				{
					this.LoadDb(this.Data.SkillDb, "db/skills.txt", reload);
					this.LoadDb(this.Data.SkillTreeDb, "db/skilltree.txt", reload);
					this.LoadDb(this.Data.AbilityDb, "db/abilities.txt", reload);
					this.LoadDb(this.Data.AbilityTreeDb, "db/abilitytree.txt", reload);
				}

				if ((toLoad & DataToLoad.Exp) != 0)
				{
					this.LoadDb(this.Data.ExpDb, "db/exp.txt", reload);
				}

				if ((toLoad & DataToLoad.Dialogues) != 0)
				{
					this.LoadDb(this.Data.DialogDb, "db/dialogues.txt", reload);
				}

				if ((toLoad & DataToLoad.Help) != 0)
				{
					this.LoadDb(this.Data.HelpDb, "db/help.txt", reload);
				}

				if ((toLoad & DataToLoad.CustomCommands) != 0)
				{
					this.LoadDb(this.Data.CustomCommandDb, "db/customcommands.txt", reload);
				}

				if ((toLoad & DataToLoad.ChatMacros) != 0)
				{
					this.LoadDb(this.Data.ChatMacroDb, "db/chatmacros.txt", reload);
				}
			}
			catch (DatabaseErrorException ex)
			{
				Log.Error(ex.ToString());
				CliUtil.Exit(1);
			}
			catch (FileNotFoundException ex)
			{
				Log.Error(ex.Message);
				CliUtil.Exit(1);
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Error while loading data.");
				CliUtil.Exit(1);
			}
		}

		/// <summary>
		/// Loads db, first from system, then from user.
		/// Logs problems as warnings.
		/// </summary>
		private void LoadDb(IDatabase db, string path, bool reload, bool log = true)
		{
			var systemPath = Path.Combine("system", path).Replace('\\', '/');
			var userPath = Path.Combine("user", path).Replace('\\', '/');

			string cachePath = null;
			//var cachePath = Path.Combine("cache", path).Replace('\\', '/');
			//cachePath = Path.ChangeExtension(cachePath, "mpk");
			//var cacheDir = Path.GetDirectoryName(cachePath);
			//if (!Directory.Exists(cacheDir))
			//	Directory.CreateDirectory(cacheDir);

			if (!File.Exists(systemPath))
				throw new FileNotFoundException("Data file '" + systemPath + "' couldn't be found.", systemPath);

			db.Load(new string[] { systemPath, userPath }, cachePath, reload);

			foreach (var ex in db.Warnings)
				Log.Warning("{0}", ex.ToString());

			if (log)
				Log.Info("  done loading {0} entries from {1}", db.Count, Path.GetFileName(path));
		}

		/// <summary>
		/// Loads given conf class and stops start up when an error
		/// occurs.
		/// </summary>
		/// <param name="conf"></param>
		protected void LoadConf(Conf conf)
		{
			try
			{
				Log.Info("Loading configuration...");
				conf.LoadAll();
			}
			catch (Exception ex)
			{
				Log.Error("Failed to load configuration: {0}", ex.Message);
				CliUtil.Exit(1, true);
			}
		}

		/// <summary>
		/// Tries to find root folder and changes the working directory to it.
		/// Exits if not successful.
		/// </summary>
		public void NavigateToRoot()
		{
			// Go back max 2 folders, the bins should be in [root]/bin/(Debug|Release)
			for (var i = 0; i < 6; ++i)
			{
				if (Directory.Exists("system") && Directory.Exists("user"))
				{
					//if (i != 0)
					//	Log.Info("Switched workign directory to '{0}'.", Directory.GetCurrentDirectory());
					return;
				}

				Directory.SetCurrentDirectory("..");
			}

			Log.Error("Unable to find root directory, that contains system and user folders.");
			CliUtil.Exit(1);
		}
	}

	[Flags]
	public enum DataToLoad
	{
		Items = 0x01,
		Maps = 0x02,
		Jobs = 0x04,
		Servers = 0x08,
		Barracks = 0x10,
		Monsters = 0x20,
		Skills = 0x40,
		Exp = 0x80,
		Dialogues = 0x100,
		Shops = 0x200,
		Help = 0x400,
		CustomCommands = 0x800,
		ChatMacros = 0x1000,

		All = 0x7FFFFFFF,
	}
}
