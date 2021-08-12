using Yuzu;

namespace Match3Template
{
	public class Profile
	{
		public static Profile Instance;

		[YuzuAfterDeserialization]
		public void AfterDeserialization()
		{ }
	}
}