using Lime;

namespace Match3Template.Types
{
	public enum DamageKind
	{
		Match,
		Line,
		Bomb,
		Lightning,
	}

	public class Piece : ItemComponent
	{
		public int Kind
		{
			get => kind;
			set
			{
				if (Owner.Animations.TryFind("Kind", out var kindAnimation)) {
					var marker = kindAnimation.Markers[value];
					Owner.RunAnimation(marker.Id, kindAnimation.Id);
				}
				kind = value;
			}
		}
		private int kind;

		public override bool CanMove => true;

		public Piece(Grid<ItemComponent> grid, Vector2 cellSize) : base(grid, cellSize)
		{
		}

		public Animation AnimateMatch()
		{
			var animation = Owner.Animations.Find("Match");
			Owner.RunAnimation("Start", "Match");
			return animation;
		}

		public Animation AnimateBlowByLine()
		{
			var animation = Owner.Animations.Find("DestroyByLine");
			Owner.RunAnimation("Start", "DestroyByLine");
			return animation;
		}

		public Animation AnimateBlowByBomb()
		{
			var animation = Owner.Animations.Find("DestroyByBomb");
			Owner.RunAnimation("Start", "DestroyByBomb");
			return animation;
		}

		public Animation AnimateBlowByLightning()
		{
			var animation = Owner.Animations.Find("DestroyByLightning");
			Owner.RunAnimation("Start", "DestroyByLightning");
			return animation;
		}
	}
}
