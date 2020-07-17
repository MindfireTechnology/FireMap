using FireMap;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using Tester.Models;

namespace Tester
{
	public class SimpleTests
	{
		public IMapper Mapper { get; }

		public SimpleTests(IMapper mapper)
		{
			Mapper = mapper;
		}

		public void Simple1to1Mapping()
		{
			var dto = new SimpleUserDTO
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

			var viewModel = Mapper.ToTester_Models_SimpleUserViewModel(dto);

			viewModel.Id.ShouldBe(dto.Id);
			viewModel.Name.ShouldBe(dto.Name);
			viewModel.City.ShouldBe(dto.City);
			viewModel.Address.ShouldBe(dto.Address);
			viewModel.Phone.ShouldBe(dto.Phone);
			viewModel.ZipCode.ShouldBe(dto.ZipCode);

		}

		public void SimpleRenameMapping()
		{
			var dto = new DifferingNamesDTO
			{
				DifferingNamesId = 42,
				FullName = "Bob Bobbington",
				City = "Springfield",
				Address = "123 Fake St",
				Phone = "555-555-5555",
				ZipCode = "12345"
			};

			var entity = Mapper.ToTester_Models_SimpleUserEntity(dto);

			entity.Id.ShouldBe(dto.DifferingNamesId);
			entity.Name.ShouldBe(dto.FullName);
			entity.City.ShouldBe(dto.City);
			entity.Address.ShouldBe(dto.Address);
			entity.Phone.ShouldBe(dto.Phone);
			entity.ZipCode.ShouldBe(dto.ZipCode);
		}
	}
}
