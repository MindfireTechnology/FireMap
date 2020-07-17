using FireMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tester.Models
{
	[MapClassTo(typeof(SimpleUserEntity))]
	public class DifferingNamesDTO
	{
		[MapMemberTo(typeof(SimpleUserEntity), Name = "Id")]
		public int DifferingNamesId { get; set; }

		[MapMemberTo(typeof(SimpleUserEntity), Name = "Name")]
		public string FullName { get; set; }
		public string Address { get; set; }
		public string City { get; set; }
		public string ZipCode { get; set; }
		public string Phone { get; set; }
	}
}
