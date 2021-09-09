using Lime;
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
	}
}

