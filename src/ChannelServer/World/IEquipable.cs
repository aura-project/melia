﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melia.Channel.World
{
	public interface IEquipable
	{
		void OnEquip();
		void OnUnequip();
	}
}
