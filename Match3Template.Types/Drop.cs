using Lime;

namespace Match3Template.Types
{
	public class Drop : ItemComponent
	{
		public Drop(Grid<ItemComponent> grid) : base(grid)
		{
		}

		public override bool CanMove => true;
	}
}

