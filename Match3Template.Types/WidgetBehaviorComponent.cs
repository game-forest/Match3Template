using Lime;

namespace Match3Template.Types
{
	[UpdateStage(typeof(EarlyUpdateStage))]
	public class WidgetBehaviorComponent : BehaviorComponent
	{
		public Widget Widget { get; private set; }

		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			Widget = Owner as Widget;
		}
	}
}

