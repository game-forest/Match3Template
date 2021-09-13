using Lime;
using System.Collections.Generic;
using Yuzu;

namespace Match3Template.Types
{
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

		[YuzuMember]
		public List<int> AllowedPieces { get; private set; } = new List<int> { 0, 1, 2, 3, 4 };

		[YuzuMember]
		public int DropCount { get; set; } = 4;

		[YuzuMember]
		public int BlockerCount { get; set; } = 8;

		[YuzuMember]
		public List<IntVector2> CutoutCells { get; private set; } = new List<IntVector2>();

		[YuzuMember]
		public List<IntVector2> DropCells { get; private set; } = new List<IntVector2>();

		[YuzuMember]
		public List<IntVector2> BlockerCells { get; private set; } = new List<IntVector2>();

		[YuzuMember]
		public List<(IntVector2, int)> PieceCells { get; private set; } = new List<(IntVector2, int)>();
	}
}

