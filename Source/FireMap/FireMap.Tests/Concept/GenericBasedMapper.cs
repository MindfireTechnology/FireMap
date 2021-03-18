using System;
using System.Collections.Generic;
using System.Text;

namespace FireMap.Tests.Concept
{
	public class GenericBasedMapper
	{
		public TOut MapTo<TOut>(FireMap.Tests.Concept.Entity.Order value) where TOut : class, new()
		{
			// All of the mappings with `FireMap.Tests.Concept.Entity` as input,
			// which output should we choose?

			if (typeof(TOut) == typeof(FireMap.Tests.Concept.ViewModel.Order))
			{
				// Map this sucka!
				return new TOut();
			}

			return default(TOut);
		}
	}
}

namespace FireMap.Tests.Concept.Entity
{
}

namespace FireMap.Tests.Concept.ViewModel
{
}

namespace FireMap.Tests.Concept.Model
{
}
