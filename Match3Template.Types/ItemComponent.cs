using Lime;
using System.Collections.Generic;

namespace Match3Template.Types
{
	public class ItemComponent : NodeComponent
	{
		private BoardConfigComponent boardConfig;
		private Match3ConfigComponent match3Config;
		private Widget widget;

		public int Kind { get; set; }

		// TODO: make private again
		public IntVector2 gridPosition;

		public IntVector2 GridPosition
		{
			get => gridPosition;
			set
			{
				gridPosition = value;
				widget.Position = WidgetPosition(value);
			}
		}

		public ItemComponent(BoardConfigComponent boardConfig, Match3ConfigComponent match3Config)
		{
			this.boardConfig = boardConfig;
			this.match3Config = match3Config;
		}

		public void SetPieceKind(int kind)
		{
			var kindAnimation = Owner.Animations.Find("Color");
			var marker = kindAnimation.Markers[kind];
			Owner.RunAnimation(marker.Id, kindAnimation.Id);
			Kind = kind;
		}

		public Animation AnimateShow()
		{
			return Owner.RunAnimation("Start", "Show");
		}

		public Animation AnimateDropDownFall()
		{
			return Owner.RunAnimation("Fall", "DropDown");
		}

		public Animation AnimateDropDownLand()
		{
			return Owner.RunAnimation("Land", "DropDown");
		}

		public Animation AnimateSelect()
		{
			return Owner.RunAnimation("Select", "Selection");
		}

		public Animation AnimateUnselect()
		{
			return Owner.RunAnimation("Unselect", "Selection");
		}

		public Animation AnimateMatch()
		{
			var animation = Owner.Animations.Find("Match");
			Owner.RunAnimation("Start", "Match");
			return animation;
		}

		public IEnumerator<object> MoveTo(IntVector2 position, Grid grid)
		{
			grid[GridPosition] = null;
			grid[position] = this;
			gridPosition = position;
			var p0 = widget.Position;
			var p1 = WidgetPosition(position);
			var t = match3Config.OneCellFallTime;
			bool finished = false;
			while (true) {
				t -= Task.Current.Delta;
				if (t < 0.0f) {
					t = 0.0f;
					finished = true;
					GridPosition = position;
				}
				widget.Position = Mathf.Lerp(1.0f - t / match3Config.OneCellFallTime, p0, p1);
				if (finished) {
					yield break;
				}
				yield return null;
			}
		}

		public Vector2 WidgetPosition(IntVector2 position)
		{
			return (Vector2)position * widget.Size + widget.Size * 0.5f;
		}

		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			widget = Owner as Widget;
			widget.HitTestTarget = true;
		}
	}
}

