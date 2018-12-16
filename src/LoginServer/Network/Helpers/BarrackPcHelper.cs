// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see licence.txt in the main folder

using System;
using System.Linq;
using Melia.Shared.Const;
using Melia.Shared.Network;
using Melia.Shared.Network.Helpers;
using Melia.Shared.World.ObjectProperties;

namespace Melia.Login.Network.Helpers
{
	public static class BarrackPcHelper
	{
		public static void AddBarrackPc(this Packet packet, IBarrackPc pc)
		{
			var equipProperties = pc.GetEquipmentProperties();
			if (equipProperties.Length != Items.EquipSlotCount)
				throw new InvalidOperationException("Expected " + Items.EquipSlotCount + " items for property list.");

			var jobIds = pc.GetJobIds();

			packet.AddAppearanceBarrackPc(pc);

			// Equip properties
			foreach (var propertiesObj in equipProperties)
			{
				var properties = propertiesObj.GetAll();
				var propertiesSize = properties.Sum(a => a.Size);

				packet.PutShort(propertiesSize);
				packet.AddProperties(properties);
			}

			// [i170175] maybe hat visibility?
			packet.PutByte(1);
			packet.PutByte(1);
			packet.PutByte(1);

			packet.PutByte(1);

			// Job list?
			// Example: A Mage that switched to Pyromancer has two
			//   elements in this list, 2001 and 2002.
			packet.PutShort(jobIds.Length);
			foreach (var jobId in jobIds)
				packet.PutShort((short)jobId);

			packet.PutInt(0); // i1
			packet.PutInt(0); // i2
		}
	}

	/// <summary>
	/// Represents an entity in the barrack.
	/// </summary>
	public interface IBarrackPc : IAppearanceBarrackPc
	{
		Properties[] GetEquipmentProperties();
		JobId[] GetJobIds();
	}
}
