using Lime;
using System;

namespace Match3Template.Types
{

	public class Blocker : ItemComponent
	{
		public Blocker(
			Node itemWidget,
			IntVector2 gridPosition,
			Action<ItemComponent, IntVector2> onSetGridPosition,
			Action<ItemComponent> onKill
		) : base(itemWidget, gridPosition, onSetGridPosition, onKill) {
		}

		public int BlockerLives { get; set; } = 2;

		public Animation AnimateBlockerDamage1()
		{
			var animation = Owner.Animations.Find("Progress");
			Owner.RunAnimation("Match01", "Progress");
			return animation;
		}

		public Animation AnimateBlockerDamage2()
		{
			var animation = Owner.Animations.Find("Progress");
			Owner.RunAnimation("Match02", "Progress");
			return animation;
		}
	}
}

