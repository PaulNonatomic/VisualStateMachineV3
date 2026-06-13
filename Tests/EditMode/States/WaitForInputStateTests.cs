using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class WaitForKeyStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(WaitForKeyState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Indigo, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Command, stateStyle.Icon);
		}

		[Test]
		public void DefaultKey_IsSpace()
		{
			var state = new WaitForKeyState();

			Assert.AreEqual(KeyCode.Space, state.Key);
		}

		[Test]
		public void DefaultInputMode_IsDown()
		{
			var state = new WaitForKeyState();

			Assert.AreEqual(KeyInputMode.Down, state.InputMode);
		}

		[Test]
		public void DefaultOnPressed_IsNull()
		{
			var state = new WaitForKeyState();

			Assert.IsNull(state.OnPressed);
		}

		[Test]
		public void HasKeyDataIn()
		{
			var field = typeof(WaitForKeyState).GetField("Key");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Key", attribute.DisplayName);
		}

		[Test]
		public void HasInputModeDataIn()
		{
			var field = typeof(WaitForKeyState).GetField("InputMode");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Input Mode", attribute.DisplayName);
		}

		[Test]
		public void HasPressedTransition()
		{
			var field = typeof(WaitForKeyState).GetField("OnPressed");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Pressed", attribute.DisplayName);
		}

		[Test]
		public void InheritsFromState()
		{
			Assert.IsTrue(typeof(Nonatomic.VSM.Core.State).IsAssignableFrom(typeof(WaitForKeyState)));
		}
	}

	[TestFixture]
	public class WaitForAnyKeyStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(WaitForAnyKeyState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Indigo, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Command, stateStyle.Icon);
		}

		[Test]
		public void DefaultOnPressed_IsNull()
		{
			var state = new WaitForAnyKeyState();

			Assert.IsNull(state.OnPressed);
		}

		[Test]
		public void HasPressedTransition()
		{
			var field = typeof(WaitForAnyKeyState).GetField("OnPressed");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Pressed", attribute.DisplayName);
		}

		[Test]
		public void InheritsFromState()
		{
			Assert.IsTrue(typeof(Nonatomic.VSM.Core.State).IsAssignableFrom(typeof(WaitForAnyKeyState)));
		}
	}

	[TestFixture]
	public class WaitForMouseButtonStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateType = typeof(WaitForMouseButtonState);

			var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.Indigo, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Archery, stateStyle.Icon);
		}

		[Test]
		public void DefaultButton_IsZero()
		{
			var state = new WaitForMouseButtonState();

			Assert.AreEqual(0, state.Button);
		}

		[Test]
		public void DefaultInputMode_IsDown()
		{
			var state = new WaitForMouseButtonState();

			Assert.AreEqual(KeyInputMode.Down, state.InputMode);
		}

		[Test]
		public void DefaultOnClicked_IsNull()
		{
			var state = new WaitForMouseButtonState();

			Assert.IsNull(state.OnClicked);
		}

		[Test]
		public void HasButtonDataIn()
		{
			var field = typeof(WaitForMouseButtonState).GetField("Button");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Button", attribute.DisplayName);
		}

		[Test]
		public void HasInputModeDataIn()
		{
			var field = typeof(WaitForMouseButtonState).GetField("InputMode");
			var attribute = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Input Mode", attribute.DisplayName);
		}

		[Test]
		public void HasMousePositionDataOut()
		{
			var field = typeof(WaitForMouseButtonState).GetField("MousePosition");
			var attribute = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Mouse Position", attribute.DisplayName);
		}

		[Test]
		public void HasClickedTransition()
		{
			var field = typeof(WaitForMouseButtonState).GetField("OnClicked");
			var attribute = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Clicked", attribute.DisplayName);
		}

		[Test]
		public void InheritsFromState()
		{
			Assert.IsTrue(typeof(Nonatomic.VSM.Core.State).IsAssignableFrom(typeof(WaitForMouseButtonState)));
		}
	}

	[TestFixture]
	public class KeyInputModeEnumTests
	{
		[Test]
		public void HasExpectedValues()
		{
			Assert.AreEqual(0, (int)KeyInputMode.Down);
			Assert.AreEqual(1, (int)KeyInputMode.Up);
			Assert.AreEqual(2, (int)KeyInputMode.Held);
		}

		[Test]
		public void HasThreeValues()
		{
			var values = Enum.GetValues(typeof(KeyInputMode));
			Assert.AreEqual(3, values.Length);
		}
	}
}
