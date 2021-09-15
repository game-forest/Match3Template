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

	public class ItemComponent : NodeComponent
	{
		private readonly Grid<ItemComponent> grid;
		private Widget widget;
		private Task monitorTask;
		IPresenter hasTaskPresenter = null;
		public ItemType Type { get; set; }
		public Task Task { get; private set; }
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

		public void SwapWith(ItemComponent item)
		{
			grid[item.GridPosition] = this;
			grid[GridPosition] = item;
			Lime.Toolbox.Swap(ref item.gridPosition, ref gridPosition);
		}

		public ItemComponent(Grid<ItemComponent> grid)
		{
			this.grid = grid;
		}

		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			widget = Owner as Widget;
			widget.HitTestTarget = true;
			if (monitorTask != null && oldOwner != null) {
				oldOwner.Tasks.Remove(monitorTask);
				Owner.Tasks.Add(monitorTask);
			} else {
				monitorTask = Owner.Tasks.Add(MonitorTask());
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			grid[gridPosition] = null;
		}

		private IEnumerator<object> MonitorTask()
		{
			while (true) {
				if (Task != null && Task.Completed) {
					Task = null;
					Owner.CompoundPostPresenter.Remove(hasTaskPresenter);
				}
				if (Task != null && !Owner.CompoundPostPresenter.Contains(hasTaskPresenter)) {
					Owner.CompoundPostPresenter.Add(hasTaskPresenter = new WidgetBoundsPresenter(Color4.Green, 2.0f));
				}
				yield return null;
			}
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

		public Animation AnimateShow()
		{
			return Owner.RunAnimation("Start", "Show");
		}

		public Animation AnimateIdle()
		{
			if (Owner.Animations.TryFind("Idle", out var a)) {
				a.Run("Start");
			}
			return a;
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

		public IEnumerator<object> MoveTo(IntVector2 position, float time)
		{
			GridPosition = position;
			var p0 = widget.Position;
			var p1 = WidgetPosition(position);
			var t = time;
			do {
				t -= Task.Current.Delta;
				widget.Position = t < 0.0f ? p1 : Mathf.Lerp(1.0f - t / time, p0, p1);
				yield return null;
			} while (t > 0.0f);
		}

		public Vector2 WidgetPosition(IntVector2 position)
		{
			return (Vector2)position * widget.Size + widget.Size * 0.5f;
		}
	}
}

