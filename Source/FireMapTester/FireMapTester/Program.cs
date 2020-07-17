using System;
using FireMap;

namespace Tester
{
	class Program
	{
		static void Main(string[] args)
		{
			var mapper = new Mapper();

			RunSimpleTests(mapper);

			Console.Write("Press enter to continue...");
			Console.ReadLine();
		}

		static void RunSimpleTests(IMapper mapper)
		{
			var simpleTests = new SimpleTests(mapper);
			simpleTests.Simple1to1Mapping();
			simpleTests.SimpleRenameMapping();

			Console.WriteLine("Passed all simple tests");
		}
	}
}