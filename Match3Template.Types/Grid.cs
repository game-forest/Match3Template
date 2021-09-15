using Lime;
using System.Collections.Generic;

namespace Match3Template.Types
{
	public class Grid<T>
	{
		private readonly Dictionary<IntVector2, T> state = new Dictionary<IntVector2, T>();

		public T this[IntVector2 position]
		{
			get
			{
				if (!state.ContainsKey(position)) {
					state.Add(position, default);
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

