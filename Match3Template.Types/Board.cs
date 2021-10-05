using Lime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Debug = System.Diagnostics.Debug;

namespace Match3Template.Types
{
	public class DropCompletedEventArgs : EventArgs
	{
		public Widget ItemWidget { get; set; }
	}

	public class TurnMadeEventArgs : EventArgs
	{

	}

	public class Board
	{
		public event EventHandler<DropCompletedEventArgs> DropCompleted;
		public event EventHandler<TurnMadeEventArgs> TurnMade;

		private readonly Widget topLevelContainer;
		private readonly Frame itemContainer;
		private readonly Frame bgContainer;
		private readonly BoardConfigComponent boardConfig;
		private readonly Match3ConfigComponent match3Config;
		private readonly Widget pieceTemplate;
		private readonly Widget dropTemplate;
		private readonly Widget blockerTemplate;
		private readonly Widget cutoutCellTemplate;
		private readonly Widget lineBonusTemplate;
		private readonly Widget bombBonusTemplate;
		private readonly Widget lightningBonusTemplate;
		private readonly Widget lineBonusFxTemplate;
		private readonly Widget lightningBonusFxTemplate;

		private readonly Grid<ItemComponent> grid = new Grid<ItemComponent>();
		private readonly List<ItemComponent> items = new List<ItemComponent>();

		private Vector2 CellSize => new Vector2(90, 90);

		internal int GetTurnCount() => boardConfig.TurnCount;

		internal int GetDropCount() => boardConfig.DropCount;

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

			boardWidget = boardWidget["BoardContainer"];

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
			cutoutCellTemplate = Node.Load<Widget>("Game/Match3/ObstacleUnbreakable");
			lineBonusTemplate = Node.Load<Widget>("Game/Match3/BonusLine");
			bombBonusTemplate = Node.Load<Widget>("Game/Match3/BonusBomb");
			lightningBonusTemplate = Node.Load<Widget>("Game/Match3/BonusLightning");
			lineBonusFxTemplate = Node.Load<Widget>("Game/Match3/BonusLineFX");
			lightningBonusFxTemplate = Node.Load<Widget>("Game/Match3/BonusLightningFX");

			bgContainer = new Frame();
			itemContainer = new Frame();
			FillBoard();

			bgContainer.Width = boardConfig.ColumnCount * match3Config.CellSize.Width;
			bgContainer.Height = boardConfig.RowCount * match3Config.CellSize.Height;
			topLevelContainer.Nodes.Insert(0, bgContainer);
			bgContainer.CenterOnParent();

			itemContainer.Width = boardConfig.ColumnCount * match3Config.CellSize.Width;
			itemContainer.Height = boardConfig.RowCount * match3Config.CellSize.Height;
			topLevelContainer.Nodes.Insert(0, itemContainer);
			itemContainer.CenterOnParent();

			topLevelContainer.Tasks.Add(Update);
			containerBoundsPresenter = new WidgetBoundsPresenter(Color4.Red, 2.0f);
			topLevelContainer.Tasks.Add(CheckCheatsTask);

			topLevelContainer.Tasks.Add(Task.Repeat(() => {
				UpdateBoardScale();
				return true;
			}));
			UpdateBoardScale();
		}

		void UpdateBoardScale()
		{
			var widthAspect = topLevelContainer.Width / itemContainer.Width;
			var heightAspect = topLevelContainer.Height / itemContainer.Height;
			match3Config.BoardScale = Mathf.Min(widthAspect, heightAspect);
			itemContainer.Scale = new Vector2(match3Config.BoardScale);
			itemContainer.CenterOnParent();
			bgContainer.Scale = new Vector2(match3Config.BoardScale);
			bgContainer.CenterOnParent();
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

		private IEnumerator<object> CheckCheatsTask()
		{
			while (true) {
				var containerPresenter = itemContainer.CompoundPostPresenter;
				if (ICheatManager.Instance.DebugMatch3) {
					if (!containerPresenter.Contains(containerBoundsPresenter)) {
						containerPresenter.Add(containerBoundsPresenter);
					}
				} else {
					containerPresenter.Remove(containerBoundsPresenter);
				}
				yield return 0.1f;
			}

		}

		private readonly IPresenter containerBoundsPresenter;

		private static readonly IntVector2[] directionVectors =
			{ IntVector2.Right, IntVector2.Down, IntVector2.Left, IntVector2.Up };

		[Flags]
		private enum Direction
		{
			Right = 0,
			Down = 1,
			Left = 2,
			Up = 4,
			Horizontal = Right | Left,
			Vertical = Up | Down,
			Any = Horizontal | Vertical,
		}

		private static readonly Direction[] directions = {
			Direction.Right, Direction.Down, Direction.Left, Direction.Up
		};

		private IEnumerable<(ItemComponent Item, IntVector2 Delta, Direction Direction)> EnumerateAdjacentItems(
			IntVector2 position
		) {
			for (int i = 0; i < 4; i++) {
				var delta = directionVectors[i];
				yield return (grid[position + delta], delta, directions[i]);
			}
		}

		private void FillBoard()
		{
			int dropCountInAsset = 0;
			int blockerCountInAsset = 0;
			int dropToSpawn = 0;
			if (!string.IsNullOrEmpty(boardConfig.LevelFileName)) {
				using var stream = AssetBundle.Current.OpenFile(boardConfig.LevelFileName + ".txt");
				using var reader = new StreamReader(stream);
				int rowCount = 0;
				int columnCount = 0;
				while (!reader.EndOfStream) {
					var line = reader.ReadLine();
					columnCount = Math.Max(columnCount, line.Length);
					int x = 0;
					ItemComponent item = null;
					foreach (var c in line) {
						var p = new IntVector2(x, rowCount);
						switch (c) {
							case '0':
							case '1':
							case '2':
							case '3':
							case '4': {
								item = CreatePiece(p, c - '0');
								break;
							}
							case 'D': {
								item = CreateDrop(p);
								dropCountInAsset++;
								break;
							}
							case 'B': {
								item = CreateBlocker(p);
								blockerCountInAsset++;
								break;
							}
							case 'U': {
								item = CreateCutoutCell(p);
								break;
							}
						}
						if (item != null) {
							item.AnimateShown();
						}
						x++;
					}
					rowCount++;
				}
				boardConfig.RowCount = Math.Max(boardConfig.RowCount, rowCount);
				boardConfig.ColumnCount = Math.Max(boardConfig.ColumnCount, columnCount);
				if (boardConfig.DropCount > dropCountInAsset) {
					boardConfig.DropCount -= dropCountInAsset;
					dropToSpawn = boardConfig.DropCount;
				} else {
					boardConfig.DropCount = dropCountInAsset;
					dropToSpawn = 0;
				}
			}

			if (boardConfig.PreFillBoard) {
				for (int i = 0; i < boardConfig.DropCount - dropCountInAsset; i++) {
					if (TryGetRandomEmptyCell(boardConfig.RowCount - 1, boardConfig.ColumnCount, out var p)) {
						var drop = CreateDrop(p);
						drop.AnimateShown();
					}
				}

				for (int i = 0; i < boardConfig.BlockerCount - blockerCountInAsset; i++) {
					if (TryGetRandomEmptyCell(boardConfig.RowCount - 1, boardConfig.ColumnCount, out var p)) {
						var blocker = CreateBlocker(p);
						blocker.AnimateShown();
					}
				}

				if (boardConfig.AllowedPieces.Any()) {
					int pieceCount = boardConfig.ColumnCount * boardConfig.RowCount;
					for (int i = 0; i < pieceCount; i++) {
						var gridPosition = new IntVector2(i % boardConfig.ColumnCount, i / boardConfig.ColumnCount);
						if (grid[gridPosition] == null) {
							var adjacentKinds = EnumerateAdjacentItems(gridPosition)
								.Where(i => i.Item is Piece piece)
								.Select(i => (i.Item as Piece).Kind)
								.Distinct();
							var allowedKinds = boardConfig.AllowedPieces.Except(adjacentKinds).ToList();
							if (!allowedKinds.Any()) {
								allowedKinds.Add(boardConfig.AllowedPieces.RandomItem());
							}
							int pieceKind = Mathf.RandomItem(allowedKinds);
							var item = CreatePiece(gridPosition, pieceKind);
							item.AnimateShown();
						}
					}
				}
			}

			var backgroundCellTemplate = Node.Load<Frame>("Game/Match3/Cell")["Cell"];
			for (int i = 0; i < boardConfig.RowCount; i++) {
				for (int j = 0; j < boardConfig.ColumnCount; j++) {
					var p = new IntVector2(j, i);
					if (grid[p] is CutoutCell) {
						continue;
					}
					var bg = backgroundCellTemplate.Clone<Image>();
					bg.Position = match3Config.GridPositionToWidgetPosition(p);
					bgContainer.AddNode(bg);
				}
			}

			bool TryGetRandomEmptyCell(int maxRow, int maxColumn, out IntVector2 cell)
			{
				cell = default;
				var cells = Enumerable.Range(0, maxColumn)
					.SelectMany(x => Enumerable.Range(0, maxRow).Select(y => new IntVector2(x, y)));
				var emptyCells = cells.Where(c => grid[c] == null);
				if (emptyCells.Any()) {
					cell = emptyCells.ToList().RandomItem();
					return true;
				} else {
					return false;
				}
			}
		}

		private void SpawnItems()
		{
			for (int i = 0; i < boardConfig.ColumnCount; i++) {
				var gridPosition = new IntVector2(i, 0);
				if (grid[gridPosition] == null) {
					int pieceKind = Mathf.RandomItem(boardConfig.AllowedPieces);
					var item = CreatePiece(gridPosition, pieceKind);
					item.RunAnimationTask(item.AnimateShow());
				}
			}
		}

		private Piece CreatePiece(IntVector2 gridPosition, int kind)
		{
			var w = pieceTemplate.Clone<Widget>();
			var piece = new Piece(grid);
			SetupItem(piece, w, gridPosition);
			piece.Kind = kind;
			return piece;
		}

		private Blocker CreateBlocker(IntVector2 gridPosition)
		{
			var w = blockerTemplate.Clone<Widget>();
			var blocker = new Blocker(grid);
			SetupItem(blocker, w, gridPosition);
			return blocker;
		}

		private Drop CreateDrop(IntVector2 gridPosition)
		{
			var w = dropTemplate.Clone<Widget>();
			var drop = new Drop(grid);
			SetupItem(drop, w, gridPosition);
			return drop;
		}

		private CutoutCell CreateCutoutCell(IntVector2 gridPosition)
		{
			var w = cutoutCellTemplate.Clone<Widget>();
			var cutoutCell = new CutoutCell(grid);
			SetupItem(cutoutCell, w, gridPosition);
			return cutoutCell;
		}

		private Bonus CreateBonus(IntVector2 gridPosition, BonusKind bonusKind)
		{
			var w = bonusKind switch {
				BonusKind.HorizontalLine => lineBonusTemplate.Clone<Widget>(),
				BonusKind.VerticalLine => lineBonusTemplate.Clone<Widget>(),
				BonusKind.Bomb => bombBonusTemplate.Clone<Widget>(),
				BonusKind.Lightning => lightningBonusTemplate.Clone<Widget>(),
				_ => throw new NotImplementedException(),
			};
			if (bonusKind == BonusKind.VerticalLine) {
				w.Rotation = 90;
			}
			var bonus = new Bonus(grid) {
				BonusKind = bonusKind
			};
			SetupItem(bonus, w, gridPosition);
			bonus.RunAnimationTask(bonus.AnimateShow());
			return bonus;
		}

		private void SetupItem(ItemComponent item, Widget widget, IntVector2 gridPosition)
		{
			if (items.Where(i => i.GridPosition == gridPosition).Any()) {
				throw new InvalidOperationException();
			}
			widget.Components.Add(item);
			itemContainer.AddNode(widget);
			item.GridPosition = gridPosition;
			item.Owner.AsWidget.Position = match3Config.GridPositionToWidgetPosition(gridPosition);
			items.Add(item);
			item.AnimateIdle();
		}

		private void Fall()
		{
			items.Sort((a, b) => - a.GridPosition.Y + b.GridPosition.Y);
			var completedDrops = new List<ItemComponent>();
			foreach (var item in items) {
				if (item.Task != null) {
					continue;
				}
				if (
					CanFall(item)
					|| CanFallDiagonallyRight(item)
					|| CanFallDiagonallyLeft(item)
				) {
					item.RunTask(FallTask(item));
				} else if (item is Drop && item.GridPosition.Y == boardConfig.RowCount - 1) {
					completedDrops.Add(item);
				} else
				if (!item.CanMove) {
					// Gap filling
					var p = item.GridPosition;
					var belowPosition = p + IntVector2.Down;
					while (true) {
						if (belowPosition.Y == boardConfig.ColumnCount) {
							break;
						}
						var below = grid[belowPosition];
						var belowLeft = grid[belowPosition + IntVector2.Left];
						var belowRight = grid[belowPosition + IntVector2.Right];
						if (below == null) {
							if (belowLeft != null && belowLeft.Task == null && !(belowLeft.CanMove)) {
								belowLeft.GridPosition = belowPosition;
								belowLeft.RunTask(MoveToTask(belowLeft));
								break;
							} else if (belowRight != null && belowRight.Task == null && !(belowRight.CanMove)) {
								belowRight.GridPosition = belowPosition;
								belowRight.RunTask(MoveToTask(belowRight));
								break;
							} else if (
								(belowLeft == null || belowLeft.CanMove)
								&& (belowRight == null || belowRight.CanMove)
							) {
								belowPosition += IntVector2.Down;
								// TODO: lift pieces somehow
								//below = grid[belowPosition];
								//if (below != null && below.Task == null && below.Type != ItemType.Blocker) {
								//	below.GridPosition = belowPosition + IntVector2.Up;
								//	break;
								//} else {
								//	break;
								//}
							} else {
								break;
							}
						} else {
							break;
						}
					}
				}
			}
			foreach (var item in completedDrops) {
				var e = new DropCompletedEventArgs() {
					ItemWidget = item.Owner.AsWidget
				};
				items.Remove(item);
				item.Kill();
				item.Owner.Components.Remove(item);
				DropCompleted?.Invoke(this, e);
			}
		}

		private IEnumerator<object> MoveToTask(ItemComponent item)
		{
			yield return item.MoveTo(item.GridPosition, match3Config.OneCellFallTime);
		}

		private bool CanFall(ItemComponent item)
		{
			var belowPosition = item.GridPosition + IntVector2.Down;
			return item.CanMove
				&& grid[belowPosition] == null
				&& belowPosition.Y < boardConfig.RowCount;
		}

		private bool CanFallDiagonallyRight(ItemComponent item)
		{
			var belowPosition = item.GridPosition + IntVector2.Down + IntVector2.Right;
			var sidePosition = item.GridPosition + IntVector2.Right;
			return item.CanMove
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
			return item.CanMove
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
					if (item == null || item.Task != null || !item.CanMove) {
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
			if (!input.IsTouching(i) && touchDelta.Length < match3Config.InputDetectionLength) {
				if (item is Bonus bonus) {
					yield return BlowTask(item, DamageKind.Match);
				}
				yield break;
			}
			if (!TryGetProjectionAxis(touchDelta, out var projectionAxis)) {
				yield break;
			}
			var nextItem = grid[item.GridPosition + projectionAxis];
			if (nextItem?.Task != null || (!nextItem?.CanMove ?? false)) {
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
				// Block 3 items above to prevent them from falling
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
				projectionAmount = Mathf.Clamp(
					value: 1.0f / match3Config.BoardScale * projectionAmount,
					min: 0,
					max: item.Owner.AsWidget.Width
				);
				item.Owner.AsWidget.Position = match3Config.GridPositionToWidgetPosition(item.GridPosition)
					+ projectionAmount * (Vector2)projectionAxis;
				yield return null;
			}
			bool turnMade = false;
			item.AnimateUnselect();
			if (projectionAmount > match3Config.DragPercentOfPieceSizeRequiredForSwapActivation) {
				if (nextItem == null) {
					item.GridPosition += projectionAxis;
					if (match3Config.SwapBackOnNonMatchingSwap) {
						if (item is Piece piece) {
							var match = FindMatchForItem(piece);
							if (FindMatchForItem(piece).Any()) {
								turnMade = true;
								yield return item.MoveTo(item.GridPosition, match3Config.PieceReturnOnTouchEndTime);
								movingBack = false;
								if (grid[item.GridPosition - projectionAxis] == null) {
									item.GridPosition -= projectionAxis;
								}
							} else {
								BlowMatch(match);
							}
						}

					} else {
						turnMade = true;
					}
				} else {
					item.SwapWith(nextItem);
					if (match3Config.SwapBackOnNonMatchingSwap) {
						if (item is Piece piece && FindMatchForItem(piece).Any() && nextItem is Piece nextPiece && FindMatchForItem(nextPiece).Any()) {
							turnMade = true;
							yield return item.MoveTo(item.GridPosition, match3Config.PieceReturnOnTouchEndTime);
							item.SwapWith(nextItem);
							var i0 = item.Owner.Parent.Nodes.IndexOf(item.Owner);
							var i1 = nextItem.Owner.Parent.Nodes.IndexOf(nextItem.Owner);
							item.Owner.Parent.Nodes.Swap(i0, i1);
						}
					} else {
						turnMade = true;
					}
				}
			}
			yield return item.MoveTo(item.GridPosition, match3Config.PieceReturnOnTouchEndTime);
			finished = true;
			movingBack = false;
			if (turnMade) {
				TurnMade?.Invoke(this, new TurnMadeEventArgs());
			}
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
			var matches = FindAllMatches();
			foreach (var match in matches) {
				BlowMatch(match);
			}
		}

		private List<List<Piece>> FindAllMatches()
		{
			var hGrid = new Grid<int>();
			var vGrid = new Grid<int>();
			var hlGrid = new Grid<int>();
			var vlGrid = new Grid<int>();
			var matches = new List<List<Piece>>();
			var intersections = new Queue<IntVector2>();

			FillGrid(hGrid, hlGrid, 0);
			FillGrid(vGrid, vlGrid, 1);

			while (intersections.Any()) {
				var i = intersections.Dequeue();
				var match = TraceMatch(hGrid, i, 0).Union(TraceMatch(vGrid, i, 1)).Distinct().ToList();
				matches.Add(match);
				match.First().SpawnBonus = BonusKind.Bomb;
			}

			for (int x = 0; x < boardConfig.ColumnCount; x++) {
				for (int y = 0; y < boardConfig.RowCount; y++) {
					var p = new IntVector2(x, y);
					var ph = hGrid[p];
					var pv = vGrid[p];
					if (ph >= 3) {
						var match = TraceMatch(hGrid, p, 0);
						matches.Add(match);
						match.First().SpawnBonus = match.Count > 3 ? BonusKind.HorizontalLine : BonusKind.None;
					}
					if (pv >= 3) {
						var match = TraceMatch(vGrid, p, 1);
						matches.Add(match);
						match.First().SpawnBonus = match.Count > 3 ? BonusKind.VerticalLine : BonusKind.None;
					}
				}
			}

			return matches;

			void FillGrid(Grid<int> mGrid, Grid<int> lGrid, int d)
			{
				var dims = new [] { boardConfig.ColumnCount, boardConfig.RowCount };
				for (int i = 0; i < dims[d]; i++) {
					ItemComponent a = null;
					for (int j = 0; j < dims[(d + 1) % 2]; j++) {
						var t = new [] { i, j };
						var p = new IntVector2(t[d], t[(d + 1) % 2]);
						if (j == 0) {
							a = grid[p];
							continue;
						}
						var b = grid[p];
						var pp = p - DeltaFromDirection(d);
						if (CanCombineIntoMatch(a, b)) {
							if (mGrid[pp] == 0) {
								mGrid[pp] = 1;
							}
							mGrid[p] = mGrid[pp] + 1;
						} else {
							if (mGrid[pp] >= 5) {
								var match = TraceMatch(mGrid, pp, d);
								matches.Add(match);
								match.First().SpawnBonus = BonusKind.Lightning;
							} else {
								FillMatchSize(mGrid, lGrid, pp, d);
								TraceIntersections(mGrid, pp, d);
							}
						}
						a = b;
					}
				}
			}

			static bool CanCombineIntoMatch(ItemComponent a, ItemComponent b)
			{
				return a != null
					&& b != null
					&& a.Task == null
					&& b.Task == null
					&& a is Piece pieceA
					&& b is Piece pieceB
					&& pieceA.Kind == pieceB.Kind;
			}

			void FillMatchSize(Grid<int> mGrid, Grid<int> lGrid, IntVector2 s, int d)
			{
				var step = DeltaFromDirection(d);
				var v = mGrid[s];
				while (mGrid[s] != 0) {
					lGrid[s] = v;
					s -= step;
				}
			}

			void TraceIntersections(Grid<int> mGrid, IntVector2 s, int d)
			{
				var step = DeltaFromDirection(d);
				while (mGrid[s] != 0) {
					if (hlGrid[s] >= 3 && vlGrid[s] >= 3) {
						intersections.Enqueue(s);
					}
					s -= step;
				}
			}

			List<Piece> TraceMatch(Grid<int> mGrid, IntVector2 s, int d)
			{
				var step = DeltaFromDirection(d);
				while (mGrid[s] < mGrid[s + step]) {
					s += step;
				}
				var r = new List<Piece>();
				while (mGrid[s] != 0) {
					r.Add((Piece)grid[s]);
					mGrid[s] = 0;
					s -= step;
				}
				return r;
			}

			IntVector2 DeltaFromDirection(int d)
			{
				var td = new[] { 0, 1 };
				return new IntVector2(td[d], td[(d + 1) % 2]);
			}
		}

		private IEnumerable<Piece> FindMatchForItem(Piece piece)
		{
			if (piece.Task != null) {
				return Array.Empty<Piece>();
			}
			HashSet<IntVector2> Visited = new HashSet<IntVector2>();
			Queue<(Piece, Direction)> queue = new Queue<(Piece, Direction)>();
			List<Piece> horizontalMatch = new List<Piece>();
			List<Piece> verticalMatch = new List<Piece>();
			Visited.Add(piece.GridPosition);
			horizontalMatch.Add(piece);
			verticalMatch.Add(piece);
			queue.Enqueue((piece, Direction.Any));
			while (queue.Any()) {
				var (currentItem, direction) = queue.Dequeue();
				for (int i = 0; i < 4; i++) {
					var delta = new IntVector2(
						x: Math.Abs(i - 1) - 1,
						y: -Math.Abs(i - 2) + 1
					);
					Direction nextDirection = delta switch {
						IntVector2 (-1, 0) => Direction.Horizontal,
						IntVector2 (1, 0) => Direction.Horizontal,
						IntVector2 (0, -1) => Direction.Vertical,
						IntVector2 (0, 1) => Direction.Vertical,
						_ => throw new NotImplementedException()
					};
					if (direction != Direction.Any && nextDirection != direction) {
						continue;
					}
					var nextPosition = currentItem.GridPosition + delta;
					if (Visited.Contains(nextPosition)) {
						continue;
					}
					var nextPiece = grid[nextPosition] as Piece;
					Visited.Add(nextPosition);
					if (
						nextPiece != null
						&& nextPiece.Kind == currentItem.Kind
						&& nextPiece.Task == null
					) {
						queue.Enqueue((nextPiece, nextDirection));
						switch (nextDirection) {
							case Direction.Horizontal:
								horizontalMatch.Add(nextPiece);
								break;
							case Direction.Vertical:
								verticalMatch.Add(nextPiece);
								break;
						}
					}
				}
			}
			var r = new List<Piece>();
			if (horizontalMatch.Count >= 3) {
				r.AddRange(horizontalMatch);
			}
			if (verticalMatch.Count >= 3) {
				r.AddRange(verticalMatch);
			}
			return r.Distinct();
		}

		private void BlowMatch(IEnumerable<ItemComponent> match)
		{
			foreach (var item in match) {
				Debug.Assert(item.Task == null);
				item.RunTask(BlowTask(item, DamageKind.Match));
			}
		}

		private IEnumerator<object> BlowTask(ItemComponent item, DamageKind damageKind)
		{
			Animation animation = null;
			switch (item) {
				case Piece piece: {
					animation = damageKind switch {
						DamageKind.Match => piece.AnimateMatch(),
						DamageKind.Line => piece.AnimateBlowByLine(),
						DamageKind.Bomb => piece.AnimateBlowByBomb(),
						DamageKind.Lightning => piece.AnimateBlowByLightning(),
						_ => throw new NotImplementedException(),
					};
					break;
				}
				case Blocker blocker: {
					animation = blocker.BlockerLives switch {
						2 => blocker.AnimateBlockerDamage1(),
						1 => blocker.AnimateBlockerDamage2(),
						0 => throw new NotImplementedException(),
						_ => throw new NotImplementedException(),
					};
					break;
				}
				case Drop drop: {
					yield break;
				}
				case Bonus bonus: {
					animation = null;//bonus.AnimateAct();
					break;
				}
			};

			yield return animation;

			{
				if (item is Piece piece && damageKind == DamageKind.Match) {
					ApplyDamageToAdjacentCells(item.GridPosition, DamageKind.Match);
				}

				if (item is Blocker blocker) {
					blocker.BlockerLives--;
					if (blocker.BlockerLives > 0) {
						yield break;
					}
				}

				if (item is Bonus bonus) {
					var blowBonusAnimation = bonus.AnimateAct();
					if (bonus.BonusKind == BonusKind.HorizontalLine) {
						RunHorizontalLineBonusEffect(item.GridPosition);
						for (int i = 0; i < boardConfig.ColumnCount; i++) {
							var blownItem = grid[new IntVector2(i, item.GridPosition.Y)];
							if (TryBlow(blownItem, DamageKind.Line, out var blowTask)) {
								blownItem.RunTask(blowTask);
							}
						}
					} else if (bonus.BonusKind == BonusKind.VerticalLine) {
						RunVerticalLineBonusEffect(item.GridPosition);
						for (int i = 0; i < boardConfig.RowCount; i++) {
							var blownItem = grid[new IntVector2(item.GridPosition.X, i)];
							if (TryBlow(blownItem, DamageKind.Line, out var blowTask)) {
								blownItem.RunTask(blowTask);
							}
						}
					} else if (bonus.BonusKind == BonusKind.Bomb) {
						for (int i = item.GridPosition.X - 1; i <= item.GridPosition.X + 1; i++) {
							for (int j = item.GridPosition.Y - 1; j <= item.GridPosition.Y + 1; j++) {
								var blownItem = grid[new IntVector2(i, j)];
								if (TryBlow(blownItem, DamageKind.Bomb, out var blowTask)) {
									blownItem.RunTask(blowTask);
								}
							}
						}
					} else if (bonus.BonusKind == BonusKind.Lightning) {
						int[] maxPerKind = new int [boardConfig.AllowedPieces.Max() + 1];
						foreach (var i in items.OfType<Piece>()) {
							maxPerKind[i.Kind]++;
						}
						var kind = Array.IndexOf(maxPerKind, maxPerKind.Max());
						var delay = match3Config.DelayBetweenLightningStrikes;
						foreach (var i in items.OfType<Piece>().ToList()) {
							if (i.Task == null && i.Kind == kind) {
								Debug.Assert(i.SpawnBonus == BonusKind.None);
								// i.SpawnBonus = BonusKind.Lightning;
								if (TryBlow(i, DamageKind.Lightning, out var blowTask)) {
									var fx = CreateLightningBonusEffectPart(item.GridPosition, i.GridPosition);
									if (delay != 0.0f) {
										bonus.AnimateAct();
									}
									i.RunTask(Task.Sequence(
										WaitForAnimationTask(fx.RunAnimation("Start", "Act")),
										blowTask
									));
									if (delay != 0.0f) {
										yield return delay;
									}
								}
							}
						}
					}

					yield return blowBonusAnimation;

					bool TryBlow(ItemComponent i, DamageKind d, out IEnumerator<object> blowTask)
					{
						blowTask = null;
						if (i == null || i.Task != null) {
							return false;
						}
						blowTask = BlowTask(i, d);
						return true;
					}

					static IEnumerator<object> WaitForAnimationTask(Animation animation)
					{
						yield return animation;
					}
				}
			}

			items.Remove(item);
			item.Kill();
			item.Owner.UnlinkAndDispose();
			{
				if (item is Piece piece && piece.SpawnBonus != BonusKind.None) {
					CreateBonus(item.GridPosition, piece.SpawnBonus);
				}
			}
		}

		private void ApplyDamageToAdjacentCells(IntVector2 position, DamageKind damageKind)
		{
			for (int i = 0; i < 4; i++) {
				var delta = new IntVector2(
					x: Math.Abs(i - 1) - 1,
					y: -Math.Abs(i - 2) + 1
				);
				ApplyDamage(position + delta, damageKind);
			}
		}

		private void ApplyDamage(IntVector2 position, DamageKind damageKind)
		{
			var item = grid[position];
			if (item == null || item.Task != null) {
				return;
			}
			// Only blockers accept damage.
			if (item is Blocker) {
				item.RunTask(BlowTask(item, damageKind));
			}
		}

		private void RunHorizontalLineBonusEffect(IntVector2 position)
		{
			var leftFx = CreateLineBonusEffectPart(position, 2, position.X + 1);
			var rightFx = CreateLineBonusEffectPart(position, 0, boardConfig.ColumnCount - position.X);
			topLevelContainer.Tasks.Add(RunBonusAnimationTask(leftFx, rightFx));
		}

		private void RunVerticalLineBonusEffect(IntVector2 position)
		{
			var upFx = CreateLineBonusEffectPart(position, 3, position.Y + 1);
			var downFx = CreateLineBonusEffectPart(position, 1, boardConfig.RowCount - position.Y);
			topLevelContainer.Tasks.Add(RunBonusAnimationTask(upFx, downFx));
		}

		private Widget CreateLineBonusEffectPart(IntVector2 position, int direction, int length)
		{
			var fx = lineBonusFxTemplate.Clone<Widget>();
			fx.Rotation = 90 * direction;
			// TODO: fx container
			itemContainer.Nodes.Insert(0, fx);
			fx.Position = match3Config.GridPositionToWidgetPosition(position);
			fx.Width *= length;
			if (ICheatManager.Instance.DebugMatch3) {
				fx.CompoundPostPresenter.Add(new WidgetBoundsPresenter(Color4.Blue, 2.0f));
			}
			return fx;
		}

		private IEnumerator<object> RunBonusAnimationTask(params Widget[] effects)
		{
			var animations = effects.Select(e => e.RunAnimation("Start", "Act")).ToList();
			while (true) {
				if (!animations.Any(a => a.IsRunning)) {
					break;
				}
				yield return null;
			}
			foreach (var w in effects) {
				w.UnlinkAndDispose();
			}
		}

		private Widget CreateLightningBonusEffectPart(IntVector2 fromPosition, IntVector2 toPosition)
		{
			var fx = lightningBonusFxTemplate.Clone<Widget>();
			// TODO: fx container
			itemContainer.Nodes.Insert(0, fx);
			fx.Position = match3Config.GridPositionToWidgetPosition(fromPosition);
			var spline = fx.Find<Spline>("Spline");
			var endPoint = spline.Find<SplinePoint>("End");
			var t = itemContainer.CalcTransitionToSpaceOf(spline);
			var endPosition = match3Config.GridPositionToWidgetPosition(toPosition);
			endPosition = t * endPosition;
			endPoint.Position = endPosition / spline.Size;
			return fx;
		}
	}

	public static class IntVector2Extensions
	{
		public static void Deconstruct(this IntVector2 v, out int x, out int y)
		{
			x = v.X;
			y = v.Y;
		}
	}
}

