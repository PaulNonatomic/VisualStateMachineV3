using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Ports;
using Nonatomic.VSM.Editor.Services;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Search window for adding state nodes to the graph.
	/// </summary>
	public class StateSearchWindow : ScriptableObject, ISearchWindowProvider
	{
		private const string BuiltInNamespace = "Nonatomic.VSM.States";

		private StateMachineGraphView _graphView;
		private StateMachineGraphWindow _window;
		private Vector2 _creationPosition;

		// Cached icons
		private static Texture2D _flowControlIcon;
		private static Texture2D _organizationIcon;
		private static Texture2D _builtInIcon;
		private static Texture2D _projectStatesIcon;
		private static Texture2D _projectFolderIcon;
		private static Texture2D _entryIcon;
		private static Texture2D _exitIcon;
		private static Texture2D _portalIcon;
		private static Texture2D _subGraphIcon;
		private static Texture2D _groupIcon;
		private static Texture2D _defaultStateIcon;

		// Built-in state category icons
		private static Texture2D _logicIcon;
		private static Texture2D _mathIcon;
		private static Texture2D _timingIcon;
		private static Texture2D _gameObjectIcon;
		private static Texture2D _audioIcon;
		private static Texture2D _debugIcon;

		private static readonly Dictionary<string, Texture2D> CategoryIcons = new();

		public void Initialize(StateMachineGraphView graphView, StateMachineGraphWindow window, Vector2 position)
		{
			_graphView = graphView;
			_window = window;
			_creationPosition = position;
			CacheIcons();
		}

		private static void CacheIcons()
		{
			if (_flowControlIcon != null) return;

			_flowControlIcon = GetUnityIcon("d_FilterByType");
			_organizationIcon = GetUnityIcon("d_RectTransformBlueprint");
			_builtInIcon = GetUnityIcon("d_Favorite Icon");
			_projectStatesIcon = GetUnityIcon("d_FolderOpened Icon");
			_projectFolderIcon = GetUnityIcon("d_Folder Icon");
			_entryIcon = GetUnityIcon("d_PlayButton");
			_exitIcon = GetUnityIcon("d_PreMatQuad");
			_portalIcon = GetUnityIcon("d_Linked");
			_subGraphIcon = GetUnityIcon("d_PrefabModel Icon");
			_groupIcon = GetUnityIcon("d_RectTool");
			_defaultStateIcon = GetUnityIcon("d_Animation.Record");

			// Category icons for built-in states
			_logicIcon = GetUnityIcon("d_FilterByType");
			_mathIcon = GetUnityIcon("d_HoloLensInputModule Icon") ?? GetUnityIcon("d_Favorite Icon");
			_timingIcon = GetUnityIcon("d_UnityEditor.AnimationWindow");
			_gameObjectIcon = GetUnityIcon("d_GameObject Icon");
			_audioIcon = GetUnityIcon("d_AudioSource Icon");
			_debugIcon = GetUnityIcon("d_Debug");

			CategoryIcons["Logic"] = _logicIcon;
			CategoryIcons["Math"] = _mathIcon;
			CategoryIcons["Timing"] = _timingIcon;
			CategoryIcons["GameObject"] = _gameObjectIcon;
			CategoryIcons["Audio"] = _audioIcon;
			CategoryIcons["Debug"] = _debugIcon;
		}

		private static Texture2D GetUnityIcon(string iconName)
		{
			var content = EditorGUIUtility.IconContent(iconName);
			return content?.image as Texture2D;
		}

		public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
		{
			var tree = new List<SearchTreeEntry>
			{
				new SearchTreeGroupEntry(new GUIContent("Add Node"), 0)
			};

			// Flow Control group with icon
			tree.Add(new SearchTreeGroupEntry(new GUIContent("Flow Control", _flowControlIcon), 1));
			tree.Add(new SearchTreeEntry(new GUIContent("Entry Node", _entryIcon)) { level = 2, userData = "EntryNode" });
			tree.Add(new SearchTreeEntry(new GUIContent("Exit Node", _exitIcon)) { level = 2, userData = "ExitNode" });
			tree.Add(new SearchTreeEntry(new GUIContent("Portal Out", _portalIcon)) { level = 2, userData = "PortalOutNode" });
			tree.Add(new SearchTreeEntry(new GUIContent("Portal In", _portalIcon)) { level = 2, userData = "PortalInNode" });
			tree.Add(new SearchTreeEntry(new GUIContent("Sub-State Machine", _subGraphIcon)) { level = 2, userData = "SubStateMachine" });

			// Organization group with icon
			tree.Add(new SearchTreeGroupEntry(new GUIContent("Organization", _organizationIcon), 1));
			tree.Add(new SearchTreeEntry(new GUIContent("Group", _groupIcon)) { level = 2, userData = "Group" });

			// Separate states into built-in and project, then further split by
			// whether they have an explicit Category attribute
			var builtInUncategorized = new List<Type>();
			var projectUncategorized = new Dictionary<string, List<Type>>();
			var categorizedStates = new Dictionary<string, List<Type>>();

			foreach (var stateType in PortDiscoveryService.GetAllStateTypes())
			{
				var category = GetExplicitCategory(stateType);
				if (category != null)
				{
					if (!categorizedStates.ContainsKey(category))
					{
						categorizedStates[category] = new List<Type>();
					}
					categorizedStates[category].Add(stateType);
					continue;
				}

				var ns = stateType.Namespace ?? "Global";
				if (ns == BuiltInNamespace)
				{
					builtInUncategorized.Add(stateType);
				}
				else
				{
					if (!projectUncategorized.ContainsKey(ns))
					{
						projectUncategorized[ns] = new List<Type>();
					}
					projectUncategorized[ns].Add(stateType);
				}
			}

			// Add Built-in States group with subcategories (name-heuristic based)
			if (builtInUncategorized.Count > 0)
			{
				tree.Add(new SearchTreeGroupEntry(new GUIContent("Built-in States", _builtInIcon), 1));
				AddCategorizedBuiltInStates(tree, builtInUncategorized);
			}

			// Add explicit-category groups as top-level sections
			if (categorizedStates.Count > 0)
			{
				AddExplicitCategoryStates(tree, categorizedStates);
			}

			// Add Project States (uncategorized, grouped by namespace)
			if (projectUncategorized.Count > 0)
			{
				tree.Add(new SearchTreeGroupEntry(new GUIContent("Project States", _projectStatesIcon), 1));

				var sortedNamespaces = new List<string>(projectUncategorized.Keys);
				sortedNamespaces.Sort();

				foreach (var ns in sortedNamespaces)
				{
					var displayName = FormatNamespaceDisplay(ns);
					tree.Add(new SearchTreeGroupEntry(new GUIContent(displayName, _projectFolderIcon), 2));

					var types = projectUncategorized[ns];
					types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

					foreach (var type in types)
					{
						AddStateEntry(tree, type, 3);
					}
				}
			}

			return tree;
		}

		private void AddExplicitCategoryStates(List<SearchTreeEntry> tree, Dictionary<string, List<Type>> categorizedStates)
		{
			var sortedCategories = categorizedStates.Keys.ToList();
			sortedCategories.Sort(StringComparer.Ordinal);

			foreach (var category in sortedCategories)
			{
				var types = categorizedStates[category];
				types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

				var parts = category.Split('/');
				var rootCategory = parts[0];
				var icon = CategoryIcons.TryGetValue(rootCategory, out var cachedIcon) ? cachedIcon : _defaultStateIcon;

				// Single-level category: top-level group with states inside
				if (parts.Length == 1)
				{
					tree.Add(new SearchTreeGroupEntry(new GUIContent(rootCategory, icon), 1));

					foreach (var type in types)
					{
						AddStateEntry(tree, type, 2);
					}
				}
				else
				{
					// Multi-level category path: create nested groups
					// First ensure the root group exists (tracked to avoid duplicates)
					tree.Add(new SearchTreeGroupEntry(new GUIContent(rootCategory, icon), 1));

					for (var i = 1; i < parts.Length; i++)
					{
						tree.Add(new SearchTreeGroupEntry(new GUIContent(parts[i], _projectFolderIcon), i + 1));
					}

					foreach (var type in types)
					{
						AddStateEntry(tree, type, parts.Length + 1);
					}
				}
			}
		}

		private void AddCategorizedBuiltInStates(List<SearchTreeEntry> tree, List<Type> builtInStates)
		{
			// Categorize built-in states by name heuristics (no explicit Category attribute)
			var categories = new Dictionary<string, (Texture2D icon, List<Type> types)>
			{
				["Logic"] = (_logicIcon, new List<Type>()),
				["Timing"] = (_timingIcon, new List<Type>()),
				["GameObject"] = (_gameObjectIcon, new List<Type>()),
				["Audio"] = (_audioIcon, new List<Type>()),
				["Debug"] = (_debugIcon, new List<Type>()),
				["Other"] = (_defaultStateIcon, new List<Type>())
			};

			foreach (var stateType in builtInStates)
			{
				var category = GetBuiltInCategoryByName(stateType.Name);
				if (!categories.ContainsKey(category))
				{
					categories[category] = (_defaultStateIcon, new List<Type>());
				}
				categories[category].types.Add(stateType);
			}

			// Add each category that has states
			foreach (var kvp in categories)
			{
				if (kvp.Value.types.Count == 0) continue;

				tree.Add(new SearchTreeGroupEntry(new GUIContent(kvp.Key, kvp.Value.icon), 2));

				kvp.Value.types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

				foreach (var type in kvp.Value.types)
				{
					AddStateEntry(tree, type, 3);
				}
			}
		}

		/// <summary>
		/// Returns the explicit Category from StateInfoAttribute, or null if not set.
		/// </summary>
		private static string GetExplicitCategory(Type stateType)
		{
			var infoAttr = stateType.GetCustomAttribute<StateInfoAttribute>();
			return string.IsNullOrEmpty(infoAttr?.Category) ? null : infoAttr.Category;
		}

		/// <summary>
		/// Fallback name-based categorization for built-in states without an explicit Category.
		/// </summary>
		private static string GetBuiltInCategoryByName(string typeName)
		{
			if (typeName.Contains("Condition") ||
			    typeName.Contains("Compare") ||
			    typeName.Contains("Random") && typeName.Contains("Branch"))
			{
				return "Logic";
			}

			if (typeName.Contains("Delay") ||
			    typeName.Contains("Wait"))
			{
				return "Timing";
			}

			if (typeName.Contains("Spawn") ||
			    typeName.Contains("Destroy") ||
			    typeName.Contains("Transform") ||
			    typeName.Contains("Animator") ||
			    typeName.Contains("SetActive"))
			{
				return "GameObject";
			}

			if (typeName.Contains("Audio"))
			{
				return "Audio";
			}

			if (typeName.Contains("Log") ||
			    typeName.Contains("Debug") ||
			    typeName.Contains("Invoke"))
			{
				return "Debug";
			}

			return "Other";
		}

		private void AddStateEntry(List<SearchTreeEntry> tree, Type type, int level)
		{
			var manifest = PortDiscoveryService.GetPortManifest(type);
			var portInfo = $"In: {manifest.InputPorts.Count}, Out: {manifest.TransitionPorts.Count}";
			var icon = GetStateIcon(type);

			tree.Add(new SearchTreeEntry(new GUIContent($"{type.Name} ({portInfo})", icon))
			{
				level = level,
				userData = type
			});
		}

		private static Texture2D GetStateIcon(Type stateType)
		{
			var styleAttr = stateType.GetCustomAttribute<StateStyleAttribute>();
			if (!string.IsNullOrEmpty(styleAttr?.Icon))
			{
				// Try custom icon first
				var icon = ImageService.LoadNodeIcon(styleAttr.Icon);
				if (icon != null) return icon;

				// Fall back to Unity built-in icon
				icon = GetUnityIcon(styleAttr.Icon);
				if (icon != null) return icon;
			}

			return _defaultStateIcon;
		}

		private static string FormatNamespaceDisplay(string ns)
		{
			if (string.IsNullOrEmpty(ns) || ns == "Global")
			{
				return "Custom States";
			}

			// Split namespace into parts
			var parts = ns.Split('.');
			if (parts.Length == 0)
			{
				return ns;
			}

			var lastPart = parts[parts.Length - 1];

			// If the last part is a generic name like "States", use the project name instead
			// This prevents multiple "States" categories when different projects all use a States folder
			if (IsGenericNamespacePart(lastPart) && parts.Length >= 2)
			{
				var projectPart = parts[parts.Length - 2];

				// If the project part is also generic (e.g., "Samples"), go one level deeper
				if (IsGenericNamespacePart(projectPart) && parts.Length >= 3)
				{
					return parts[parts.Length - 3];
				}

				return projectPart;
			}

			return lastPart;
		}

		private static bool IsGenericNamespacePart(string part)
		{
			// List of generic namespace parts that shouldn't be used as category names
			var genericParts = new[]
			{
				"States",
				"State",
				"Scripts",
				"Core",
				"Common",
				"Shared",
				"Base",
				"Components",
				"Behaviours",
				"Behaviors"
			};

			foreach (var generic in genericParts)
			{
				if (string.Equals(part, generic, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
		{
			if (entry.userData is Type stateType)
			{
				_graphView.CreateStateNode(stateType, _creationPosition);
				return true;
			}

			if (entry.userData is string nodeType)
			{
				switch (nodeType)
				{
					case "EntryNode":
						_graphView.CreateEntryNode(_creationPosition);
						return true;
					case "ExitNode":
						_graphView.CreateExitNode(_creationPosition);
						return true;
					case "PortalOutNode":
						_graphView.CreatePortalOutNode(_creationPosition);
						return true;
					case "PortalInNode":
						_graphView.CreatePortalInNode(_creationPosition);
						return true;
					case "SubStateMachine":
						_graphView.CreateSubStateMachineNode(null, _creationPosition);
						return true;
					case "Group":
						_graphView.CreateRegion(_creationPosition);
						return true;
				}
			}

			return false;
		}
	}
}
