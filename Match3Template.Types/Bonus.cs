using Lime;
using System;

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
		public Bonus(
			Node itemWidget,
			IntVector2 gridPosition,
			Action<ItemComponent, IntVector2> onSetGridPosition,
			Action<ItemComponent> onKill
		) : base(itemWidget, gridPosition, onSetGridPosition, onKill) {
		}

		public BonusKind BonusKind { get; set; } = BonusKind.None;

		public override bool CanMove => true;

		public Animation AnimateAct()
		{
			return Owner.RunAnimation("Start", "Act");
		}
	}
}

