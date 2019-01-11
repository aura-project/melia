// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System.Collections.Generic;
using System.Linq;
using Melia.Login.Database;
using Melia.Login.Network.Helpers;
using Melia.Shared.Const;
using Melia.Shared.World;
using Melia.Shared.World.ObjectProperties;

namespace Melia.Login.World
{
	public class Character : IBarrackPc
	{
		/// <summary>
		/// Character's unique id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// Id of the character's account.
		/// </summary>
		public long AccountId { get; set; }

		/// <summary>
		/// Index of character in character list.
		/// </summary>
		public byte Index { get; set; }

		/// <summary>
		/// Character's name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Character's team name.
		/// </summary>
		public string TeamName { get; set; }

		/// <summary>
		/// Character's job.
		/// </summary>
		public JobId JobId { get; set; }

		/// <summary>
		/// Character's gender.
		/// </summary>
		public Gender Gender { get; set; }

		/// <summary>
		/// Character's hair style.
		/// </summary>
		public byte Hair { get; set; }

		/// <summary>
		/// Equipped items.
		/// </summary>
		public EquipItem[] Equipment { get; private set; }

		/// <summary>
		/// List of character's jobs.
		/// </summary>
		public HashSet<JobId> Jobs { get; } = new HashSet<JobId>();

		/// <summary>
		/// Specifies which hats are visible on the character.
		/// </summary>
		public HatVisibleStates VisibleHats { get { return HatVisibleStates.Hat1 | HatVisibleStates.Hat2 | HatVisibleStates.Hat3; } }

		/// <summary>
		/// Character's level.
		/// </summary>
		public int Level { get; set; }

		/// <summary>
		/// Layer in the barrack that the character should appear in.
		/// </summary>
		public int BarrackLayer { get; set; }

		/// <summary>
		/// Character's position in barracks.
		/// </summary>
		public Position BarrackPosition { get; set; }

		/// <summary>
		/// The channel the character is currently on.
		/// </summary>
		public int Channel { get; set; }

		/// <summary>
		/// The map the character is in.
		/// </summary>
		public int MapId { get; set; }

		/// <summary>
		/// Character's current position.
		/// </summary>
		public Position Position { get; set; }

		/// <summary>
		/// Returns stance, based on job and other factors.
		/// </summary>
		public int Stance
		{
			get
			{
				var rightHand = this.Equipment[(int)EquipSlot.RightHand].Type;
				var leftHand = this.Equipment[(int)EquipSlot.LeftHand].Type;

				return LoginServer.Instance.Data.StanceConditionDb.FindStanceId(this.JobId, false, rightHand, leftHand);
			}
		}

		/// <summary>
		/// Hp multiplicator from the basic job.
		/// </summary>
		public float HpRateByJob { get; set; }

		/// <summary>
		/// Current HP.
		/// </summary>
		public int Hp { get; set; }

		/// <summary>
		/// Sp multiplicator from the basic job.
		/// </summary>
		public float SpRateByJob { get; set; }

		/// <summary>
		/// Current SP.
		/// </summary>
		public int Sp { get; set; }

		/// <summary>
		/// Gets or set Job Stamina.
		/// </summary>
		public int StaminaByJob { get; set; }

		/// <summary>
		/// Current stamina.
		/// </summary>
		public int Stamina { get; set; }

		/// <summary>
		/// Gets or sets character's Item Bonus strength (STR).
		/// </summary>
		public int StrByJob { get; set; }

		/// <summary>
		/// Gets or sets character's Job vitality (CON).
		/// </summary>
		public int ConByJob { get; set; }

		/// <summary>
		/// Gets or sets character's Job intelligence (INT).
		/// </summary>
		public int IntByJob { get; set; }

		/// <summary>
		/// Gets or sets character's Job spirit (SPR/MNA).
		/// </summary>
		public int SprByJob { get; set; }

		/// <summary>
		/// Gets or sets character's Job agility (DEX).
		/// </summary>
		public int DexByJob { get; set; }

		/// <summary>
		/// Creates new character.
		/// </summary>
		public Character()
		{
			this.Level = 1;
			this.Channel = 1;
			this.BarrackLayer = 1;

			this.Equipment = new EquipItem[Items.EquipSlotCount];
			for (var i = 0; i < Items.EquipSlotCount; ++i)
			{
				var itemId = Items.DefaultItems[i];
				var slot = (EquipSlot)i;

				this.Equipment[i] = new EquipItem(itemId, slot);
			}
		}

		/// <summary>
		/// Returns ids of equipped items.
		/// </summary>
		/// <returns></returns>
		public int[] GetEquipIds()
		{
			return this.Equipment.Select(a => a.Id).ToArray();
		}

		/// <summary>
		/// Returns the equipment properties as an array.
		/// </summary>
		/// <returns></returns>
		public Properties[] GetEquipmentProperties()
		{
			return this.Equipment.Select(a => a.Properties).ToArray();
		}

		/// <summary>
		/// Sets the equipment for a character.
		/// </summary>
		/// <returns></returns>
		public void SetEquipment(IDictionary<EquipSlot, int> equipment)
		{
			foreach (var item in equipment)
			{
				var slot = item.Key;
				var itemId = item.Value;

				this.Equipment[(int)slot] = new EquipItem(itemId, slot);
			}
		}

		/// <summary>
		/// Returns ids of character's jobs.
		/// </summary>
		/// <returns></returns>
		public JobId[] GetJobIds()
		{
			return this.Jobs.OrderBy(a => a).ToArray();
		}
	}
}
