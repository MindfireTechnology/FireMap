using System;
using FireMap;

namespace Tester
{
	class Program
	{
		static void Main(string[] args)
		{
			var user = new UserTester
			{
				Id = 1,
				Address = "123 Fake St",
				City = "Springfield",
				Name = "My User",
				Phone = "555-555-5555",
				ZipCode = "12345"
			};

			IMapper mapper = new Mapper();
			//var entity = mapper.ToUserEntity(user);
			var vm = mapper.ToUser(user);

			Console.WriteLine($@"User Entity:
Id: {entity.Id},
Address: {entity.Address},
City: {entity.City},
Name: {entity.Name},
Phone: {entity.Phone},
ZipCode: {entity.ZipCode}");
		}
	}

	//[MapClass(typeof(UserEntity))]
	[MapClass(typeof(User))]
	public class UserTester
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Address { get; set; }
		public string City { get; set; }
		public string ZipCode { get; set; }
		public string Phone { get; set; }
	}
}