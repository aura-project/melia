// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using Melia.Shared.Const;

namespace Melia.Shared.Network.Helpers
{
	public static class AppearancePcHelper
	{
		public static void AddAppearancePc(this Packet packet, IAppearancePc appearancePc)
		{
			packet.PutString(appearancePc.Name, 65);
			packet.PutString(appearancePc.TeamName, 64);
			packet.PutEmptyBin(7);
			packet.PutLong(appearancePc.AccountId);
			packet.PutShort(appearancePc.Stance);
			packet.PutShort(0);
			packet.PutShort((short)appearancePc.JobId);
			packet.PutByte((byte)appearancePc.Gender);
			packet.PutByte(0);
			packet.PutInt(appearancePc.Level);

			packet.PutInt(0); // i8

			// Items
			var equipIds = appearancePc.GetEquipIds();
			if (equipIds.Length != Items.EquipSlotCount)
				throw new InvalidOperationException("Incorrect amount of equipment (" + equipIds.Length + ").");

			for (var i = 0; i < equipIds.Length; ++i)
				packet.PutInt(equipIds[i]);

			// [i10671, 2015-10-26 iCBT2] ?
			{
				packet.PutInt(0);
				packet.PutInt(0);
			}

			packet.PutShort(appearancePc.Hair);

			// Up until i171032 we had these as "pose" and "team id",
			// but they actually contain the visible hats, so we either
			// had them wrong, or they changed. This was first noticed in
			// i170175.
			{
				//packet.PutShort(0); // Pose
				//packet.PutInt(0); // Team ID

				packet.PutByte(0);
				packet.PutByte((appearancePc.VisibleHats & HatVisibleStates.Hat1) != 0);
				packet.PutByte((appearancePc.VisibleHats & HatVisibleStates.Hat2) != 0);
				packet.PutByte((appearancePc.VisibleHats & HatVisibleStates.Hat3) != 0);
				packet.PutShort(0); // s2
			}

			packet.PutInt(0); // i9
		}
	}

	public interface IAppearancePc
	{
		long AccountId { get; }
		string Name { get; }
		string TeamName { get; }
		int Stance { get; }
		JobId JobId { get; }
		Gender Gender { get; }
		int Level { get; }
		byte Hair { get; }
		HatVisibleStates VisibleHats { get; }

		int[] GetEquipIds();
	}
}
