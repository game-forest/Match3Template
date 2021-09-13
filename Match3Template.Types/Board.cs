using Lime;
using System;
using System.Collections.Generic;
using System.Linq;
using Debug = System.Diagnostics.Debug;

namespace Match3Template.Types
{
	[TangerineRegisterNode]
	public class TangerineBoard : Frame
	{
		public TangerineBoard()
		{

		}

		protected override void OnParentChanged(Node oldParent)
		{
			base.OnParentChanged(oldParent);
			if (Parent == null) {
				return;
			}
			var board = Board.CreateBoard(GetRoot().AsWidget);
		}
	}

	public class Board
	{
		private readonly Widget topLevelContainer;
		private readonly Frame itemContainer;
		private readonly BoardConfigComponent boardConfig;
		private readonly Match3ConfigComponent match3Config;
		private readonly Widget pieceTemplate;
		private readonly Widget dropTemplate;
		private readonly Widget blockerTemplate;
		private readonly Widget lineBonusTemplate;
		private readonly Widget bombBonusTemplate;
		private readonly Widget lightningBonusTemplate;
		private readonly Widget lineBonusFxTemplate;
		private readonly Widget lightningBonusFxTemplate;

		private readonly Grid grid = new Grid();
		private readonly List<ItemComponent> items = new List<ItemComponent>();

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
			pieceTemplate = Node.Load<Widget>("Game/Match3/MultiMarble");
			dropTemplate = Node.Load<Widget>("Game/Match3/Drop");
			blockerTemplate = Node.Load<Widget>("Game/Match3/ObstacleSticker");
			lineBonusTemplate = Node.Load<Widget>("Game/Match3/BonusLine");
			bombBonusTemplate = Node.Load<Widget>("Game/Match3/BonusBomb");
			lightningBonusTemplate = Node.Load<Widget>("Game/Match3/BonusLightning");
			lineBonusFxTemplate = Node.Load<Widget>("Game/Match3/BonusLineFX");
			lightningBonusFxTemplate = Node.Load<Widget>("Game/Match3/BonusLightningFX");
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
			FillBoard();
			while (true) {
				SpawnItems();
				Fall();
				HandleInput();
				CheckMatches();
				yield return null;
			}
		}

		private void FillBoard()
		{
			for (int i = 0; i < boardConfig.DropCount; i++) {
				var drop = CreateItem(RandomEmptyCell(), ItemType.Drop, -1);
			}

			for (int i = 0; i < boardConfig.BlockerCount; i++) {
				var blocker = CreateItem(RandomEmptyCell(), ItemType.Blocker, -1);
			}

			//for (int i = 0; i < boardConfig.ColumnCount * boardConfig.RowCount; i++) {
			//	var gridPosition = new IntVector2(i % boardConfig.ColumnCount, i / boardConfig.RowCount);
			//	if (grid[gridPosition] == null) {
			//		int pieceKind = Mathf.RandomItem(boardConfig.AllowedPieces);
			//		CreateItem(gridPosition, ItemType.Piece, pieceKind);
			//	}
			//}
			////foreach (item in items) {
			////	match =
			////}

			IntVector2 RandomEmptyCell()
			{
				do {
					var gridPosition = new IntVector2(
						Mathf.RandomInt(boardConfig.ColumnCount), Mathf.RandomInt(boardConfig.RowCount)
					);
					if (grid[gridPosition] == null) {
						return gridPosition;
					}
				} while (true);
			}
		}

		private void SpawnItems()
		{
			for (int i = 0; i < boardConfig.ColumnCount; i++) {
				var gridPosition = new IntVector2(i, 0);
				if (grid[gridPosition] == null) {
					int pieceKind = Mathf.RandomItem(boardConfig.AllowedPieces);
					CreateItem(gridPosition, ItemType.Piece, pieceKind);
				}
			}
		}

		private ItemComponent CreateItem(IntVector2 gridPosition, ItemType type, int kind)
		{
			if (items.Where(i => i.GridPosition == gridPosition).Any()) {
				throw new InvalidOperationException();
			}
			var w = type switch {
				ItemType.Piece => pieceTemplate.Clone<Widget>(),
				ItemType.Blocker => blockerTemplate.Clone<Widget>(),
				ItemType.Drop => dropTemplate.Clone<Widget>(),
				_ => throw new NotImplementedException(),
			};
			ItemComponent item;
			w.Components.Add(item = new ItemComponent(grid));
			item.Kind = kind;
			item.Type = type;
			itemContainer.AddNode(w);
			item.GridPosition = gridPosition;
			item.Owner.AsWidget.Position = item.WidgetPosition(gridPosition);
			items.Add(item);
			item.RunTask(AnimateTask(item.AnimateShow()));
			item.AnimateIdle();
			return item;
		}
		private IEnumerator<object> AnimateTask(Animation animation)
		{
			yield return animation;
		}

		private void Fall()
		{
			items.Sort((a, b) => - a.GridPosition.Y + b.GridPosition.Y);
			foreach (var item in items) {
				if (item.Task != null) {
					continue;
				}
				if (
					CanFall(item)
					|| CanFallDiagonallyRight(item)
					|| CanFallDiagonallyLeft(item))
				{
					item.RunTask(FallTask(item));
				}
			}
		}

		private bool CanFall(ItemComponent item)
		{
			var belowPosition = item.GridPosition + IntVector2.Down;
			return item.Type != ItemType.Blocker
				&& grid[belowPosition] == null
				&& belowPosition.Y < boardConfig.RowCount;
		}

		private bool CanFallDiagonallyRight(ItemComponent item)
		{
			var belowPosition = item.GridPosition + IntVector2.Down + IntVector2.Right;
			var sidePosition = item.GridPosition + IntVector2.Right;
			return item.Type != ItemType.Blocker
				&& grid[belowPosition] == null
				&& belowPosition.Y < boardConfig.RowCount
				&& belowPosition.X < boardConfig.ColumnCount
				&& grid[sidePosition] == null
				&& sidePosition.X < boardConfig.ColumnCount
				&& sidePosition.X >= 0;
		}

		private bool CanFallDiagonallyLeft(ItemComponent item)
		{
			var belowPosition = item.GridPosition + IntVector2.Down + IntVector2.Left;
			var sidePosition = item.GridPosition + IntVector2.Left;
			return item.Type != ItemType.Blocker
				&& grid[belowPosition] == null
				&& belowPosition.Y < boardConfig.RowCount
				&& belowPosition.X >= 0
				&& grid[sidePosition] == null
				&& sidePosition.X < boardConfig.ColumnCount
				&& sidePosition.X >= 0;
		}

		private IEnumerator<object> FallTask(ItemComponent item)
		{
			var a = item.AnimateDropDownFall();
			if (match3Config.WaitForAnimateDropDownFall) {
				yield return a;
			}
			while (true) {
				while (CanFall(item)) {
					var belowPosition = item.GridPosition + IntVector2.Down;
					yield return item.MoveTo(belowPosition, match3Config.OneCellFallTime);
				}
				if (CanFallDiagonallyRight(item)) {
					var belowPosition = item.GridPosition + IntVector2.Down + IntVector2.Right;
					yield return item.MoveTo(belowPosition, match3Config.OneCellFallTime * Mathf.Sqrt(2));
				} else if (CanFallDiagonallyLeft(item)) {
					var belowPosition = item.GridPosition + IntVector2.Down + IntVector2.Left;
					yield return item.MoveTo(belowPosition, match3Config.OneCellFallTime * Mathf.Sqrt(2));
				} else {
					break;
				}
			}
			a = item.AnimateDropDownLand();
			if (match3Config.WaitForAnimateDropDownLand) {
				yield return a;
			}
		}

		private void HandleInput()
		{
			for (int i = 0; i < 4; i++) {
				if (Window.Current.Input.WasTouchBegan(i)) {
					var item = WidgetContext.Current.NodeUnderMouse?.Components.Get<ItemComponent>();
					if (item == null || item.Task != null || item.Type == ItemType.Blocker) {
						continue;
					}
					item.RunTask(InputTask(item, i));
				}
			}
		}

		private IEnumerator<object> InputTask(ItemComponent item, int i)
		{
			var input = Window.Current.Input;
			var touchPosition0 = input.GetTouchPosition(i);
			Vector2 touchDelta;
			var originalItemPosition = item.Owner.AsWidget.Position;
			do {
				yield return null;
				touchDelta = input.GetTouchPosition(i) - touchPosition0;
			} while (
				touchDelta.Length < match3Config.InputDetectionLength && input.IsTouching(i)
			);
			if (!TryGetProjectionAxis(touchDelta, out var projectionAxis)) {
				// item.AnimateUnselect();
				yield break;
			}
			var nextItem = grid[item.GridPosition + projectionAxis];
			if (nextItem?.Task != null) {
				yield break;
			}
			item.AnimateSelect();
			// Make sure touched widget is above other widgets.
			item.Owner.Parent.Nodes.Swap(0, item.Owner.Parent.Nodes.IndexOf(item.Owner));
			bool finished = false;
			bool movingBack = true;
			if (nextItem != null) {
				var nextItemOriginalPosition = nextItem.Owner.AsWidget.Position;
				Func<bool> syncPosition = () => {
					nextItem.Owner.AsWidget.Position = nextItemOriginalPosition
						+ (originalItemPosition - item.Owner.AsWidget.Position);
					return !finished;
				};
				nextItem.RunTask(Task.Repeat(syncPosition));
			} else {
				if (projectionAxis.Y == 0) {
					var itemAbove = grid[item.GridPosition /*- projectionAxis*/ + IntVector2.Up];
					if (itemAbove != null && itemAbove.Task == null) {
						itemAbove.RunTask(Task.Repeat(() => {
							return movingBack;
						}));
					}
					itemAbove = grid[item.GridPosition - projectionAxis + IntVector2.Up];
					if (itemAbove != null && itemAbove.Task == null) {
						itemAbove.RunTask(Task.Repeat(() => {
							return movingBack;
						}));
					}
					itemAbove = grid[item.GridPosition + projectionAxis + IntVector2.Up];
					if (itemAbove != null && itemAbove.Task == null) {
						itemAbove.RunTask(Task.Repeat(() => {
							return movingBack;
						}));
					}
				}
			}
			float projectionAmount = 0.0f;
			while (input.IsTouching(i)) {
				touchDelta = input.GetTouchPosition(i) - touchPosition0;
				projectionAmount = Vector2.DotProduct((Vector2)projectionAxis, touchDelta);
				projectionAmount = Mathf.Clamp(projectionAmount, 0, item.Owner.AsWidget.Width);
				item.Owner.AsWidget.Position = item.WidgetPosition(item.GridPosition)
					+ projectionAmount * (Vector2)projectionAxis;
				yield return null;
			}
			item.AnimateUnselect();
			if (projectionAmount > match3Config.DragPercentOfPieceSizeRequiredForSwapActivation) {
				if (nextItem == null) {
					item.GridPosition += projectionAxis;
					if (match3Config.SwapBackOnNonMatchingSwap) {
						var match = FindMatchForItem(item);
						if (FindMatchForItem(item).Count < 3) {
							yield return item.MoveTo(item.GridPosition, match3Config.PieceReturnOnTouchEndTime);
							movingBack = false;
							if (grid[item.GridPosition - projectionAxis] == null) {
								item.GridPosition -= projectionAxis;
							}
						} else {
							yield return BlowMatch(match);
						}
					}
				} else {
					item.SwapWith(nextItem);
					if (match3Config.SwapBackOnNonMatchingSwap) {
						if (FindMatchForItem(item).Count < 3 && FindMatchForItem(nextItem).Count < 3) {
							yield return item.MoveTo(item.GridPosition, match3Config.PieceReturnOnTouchEndTime);
							item.SwapWith(nextItem);
							var i0 = item.Owner.Parent.Nodes.IndexOf(item.Owner);
							var i1 = nextItem.Owner.Parent.Nodes.IndexOf(nextItem.Owner);
							item.Owner.Parent.Nodes.Swap(i0, i1);
						}
					}
				}
			}
			yield return item.MoveTo(item.GridPosition, match3Config.PieceReturnOnTouchEndTime);
			finished = true;
			movingBack = false;
		}

		private bool TryGetProjectionAxis(Vector2 touchDelta, out IntVector2 projectionAxis)
		{
			var angle = Mathf.Wrap360(Mathf.RadToDeg * Mathf.Atan2(touchDelta));
			int sectorIndex = 3 - ((int)((angle + 45.0f) / 90)) % 4;
			projectionAxis = new IntVector2(Math.Abs(sectorIndex - 1) - 1, 1 - Math.Abs(sectorIndex - 2));
			var halfDeadAngle = match3Config.DiagonalSwipeDeadZoneAngle * 0.5f;
			if (angle % 90.0f > 45.0f - halfDeadAngle && angle % 90.0f < 45.0f + halfDeadAngle) {
				return false;
			}
			return true;
		}

		private void CheckMatches()
		{
			HashSet<ItemComponent> blownItems = new HashSet<ItemComponent>();
			HashSet<ItemComponent> matchedItems = new HashSet<ItemComponent>();
			foreach (var item in items) {
				if (matchedItems.Contains(item) || blownItems.Contains(item)) {
					continue;
				}
				var match = FindMatchForItem(item);
				foreach (var matchedItem in match) {
					matchedItems.Add(matchedItem);
				}
				if (match.Count >= 3 && match.All(i => i.Task == null)) {
					foreach (var matchedItem in match) {
						matchedItem.RunTask(BlowTask(matchedItem));
						blownItems.Add(matchedItem);
					}
				}
			}
			foreach (var item in blownItems) {
				items.Remove(item);
			}
		}

		private List<ItemComponent> FindMatchForItem(ItemComponent item)
		{
			if (item.Type != ItemType.Piece) {
				return new List<ItemComponent>();
			}
			HashSet<IntVector2> Visited = new HashSet<IntVector2>();
			Queue<ItemComponent> queue = new Queue<ItemComponent>();
			List<ItemComponent> match = new List<ItemComponent>();
			Visited.Add(item.GridPosition);
			match.Add(item);
			queue.Enqueue(item);
			while (queue.Any()) {
				var currentItem = queue.Dequeue();
				for (int i = 0; i < 4; i++) {
					var nextPosition = currentItem.GridPosition
						+ new IntVector2(
							x: Math.Abs(i - 1) - 1,
							y: -Math.Abs(i - 2) + 1
						);
					if (Visited.Contains(nextPosition)) {
						continue;
					}
					var nextItem = grid[nextPosition];
					Visited.Add(nextPosition);
					if (
						nextItem != null
						&& nextItem.Kind == currentItem.Kind
						//&& nextItem.Task == null
					) {
						queue.Enqueue(nextItem);
						Debug.Assert(!match.Contains(nextItem));
						match.Add(nextItem);
					}
				}
			}
			return match;
		}

		private IEnumerator<object> BlowMatch(List<ItemComponent> match)
		{
			var tasks = new List<Lime.Task>();
			foreach (var item in match) {
				tasks.Add(item.Owner.Tasks.Add(BlowTask(item)));
			}
			while (!tasks.All(t => t.Completed)) {
				yield return null;
			}
		}

		private IEnumerator<object> BlowTask(ItemComponent item)
		{
			yield return item.AnimateMatch();
			items.Remove(item);
			item.Owner.UnlinkAndDispose();
		}
	}
}

