using System;
using System.Collections.Generic;

namespace Mustache
{
	internal sealed class UnlessTagDefinition : ConditionTagDefinition
	{
		public UnlessTagDefinition()
			: base("unless")
		{
		}

		protected override bool GetIsContextSensitive()
		{
			return false;
		}

		public override bool ShouldGeneratePrimaryGroup(Dictionary<string, object> arguments)
		{
			object condition = arguments["condition"];
			return !isConditionSatisfied(condition);
		}
	}
}
