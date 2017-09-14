﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using Melia.Shared.Const;
using Newtonsoft.Json.Linq;

namespace Melia.Shared.Data.Database
{
	[Serializable]
	public class AbilityTreeData
	{
		public JobId JobId { get; set; }
		public int AbilityId { get; set; }
		public int MaxLevel { get; set; }
		public int Price { get; set; }
		public string PriceCalc { get; set; }
	}

	/// <summary>
	/// Ability tree database.
	/// </summary>
	public class AbilityTreeDb : DatabaseJson<AbilityTreeData>
	{
		/// <summary>
		/// Returns data for the given job and ability, or null if no
		/// data exists.
		/// </summary>
		/// <param name="jobId"></param>
		/// <param name="abilityId"></param>
		/// <returns></returns>
		public AbilityTreeData Find(JobId jobId, int abilityId)
		{
			return this.Entries.FirstOrDefault(a => a.JobId == jobId && a.AbilityId == abilityId);
		}

		/// <summary>
		/// Returns abilities the given job can learn.
		/// </summary>
		/// <param name="jobId"></param>
		/// <returns></returns>
		public IEnumerable<AbilityTreeData> Find(JobId jobId)
		{
			return this.Entries.Where(a => a.JobId == jobId);
		}

		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("jobId", "abilityId", "maxLevel");

			var data = new AbilityTreeData();

			data.JobId = (JobId)entry.ReadInt("jobId");
			data.AbilityId = entry.ReadInt("abilityId");
			data.MaxLevel = entry.ReadInt("maxLevel");
			data.Price = entry.ReadInt("price", 0);
			data.PriceCalc = entry.ReadString("priceCalc", "");

			this.Entries.RemoveAll(a => a.JobId == data.JobId && a.AbilityId == data.AbilityId);
			this.Entries.Add(data);
		}
	}
}
