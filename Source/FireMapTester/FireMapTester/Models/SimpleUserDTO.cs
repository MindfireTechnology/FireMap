using System;
using System.Collections.Generic;
using System.Text;
using FireMap;

namespace Tester.Models
{
	[MapClassTo(typeof(SimpleUserViewModel))]
	[MapClassTo(typeof(SimpleUserEntity))]
	public class SimpleUserDTO
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Address { get; set; }
		public string City { get; set; }
		public string ZipCode { get; set; }
		public string Phone { get; set; }
	}
}
