using Lime;
using System;
using System.Collections.Generic;
using System.Linq;
using Yuzu;

namespace Match3Template.Types
{
	[TangerineRegisterComponent]
	public class CreateGameScreenComponent : WidgetBehaviorComponent
	{
		protected override void Start()
		{
			base.Start();
#if !TANGERINE
			Owner.Components.Add(new GameScreenComponent());
			Owner.Components.Remove(this);
#endif // !TANGERINE
		}
	}

	public class GameScreenComponent : WidgetBehaviorComponent
	{
		private Board board;
		private Widget goalIcon;
		private IText goalValueText;
		private IText turnsLeftValueText;
		private int dropLeftCount;
		private int dropMadeCount;
		private int turnsLeft;
		private int dropCountGoal;

		public GameScreenComponent()
		{

		}

		private void OnDropCompleted(object sender, DropCompletedEventArgs e)
		{
			Owner.Tasks.Add(FlyDropTask(e.ItemWidget));
		}

		private IEnumerator<object> FlyDropTask(Widget drop)
		{
			var position = drop.CalcPositionInSpaceOf(Widget);
			drop.Unlink();
			Widget.Nodes.Insert(0, drop);
			drop.Position = position;
			position = goalIcon.CalcPositionInSpaceOf(Widget);
			var point = drop.Find<SplinePoint>("Spline/End");
			var spline = point.Parent as Spline;
			var t = Widget.CalcTransitionToSpaceOf(spline);
			position = t * position;
			point.Position = position / spline.Size;
			var animation = drop.Animations.Find("Collect");
			animation.Run("Start");
			yield return animation;
			dropLeftCount--;
			dropMadeCount++;
			drop.UnlinkAndDispose();
			goalValueText.Text = dropLeftCount.ToString();

			animation = Widget.Parent.Animations.Find("Progress");
			var markerId = $"Act0{dropMadeCount}";
			if (animation.Markers.TryFind(markerId, out var marker)) {
				animation.Run(markerId);
				yield return animation;
			}

			var count = animation.Markers.Count(m => m.Id.StartsWith("Act"));

			if (dropMadeCount == count) {
				yield return Widget.RunAnimation("Start", "CongratulationsMessage");
			}

			if (dropLeftCount == 0) {
				Win();
			}
		}

		private void Lose()
		{
			if (IProgress.Instance.Coins >= 10) {
				Owner.RunAnimation("Show", "LevelBuyMoves");
			} else {
				Owner.RunAnimation("Show", "LevelLose");
			}
		}

		protected override void Update(float delta)
		{
			base.Update(delta);
			if (Window.Current.Input.WasKeyPressed(Key.W)) {
				Win();
			}
			if (Window.Current.Input.WasKeyPressed(Key.L)) {
				Lose();
			}
		}

		protected override void Start()
		{
			base.Start();
			goalIcon = Widget["GoalsIcon"];
			goalValueText = Widget["GoalsValue"] as IText;
			turnsLeftValueText = Widget["LimitsValue"] as IText;
			board = Board.CreateBoard(Owner.GetRoot().AsWidget);
			dropLeftCount = dropCountGoal = board.GetDropCount();
			turnsLeft = board.GetTurnCount();
			turnsLeftValueText.Text = turnsLeft.ToString();
			goalValueText.Text = dropCountGoal.ToString();
			board.DropCompleted += OnDropCompleted;
			board.TurnMade += OnTurnMade;
			Widget["LevelBuyMoves/BtnOk"].Clicked += () => {
				IProgress.Instance.Coins -= 10;
				board.SetTurnCount(5);
				turnsLeft = 5;
				turnsLeftValueText.Text = turnsLeft.ToString();
			};
		}

		private void OnTurnMade(object sender, TurnMadeEventArgs e)
		{
			turnsLeft--;
			turnsLeftValueText.Text = turnsLeft.ToString();
			if (turnsLeft == 0) {
				Lose();
			}
		}

		private void Win()
		{
			Owner.RunAnimation("Show", "LevelWin");
			IProgress.Instance.Coins += 50;
		}

		protected override void Stop(Node owner)
		{
			board.DropCompleted -= OnDropCompleted;
			board.TurnMade -= OnTurnMade;
			board = null;
			base.Stop(owner);
		}
	}
}

