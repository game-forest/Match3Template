using Lime;
using System;

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
					var markerName = (value + 1).ToString();
					var marker = kindAnimation.Markers.TryFind(markerName);
					if (marker == null) {
						markerName = (value + 1).ToString("D2");
						marker = kindAnimation.Markers.TryFind(markerName);
					}
					if (marker != null) {
						Owner.RunAnimation(marker.Id, kindAnimation.Id);
					}
				}
				kind = value;
			}
		}
		private int kind;

		public BonusKind SpawnBonus { get; set; } = BonusKind.None;

		public override bool CanMove => true;

		public Piece(
			Node itemWidget,
			IntVector2 gridPosition,
			Action<ItemComponent, IntVector2> onSetGridPosition,
			Action<ItemComponent> onKill
		) : base(itemWidget, gridPosition, onSetGridPosition, onKill) {
			//var kindAnimation = Owner.Animations.Find("Kind");
			//Owner.RunAnimation(marker.Id, kindAnimation.Id);
			//var marker = kindAnimation.Markers[kind];
			//this.kind = kind;
		}

		public bool CanMatch(Piece otherPiece)
		{
			return otherPiece != null
				&& otherPiece.kind == this.kind;
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

