using Lime;

namespace Match3Template.Types
{
	public class Drop : ItemComponent
	{
		public Drop(Grid<ItemComponent> grid, Vector2 cellSize) : base(grid, cellSize)
		{
		}

		public override bool CanMove => true;
	}
}

