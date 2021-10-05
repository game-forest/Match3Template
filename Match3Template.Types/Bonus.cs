using Lime;

namespace Match3Template.Types
{
	public enum BonusKind
	{
		HorizontalLine,
		VerticalLine,
		Bomb,
		Lightning,
		None
	}

	public class Bonus : ItemComponent
	{
		public Bonus(Grid<ItemComponent> grid) : base(grid)
		{
		}

		public BonusKind BonusKind { get; set; } = BonusKind.None;

		public override bool CanMove => true;

		public Animation AnimateAct()
		{
			return Owner.RunAnimation("Start", "Act");
		}
	}
}

