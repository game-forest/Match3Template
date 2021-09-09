using Lime;
using System.Collections.Generic;

namespace Match3Template.Types
{
public class Grid
	{
		private readonly Dictionary<IntVector2, ItemComponent> state = new Dictionary<IntVector2, ItemComponent>();

		public ItemComponent this[IntVector2 position]
		{
			get
			{
				if (!state.ContainsKey(position)) {
					state.Add(position, null);
				}
				return state[position];
			}

			set
			{
				state[position] = value;
			}
		}
	}
}

