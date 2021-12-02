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

		private readonly Widget boardContainer;
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

		internal int GetTurnCount() => boardConfig.TurnCount;

		internal void SetTurnCount(int turnCount) => boardConfig.TurnCount = turnCount;

		internal int GetDropCount() => boardConfig.DropCount;

		public static Board CreateBoard(Widget root)
		{
			var match3Config = root.SelfAndDescendants
				.FirstOrDefault(n => n.Components.Contains<Match3ConfigComponent>())
				?.Components
				.Get<Match3ConfigComponent>();

			var boardContainer = root.SelfAndDescendants
				.FirstOrDefault(n => n.Components.Contains<BoardConfigComponent>())
				as Widget;

			var boardConfig = boardContainer?.Components.Get<BoardConfigComponent>();

			boardContainer = boardContainer["BoardContainer"];

			return new Board(boardContainer, boardConfig, match3Config);
		}

		public Board(Widget boardContainer, BoardConfigComponent boardConfig, Match3ConfigComponent match3Config)
		{
			this.boardContainer = boardContainer;
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

			bgContainer.Width = boardConfig.ColumnCount * match3Config.CellSize;
			bgContainer.Height = boardConfig.RowCount * match3Config.CellSize;
			this.boardContainer.Nodes.Insert(0, bgContainer);
			bgContainer.CenterOnParent();

			itemContainer.Width = boardConfig.ColumnCount * match3Config.CellSize;
			itemContainer.Height = boardConfig.RowCount * match3Config.CellSize;
			this.boardContainer.Nodes.Insert(0, itemContainer);
			itemContainer.CenterOnParent();

			this.boardContainer.Tasks.Add(this.Update);
			containerBoundsPresenter = new WidgetBoundsPresenter(Color4.Red, 2.0f);
			this.boardContainer.Tasks.Add(this.CheckCheatsTask);

			this.boardContainer.Tasks.Add(Task.Repeat(() => {
				UpdateBoardScale();
				return true;
			}));
			UpdateBoardScale();
		}

		void UpdateBoardScale()
		{
			var widthAspect = boardContainer.Width / itemContainer.Width;
			var heightAspect = boardContainer.Height / itemContainer.Height;
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
				ProcessMatches();
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

		private static readonly IntVector2[] directionVectors =	{
			IntVector2.Right,
			IntVector2.Down,
			IntVector2.Left,
			IntVector2.Up
		};

		[Flags]
		private enum Direction
		{
			Right = 1,
			Down = 2,
			Left = 4,
			Up = 8,
			Horizontal = Right | Left,
			Vertical = Up | Down,
			Any = Horizontal | Vertical,
		}

		private static readonly Direction[] directions = {
			Direction.Right, Direction.Down, Direction.Left, Direction.Up
		};

		private int swapIndex;

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

				if (!boardConfig.AllowedPieces.Any()) {
					boardConfig.AllowedPieces.AddRange(new [] { 0, 1, 2, 3, 4 });
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
			if (!boardConfig.AllowedPieces.Any()) {
				boardConfig.AllowedPieces.AddRange(items.OfType<Piece>().Select(p => p.Kind).Distinct());
			}
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
			var piece = new Piece(w, gridPosition, Item_SetGridPosition, Item_Kill);
			SetupItem(piece, w, gridPosition);
			piece.Kind = kind;
			return piece;
		}

		private Blocker CreateBlocker(IntVector2 gridPosition)
		{
			var w = blockerTemplate.Clone<Widget>();
			var blocker = new Blocker(w, gridPosition, Item_SetGridPosition, Item_Kill);
			SetupItem(blocker, w, gridPosition);
			return blocker;
		}

		private Drop CreateDrop(IntVector2 gridPosition)
		{
			var w = dropTemplate.Clone<Widget>();
			var drop = new Drop(w, gridPosition, Item_SetGridPosition, Item_Kill);
			SetupItem(drop, w, gridPosition);
			return drop;
		}

		private CutoutCell CreateCutoutCell(IntVector2 gridPosition)
		{
			var w = cutoutCellTemplate.Clone<Widget>();
			var cutoutCell = new CutoutCell(w, gridPosition, Item_SetGridPosition, Item_Kill);
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
			var bonus = new Bonus(w, gridPosition, Item_SetGridPosition, Item_Kill) {
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
			itemContainer.AddNode(widget);
			items.Add(item);
			item.AnimateIdle();
		}
		void Item_SetGridPosition(ItemComponent item, IntVector2 gridPosition)
		{
			grid[item.GridPosition] = null;
			System.Diagnostics.Debug.Assert(grid[gridPosition] == null);
			grid[gridPosition] = item;
		}

		void Item_Kill(ItemComponent item)
		{
			grid[item.GridPosition] = null;
			items.Remove(item);
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

		private IEnumerator<object> InputTask(ItemComponent item, int touchIndex)
		{
			var input = Window.Current.Input;
			var touchPosition0 = input.GetTouchPosition(touchIndex);
			var originalItemPosition = item.Owner.AsWidget.Position;
			Vector2 touchDelta;
			do {
				yield return null;
				touchDelta = input.GetTouchPosition(touchIndex) - touchPosition0;
			} while (
				touchDelta.Length < match3Config.InputDetectionLength && input.IsTouching(touchIndex)
			);
			if (!TryGetProjectionAxis(touchDelta, out var projectionAxis)) {
				if (item is Bonus bonus) {
					TurnMade?.Invoke(this, new TurnMadeEventArgs());
					yield return BlowTask(item, DamageKind.Match);
				}
				yield break;
			}
			var nextItem = grid[item.GridPosition + projectionAxis];
			if (nextItem == null || nextItem.Task != null || !nextItem.CanMove) {
				yield break;
			}
			item.Owner.Parent.Nodes.Swap(0, item.Owner.Parent.Nodes.IndexOf(item.Owner));
			var swapActivationDistance = match3Config.DragPercentOfPieceSizeRequiredForSwapActivation
				* 0.01 * match3Config.CellSize;
			var nextItemOriginalPosition = nextItem.Owner.AsWidget.Position;
			bool syncFinished = false;
			int swapPhase = 0;
			Func<bool> syncPosition = () => {
				var delta = (originalItemPosition - item.Owner.AsWidget.Position);
				nextItem.Owner.AsWidget.Position = nextItemOriginalPosition + delta;
				if (swapPhase == 0) {
					nextItem.ApplyAnimationPercent(delta.Length / match3Config.CellSize, "Swap", "Backward");
				} else {
					nextItem.ApplyAnimationPercent(delta.Length / match3Config.CellSize, "Swap", "Forward");
				}
				return !syncFinished;
			};
			nextItem.RunTask(Task.Repeat(syncPosition));
			float projectionAmount = 0.0f;
			while (input.IsTouching(touchIndex)) {
				touchDelta = input.GetTouchPosition(touchIndex) - touchPosition0;
				projectionAmount = Vector2.DotProduct((Vector2)projectionAxis, touchDelta);
				projectionAmount = Mathf.Clamp(
					value: 1.0f / match3Config.BoardScale * projectionAmount,
					min: 0,
					max: match3Config.CellSize
				);
				item.Owner.AsWidget.Position = ((Vector2)item.GridPosition + Vector2.Half) * match3Config.CellSize
					+ projectionAmount * (Vector2)projectionAxis;
				item.ApplyAnimationPercent(projectionAmount / match3Config.CellSize, "Swap", "Forward");
				yield return null;
			}
			bool turnMade = false;
			item.AnimateUnselect();
			var timeRatioPassed = projectionAmount / match3Config.CellSize;
			var timeRatioLeft = 1.0f - timeRatioPassed;
			if (projectionAmount > swapActivationDistance) {
				SwapItems(item, nextItem);
				yield return item.MoveTo(item.GridPosition, timeRatioLeft * match3Config.SwapTime, (t) => {
					item.ApplyAnimationPercent(timeRatioPassed + t * timeRatioLeft, "Swap", "Forward");
				});
				item.SwapIndex = swapIndex;
				nextItem.SwapIndex = swapIndex;
				swapIndex++;
				if (item is Bonus bonus) {
					TurnMade?.Invoke(this, new TurnMadeEventArgs());
					syncFinished = true;
					yield return BlowTask(item, DamageKind.Match);
					yield break;
				} else if (match3Config.SwapBackOnNonMatchingSwap) {
					bool success = false;
					var matches = FindMatches();
					foreach (var match in matches) {
						foreach (var p in match.SelectMany(i => i).Distinct()) {
							if (p == item || p == nextItem) {
								if (match.SelectMany(i => i).Except(new[] { item, nextItem }).All(i => i.Task == null)) {
									success = true;
								}
							}
						}
					}
					if (!success) {
						swapPhase = 1;
						yield return match3Config.UnsuccessfulSwapDelay;
						var i0 = item.Owner.Parent.Nodes.IndexOf(item.Owner);
						var i1 = nextItem.Owner.Parent.Nodes.IndexOf(nextItem.Owner);
						item.Owner.Parent.Nodes.Swap(i0, i1);
						SwapItems(item, nextItem);
						yield return item.MoveTo(item.GridPosition, match3Config.SwapTime, (t) => {
							item.ApplyAnimationPercent(t, "Swap", "Backward");
						});
					} else {
						turnMade = true;
					}
				} else {
					yield return item.MoveTo(item.GridPosition, match3Config.SwapTime * timeRatioLeft, (t) => {
						item.ApplyAnimationPercent(timeRatioPassed + t * timeRatioLeft, "Swap", "Forward");
					});
					turnMade = true;
				}
			} else {
				yield return item.MoveTo(item.GridPosition, match3Config.SwapTime * timeRatioPassed, (t) => {
					item.ApplyAnimationPercent((1.0f - t) * timeRatioPassed, "Swap", "Forward");
				});
			}
			syncFinished = true;
			if (turnMade) {
				TurnMade?.Invoke(this, new TurnMadeEventArgs());
			}

			bool TryGetProjectionAxis(Vector2 touchDelta, out IntVector2 projectionAxis)
			{
				projectionAxis = default;
				if (touchDelta.Length < match3Config.InputDetectionLength) {
					return false;
				}
				var angle = Mathf.Wrap360(Mathf.RadToDeg * Mathf.Atan2(touchDelta));
				int sectorIndex = 3 - ((int)((angle + 45.0f) / 90)) % 4;
				projectionAxis = new IntVector2(Math.Abs(sectorIndex - 1) - 1, 1 - Math.Abs(sectorIndex - 2));
				var halfDeadAngle = match3Config.DiagonalSwipeDeadZoneAngle * 0.5f;
				if (angle % 90.0f > 45.0f - halfDeadAngle && angle % 90.0f < 45.0f + halfDeadAngle) {
					return false;
				}
				return true;
			}

			static void SwapItems(ItemComponent lhs, ItemComponent rhs)
			{
				var t1 = lhs.GridPosition;
				var t2 = rhs.GridPosition;
				lhs.GridPosition = new IntVector2(int.MaxValue, int.MaxValue);
				rhs.GridPosition = new IntVector2(int.MinValue, int.MinValue);
				lhs.GridPosition = t2;
				rhs.GridPosition = t1;
			}
		}

		private void ProcessMatches()
		{
			var matches = FindMatches();
			foreach (var matchList in matches) {
				bool hasTask = false;
				foreach (var match in matchList) {
					if (match.Any(p => p.Task != null)) {
						hasTask = true;
						break;
					}
				}
				if (hasTask) {
					break;
				}
				var distinctPieces = matchList
					.SelectMany(i => i)
					.Distinct()
					.OrderBy(i => -i.SwapIndex)
					.ToList();
				var hasFivePlusMatch = matchList.Any(i => i.Count >= 5);
				var hasIntersections = matchList.Count > 1;
				if (hasFivePlusMatch) {
					distinctPieces.First().SpawnBonus = BonusKind.Lightning;
				} else if (hasIntersections) {
					distinctPieces.First().SpawnBonus = BonusKind.Bomb;
				} else {
					foreach (var match in matchList) {
						if (match.Count() == 4) {
							match.Sort((a, b) => b.SwapIndex - a.SwapIndex);
							match.First().SpawnBonus = match.All(i => i.GridPosition.X == match.First().GridPosition.X)
								? BonusKind.HorizontalLine : BonusKind.VerticalLine;
						}
					}
				}
				BlowMatch(distinctPieces);
			}
		}

		private List<List<List<Piece>>> FindMatches()
		{
			var matches = new List<List<List<Piece>>>();
			var matchMap = new Grid<List<List<Piece>>>();
			Pass(0);
			Pass(1);
			return matches;

			void Pass(int passIndex)
			{
				int[] boardSize = { boardConfig.ColumnCount, boardConfig.RowCount };
				for (int i = 0; i <= boardSize[passIndex]; i++) {
					Piece a = null;
					var matchLength = 1;
					List<Piece> match = new List<Piece>();
					for (int j = -1; j <= boardSize[(passIndex + 1) % 2]; j++) {
						var (x, y) = passIndex == 0 ? (i, j) : (j, i);
						var p = new IntVector2(x, y);
						var b = grid[p] as Piece;
						if (a?.CanMatch(b) ?? false) {
							matchLength++;
						} else {
							if (matchLength >= 3) {
								var newMatch = match.ToList();
								var matchList = new List<List<Piece>>();
								matchList.Add(newMatch);
								var intersectedMatchLists = match
									.Select(i => matchMap[i.GridPosition])
									.Where(i => i != null)
									.Distinct()
									.ToList();
								foreach (var ml in intersectedMatchLists) {
									matches.Remove(ml);
									foreach (var m in ml) {
										matchList.Add(m);
									}
								}
								foreach (var m in matchList) {
									foreach (var piece in m) {
										matchMap[piece.GridPosition] = matchList;
									}
								}
								matches.Add(matchList);
							}
							matchLength = 1;
							match.Clear();
						}
						match.Add(b);
						a = b;
					}
				}
			}
		}

		private void BlowMatch(IEnumerable<Piece> match)
		{
			foreach (var piece in match) {
				Debug.Assert(piece.Task == null);
				piece.RunTask(BlowTask(piece, DamageKind.Match));
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
									i.RunTask(
										(new[] {
											WaitForAnimationTask(fx.RunAnimation("Start", "Act")),
											blowTask,
										}).Cast<object>()
										.GetEnumerator()
									);
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
			boardContainer.Tasks.Add(RunBonusAnimationTask(leftFx, rightFx));
		}

		private void RunVerticalLineBonusEffect(IntVector2 position)
		{
			var upFx = CreateLineBonusEffectPart(position, 3, position.Y + 1);
			var downFx = CreateLineBonusEffectPart(position, 1, boardConfig.RowCount - position.Y);
			boardContainer.Tasks.Add(RunBonusAnimationTask(upFx, downFx));
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

