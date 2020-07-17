using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace FireMap
{
	public class MappingRecord
	{
		public INamedTypeSymbol Source { get; set; }
		public INamedTypeSymbol Destination { get; set; }
		public IEnumerable<KeyValuePair<string, string>> Members { get; set; }
	}
}
