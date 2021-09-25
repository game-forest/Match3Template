using Lime;

namespace Match3Template.Types
{
	[UpdateStage(typeof(EarlyUpdateStage))]
	public class WidgetBehaviorComponent : BehaviorComponent
	{
		public Widget Widget { get; private set; }

		protected override void Start()
		{
			base.Start();
			Widget = Owner as Widget;
		}

		protected override void Stop(Node owner)
		{
			base.Stop(owner);
			Widget = null;
		}
	}
}

