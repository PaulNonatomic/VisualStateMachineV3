using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Exports transition history to JSON for bug reports and external analysis.
	/// </summary>
	public static class TransitionHistoryExporter
	{
		/// <summary>
		/// Exports the transition history of a runtime instance to a JSON string.
		/// </summary>
		public static string ExportToJson(RuntimeInstanceInfo instance)
		{
			if (instance == null)
			{
				return "{}";
			}

			var sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendLine($"\t\"instanceId\": \"{EscapeJson(instance.InstanceId)}\",");
			sb.AppendLine($"\t\"graphName\": \"{EscapeJson(instance.Graph?.name ?? "Unknown")}\",");
			sb.AppendLine($"\t\"ownerName\": \"{EscapeJson(instance.Owner?.name ?? "Unknown")}\",");
			sb.AppendLine($"\t\"exportedAt\": \"{DateTime.Now:O}\",");
			sb.AppendLine("\t\"transitions\": [");

			var history = instance.TransitionHistory;
			for (var i = 0; i < history.Count; i++)
			{
				var record = history[i];
				var previousRecord = i > 0 ? history[i - 1] : null;
				var isLast = i == history.Count - 1;

				sb.AppendLine("\t\t{");
				sb.AppendLine($"\t\t\t\"timestamp\": \"{record.Timestamp:O}\",");
				sb.AppendLine($"\t\t\t\"from\": {{");
				sb.AppendLine($"\t\t\t\t\"nodeId\": \"{EscapeJson(record.FromNodeId ?? "")}\",");
				sb.AppendLine($"\t\t\t\t\"stateName\": \"{EscapeJson(record.FromStateName ?? "Start")}\"");
				sb.AppendLine($"\t\t\t}},");
				sb.AppendLine($"\t\t\t\"to\": {{");
				sb.AppendLine($"\t\t\t\t\"nodeId\": \"{EscapeJson(record.ToNodeId ?? "")}\",");
				sb.AppendLine($"\t\t\t\t\"stateName\": \"{EscapeJson(record.ToStateName ?? "Unknown")}\"");
				sb.AppendLine($"\t\t\t}},");
				sb.AppendLine($"\t\t\t\"triggerPort\": \"{EscapeJson(record.TriggerPortName ?? "")}\",");

				if (previousRecord != null)
				{
					var duration = record.GetDuration(previousRecord);
					sb.AppendLine($"\t\t\t\"durationFromPrevious\": \"{FormatDuration(duration)}\",");
				}
				else
				{
					sb.AppendLine($"\t\t\t\"durationFromPrevious\": null,");
				}

				sb.AppendLine("\t\t\t\"blackboard\": {");
				if (record.BlackboardSnapshot != null)
				{
					var snapshotCount = 0;
					var snapshotTotal = record.BlackboardSnapshot.Count;
					foreach (var kvp in record.BlackboardSnapshot)
					{
						snapshotCount++;
						var comma = snapshotCount < snapshotTotal ? "," : "";
						sb.AppendLine($"\t\t\t\t\"{EscapeJson(kvp.Key)}\": \"{EscapeJson(kvp.Value ?? "null")}\"{comma}");
					}
				}
				sb.AppendLine("\t\t\t}");

				sb.AppendLine(isLast ? "\t\t}" : "\t\t},");
			}

			sb.AppendLine("\t]");
			sb.AppendLine("}");

			return sb.ToString();
		}

		/// <summary>
		/// Exports the transition history to a JSON file using a save dialog.
		/// </summary>
		public static void ExportToFile(RuntimeInstanceInfo instance)
		{
			if (instance == null) return;

			var graphName = instance.Graph?.name ?? "Unknown";
			var defaultName = $"TransitionHistory_{graphName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

			var path = EditorUtility.SaveFilePanel(
				"Export Transition History",
				"",
				defaultName,
				"json");

			if (string.IsNullOrEmpty(path)) return;

			var json = ExportToJson(instance);
			File.WriteAllText(path, json, Encoding.UTF8);
		}

		private static string EscapeJson(string value)
		{
			if (string.IsNullOrEmpty(value)) return "";
			return value
				.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\t", "\\t");
		}

		private static string FormatDuration(TimeSpan duration)
		{
			if (duration.TotalMilliseconds < 1)
			{
				return "<1ms";
			}
			if (duration.TotalSeconds < 1)
			{
				return $"{duration.TotalMilliseconds:F0}ms";
			}
			if (duration.TotalMinutes < 1)
			{
				return $"{duration.TotalSeconds:F3}s";
			}
			return $"{duration.TotalMinutes:F1}m";
		}
	}
}
