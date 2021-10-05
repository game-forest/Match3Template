using Lime;
using System.Collections.Generic;
using Yuzu;

namespace Match3Template.Types
{
	[TangerineRegisterComponent]
	public class CoinBalanceComponent : WidgetBehaviorComponent
	{
		[YuzuMember]
		public string FlyFromWidgetName { get; set; }

		protected override void Update(float dt)
		{
#if !TANGERINE
			base.Update(dt);
			if (!Widget["CoinBar"].Visible) {
				return;
			}
			var coinAmountText = Widget["CoinAmount"] as IText;
			var coinSpendText = Widget["CoinSpend"] as IText;
			if (IProgress.Instance.LastSeenCoins != IProgress.Instance.Coins) {
				var diff = IProgress.Instance.Coins - IProgress.Instance.LastSeenCoins;
				IProgress.Instance.LastSeenCoins = IProgress.Instance.Coins;
				if (diff > 0) {
					Owner.Tasks.Add(FlyCoinsTask(Widget["FlyGroup"].Clone<Widget>(), IProgress.Instance.Coins.ToString()));
				} else {
					coinSpendText.Text = diff.ToString();
					coinAmountText.Text = IProgress.Instance.Coins.ToString();
					Widget.RunAnimation("Start", "Spend");
				}
			} else {
				if (string.IsNullOrEmpty(FlyFromWidgetName)) {
					coinAmountText.Text = IProgress.Instance.Coins.ToString();
				}
			}
#endif // TANGERINE
		}

		private IEnumerator<object> FlyCoinsTask(Widget flyGroup, string targetText)
		{
			Widget.Nodes.Insert(0, flyGroup);
			var coinAmountText = Widget["CoinAmount"] as IText;
			if (!string.IsNullOrEmpty(FlyFromWidgetName)) {
				var target = Owner.GetRoot().Find<Widget>(FlyFromWidgetName);
				var spline = Widget.Find<Spline>("Spline");
				var endPoint = spline.Find<SplinePoint>("Start");
				var t = target.ParentWidget.CalcTransitionToSpaceOf(spline);
				var endPosition = target.Position;
				endPosition = t * endPosition;
				endPoint.Position = endPosition / spline.Size;
			}
			var animation = flyGroup.Animations.Find("Add");
			var addMarker = animation.Markers.Find("AddAmountHere");
			animation.Run("Start");
			yield return addMarker.Time;
			Widget.RunAnimation("AddAmountHere", "Add");
			coinAmountText.Text = targetText;
			yield return animation;
			flyGroup.UnlinkAndDispose();
		}
	}

	[TangerineRegisterComponent]
	public class AddCoinsOnClickComponent : NodeComponent
	{
		[YuzuMember]
		public int Amount { get; set; }
		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			if (Owner != null) {
				var button = Owner as Button;
				button.Clicked += () => {
					IProgress.Instance.Coins += Amount;
				};
			}
		}
	}
}

