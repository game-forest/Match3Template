using Lime;
using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Match3Template.Types
{

	public class ItemComponent : WidgetBehaviorComponent
	{
		public Task Task { get; private set; }

		public void RunTask(IEnumerator<object> task)
		{
			System.Diagnostics.Debug.Assert(Task == null);
			if (ICheatManager.Instance.DebugMatch3) {
				Owner.CompoundPostPresenter.Add(TaskPresenter);
			}
			Task = Task.Sequence(task, ClearTaskTask());
			Owner.Tasks.Add(Task);
		}

		private IEnumerator<object> ClearTaskTask()
		{
			Task = null;
			if (ICheatManager.Instance.DebugMatch3) {
				Owner.CompoundPostPresenter.Remove(TaskPresenter);
			}
			yield break;
		}

		protected override void Update(float dt)
		{
			if (debugEnabled != ICheatManager.Instance.DebugMatch3) {
				debugEnabled = ICheatManager.Instance.DebugMatch3;
				if (debugEnabled && Task != null) {
					Owner.CompoundPostPresenter.Add(TaskPresenter);
				}
				if (!debugEnabled) {
					Owner.CompoundPostPresenter.Remove(TaskPresenter);
				}
			}
		}

		private bool debugEnabled;
		private static IPresenter TaskPresenter => taskPresenter ??= new WidgetBoundsPresenter(Color4.Green, 2.0f);
		private static IPresenter taskPresenter;

		public IntVector2 GridPosition
		{
			get => gridPosition;
			set
			{
				onSetGridPosition.Invoke(this, value);
				this.gridPosition = value;
			}
		}

		private Action<ItemComponent, IntVector2> onSetGridPosition;
		private Action<ItemComponent> onKill;

		private IntVector2 gridPosition = new IntVector2(int.MinValue, int.MinValue);

		public virtual bool CanMove => false;

		public int SwapIndex { get; internal set; }

		public ItemComponent(
			Node itemWidget,
			IntVector2 gridPosition,
			Action<ItemComponent, IntVector2> onSetGridPosition,
			Action<ItemComponent> onKill
		) {
			itemWidget.Components.Add(this);
			this.onSetGridPosition = onSetGridPosition;
			this.onKill = onKill;
			GridPosition = gridPosition;
			debugEnabled = ICheatManager.Instance.DebugMatch3;
		}

		public void RunAnimationTask(Animation animation) => RunTask(WaitForAnimationTask(animation));

		private static IEnumerator<object> WaitForAnimationTask(Animation animation)
		{
			yield return animation;
		}

		public void CancelTask()
		{
			Debug.Assert(Task != null);
			if (Task != null) {
				Owner.CompoundPostPresenter.Remove(TaskPresenter);
				Owner.Tasks.Remove(Task);
				Task = null;
			}
		}

		public IEnumerator<object> MoveTo(IntVector2 position, float time, Action<float> onStep = null)
		{
			GridPosition = position;
			var p0 = Widget.Position;
			var p1 = ((Vector2)position + Vector2.Half) * config.CellSize;
			if (time == 0.0f) {
				Widget.Position = p1;
				yield break;
			}
			var t = time;
			do {
				t -= Task.Current.Delta;
				Widget.Position = t < 0.0f ? p1 : Mathf.Lerp(1.0f - t / time, p0, p1);
				onStep?.Invoke(1.0f - (t < 0.0f ? 0.0f : t) / time);
				yield return null;
			} while (t > 0.0f);
		}

		public void ApplyAnimationPercent(float t, string animationName, string markerName)
		{
			var animation = Owner.Animations.Find(animationName);
			var markers = animation.Markers;
			var m0 = markers.Find(markerName);
			var m1 = markers[markers.IndexOf(m0) + 1];
			animation.Time = (m1.Time - m0.Time) * t + m0.Time;
			animation.ApplyAnimators();
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
			Widget.Position = ((Vector2)gridPosition + Vector2.Half) * config.CellSize;
		}

		private Match3ConfigComponent config = null;

		public void Kill()
		{
			Widget.HitTestTarget = false;
			// CancelTask();
			onKill?.Invoke(this);
		}

		public Animation AnimateShow() => Owner.RunAnimation("Start", "Show");
		public Animation AnimateShown() => Owner.RunAnimation("Shown", "Show");

		public Animation AnimateIdle()
		{
			if (Owner.Animations.TryFind("Idle", out var a)) {
				a.Run("Start");
			}
			return a;
		}

		public Animation AnimateDropDownFall() => Owner.RunAnimation("Fall", "DropDown");
		public Animation AnimateDropDownLand() => Owner.RunAnimation("Land", "DropDown");
		public Animation AnimateSelect() => Owner.RunAnimation("Select", "Selection");
		public Animation AnimateUnselect() => Owner.RunAnimation("Unselect", "Selection");

		internal object MoveTo(IntVector2 gridPosition, object p1, Action<float> p2)
		{
			throw new NotImplementedException();
		}
	}
}

