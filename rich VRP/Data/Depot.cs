using System;
using System.Collections.Generic;

// Analysis disable once CheckNamespace
namespace OP.Data
{

	public class Depot : AbsNode
	{

		public override AbsNode ShallowCopy()
		{
			return new Depot(Info);
		}
	}

}
