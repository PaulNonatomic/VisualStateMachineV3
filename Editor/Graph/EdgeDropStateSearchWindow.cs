using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Blackboard;
using Nonatomic.VSM.Editor.Ports;
using Nonatomic.VSM.Editor.Services;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Search window for creating and connecting states when dropping an edge.
	/// </summary>
	public class EdgeDropStateSearchWindow : ScriptableObject, ISearchWindowProvider
	{
		private const string BuiltInNamespace = "Nonatomic.VSM.States";

		private StateMachineGraphView _graphView;
		private StateMachineGraphWindow _window;
		private Vector2 _creationPosition;
		private Port _sourcePort;

		// Cached icons
		private static Texture2D _flowControlIcon;
		private static Texture2D _builtInIcon;
		private static Texture2D _projectStatesIcon;
		private static Texture2D _projectFolderIcon;
		private static Texture2D _exitIcon;
		private static Texture2D _portalIcon;
		private static Texture2D _subGraphIcon;
		private static Texture2D _defaultStateIcon;
		private static Texture2D _blackboardGetIcon;
		private static Texture2D _blackboardSetIcon;

		// Built-in state category icons
		private static Texture2D _logicIcon;
		private static Texture2D _mathIcon;
		private static Texture2D _timingIcon;
		private static Texture2D _gameObjectIcon;
		private static Texture2D _audioIcon;
		private static Texture2D _debugIcon;

		private static readonly Dictionary<string, Texture2D> CategoryIcons = new();

		public void Initialize(StateMachineGraphView graphView, StateMachineGraphWindow window, Vector2 position, Port sourcePort)
		{
			_graphView = graphView;
			_window = window;
			_creationPosition = position;
			_sourcePort = sourcePort;
			CacheIcons();
		}

		private static void CacheIcons()
		{
			if (_flowControlIcon != null) return;

			_flowControlIcon = GetUnityIcon("d_FilterByType");
			_builtInIcon = GetUnityIcon("d_Favorite Icon");
			_projectStatesIcon = GetUnityIcon("d_FolderOpened Icon");
			_projectFolderIcon = GetUnityIcon("d_Folder Icon");
			_exitIcon = GetUnityIcon("d_PreMatQuad");
			_portalIcon = GetUnityIcon("d_Linked");
			_subGraphIcon = GetUnityIcon("d_PrefabModel Icon");
			_defaultStateIcon = GetUnityIcon("d_Animation.Record");
			_blackboardGetIcon = GetUnityIcon("d_AnimatorStateTransition Icon") ?? GetUnityIcon("d_Favorite Icon");
			_blackboardSetIcon = GetUnityIcon("d_AnimatorStateTransition Icon") ?? GetUnityIcon("d_Favorite Icon");

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

			// Determine what kind of port we're dragging from
			bool isTransitionPort = _sourcePort.ClassListContains("transition-port");
			bool isDataPort = _sourcePort.ClassListContains("data-output-port");
			bool isDataInputPort = _sourcePort.ClassListContains("data-input-port");

			// Add flow control nodes for transition ports
			if (isTransitionPort)
			{
				tree.Add(new SearchTreeGroupEntry(new GUIContent("Flow Control", _flowControlIcon), 1));
				tree.Add(new SearchTreeEntry(new GUIContent("Exit Node", _exitIcon)) { level = 2, userData = "ExitNode" });
				tree.Add(new SearchTreeEntry(new GUIContent("Portal Out", _portalIcon)) { level = 2, userData = "PortalOutNode" });
				tree.Add(new SearchTreeEntry(new GUIContent("Sub-State Machine", _subGraphIcon)) { level = 2, userData = "SubStateMachine" });
			}

			// Add compatible blackboard variable nodes
			AddBlackboardVariableEntries(tree, isTransitionPort, isDataPort, isDataInputPort);

			// Separate states into built-in and project, then further split by
			// whether they have an explicit Category attribute
			var builtInUncategorized = new List<Type>();
			var projectUncategorized = new Dictionary<string, List<Type>>();
			var categorizedStates = new Dictionary<string, List<Type>>();

			foreach (var stateType in PortDiscoveryService.GetAllStateTypes())
			{
				// Filter based on port compatibility
				if (!HasCompatiblePort(stateType, isTransitionPort, isDataPort, isDataInputPort))
				{
					continue;
				}

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

		private void AddBlackboardVariableEntries(
			List<SearchTreeEntry> tree,
			bool isTransitionPort,
			bool isDataPort,
			bool isDataInputPort)
		{
			var asset = _graphView?.CurrentAsset;
			if (asset == null) return;

			var definitions = asset.BlackboardDefinitions;
			if (definitions == null || definitions.Count == 0) return;

			// Transition ports don't connect to blackboard variable nodes
			if (isTransitionPort) return;
			if (!isDataPort && !isDataInputPort) return;

			var sourceType = _sourcePort?.portType;
			if (sourceType == null) return;

			var entries = new List<(string label, BlackboardVariableDefinition def, BlackboardNodeMode mode)>();

			foreach (var definition in definitions)
			{
				var varType = definition.ValueType;
				if (varType == null) continue;

				if (isDataPort)
				{
					// Dragging from a data output port — offer Set nodes with compatible type
					if (AreTypesCompatible(sourceType, varType))
					{
						var displayName = BlackboardTypeEditorRegistry.GetDisplayName(varType);
						entries.Add(($"Set {definition.Key}  ({displayName})", definition, BlackboardNodeMode.Set));
					}
				}
				else if (isDataInputPort)
				{
					// Dragging from a data input port — offer Get nodes with compatible type
					if (AreTypesCompatible(varType, sourceType))
					{
						var displayName = BlackboardTypeEditorRegistry.GetDisplayName(varType);
						entries.Add(($"Get {definition.Key}  ({displayName})", definition, BlackboardNodeMode.Get));
					}
				}
			}

			if (entries.Count == 0) return;

			tree.Add(new SearchTreeGroupEntry(new GUIContent("Blackboard Variables", _blackboardGetIcon), 1));

			foreach (var entry in entries)
			{
				var icon = entry.mode == BlackboardNodeMode.Get ? _blackboardGetIcon : _blackboardSetIcon;
				tree.Add(new SearchTreeEntry(new GUIContent(entry.label, icon))
				{
					level = 2,
					userData = (entry.def, entry.mode)
				});
			}
		}

		private bool HasCompatiblePort(Type stateType, bool isTransitionPort, bool isDataPort, bool isDataInputPort)
		{
			var manifest = PortDiscoveryService.GetPortManifest(stateType);

			// Transition ports connect to flow input (Enter port)
			if (isTransitionPort) return true; // All states have an Enter port

			// Data output ports need compatible input ports on the target
			if (isDataPort)
			{
				var sourceType = _sourcePort.portType;

				foreach (var inputPort in manifest.InputPorts)
				{
					if (AreTypesCompatible(sourceType, inputPort.DataType))
					{
						return true;
					}
				}
			}

			// Data input ports need compatible output ports on the target
			if (isDataInputPort)
			{
				var targetType = _sourcePort.portType;

				foreach (var outputPort in manifest.OutputPorts)
				{
					if (AreTypesCompatible(outputPort.DataType, targetType))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool AreTypesCompatible(Type sourceType, Type targetType)
		{
			if (sourceType == null || targetType == null)
				return false;

			// Exact match
			if (sourceType == targetType)
				return true;

			// object output can connect to any input — runtime handles actual type validation
			if (sourceType == typeof(object))
				return true;

			// Inheritance
			if (targetType.IsAssignableFrom(sourceType))
				return true;

			return false;
		}

		public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
		{
			if (entry.userData is Type stateType)
			{
				CreateStateNodeAndConnect(stateType);
				return true;
			}

			if (entry.userData is (BlackboardVariableDefinition def, BlackboardNodeMode mode))
			{
				CreateBlackboardVariableNodeAndConnect(def, mode);
				return true;
			}

			if (entry.userData is string nodeType)
			{
				switch (nodeType)
				{
					case "ExitNode":
						CreateExitNodeAndConnect();
						return true;
					case "PortalOutNode":
						CreatePortalOutNodeAndConnect();
						return true;
					case "SubStateMachine":
						CreateSubStateMachineNodeAndConnect();
						return true;
				}
			}

			return false;
		}

		private void CreateStateNodeAndConnect(Type stateType)
		{
			if (_graphView.CurrentAsset == null)
			{
				Debug.LogError("Cannot create state node: No graph asset loaded");
				return;
			}

			Undo.RecordObject(_graphView.CurrentAsset, "Create and Connect State Node");

			// Create the state node
			var stateNode = new StateNode(stateType)
			{
				Position = _creationPosition
			};

			_graphView.CurrentAsset.AddNode(stateNode);

			// Create the node view
			var nodeView = new StateNodeView(stateNode, _graphView);
			nodeView.SetPosition(new Rect(stateNode.Position, Vector2.zero));
			_graphView.AddNodeView(nodeView, stateNode.Id);

			bool isDataInputPort = _sourcePort.ClassListContains("data-input-port");

			if (isDataInputPort)
			{
				// Dragging from an input port — find a compatible output port on the new node
				var outputPort = FindCompatibleOutputPort(nodeView as IPortProvider, stateNode);
				if (outputPort == null)
				{
					Debug.LogWarning("Could not find compatible output port on created node");
					_graphView.MarkAssetDirty();
					return;
				}

				var draggedNodeView = _sourcePort.node as INodeDataProvider;
				if (draggedNodeView == null) return;

				var connection = new Connection(
					stateNode.Id,
					outputPort.portName,
					draggedNodeView.NodeData.Id,
					_sourcePort.portName,
					ConnectionType.Data
				);

				_graphView.CurrentAsset.AddConnection(connection);

				var edge = new TransitionEdgeView(connection)
				{
					output = outputPort,
					input = _sourcePort
				};

				outputPort.Connect(edge);
				_sourcePort.Connect(edge);
				_graphView.AddEdgeView(edge, connection.Id);

				// Update inline field visibility on the target (existing) node
				if (_sourcePort.node is StateNodeView existingStateNode)
				{
					existingStateNode.UpdateInlineFieldVisibility();
				}
				else if (_sourcePort.node is SubStateMachineNodeView existingSubNode)
				{
					existingSubNode.UpdateInlineFieldVisibility();
				}

				_graphView.MarkAssetDirty();
				return;
			}

			// Find the first compatible target port
			Port targetPort = FindCompatibleInputPort(nodeView as IPortProvider);
			if (targetPort == null)
			{
				Debug.LogWarning("Could not find compatible input port on created node");
				_graphView.MarkAssetDirty();
				return;
			}

			// Determine connection type
			bool isTransitionConnection = _sourcePort.ClassListContains("transition-port");
			ConnectionType connectionType = isTransitionConnection ? ConnectionType.Transition : ConnectionType.Data;

			// Create the connection data first
			var sourceNodeView = _sourcePort.node as INodeDataProvider;
			if (sourceNodeView == null)
			{
				Debug.LogError("Source node doesn't implement INodeDataProvider");
				return;
			}

			var conn = new Connection(
				sourceNodeView.NodeData.Id,
				_sourcePort.portName,
				stateNode.Id,
				targetPort.portName,
				connectionType
			);

			_graphView.CurrentAsset.AddConnection(conn);

			// Create the edge in the UI with connection data
			var edge2 = new TransitionEdgeView(conn)
			{
				output = _sourcePort,
				input = targetPort
			};

			_sourcePort.Connect(edge2);
			targetPort.Connect(edge2);
			_graphView.AddEdgeView(edge2, conn.Id);

			// Update inline field visibility on the newly created target node
			nodeView.UpdateInlineFieldVisibility();

			_graphView.MarkAssetDirty();
		}

		private Port FindCompatibleInputPort(IPortProvider portProvider)
		{
			if (portProvider == null) return null;

			bool isTransitionPort = _sourcePort.ClassListContains("transition-port");
			bool isDataPort = _sourcePort.ClassListContains("data-output-port");

			// For transition ports, connect to Enter port
			if (isTransitionPort)
			{
				return portProvider.GetInputPort("Enter");
			}

			// For data ports, find first compatible input port
			if (isDataPort)
			{
				var sourceType = _sourcePort.portType;

				if (portProvider is INodeDataProvider nodeDataProvider &&
					nodeDataProvider.NodeData is StateNode stateNode)
				{
					var manifest = PortDiscoveryService.GetPortManifest(stateNode.StateType);

					foreach (var inputPortInfo in manifest.InputPorts)
					{
						if (AreTypesCompatible(sourceType, inputPortInfo.DataType))
						{
							var port = portProvider.GetInputPort(inputPortInfo.FieldName);
							if (port != null) return port;
						}
					}
				}
			}

			return null;
		}

		private Port FindCompatibleOutputPort(IPortProvider portProvider, StateNode stateNode)
		{
			if (portProvider == null) return null;

			var targetType = _sourcePort.portType;
			var manifest = PortDiscoveryService.GetPortManifest(stateNode.StateType);

			foreach (var outputPortInfo in manifest.OutputPorts)
			{
				if (AreTypesCompatible(outputPortInfo.DataType, targetType))
				{
					var port = portProvider.GetOutputPort(outputPortInfo.FieldName);
					if (port != null) return port;
				}
			}

			return null;
		}

		private void CreateExitNodeAndConnect()
		{
			if (_graphView.CurrentAsset == null) return;

			Undo.RecordObject(_graphView.CurrentAsset, "Create and Connect Exit Node");

			// Create the exit node
			var exitNode = new ExitNode
			{
				Position = _creationPosition
			};

			_graphView.CurrentAsset.AddNode(exitNode);

			// Create the node view
			var nodeView = new ExitNodeView(exitNode);
			nodeView.SetPosition(new Rect(exitNode.Position, Vector2.zero));
			_graphView.AddNodeView(nodeView, exitNode.Id);

			// Connect to the Enter port
			var targetPort = nodeView.GetInputPort("Enter");
			if (targetPort != null)
			{
				CreateConnection(exitNode.Id, targetPort, "Enter");
			}

			_graphView.MarkAssetDirty();
		}

		private void CreatePortalOutNodeAndConnect()
		{
			if (_graphView.CurrentAsset == null) return;

			Undo.RecordObject(_graphView.CurrentAsset, "Create and Connect Portal Out Node");

			// Create the portal out node with auto-assigned channel
			var portalOutNode = new PortalOutNode
			{
				Position = _creationPosition
			};

			_graphView.CurrentAsset.AddNode(portalOutNode);

			// Create the node view
			var nodeView = new PortalOutNodeView(portalOutNode, _graphView);
			nodeView.SetPosition(new Rect(portalOutNode.Position, Vector2.zero));
			_graphView.AddNodeView(nodeView, portalOutNode.Id);

			// Connect to the Enter port
			var targetPort = nodeView.GetInputPort("Enter");
			if (targetPort != null)
			{
				CreateConnection(portalOutNode.Id, targetPort, "Enter");
			}

			_graphView.MarkAssetDirty();
		}

		private void CreateSubStateMachineNodeAndConnect()
		{
			if (_graphView.CurrentAsset == null) return;

			Undo.RecordObject(_graphView.CurrentAsset, "Create and Connect Sub-State Machine Node");

			// Create the sub-state machine node
			var subNode = new SubStateMachineNode
			{
				Position = _creationPosition
			};

			_graphView.CurrentAsset.AddNode(subNode);

			// Create the node view
			var nodeView = new SubStateMachineNodeView(subNode, _graphView);
			nodeView.SetPosition(new Rect(subNode.Position, Vector2.zero));
			_graphView.AddNodeView(nodeView, subNode.Id);

			// Connect to the Enter port
			var targetPort = nodeView.GetInputPort("Enter");
			if (targetPort != null)
			{
				CreateConnection(subNode.Id, targetPort, "Enter");
			}

			_graphView.MarkAssetDirty();
		}

		private void CreateBlackboardVariableNodeAndConnect(BlackboardVariableDefinition definition, BlackboardNodeMode mode)
		{
			if (_graphView.CurrentAsset == null) return;

			Undo.RecordObject(_graphView.CurrentAsset, "Create and Connect Blackboard Variable Node");

			var varNode = new BlackboardVariableNode(definition.Key, definition.ValueType, mode)
			{
				Position = _creationPosition
			};

			_graphView.CurrentAsset.AddNode(varNode);

			var nodeView = new BlackboardVariableNodeView(varNode, _graphView);
			nodeView.SetPosition(new Rect(varNode.Position, Vector2.zero));
			_graphView.AddNodeView(nodeView, varNode.Id);

			// Determine source and target ports for the connection
			Port targetPort;
			string targetPortName;
			var sourceNodeView = _sourcePort.node as INodeDataProvider;
			if (sourceNodeView == null) return;

			if (mode == BlackboardNodeMode.Set)
			{
				// Source data-out → Set node's input
				targetPort = nodeView.GetInputPort(null);
				targetPortName = targetPort?.portName ?? "";
			}
			else
			{
				// Get node's output → Source data-in
				// Swap: the blackboard node is the source, the dragged port is the target
				var getPort = nodeView.GetOutputPort(null);
				if (getPort == null) return;

				var connection = new Connection(
					varNode.Id,
					getPort.portName,
					sourceNodeView.NodeData.Id,
					_sourcePort.portName,
					ConnectionType.Data
				);

				_graphView.CurrentAsset.AddConnection(connection);

				var edge = new TransitionEdgeView(connection)
				{
					output = getPort,
					input = _sourcePort
				};

				getPort.Connect(edge);
				_sourcePort.Connect(edge);
				_graphView.AddEdgeView(edge, connection.Id);

				_graphView.MarkAssetDirty();
				return;
			}

			if (targetPort == null) return;

			var setConnection = new Connection(
				sourceNodeView.NodeData.Id,
				_sourcePort.portName,
				varNode.Id,
				targetPortName,
				ConnectionType.Data
			);

			_graphView.CurrentAsset.AddConnection(setConnection);

			var setEdge = new TransitionEdgeView(setConnection)
			{
				output = _sourcePort,
				input = targetPort
			};

			_sourcePort.Connect(setEdge);
			targetPort.Connect(setEdge);
			_graphView.AddEdgeView(setEdge, setConnection.Id);

			_graphView.MarkAssetDirty();
		}

		private void CreateConnection(string targetNodeId, Port targetPort, string targetPortName)
		{
			// Create the connection data
			var sourceNodeView = _sourcePort.node as INodeDataProvider;
			if (sourceNodeView == null) return;

			var connection = new Connection(
				sourceNodeView.NodeData.Id,
				_sourcePort.portName,
				targetNodeId,
				targetPortName,
				ConnectionType.Transition
			);

			_graphView.CurrentAsset.AddConnection(connection);

			// Create the edge in the UI
			var edge = new TransitionEdgeView(connection)
			{
				output = _sourcePort,
				input = targetPort
			};

			_sourcePort.Connect(edge);
			targetPort.Connect(edge);
			_graphView.AddEdgeView(edge, connection.Id);
		}
	}
}
