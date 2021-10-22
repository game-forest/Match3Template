using Lime;
using System;

namespace Match3Template.Types
{
	public class CutoutCell : ItemComponent
	{
		public CutoutCell(
			Node itemWidget,
			IntVector2 gridPosition,
			Action<ItemComponent, IntVector2> onSetGridPosition,
			Action<ItemComponent> onKill
		) : base(itemWidget, gridPosition, onSetGridPosition, onKill) {
		}
	}
}

