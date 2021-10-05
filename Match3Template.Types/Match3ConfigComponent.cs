using Lime;
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
		public float OneCellFallTime { get; set; } = 0.1f;

		[YuzuMember]
		public float PieceReturnOnTouchEndTime { get; set; } = 0.1f;

		[YuzuMember]
		public float InputDetectionLength { get; set; } = 10f;

		[YuzuMember]
		public float DragPercentOfPieceSizeRequiredForSwapActivation { get; set; } = 30.0f;

		[YuzuMember]
		public bool WaitForAnimateDropDownLand { get; set; } = true;

		[YuzuMember]
		public bool WaitForAnimateDropDownFall { get; set; } = true;

		[YuzuMember]
		public bool SwapBackOnNonMatchingSwap { get; set; } = false;

		[YuzuMember]
		public float DiagonalSwipeDeadZoneAngle { get; set; } = 30.0f;

		[YuzuMember]
		public float DelayBetweenLightningStrikes { get; set; } = 0.1f;

		[YuzuMember]
		public Size CellSize { get; set; } = new Size(90, 90);

		public Vector2 GridPositionToWidgetPosition(IntVector2 position)
		{
			var size = (Vector2)CellSize;
			return (Vector2)position * size + size * 0.5f;
		}
	}
}

