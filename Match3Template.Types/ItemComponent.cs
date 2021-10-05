using Lime;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Match3Template.Types
{
	public class ItemComponent : WidgetBehaviorComponent
	{
		public Task Task { get; private set; }

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

		public virtual bool CanMove => false;

		private IntVector2 gridPosition = new IntVector2(int.MinValue, int.MinValue);
		private Grid<ItemComponent> grid;
		private IPresenter hasTaskPresenter;

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

		public void RunTask(IEnumerator<object> task)
		{
			Debug.Assert(Task == null);
			Task = Owner.Tasks.Add(task);
		}

		public void RunTask(Task task)
		{
			Debug.Assert(Task == null);
			Owner.Tasks.Add(task);
			Task = task;
		}

		public void RunAnimationTask(Animation animation)
		{
			RunTask(WaitForAnimationTask(animation));
		}

		private IEnumerator<object> WaitForAnimationTask(Animation animation)
		{
			yield return animation;
		}

		public void CancelTask()
		{
			//Debug.Assert(Task != null);
			if (Task != null) {
				Owner.CompoundPostPresenter.Remove(hasTaskPresenter);
				Owner.Tasks.Remove(Task);
				Task = null;
			}
		}

		public IEnumerator<object> MoveTo(IntVector2 position, float time)
		{
			GridPosition = position;
			var p0 = Widget.Position;
			var p1 = config.GridPositionToWidgetPosition(position);
			var t = time;
			do {
				t -= Task.Current.Delta;
				Widget.Position = t < 0.0f ? p1 : Mathf.Lerp(1.0f - t / time, p0, p1);
				yield return null;
			} while (t > 0.0f);
		}

		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			if (Widget != null && CanMove) {
				Widget.HitTestTarget = true;
			}
		}

		protected override void Start()
		{
			var n = Owner;
			while (config == null) {
				config = n.Components.Get<Match3ConfigComponent>();
				n = n.Parent;
			}
		}

		private Match3ConfigComponent config = null;

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
	}
}

