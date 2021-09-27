using Lime;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Match3Template.Types
{
	public enum ItemType
	{
		Piece,
		Blocker,
		Drop
	}

	public enum BonusType
	{
		HorizontalLine,
		VerticalLine,
		Bomb,
		Lightning,
		None
	}

	public enum DamageKind
	{
		Match,
		Line,
		Bomb,
		Lightning,
	}

	public class ItemComponent : WidgetBehaviorComponent
	{
		public ItemType Type { get; set; }
		public Task Task { get; private set; }
		public BonusType BonusType { get; set; } = BonusType.None;

		public int BlockerLives { get; set; } = 2;

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
		public IntVector2 GridPosition
		{
			get => gridPosition;
			set
			{
				grid[gridPosition] = null;
				gridPosition = value;
				grid[value] = this;
			}
		}
		private IntVector2 gridPosition = new IntVector2(int.MinValue, int.MinValue);
		private Grid<ItemComponent> grid;
		private IPresenter hasTaskPresenter;

		public void SwapWith(ItemComponent item)
		{
			grid[item.GridPosition] = this;
			grid[GridPosition] = item;
			Lime.Toolbox.Swap(ref item.gridPosition, ref gridPosition);
		}

		private Vector2 cellSize;

		public ItemComponent(Grid<ItemComponent> grid, Vector2 cellSize)
		{
			this.grid = grid;
			this.cellSize = cellSize;
		}

		public void RunTask(IEnumerator<object> task)
		{
			//Debug.Assert(Task == null);
			Task = Owner.Tasks.Add(task);
		}

		public void CancelTask()
		{
			// Debug.Assert(Task != null);
			if (Task != null) {
				Owner.CompoundPostPresenter.Remove(hasTaskPresenter);
				Owner.Tasks.Remove(Task);
				Task = null;
			}
		}

		public Animation AnimateShowBonus()
		{
			return bonusWidget.RunAnimation("Start", "Show");
		}

		public Animation AnimateActBonus()
		{
			return bonusWidget.RunAnimation("Start", "Act");
		}

		public Animation AnimateShow()
		{
			return Owner.RunAnimation("Start", "Show");
		}

		public Animation AnimateShown()
		{
			return Owner.RunAnimation("Shown", "Show");
		}

		public Animation AnimateIdle()
		{
			if (bonusWidget != null) {
				if (bonusWidget.Animations.TryFind("Idle", out var a)) {
					a.Run("Start");
				}
			}
			{
				if (Owner.Animations.TryFind("Idle", out var a)) {
					a.Run("Start");
				}
				return a;
			}
		}

		public Animation AnimateDropDownFall()
		{
			if (bonusWidget != null) {
				bonusWidget.RunAnimation("Fall", "DropDown");
			}
			return Owner.RunAnimation("Fall", "DropDown");
		}

		public Animation AnimateDropDownLand()
		{
			if (bonusWidget != null) {
				bonusWidget.RunAnimation("Land", "DropDown");
			}
			return Owner.RunAnimation("Land", "DropDown");
		}

		public Animation AnimateSelect()
		{
			if (bonusWidget != null) {
				bonusWidget.RunAnimation("Select", "Selection");
			}
			return Owner.RunAnimation("Select", "Selection");
		}

		public Animation AnimateUnselect()
		{
			if (bonusWidget != null) {
				bonusWidget.RunAnimation("Unselect", "Selection");
			}
			return Owner.RunAnimation("Unselect", "Selection");
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

		public IEnumerator<object> MoveTo(IntVector2 position, float time)
		{
			GridPosition = position;
			var p0 = Widget.Position;
			var p1 = GridPositionToWidgetPosition(position);
			var t = time;
			do {
				t -= Task.Current.Delta;
				Widget.Position = t < 0.0f ? p1 : Mathf.Lerp(1.0f - t / time, p0, p1);
				yield return null;
			} while (t > 0.0f);
		}

		public Vector2 GridPositionToWidgetPosition(IntVector2 position)
		{
			return (Vector2)position * cellSize + cellSize * 0.5f;
		}

		internal void SetBonus(Widget widget, BonusType bonus)
		{
			bonusWidget = widget;
			BonusType = bonus;
			Owner.Nodes.Insert(0, bonusWidget);
			bonusWidget.CenterOnParent();
		}

		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			if (Widget != null) {
				Widget.HitTestTarget = true;
			}
		}

		public void Kill()
		{
			Widget.HitTestTarget = false;
			CancelTask();
			grid[GridPosition] = null;
			grid = null;
		}

		protected override void Update(float delta)
		{
			base.Update(delta);
			if (Task != null && Task.Completed) {
				Task = null;
				Owner.CompoundPostPresenter.Remove(hasTaskPresenter);
			}
			if (
				Task != null
				&& !Owner.CompoundPostPresenter.Contains(hasTaskPresenter)
				&& ICheatManager.Instance.DebugMatch3
			) {
				Owner.CompoundPostPresenter.Add(hasTaskPresenter = new WidgetBoundsPresenter(Color4.Green, 2.0f));
			}
		}

		private Widget bonusWidget;
	}
}

