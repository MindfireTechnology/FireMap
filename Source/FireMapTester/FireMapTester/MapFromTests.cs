using FireMap;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using Tester.Models;

namespace Tester
{
	public class MapFromTests
	{
		public IMapper Mapper { get; }

		public MapFromTests(IMapper mapper)
		{
			Mapper = mapper;
		}

		public void TestFromMapping()
		{
			var dto = new MapFromDTO
			{
				Id = 1,
				Name = "Bob",
				City = "Springfield",
				Address = "123 Fake St",
				Phone = "555-555-5555",
				ZipCode = "12345"
			};

			var entity = Mapper.ToTester_Models_SimpleUserEntity(dto);

			entity.Id.ShouldBe(dto.Id);
			entity.Name.ShouldBe(dto.Name);
			entity.City.ShouldBe(dto.City);
			entity.Address.ShouldBe(dto.Address);
			entity.Phone.ShouldBe(dto.Phone);
			entity.ZipCode.ShouldBe(dto.ZipCode);

			var simpleDto = new SimpleUserDTO
			{
				Id = 1,
				Name = "Bob",
				City = "Springfield",
				Address = "123 Fake St",
				Phone = "555-555-5555",
				ZipCode = "12345"
			};

			var back = Mapper.ToTester_Models_MapFromDTO(simpleDto);
			back.Id.ShouldBe(dto.Id);
			back.Name.ShouldBe(dto.Name);
			back.City.ShouldBe(dto.City);
			back.Address.ShouldBe(dto.Address);
			back.Phone.ShouldBe(dto.Phone);
			back.ZipCode.ShouldBe(dto.ZipCode);
		}
	}
}
