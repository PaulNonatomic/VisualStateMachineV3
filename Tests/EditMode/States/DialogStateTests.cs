using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class DialogStateTests
	{
		[Test]
		public void DialogState_HasCorrectAttributes()
		{
			var stateType = typeof(DialogState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Note, stateStyle.Icon);
		}

		[Test]
		public void DialogState_HasTextDataIn()
		{
			var field = typeof(DialogState).GetField("Text");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Text", attribute.DisplayName);
			Assert.IsTrue(attribute.Required);
		}

		[Test]
		public void DialogState_HasSpeakerDataIn()
		{
			var field = typeof(DialogState).GetField("Speaker");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Speaker", attribute.DisplayName);
		}

		[Test]
		public void DialogState_HasPortraitDataIn()
		{
			var field = typeof(DialogState).GetField("Portrait");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Portrait", attribute.DisplayName);
		}

		[Test]
		public void DialogState_HasAdvanceKeyDataIn()
		{
			var field = typeof(DialogState).GetField("AdvanceKey");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Advance Key", attribute.DisplayName);
		}

		[Test]
		public void DialogState_HasOutputTextDataOut()
		{
			var field = typeof(DialogState).GetField("OutputText");
			var attribute = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Text", attribute.DisplayName);
		}

		[Test]
		public void DialogState_HasOutputSpeakerDataOut()
		{
			var field = typeof(DialogState).GetField("OutputSpeaker");
			var attribute = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Speaker", attribute.DisplayName);
		}

		[Test]
		public void DialogState_HasOnContinueTransition()
		{
			var field = typeof(DialogState).GetField("OnContinue");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Continue", attribute.DisplayName);
		}

		[Test]
		public void DialogState_AdvanceKeyDefaultsToSpace()
		{
			var state = new DialogState();

			Assert.AreEqual(KeyCode.Space, state.AdvanceKey);
		}

		[Test]
		public void DialogState_SetsOutputTextOnEnter()
		{
			var state = new DialogState { Text = "Hello, world!", Speaker = "NPC" };

			state.OnEnter();

			Assert.AreEqual("Hello, world!", state.OutputText);
			Assert.AreEqual("NPC", state.OutputSpeaker);
		}

		[Test]
		public void DialogState_NullText_InvokesOnContinue()
		{
			var state = new DialogState { Text = null };
			var transitionInvoked = false;
			state.OnContinue = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void DialogState_EmptyText_InvokesOnContinue()
		{
			var state = new DialogState { Text = "" };
			var transitionInvoked = false;
			state.OnContinue = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void DialogState_NullOnContinue_DoesNotThrow()
		{
			var state = new DialogState
			{
				Text = null,
				OnContinue = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class TimedDialogStateTests
	{
		[Test]
		public void TimedDialogState_HasCorrectAttributes()
		{
			var stateType = typeof(TimedDialogState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Note, stateStyle.Icon);
		}

		[Test]
		public void TimedDialogState_HasTextDataIn()
		{
			var field = typeof(TimedDialogState).GetField("Text");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Text", attribute.DisplayName);
			Assert.IsTrue(attribute.Required);
		}

		[Test]
		public void TimedDialogState_HasSpeakerDataIn()
		{
			var field = typeof(TimedDialogState).GetField("Speaker");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Speaker", attribute.DisplayName);
		}

		[Test]
		public void TimedDialogState_HasDurationDataIn()
		{
			var field = typeof(TimedDialogState).GetField("Duration");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Duration", attribute.DisplayName);
		}

		[Test]
		public void TimedDialogState_HasAllowSkipDataIn()
		{
			var field = typeof(TimedDialogState).GetField("AllowSkip");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Allow Skip", attribute.DisplayName);
		}

		[Test]
		public void TimedDialogState_HasSkipKeyDataIn()
		{
			var field = typeof(TimedDialogState).GetField("SkipKey");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Skip Key", attribute.DisplayName);
		}

		[Test]
		public void TimedDialogState_HasProgressDataOut()
		{
			var field = typeof(TimedDialogState).GetField("Progress");
			var attribute = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Progress", attribute.DisplayName);
		}

		[Test]
		public void TimedDialogState_HasOnContinueTransition()
		{
			var field = typeof(TimedDialogState).GetField("OnContinue");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Continue", attribute.DisplayName);
		}

		[Test]
		public void TimedDialogState_DurationDefaultsToThree()
		{
			var state = new TimedDialogState();

			Assert.AreEqual(3f, state.Duration);
		}

		[Test]
		public void TimedDialogState_AllowSkipDefaultsToTrue()
		{
			var state = new TimedDialogState();

			Assert.IsTrue(state.AllowSkip);
		}

		[Test]
		public void TimedDialogState_SkipKeyDefaultsToSpace()
		{
			var state = new TimedDialogState();

			Assert.AreEqual(KeyCode.Space, state.SkipKey);
		}

		[Test]
		public void TimedDialogState_SetsOutputTextOnEnter()
		{
			var state = new TimedDialogState { Text = "Narration text", Speaker = "Narrator" };

			state.OnEnter();

			Assert.AreEqual("Narration text", state.OutputText);
			Assert.AreEqual("Narrator", state.OutputSpeaker);
		}

		[Test]
		public void TimedDialogState_ZeroDuration_InvokesOnContinue()
		{
			var state = new TimedDialogState { Text = "Quick text", Duration = 0f };
			var transitionInvoked = false;
			state.OnContinue = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
			Assert.AreEqual(1f, state.Progress);
		}

		[Test]
		public void TimedDialogState_NullText_InvokesOnContinue()
		{
			var state = new TimedDialogState { Text = null };
			var transitionInvoked = false;
			state.OnContinue = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void TimedDialogState_NullOnContinue_DoesNotThrow()
		{
			var state = new TimedDialogState
			{
				Text = null,
				OnContinue = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class DialogChoiceStateTests
	{
		[Test]
		public void DialogChoiceState_HasCorrectAttributes()
		{
			var stateType = typeof(DialogChoiceState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Menu, stateStyle.Icon);
		}

		[Test]
		public void DialogChoiceState_HasChoiceADataIn()
		{
			var field = typeof(DialogChoiceState).GetField("ChoiceA");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice A", attribute.DisplayName);
			Assert.IsTrue(attribute.Required);
		}

		[Test]
		public void DialogChoiceState_HasChoiceBDataIn()
		{
			var field = typeof(DialogChoiceState).GetField("ChoiceB");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice B", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasChoiceCDataIn()
		{
			var field = typeof(DialogChoiceState).GetField("ChoiceC");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice C", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasChoiceDDataIn()
		{
			var field = typeof(DialogChoiceState).GetField("ChoiceD");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice D", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasSelectedIndexDataOut()
		{
			var field = typeof(DialogChoiceState).GetField("SelectedIndex");
			var attribute = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Selected Index", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasSelectedTextDataOut()
		{
			var field = typeof(DialogChoiceState).GetField("SelectedText");
			var attribute = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Selected Text", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasOnChoiceATransition()
		{
			var field = typeof(DialogChoiceState).GetField("OnChoiceA");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice A", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasOnChoiceBTransition()
		{
			var field = typeof(DialogChoiceState).GetField("OnChoiceB");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice B", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasOnChoiceCTransition()
		{
			var field = typeof(DialogChoiceState).GetField("OnChoiceC");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice C", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_HasOnChoiceDTransition()
		{
			var field = typeof(DialogChoiceState).GetField("OnChoiceD");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Choice D", attribute.DisplayName);
		}

		[Test]
		public void DialogChoiceState_SetsSelectedIndexToMinusOneOnEnter()
		{
			var state = new DialogChoiceState { ChoiceA = "Yes" };

			state.OnEnter();

			Assert.AreEqual(-1, state.SelectedIndex);
			Assert.IsNull(state.SelectedText);
		}

		[Test]
		public void DialogChoiceState_NullChoiceA_DoesNotThrow()
		{
			var state = new DialogChoiceState { ChoiceA = null };

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class ClearDialogStateTests
	{
		[Test]
		public void ClearDialogState_HasCorrectAttributes()
		{
			var stateType = typeof(ClearDialogState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Orange, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Erase, stateStyle.Icon);
		}

		[Test]
		public void ClearDialogState_HasOnClearDataIn()
		{
			var field = typeof(ClearDialogState).GetField("OnClear");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("On Clear", attribute.DisplayName);
		}

		[Test]
		public void ClearDialogState_HasOnCompleteTransition()
		{
			var field = typeof(ClearDialogState).GetField("OnComplete");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Done", attribute.DisplayName);
		}

		[Test]
		public void ClearDialogState_InvokesOnComplete()
		{
			var state = new ClearDialogState();
			var transitionInvoked = false;
			state.OnComplete = () => transitionInvoked = true;

			state.OnEnter();

			Assert.IsTrue(transitionInvoked);
		}

		[Test]
		public void ClearDialogState_NullOnClear_DoesNotThrow()
		{
			var state = new ClearDialogState
			{
				OnClear = null,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class DialogStatesCommonTests
	{
		[Test]
		public void AllDialogStates_HaveOrangeColor()
		{
			var stateTypes = new[]
			{
				typeof(DialogState),
				typeof(TimedDialogState),
				typeof(DialogChoiceState),
				typeof(ClearDialogState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(StateColors.Orange, stateStyle.Color,
					$"{stateType.Name} should have Orange color");
			}
		}

		[Test]
		public void AllDialogStates_HaveStateInfoAttribute()
		{
			var stateTypes = new[]
			{
				typeof(DialogState),
				typeof(TimedDialogState),
				typeof(DialogChoiceState),
				typeof(ClearDialogState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
				Assert.IsNotNull(stateInfo, $"{stateType.Name} should have StateInfoAttribute");
				Assert.IsFalse(string.IsNullOrEmpty(stateInfo.Tooltip),
					$"{stateType.Name} should have a tooltip");
			}
		}
	}
}
