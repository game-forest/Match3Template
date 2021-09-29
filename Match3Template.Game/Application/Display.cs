using System;
using Lime;
using Yuzu;

namespace Match3Template.Application
{
	public struct Display : IEquatable<Display>
	{
		[YuzuMember]
		public string Name;

		[YuzuMember]
		public IntVector2 Resolution;

		[YuzuMember]
		public int Dpi;

		public Display(string name, int width, int height, int dpi, bool isTablet)
			: this()
		{
			Name = name;
			Resolution = new IntVector2(width, height);
			Dpi = dpi;
			IsTablet = isTablet;
		}

		public bool Equals(Display other)
		{
			return other.Name == Name;
		}

		private Vector2 PhysicalSize => (Vector2)Resolution / Dpi;

		[YuzuMember]
		public bool IsTablet { get; set; }
	}
}
