using System;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Dialogue
{
	/// <summary>
	/// A single line of dialogue with speaker name and text.
	/// </summary>
	[Serializable]
	public struct DialogueLine
	{
		[SerializeField] private string _speaker;
		[SerializeField] [TextArea(2, 4)] private string _text;

		public string Speaker => _speaker;
		public string Text => _text;

		public DialogueLine(string speaker, string text)
		{
			_speaker = speaker;
			_text = text;
		}
	}
}
