using FireMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tester.Models
{
	[MapClassTo(typeof(SimpleUserEntity))]
	public class CustomUserDTO
	{
		public int Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Address { get; set; }
		public string City { get; set; }
		public string ZipCode { get; set; }
		public string Phone { get; set; }
	}
}
