using System;
using System.Collections.Generic;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// A lightweight search bar overlay for finding and navigating to nodes in the graph.
	/// Appears at the top-center of the graph view, similar to Ctrl+F in text editors.
	/// </summary>
	public class NodeSearchBar : VisualElement
	{
		private readonly StateMachineGraphWindow _window;
		private readonly TextField _searchField;
		private readonly Label _matchLabel;
		private readonly Button _prevButton;
		private readonly Button _nextButton;
		private readonly Button _closeButton;

		private StateMachineGraph _asset;
		private readonly List<SearchResult> _results = new();
		private int _currentIndex = -1;

		public NodeSearchBar(StateMachineGraphWindow window)
		{
			_window = window;

			AddToClassList("node-search-bar");

			style.position = Position.Absolute;
			style.top = 8;
			style.left = new StyleLength(new Length(50, LengthUnit.Percent));
			style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Percent), 0));
			style.flexDirection = FlexDirection.Row;
			style.alignItems = Align.Center;
			style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
			style.borderTopLeftRadius = 6;
			style.borderTopRightRadius = 6;
			style.borderBottomLeftRadius = 6;
			style.borderBottomRightRadius = 6;
			style.borderTopWidth = 1;
			style.borderBottomWidth = 1;
			style.borderLeftWidth = 1;
			style.borderRightWidth = 1;
			style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);
			style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
			style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
			style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
			style.paddingLeft = 8;
			style.paddingRight = 4;
			style.paddingTop = 4;
			style.paddingBottom = 4;
			style.display = DisplayStyle.None;

			// Search field
			_searchField = new TextField();
			_searchField.style.minWidth = 200;
			_searchField.style.flexGrow = 1;
			_searchField.RegisterValueChangedCallback(OnSearchChanged);
			_searchField.RegisterCallback<KeyDownEvent>(OnSearchFieldKeyDown);
			Add(_searchField);

			// Match label
			_matchLabel = new Label("");
			_matchLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			_matchLabel.style.fontSize = 11;
			_matchLabel.style.marginLeft = 8;
			_matchLabel.style.marginRight = 4;
			_matchLabel.style.minWidth = 50;
			_matchLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			Add(_matchLabel);

			// Previous button
			_prevButton = CreateNavButton("\u25B2", "Previous match (Shift+Enter)", GoToPrevious);
			Add(_prevButton);

			// Next button
			_nextButton = CreateNavButton("\u25BC", "Next match (Enter)", GoToNext);
			Add(_nextButton);

			// Close button
			_closeButton = new Button(Hide);
			_closeButton.text = "\u2715";
			_closeButton.tooltip = "Close (Escape)";
			_closeButton.style.width = 22;
			_closeButton.style.height = 22;
			_closeButton.style.marginLeft = 4;
			_closeButton.style.paddingLeft = 0;
			_closeButton.style.paddingRight = 0;
			_closeButton.style.paddingTop = 0;
			_closeButton.style.paddingBottom = 0;
			_closeButton.style.fontSize = 10;
			_closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
			_closeButton.style.backgroundColor = Color.clear;
			_closeButton.style.borderTopWidth = 0;
			_closeButton.style.borderBottomWidth = 0;
			_closeButton.style.borderLeftWidth = 0;
			_closeButton.style.borderRightWidth = 0;
			Add(_closeButton);

			// Prevent clicks from propagating to graph view
			RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
		}

		public bool IsVisible => resolvedStyle.display == DisplayStyle.Flex;

		public void Show()
		{
			style.display = DisplayStyle.Flex;
			BringToFront();
			schedule.Execute(() =>
			{
				_searchField.Focus();
				_searchField.SelectAll();
			});
		}

		public void Hide()
		{
			style.display = DisplayStyle.None;
			_searchField.value = "";
			ClearResults();
		}

		public void Toggle()
		{
			if (IsVisible)
			{
				Hide();
			}
			else
			{
				Show();
			}
		}

		public void PopulateFromAsset(StateMachineGraph asset)
		{
			_asset = asset;

			if (IsVisible && !string.IsNullOrEmpty(_searchField.value))
			{
				PerformSearch(_searchField.value);
			}
		}

		private void OnSearchChanged(ChangeEvent<string> evt)
		{
			PerformSearch(evt.newValue);
		}

		private void OnSearchFieldKeyDown(KeyDownEvent evt)
		{
			switch (evt.keyCode)
			{
				case KeyCode.Escape:
					Hide();
					evt.StopPropagation();
					break;

				case KeyCode.Return or KeyCode.KeypadEnter:
					if (evt.shiftKey)
					{
						GoToPrevious();
					}
					else
					{
						GoToNext();
					}
					evt.StopPropagation();
					break;

				case KeyCode.F3:
					if (evt.shiftKey)
					{
						GoToPrevious();
					}
					else
					{
						GoToNext();
					}
					evt.StopPropagation();
					break;
			}
		}

		private void PerformSearch(string query)
		{
			_results.Clear();
			_currentIndex = -1;

			if (string.IsNullOrEmpty(query) || _asset == null)
			{
				UpdateMatchLabel();
				UpdateButtonStates();
				return;
			}

			var queryLower = query.ToLowerInvariant();

			foreach (var node in _asset.GetAllNodes())
			{
				if (node == null) continue;

				var displayName = GetNodeDisplayName(node);
				var typeName = GetNodeTypeName(node);

				var nameScore = FuzzyScore(queryLower, displayName.ToLowerInvariant());
				var typeScore = FuzzyScore(queryLower, typeName.ToLowerInvariant());
				var bestScore = Math.Max(nameScore, typeScore);

				if (bestScore > 0)
				{
					_results.Add(new SearchResult
					{
						Id = node.Id,
						DisplayName = displayName,
						Score = bestScore,
						Type = SearchResultType.Node
					});
				}
			}

			foreach (var region in _asset.Regions)
			{
				if (region == null || string.IsNullOrEmpty(region.Title)) continue;

				var score = FuzzyScore(queryLower, region.Title.ToLowerInvariant());

				if (score > 0)
				{
					_results.Add(new SearchResult
					{
						Id = region.Id,
						DisplayName = region.Title,
						Score = score,
						Type = SearchResultType.Region
					});
				}
			}

			// Sort by score descending (best matches first)
			_results.Sort((a, b) => b.Score.CompareTo(a.Score));

			if (_results.Count > 0)
			{
				_currentIndex = 0;
				NavigateToResult(0);
			}

			UpdateMatchLabel();
			UpdateButtonStates();
		}

		/// <summary>
		/// Computes a fuzzy match score between a query and a target string.
		/// Returns 0 if no match. Higher scores indicate better matches.
		/// </summary>
		public static int FuzzyScore(string query, string target)
		{
			if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
			{
				return 0;
			}

			// Exact match
			if (target == query)
			{
				return 10000;
			}

			// Starts with
			if (target.StartsWith(query))
			{
				return 5000 + (query.Length * 100 / target.Length);
			}

			// Contains substring
			var containsIndex = target.IndexOf(query, StringComparison.Ordinal);
			if (containsIndex >= 0)
			{
				// Bonus for matching at word boundaries
				var wordBoundaryBonus = 0;
				if (containsIndex > 0 && !char.IsLetterOrDigit(target[containsIndex - 1]))
				{
					wordBoundaryBonus = 500;
				}

				return 2000 + wordBoundaryBonus + (query.Length * 100 / target.Length);
			}

			// Fuzzy subsequence match
			var queryIndex = 0;
			var totalGap = 0;
			var lastMatchIndex = -1;
			var consecutiveBonus = 0;

			for (var i = 0; i < target.Length && queryIndex < query.Length; i++)
			{
				if (target[i] == query[queryIndex])
				{
					if (lastMatchIndex >= 0)
					{
						var gap = i - lastMatchIndex - 1;
						totalGap += gap;

						if (gap == 0)
						{
							consecutiveBonus += 50;
						}
					}

					lastMatchIndex = i;
					queryIndex++;
				}
			}

			// All query characters must be found
			if (queryIndex < query.Length)
			{
				return 0;
			}

			// Score based on match density and consecutive matches
			var baseScore = 500;
			var gapPenalty = totalGap * 10;
			var lengthRatio = query.Length * 100 / target.Length;

			return Math.Max(1, baseScore - gapPenalty + consecutiveBonus + lengthRatio);
		}

		private void GoToNext()
		{
			if (_results.Count == 0) return;

			_currentIndex = (_currentIndex + 1) % _results.Count;
			NavigateToResult(_currentIndex);
			UpdateMatchLabel();
		}

		private void GoToPrevious()
		{
			if (_results.Count == 0) return;

			_currentIndex = (_currentIndex - 1 + _results.Count) % _results.Count;
			NavigateToResult(_currentIndex);
			UpdateMatchLabel();
		}

		private void NavigateToResult(int index)
		{
			if (index < 0 || index >= _results.Count) return;

			var result = _results[index];
			var graphView = _window?.GraphView;
			if (graphView == null) return;

			if (result.Type == SearchResultType.Region)
			{
				graphView.SelectRegion(result.Id);
				graphView.CenterOnRegion(result.Id, graphView.UseLerpAnimation);
			}
			else
			{
				graphView.SelectNode(result.Id);
				graphView.CenterOnState(result.Id, graphView.UseLerpAnimation);
			}
		}

		private void ClearResults()
		{
			_results.Clear();
			_currentIndex = -1;
			UpdateMatchLabel();
			UpdateButtonStates();
		}

		private void UpdateMatchLabel()
		{
			if (_results.Count == 0)
			{
				var hasQuery = !string.IsNullOrEmpty(_searchField.value);
				_matchLabel.text = hasQuery ? "No results" : "";
				_matchLabel.style.color = hasQuery
					? new Color(0.8f, 0.4f, 0.4f)
					: new Color(0.6f, 0.6f, 0.6f);
			}
			else
			{
				_matchLabel.text = $"{_currentIndex + 1} of {_results.Count}";
				_matchLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			}
		}

		private void UpdateButtonStates()
		{
			var hasResults = _results.Count > 0;
			_prevButton.SetEnabled(hasResults);
			_nextButton.SetEnabled(hasResults);
		}

		private static Button CreateNavButton(string text, string tooltip, Action onClick)
		{
			var button = new Button(onClick);
			button.text = text;
			button.tooltip = tooltip;
			button.style.width = 22;
			button.style.height = 22;
			button.style.marginLeft = 2;
			button.style.paddingLeft = 0;
			button.style.paddingRight = 0;
			button.style.paddingTop = 0;
			button.style.paddingBottom = 0;
			button.style.fontSize = 10;
			button.style.unityTextAlign = TextAnchor.MiddleCenter;
			button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
			button.style.borderTopLeftRadius = 3;
			button.style.borderTopRightRadius = 3;
			button.style.borderBottomLeftRadius = 3;
			button.style.borderBottomRightRadius = 3;
			button.style.borderTopWidth = 1;
			button.style.borderBottomWidth = 1;
			button.style.borderLeftWidth = 1;
			button.style.borderRightWidth = 1;
			button.style.borderTopColor = new Color(0.15f, 0.15f, 0.15f);
			button.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);
			button.style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f);
			button.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);
			return button;
		}

		private static string GetNodeDisplayName(GraphNode node)
		{
			if (!string.IsNullOrEmpty(node.DisplayName))
			{
				return node.DisplayName;
			}

			if (node is StateNode stateNode && stateNode.StateType != null)
			{
				return stateNode.StateType.Name;
			}

			return node.NodeType.ToString();
		}

		private static string GetNodeTypeName(GraphNode node)
		{
			if (node is StateNode stateNode && stateNode.StateType != null)
			{
				return stateNode.StateType.Name;
			}

			return node.NodeType.ToString();
		}

		private enum SearchResultType
		{
			Node,
			Region
		}

		private struct SearchResult
		{
			public string Id;
			public string DisplayName;
			public int Score;
			public SearchResultType Type;
		}
	}
}
