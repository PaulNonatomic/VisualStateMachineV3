using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Graph;

namespace Nonatomic.VSM.Editor.Analysis
{
	/// <summary>
	/// Detects potential same-frame transition loops in state machine graphs.
	/// </summary>
	public static class TransitionLoopDetector
	{
		/// <summary>
		/// Maximum depth to search for loops.
		/// </summary>
		private const int MaxSearchDepth = 50;

		/// <summary>
		/// Analyze a graph for potential transition loops.
		/// </summary>
		public static LoopAnalysisResult Analyze(StateMachineGraph graph)
		{
			var result = new LoopAnalysisResult();

			if (graph == null)
			{
				return result;
			}

			// Build adjacency list for transitions
			var adjacencyList = BuildTransitionAdjacencyList(graph);

			// Check each state node as a potential loop starting point
			foreach (var node in graph.GetAllNodes())
			{
				if (node.NodeType != GraphNodeType.State)
				{
					continue;
				}

				var loops = FindLoopsFromNode(node.Id, adjacencyList, graph);
				result.DetectedLoops.AddRange(loops);
			}

			// Analyze instant transition chains
			var instantChains = FindInstantTransitionChains(graph);
			result.InstantTransitionChains.AddRange(instantChains);

			return result;
		}

		/// <summary>
		/// Check if a specific path contains a potential loop.
		/// </summary>
		public static bool IsPathAtRisk(StateMachineGraph graph, List<string> path)
		{
			if (path == null || path.Count < 2)
			{
				return false;
			}

			// Check if path forms a cycle
			var seen = new HashSet<string>();
			foreach (var nodeId in path)
			{
				if (seen.Contains(nodeId))
				{
					return true;
				}

				seen.Add(nodeId);
			}

			return false;
		}

		private static Dictionary<string, List<TransitionInfo>> BuildTransitionAdjacencyList(StateMachineGraph graph)
		{
			var adjacency = new Dictionary<string, List<TransitionInfo>>();

			foreach (var node in graph.GetAllNodes())
			{
				adjacency[node.Id] = new List<TransitionInfo>();
			}

			foreach (var connection in graph.Connections)
			{
				if (connection.ConnectionType != ConnectionType.Transition)
				{
					continue;
				}

				if (!adjacency.ContainsKey(connection.SourceNodeId))
				{
					adjacency[connection.SourceNodeId] = new List<TransitionInfo>();
				}

				adjacency[connection.SourceNodeId].Add(new TransitionInfo
				{
					TargetNodeId = connection.TargetNodeId,
					TransitionTime = connection.TransitionTime,
					IsInstant = connection.TransitionTime <= 0
				});
			}

			return adjacency;
		}

		private static List<DetectedLoop> FindLoopsFromNode(
			string startNodeId,
			Dictionary<string, List<TransitionInfo>> adjacency,
			StateMachineGraph graph)
		{
			var loops = new List<DetectedLoop>();
			var visited = new HashSet<string>();
			var path = new List<string>();

			DFS(startNodeId, adjacency, visited, path, loops, graph, 0);

			return loops;
		}

		private static void DFS(
			string nodeId,
			Dictionary<string, List<TransitionInfo>> adjacency,
			HashSet<string> visited,
			List<string> path,
			List<DetectedLoop> loops,
			StateMachineGraph graph,
			int depth)
		{
			if (depth > MaxSearchDepth)
			{
				return;
			}

			if (visited.Contains(nodeId))
			{
				// Found a loop
				var loopStartIndex = path.IndexOf(nodeId);
				if (loopStartIndex >= 0)
				{
					var loopPath = path.Skip(loopStartIndex).ToList();
					loopPath.Add(nodeId);

					// Check if all transitions in loop are instant
					var allInstant = IsAllInstantTransitions(loopPath, adjacency);

					loops.Add(new DetectedLoop
					{
						NodeIds = loopPath,
						IsAllInstant = allInstant,
						Severity = allInstant ? LoopSeverity.Critical : LoopSeverity.Warning
					});
				}

				return;
			}

			visited.Add(nodeId);
			path.Add(nodeId);

			if (adjacency.TryGetValue(nodeId, out var transitions))
			{
				foreach (var transition in transitions)
				{
					DFS(transition.TargetNodeId, adjacency, visited, path, loops, graph, depth + 1);
				}
			}

			path.RemoveAt(path.Count - 1);
			visited.Remove(nodeId);
		}

		private static bool IsAllInstantTransitions(List<string> loopPath, Dictionary<string, List<TransitionInfo>> adjacency)
		{
			for (var i = 0; i < loopPath.Count - 1; i++)
			{
				var fromNode = loopPath[i];
				var toNode = loopPath[i + 1];

				if (adjacency.TryGetValue(fromNode, out var transitions))
				{
					var transition = transitions.FirstOrDefault(t => t.TargetNodeId == toNode);
					if (transition != null && !transition.IsInstant)
					{
						return false;
					}
				}
			}

			return true;
		}

		private static List<InstantTransitionChain> FindInstantTransitionChains(StateMachineGraph graph)
		{
			var chains = new List<InstantTransitionChain>();
			var instantConnections = graph.Connections
				.Where(c => c.ConnectionType == ConnectionType.Transition && c.TransitionTime <= 0)
				.ToList();

			if (instantConnections.Count == 0)
			{
				return chains;
			}

			// Find chains of consecutive instant transitions
			var visited = new HashSet<string>();

			foreach (var connection in instantConnections)
			{
				if (visited.Contains(connection.SourceNodeId))
				{
					continue;
				}

				var chain = TraceInstantChain(connection.SourceNodeId, instantConnections, visited);
				if (chain.NodeIds.Count > 2)
				{
					chains.Add(chain);
				}
			}

			return chains;
		}

		private static InstantTransitionChain TraceInstantChain(
			string startNodeId,
			List<Connection> instantConnections,
			HashSet<string> visited)
		{
			var chain = new InstantTransitionChain();
			var currentNode = startNodeId;
			var seenInChain = new HashSet<string>();

			while (!string.IsNullOrEmpty(currentNode) && !seenInChain.Contains(currentNode))
			{
				chain.NodeIds.Add(currentNode);
				seenInChain.Add(currentNode);
				visited.Add(currentNode);

				var nextConnection = instantConnections.FirstOrDefault(c => c.SourceNodeId == currentNode);
				currentNode = nextConnection?.TargetNodeId;
			}

			// Check if chain forms a cycle
			if (!string.IsNullOrEmpty(currentNode) && seenInChain.Contains(currentNode))
			{
				chain.NodeIds.Add(currentNode);
				chain.FormsCycle = true;
			}

			chain.Length = chain.NodeIds.Count;
			return chain;
		}

		/// <summary>
		/// Get warning messages for display in the editor.
		/// </summary>
		public static List<GraphValidationIssue> GetValidationIssues(LoopAnalysisResult result, StateMachineGraph graph)
		{
			var issues = new List<GraphValidationIssue>();

			foreach (var loop in result.DetectedLoops)
			{
				var severity = loop.Severity == LoopSeverity.Critical
					? GraphValidationSeverity.Error
					: GraphValidationSeverity.Warning;

				var nodeNames = loop.NodeIds.Select(id =>
				{
					var node = graph.GetNode(id);
					return node?.DisplayName ?? id;
				});

				var message = loop.IsAllInstant
					? $"Critical: Potential infinite loop with instant transitions: {string.Join(" -> ", nodeNames)}"
					: $"Warning: Potential loop detected: {string.Join(" -> ", nodeNames)}";

				issues.Add(new GraphValidationIssue(severity, message, loop.NodeIds.FirstOrDefault()));
			}

			foreach (var chain in result.InstantTransitionChains.Where(c => c.FormsCycle))
			{
				issues.Add(new GraphValidationIssue(
					GraphValidationSeverity.Error,
					$"Critical: Instant transition cycle detected with {chain.Length} states",
					chain.NodeIds.FirstOrDefault()
				));
			}

			foreach (var chain in result.InstantTransitionChains.Where(c => !c.FormsCycle && c.Length > 5))
			{
				issues.Add(new GraphValidationIssue(
					GraphValidationSeverity.Warning,
					$"Warning: Long instant transition chain ({chain.Length} states) may cause performance issues",
					chain.NodeIds.FirstOrDefault()
				));
			}

			return issues;
		}
	}

	/// <summary>
	/// Result of loop analysis.
	/// </summary>
	public class LoopAnalysisResult
	{
		public List<DetectedLoop> DetectedLoops { get; } = new List<DetectedLoop>();
		public List<InstantTransitionChain> InstantTransitionChains { get; } = new List<InstantTransitionChain>();

		public bool HasCriticalIssues =>
			DetectedLoops.Any(l => l.Severity == LoopSeverity.Critical) ||
			InstantTransitionChains.Any(c => c.FormsCycle);
	}

	/// <summary>
	/// Represents a detected potential loop in the graph.
	/// </summary>
	public class DetectedLoop
	{
		public List<string> NodeIds { get; set; } = new List<string>();
		public bool IsAllInstant { get; set; }
		public LoopSeverity Severity { get; set; }
	}

	/// <summary>
	/// Represents a chain of instant (zero-time) transitions.
	/// </summary>
	public class InstantTransitionChain
	{
		public List<string> NodeIds { get; } = new List<string>();
		public int Length { get; set; }
		public bool FormsCycle { get; set; }
	}

	/// <summary>
	/// Internal transition info for analysis.
	/// </summary>
	internal class TransitionInfo
	{
		public string TargetNodeId { get; set; }
		public float TransitionTime { get; set; }
		public bool IsInstant { get; set; }
	}

	/// <summary>
	/// Severity of a detected loop.
	/// </summary>
	public enum LoopSeverity
	{
		Warning,
		Critical
	}
}
