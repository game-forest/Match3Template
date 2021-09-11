using Lime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Match3Template.Types
{
	public class Board
	{
		private readonly Widget topLevelContainer;
		private readonly Frame itemContainer;
		private readonly BoardConfigComponent boardConfig;
		private readonly Match3ConfigComponent match3Config;
		private readonly Widget boardTemplateScene;
		private readonly Widget pieceTemplate;
		private readonly Grid grid = new Grid();
		private readonly List<ItemComponent> items = new List<ItemComponent>();
		private static readonly List<ItemComponent> itemPool = new List<ItemComponent>();

		public static Board CreateBoard(Widget root)
		{
			var match3Config = root.SelfAndDescendants
				.FirstOrDefault(n => n.Components.Contains<Match3ConfigComponent>())
				?.Components
				.Get<Match3ConfigComponent>();

			var boardWidget = root.SelfAndDescendants
				.FirstOrDefault(n => n.Components.Contains<BoardConfigComponent>())
				as Widget;

			var boardConfig = boardWidget?.Components.Get<BoardConfigComponent>();

			return new Board(boardWidget, boardConfig, match3Config);
		}

		public Board(Widget boardWidget, BoardConfigComponent boardConfig, Match3ConfigComponent match3Config)
		{
			this.topLevelContainer = boardWidget;
			this.boardConfig = boardConfig;
			this.match3Config = match3Config;
			boardTemplateScene = Node.Load<Frame>("Shell/Board");
			pieceTemplate = boardTemplateScene["MultiMarble"] as Frame;
			itemContainer = new Frame {
				//ClipChildren = ClipMethod.ScissorTest,
				Width = boardConfig.ColumnCount * pieceTemplate.Width,
				Height = boardConfig.RowCount * pieceTemplate.Height
			};
			topLevelContainer.Nodes.Insert(0, itemContainer);
			itemContainer.CenterOnParent();
			itemContainer.CompoundPostPresenter.Add(new WidgetBoundsPresenter(Color4.Red, 2.0f));
			topLevelContainer.Tasks.Add(Update);
		}

		private IEnumerator<object> Update()
		{
			while (true) {
				SpawnItems();
				Fall();
				HandleInput();
				CheckMatches();
				yield return null;
			}
		}

		private void SpawnItems()
		{
			for (int i = 0; i < boardConfig.ColumnCount; i++) {
				var gridPosition = new IntVector2(i, -1);
				if (grid[gridPosition] == null) {
					int pieceKind = Mathf.RandomInt(0, GetPieceKindCount() - 1);
					var piece = CreatePiece(gridPosition, pieceKind);
					piece.AnimateShow();
				}
			}
		}

		private ItemComponent CreatePiece(IntVector2 gridPosition, int kind)
		{
			if (items.Where(i => i.GridPosition == gridPosition).Any()) {
				throw new InvalidOperationException();
			}
			var w = pieceTemplate.Clone<Widget>();
			ItemComponent piece;
			w.Components.Add(piece = new ItemComponent(boardConfig, match3Config));
			piece.SetPieceKind(kind);
			itemContainer.AddNode(w);
			piece.GridPosition = gridPosition;
			grid[piece.GridPosition] = piece;
			items.Add(piece);
			return piece;
		}

		private int GetPieceKindCount()
		{
			var kindAnimation = pieceTemplate.Animations.Find("Color");
			return kindAnimation.Markers.Count;
		}

		private void Fall()
		{
			foreach (var item in items) {
				if (item.Task != null) {
					continue;
				}
				if (!CanFall(item)) {
					continue;
				}
				item.RunTask(FallTask(item));
			}
		}

		private bool CanFall(ItemComponent item)
		{
			var belowPosition = item.GridPosition + IntVector2.Down;
			return grid[belowPosition] == null && belowPosition.Y < boardConfig.RowCount;
		}

		private IEnumerator<object> FallTask(ItemComponent item)
		{
			yield return item.AnimateDropDownFall();
			while (CanFall(item)) {
				var belowPosition = item.GridPosition + IntVector2.Down;
				yield return item.MoveTo(belowPosition, grid);
			}
			var a = item.AnimateDropDownLand();
			if (match3Config.WaitForAnimateDropDownLand) {
				yield return a;
			}
		}

		private void HandleInput()
		{
			for (int i = 0; i < 4; i++) {
				if (Window.Current.Input.WasTouchBegan(i)) {
					var item = WidgetContext.Current.NodeUnderMouse?.Components.Get<ItemComponent>();
					if (item == null) {
						continue;
					}
					if (item.Task != null) {
						continue;
					}
					item.RunTask(InputTask(item, i));
				}
			}
		}

		private IEnumerator<object> InputTask(ItemComponent item, int i)
		{
			item.AnimateSelect();
			var input = Window.Current.Input;
			var p0 = input.GetTouchPosition(i);
			item.Owner.Parent.Nodes.Swap(0, item.Owner.Parent.Nodes.IndexOf(item.Owner));
			var p1 = input.GetTouchPosition(i);

			do {
				yield return null;
				p1 = input.GetTouchPosition(i);
			} while ((p1 - p0).Length < match3Config.InputDetectionLength && input.IsTouching(i));

			Vector2 projectionAxis;
			IntVector2 neighbourOffset;
			{

				var positionDelta = p1 - p0;
				var angle = Mathf.RadToDeg * Mathf.Atan2(positionDelta);
				angle = Mathf.Wrap360(angle);
				var deadZoneAngleAmount = 10;
				for (int j = 0; j < 4; j++) {
					var a = j * 90 + 45;
					if (angle < a + deadZoneAngleAmount && angle > a - deadZoneAngleAmount) {
						item.AnimateUnselect();
						yield break;
					}
				}
				int sectorIndex = (int)(angle + 45.0f) / 90;
				if (sectorIndex % 2 == 0) {
					projectionAxis = Vector2.Right;
					neighbourOffset = IntVector2.Right;
				} else {
					projectionAxis = Vector2.Down;
					neighbourOffset = IntVector2.Down;
				}
				Console.WriteLine(angle);
			}
			float projectionAmount = 0.0f;
			ItemComponent nextItem = null;
			while (input.IsTouching(i)) {
				p1 = input.GetTouchPosition(i);
				var positionDelta = p1 - p0;
				projectionAmount = Vector2.DotProduct(projectionAxis, positionDelta);
				var sign = Mathf.Sign(projectionAmount);
				projectionAmount = Mathf.Clamp(Mathf.Abs(projectionAmount), 0, item.Owner.AsWidget.Width);
				var projectedDelta = projectionAmount * sign * projectionAxis;
				item.Owner.AsWidget.Position = item.WidgetPosition(item.GridPosition) + projectedDelta;
				nextItem = grid[item.GridPosition + neighbourOffset * (sign < 0 ? -1 : 1)];
				if (nextItem != null) {
					nextItem.Owner.AsWidget.Position = nextItem.WidgetPosition(nextItem.GridPosition) - projectedDelta;
				}
				yield return null;
			}
			Console.WriteLine("Exiting");
			item.AnimateUnselect();
			if (
				projectionAmount > match3Config.DragPercentOfPieceSizeRequiredForSwapActivation
				&& nextItem != null
				&& nextItem.Task == null
			) {
				var i0 = item.Owner.Parent.Nodes.IndexOf(item.Owner);
				var i1 = nextItem.Owner.Parent.Nodes.IndexOf(nextItem.Owner);
				// item.Owner.Parent.Nodes.Swap(i0, i1);
				var temp = item.GridPosition;
				grid[temp] = nextItem;
				item.gridPosition = nextItem.GridPosition;
				grid[nextItem.GridPosition] = item;
				nextItem.gridPosition = temp;
				item.RunTask(MoveItemToOriginalPosition(item));
				nextItem.RunTask(MoveItemToOriginalPosition(nextItem));
			} else {
				item.RunTask(MoveItemToOriginalPosition(item));
				if (nextItem != null && nextItem.Task == null) {
					nextItem.RunTask(MoveItemToOriginalPosition(nextItem));
				}
			}
		}

		private IEnumerator<object> MoveItemToOriginalPosition(ItemComponent item)
		{
			var t = match3Config.PieceReturnOnTouchEndTime;
			var p0 = item.Owner.AsWidget.Position;
			var p1 = item.WidgetPosition(item.GridPosition);
			while (t > 0.0f) {
				item.Owner.AsWidget.Position = Mathf.Lerp(1 - t / match3Config.PieceReturnOnTouchEndTime, p0, p1);
				t -= Task.Current.Delta;
				if (t < 0.0f) {
					item.Owner.AsWidget.Position = p1;
				}
				yield return null;
			}
		}

		private void CheckMatches()
		{
			foreach (var item in items.ToList()) {
				int kind = item.Kind;
				int count = 1;
				List<ItemComponent> matchItems = new List<ItemComponent>();
				var p = item.GridPosition + IntVector2.Right;
				matchItems.Add(item);
				while (grid[p] != null && grid[p].Kind == kind) {
					matchItems.Add(grid[p]);
					p += IntVector2.Right;
					count++;
				}
				p = item.GridPosition - IntVector2.Right;
				while (grid[p] != null && grid[p].Kind == kind) {
					matchItems.Add(grid[p]);
					p -= IntVector2.Right;
					count++;
				}
				if (count >= 3 && matchItems.All(i => i.Task == null)) {
					foreach (var matchItem in matchItems) {
						items.Remove(matchItem);
						matchItem.RunTask(BlowTask(matchItem));
					}
				}
				// TODO: remove dup code
				count = 1;
				matchItems.Clear();
				p = item.GridPosition + IntVector2.Down;
				matchItems.Add(item);
				while (grid[p] != null && grid[p].Kind == kind) {
					matchItems.Add(grid[p]);
					p += IntVector2.Down;
					count++;
				}
				p = item.GridPosition - IntVector2.Down;
				while (grid[p] != null && grid[p].Kind == kind) {
					matchItems.Add(grid[p]);
					p -= IntVector2.Down;
					count++;
				}
				if (count >= 3 && matchItems.All(i => i.Task == null)) {
					foreach (var matchItem in matchItems) {
						items.Remove(matchItem);
						matchItem.RunTask(BlowTask(matchItem));
					}
				}
			}
		}

		private IEnumerator<object> BlowTask(ItemComponent item)
		{
			yield return item.AnimateMatch();
			grid[item.GridPosition] = null;
			items.Remove(item);
			item.Owner.UnlinkAndDispose();
		}
	}
}

