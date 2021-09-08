using Lime;
using System;
using System.Collections.Generic;
using System.Linq;
using Yuzu;

namespace Match3Template.Types
{
	[TangerineRegisterComponent]
	[AllowOnlyOneComponent]
	[TangerineMenuPath("Config/")]
	[TangerineTooltip("Configure match3 parameters.")]
	[TangerineAllowedParentTypes(typeof(Frame))]
	public class Match3ConfigComponent : NodeComponent
	{
		[YuzuMember]
		public float OneCellFallTime { get; set; } = 0.5f;
	}

	[TangerineRegisterComponent]
	[AllowOnlyOneComponent]
	[TangerineMenuPath("Config/")]
	[TangerineTooltip("Configure board parameters.")]
	[TangerineAllowedParentTypes(typeof(Frame))]
	public class BoardConfigComponent : NodeComponent
	{
		[YuzuMember]
		public int RowCount { get; set; } = 8;

		[YuzuMember]
		public int ColumnCount { get; set; } = 4;
	}

	public class ItemComponent : NodeComponent
	{
		private BoardConfigComponent boardConfig;
		private Match3ConfigComponent match3Config;
		private Widget widget;

		public int Kind { get; set; }

		private IntVector2 gridPosition;

		public int Row => GridPosition.Y;

		public int Column => GridPosition.X;

		public IntVector2 GridPosition
		{
			get => gridPosition;
			set
			{
				gridPosition = value;
				widget.Position = WidgetPosition(value.Y, value.X);
			}
		}

		public bool Moving { get; private set; }

		public ItemComponent(BoardConfigComponent boardConfig, Match3ConfigComponent match3Config)
		{
			this.boardConfig = boardConfig;
			this.match3Config = match3Config;
		}

		public void SetPieceKind(int kind)
		{
			var kindAnimation = Owner.Animations.Find("Color");
			var marker = kindAnimation.Markers[kind];
			Owner.RunAnimation(marker.Id, kindAnimation.Id);
			Kind = kind;
		}

		public void Show()
		{
			Owner.RunAnimation("Start", "Show");
		}

		public void DropDownFall()
		{
			Owner.RunAnimation("Fall", "DropDown");
		}

		public void DropDownLand()
		{
			Owner.RunAnimation("Land", "DropDown");
		}

		public void Select()
		{
			Owner.RunAnimation("Select", "Selection");
		}

		public void Deselect()
		{
			Owner.RunAnimation("Unselect", "Selection");
		}

		public Animation Match()
		{
			var animation = Owner.Animations.Find("Match");
			Owner.RunAnimation("Start", "Match");
			return animation;
		}

		public IEnumerator<object> MoveTo(IntVector2 gridPosition, Grid grid)
		{
			return MoveTo(gridPosition.Y, gridPosition.X, grid);
		}

		public IEnumerator<object> MoveTo(int row, int column, Grid grid)
		{
			Moving = true;
			var nextGridPosition = new IntVector2(column, row);
			grid[nextGridPosition] = GridState.Occupied;
			var p0 = widget.Position;
			var p1 = WidgetPosition(row, column);
			var t = match3Config.OneCellFallTime;
			bool finished = false;
			while (true) {
				t -= Task.Current.Delta;
				if (t < 0.0f) {
					t = 0.0f;
					Moving = false;
					finished = true;
					grid[GridPosition] = GridState.Free;
					GridPosition = nextGridPosition;
				}
				widget.Position = Mathf.Lerp(1.0f - t / match3Config.OneCellFallTime, p0, p1);
				if (finished) {
					yield break;
				}
				yield return null;
			}
		}

		private Vector2 WidgetPosition(int row, int column)
		{
			return new Vector2(column, row) * widget.Size + widget.Size * 0.5f;
		}

		protected override void OnOwnerChanged(Node oldOwner)
		{
			base.OnOwnerChanged(oldOwner);
			widget = Owner as Widget;
		}
	}

	public enum GridState
	{
		Free,
		Occupied,
	}

	public class Grid
	{
		private Dictionary<IntVector2, GridState> state = new Dictionary<IntVector2, GridState>();

		public GridState this[IntVector2 position]
		{
			get
			{
				if (!state.ContainsKey(position)) {
					state.Add(position, GridState.Free);
				}
				return state[position];
			}

			set
			{
				state[position] = value;
			}
		}
	}

	public class Board
	{
		private Widget topLevelContainer;
		private Frame itemContainer;
		private BoardConfigComponent boardConfig;
		private Match3ConfigComponent match3Config;
		private Widget boardTemplateScene;
		private Widget pieceTemplate;
		private Grid grid = new Grid();
		private List<ItemComponent> items = new List<ItemComponent>();
		private static List<ItemComponent> itemPool = new List<ItemComponent>();

		private int GetPieceKindCount()
		{
			var kindAnimation = pieceTemplate.Animations.Find("Color");
			return kindAnimation.Markers.Count;
		}

		private ItemComponent CreatePiece(IntVector2 gridPosition, int kind)
		{
			if (items.Where(i => i.Row == gridPosition.Y && i.Column == gridPosition.X).Any()) {
				throw new InvalidOperationException();
			}
			var w = pieceTemplate.Clone<Widget>();
			ItemComponent piece;
			w.Components.Add(piece = new ItemComponent(boardConfig, match3Config));
			piece.SetPieceKind(kind);
			itemContainer.AddNode(w);
			piece.GridPosition = gridPosition;
			grid[piece.GridPosition] =  GridState.Occupied;
			items.Add(piece);
			return piece;
		}

		public Board(Widget boardWidget, BoardConfigComponent boardConfig, Match3ConfigComponent match3Config)
		{
			this.topLevelContainer = boardWidget;
			this.boardConfig = boardConfig;
			this.match3Config = match3Config;
			boardTemplateScene = Node.Load<Frame>("Shell/Board");
			pieceTemplate = boardTemplateScene["MultiMarble"] as Frame;
			itemContainer = new Frame {
				ClipChildren = ClipMethod.ScissorTest,
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
			var blowTime = 1.0f;
			while (true) {
				for (int i = 0; i < boardConfig.ColumnCount; i++) {
					var gridPosition = new IntVector2(i, -1);
					if (grid[gridPosition] != GridState.Occupied) {
						int pieceKind = Mathf.RandomInt(0, GetPieceKindCount() - 1);
						var piece = CreatePiece(gridPosition, pieceKind);
						piece.Show();
					}
				}
				foreach (var item in items) {
					var belowPosition = new IntVector2(item.Column, item.Row + 1);
					if (grid[belowPosition] != GridState.Occupied && belowPosition.Y < boardConfig.RowCount && !item.Moving) {
						topLevelContainer.Tasks.Add(Move(item, belowPosition));
					} else {
						item.DropDownLand();
					}
				}

				blowTime -= Task.Current.Delta;
				if (blowTime < 0.0f) {
					blowTime = 1.0f;
					var item = Mathf.RandomItem(items.Where(i => !i.Moving).ToList());
					items.Remove(item);
					topLevelContainer.Tasks.Add(Blow(item));
				}
				yield return null;
			}
		}

		private IEnumerator<object> Blow(ItemComponent item)
		{
			var a = item.Match();
			while (a.IsRunning) {
				yield return null;
			}
			grid[item.GridPosition] = GridState.Free;
			//items.Remove(item);
			item.Owner.UnlinkAndDispose();
		}

		private IEnumerator<object> Move(ItemComponent item, IntVector2 toGridPosition)
		{
			item.DropDownFall();
			yield return item.MoveTo(toGridPosition, grid);
		}

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
	}
}

