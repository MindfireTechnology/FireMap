using FireMap;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using Tester.Models;

namespace Tester
{
	public class CustomMapperTests
	{
		public IMapper Mapper { get; }

		public CustomMapperTests(IMapper mapper)
		{
			Mapper = mapper;
		}

		public void MapNames()
		{
			var dto = new CustomUserDTO
			{
				Id = 1,
				FirstName = "Bob",
				LastName = "Bobbington",
				City = "Springfield",
				Address = "123 Fake St",
				Phone = "555-555-5555",
				ZipCode = "12345"
			};
			
			var entity = Mapper.ToTester_Models_SimpleUserEntity(dto);

			entity.Id.ShouldBe(dto.Id);
			entity.Name.ShouldBe($"{dto.FirstName} {dto.LastName}");
			entity.City.ShouldBe(dto.City);
			entity.Address.ShouldBe(dto.Address);
			entity.Phone.ShouldBe(dto.Phone);
			entity.ZipCode.ShouldBe(dto.ZipCode);
		}
	}

	public class CustomMapper : Mapper
	{
		public override SimpleUserEntity ToTester_Models_SimpleUserEntity(CustomUserDTO source)
		{
			var fromBase = base.ToTester_Models_SimpleUserEntity(source);
			fromBase.Name = $"{source.FirstName} {source.LastName}";
			return fromBase;
		}
	}
}
