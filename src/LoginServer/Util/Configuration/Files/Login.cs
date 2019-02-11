// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using Melia.Shared.Util;
using Melia.Shared.Util.Configuration;
using Melia.Shared.World;

namespace Melia.Login.Util.Configuration.Files
{
	/// <summary>
	/// Represents login.conf
	/// </summary>
	public class LoginConfFile : ConfFile
	{
		/// <summary>
		/// Location new characters start at.
		/// </summary>
		public Location StartLocation { get; protected set; }

		/// <summary>
		/// When enabled, client integrity checks are applied.
		/// </summary>
		public bool VerifyIpf { get; private set; }

		/// <summary>
		/// Checksum to prevent clients using modified or out-dated IPF files.
		/// </summary>
		public string IpfChecksum { get; private set; }

		public void Load()
		{
			this.Require("system/conf/login.conf");

			this.IpfChecksum = this.GetString("ipf_checksum", String.Empty);
			this.VerifyIpf = this.GetBool("ipf_verifying", false);
			this.IpfChecksum = this.GetString("ipf_checksum");

			this.StartLocation = this.LoadStartLocation();
		}

		/// <summary>
		/// Parses and returns start location from conf option.
		/// </summary>
		/// <returns></returns>
		private Location LoadStartLocation()
		{
			var startLocation = this.GetString("start_location", "f_siauliai_west, -628, 260, -1025");
			var split = startLocation.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (split.Length != 4)
			{
				Log.Warning("login.conf: Malformed start_location, using default location.");
				goto L_Default;
			}

			var mapName = split[0].Trim();
			var map = LoginServer.Instance.Data.MapDb.Find(mapName);
			if (map == null)
			{
				Log.Warning("login.conf: Start map '{0}' not found, using default location.", mapName);
				goto L_Default;
			}

			if (!int.TryParse(split[1], out var x) || !int.TryParse(split[2], out var y) || !int.TryParse(split[3], out var z))
			{
				Log.Warning("login.conf: Invalid coordinates for start_location, using default location.");
				goto L_Default;
			}

			return new Location(map.Id, x, y, z);

		L_Default:
			return new Location(1021, -628, 260, -1025);
		}
	}
}
