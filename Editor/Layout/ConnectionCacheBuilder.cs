using System.Collections.Generic;
using Nonatomic.VSM.Graph;

namespace Nonatomic.VSM.Editor.Layout
{
	public static class ConnectionCacheBuilder
	{
		public static Dictionary<string, List<Connection>> Build(
			StateMachineGraph graph,
			bool isOutgoing)
		{
			var cache = new Dictionary<string, List<Connection>>();

			foreach (var node in graph.GetAllNodes())
			{
				cache[node.Id] = new List<Connection>();
			}

			foreach (var connection in graph.Connections)
			{
				var key = isOutgoing ? connection.SourceNodeId : connection.TargetNodeId;
				cache[key].Add(connection);
			}

			return cache;
		}
	}
}
