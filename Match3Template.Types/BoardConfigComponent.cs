using Lime;
using System;
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
		public List<int> AllowedPieces { get; private set; } = new List<int> { };

		[YuzuMember]
		public int DropCount { get; set; } = 0;

		[YuzuMember]
		public int BlockerCount { get; set; } = 0;

		[YuzuMember]
		public int TurnCount { get; set; } = 0;

		[YuzuMember]
		public bool PreFillBoard { get; set; } = true;

		[TangerineFileProperty(new[] { "txt" })]
		[YuzuMember]
		public string LevelFileName { get; set; }
	}
}

