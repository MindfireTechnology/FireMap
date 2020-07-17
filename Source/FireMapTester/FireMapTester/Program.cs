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
			RunCustomMapperTests();

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

		static void RunCustomMapperTests()
		{
			var customMapper = new CustomMapper();

			var customTests = new CustomMapperTests(customMapper);
			customTests.MapNames();

			Console.WriteLine("Passed all custom mapper tests");
		}
	}
}