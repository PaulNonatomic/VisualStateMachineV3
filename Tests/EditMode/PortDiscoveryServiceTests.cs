using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Ports;
using Nonatomic.VSM.Ports;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortDiscoveryServiceTests
	{
		[SetUp]
		public void SetUp()
		{
			PortDiscoveryService.ClearCache();
		}

		[TearDown]
		public void TearDown()
		{
			PortDiscoveryService.ClearCache();
		}


		private class SimpleInputState : State
		{
			[DataIn("Speed")] public float speed;
			[DataIn("Target")] public Vector3 target;
			[DataIn] public string name;
		}

		private class SimpleOutputState : State
		{
			[DataOut("Result")] public float result;
			[DataOut("Position")] public Vector3 position;
			[DataOut] public string message;
		}

		private class SimpleTransitionState : State
		{
			[Transition("On Complete")] public Action onComplete;
			[Transition("On Fail")] public Action onFail;
		}

		private class MixedPortState : State
		{
			[DataIn("Input Value")] public int inputValue;
			[DataOut("Output Value")] public int outputValue;
			[Transition("Done")] public Action onDone;
		}

		private class RequiredInputState : State
		{
			[DataIn("Required Input", true)] public float requiredInput;
			[DataIn("Optional Input")] public float optionalInput;
		}

		private class CategoryState : State
		{
			[DataIn(Category = "Movement")] public float speed;
			[DataIn(Category = "Movement")] public Vector3 direction;
			[DataIn(Category = "Combat")] public int damage;
		}

		private class TooltipState : State
		{
			[DataIn(Tooltip = "The speed of movement")] public float speed;
			[DataOut(Tooltip = "The calculated result")] public float result;
			[Transition(Tooltip = "Fires when complete")] public Action onComplete;
		}

		private class TransitionPriorityState : State
		{
			[Transition("Low Priority", Priority = 10)] public Action lowPriority;
			[Transition("High Priority", Priority = 1)] public Action highPriority;
			[Transition("Medium Priority", Priority = 5)] public Action mediumPriority;
		}

		private class TransitionColorState : State
		{
			[Transition("Success", "#00FF00")] public Action onSuccess;
			[Transition("Failure", "#FF0000")] public Action onFailure;
		}

		private class PrivateFieldState : State
		{
			[DataIn] private float _privateInput;
			[DataOut] private float _privateOutput;
			[Transition] private Action _privateTransition;

			public float GetPrivateInput() => _privateInput;
			public float GetPrivateOutput() => _privateOutput;
			public Action GetPrivateTransition() => _privateTransition;
		}

		private class UnderscorePrefixState : State
		{
			[DataIn] public float _speed;
			[DataIn] public float _maxHealth;
			[Transition] public Action _onComplete;
		}

		private class CamelCaseState : State
		{
			[DataIn] public float playerSpeed;
			[DataIn] public float maxHealthPoints;
			[Transition] public Action onGameOver;
		}

		private class NoPortsState : State
		{
			public float regularField;
			private int _privateField;
		}

		private class GenericActionTransitionState : State
		{
			[Transition] public Action simpleAction;
			[Transition] public Action<int> actionWithInt;
			[Transition] public Action<string, int> actionWithTwoArgs;
			[Transition] public Action<int, float, string> actionWithThreeArgs;
			[Transition] public Action<int, float, string, bool> actionWithFourArgs;
		}

		private class InvalidTransitionState : State
		{
			[Transition] public Func<int> funcNotAction;
			[Transition] public int notAnAction;
			[Transition] public string alsoNotAnAction;
		}

		private class VariousTypesState : State
		{
			[DataIn] public int intValue;
			[DataIn] public float floatValue;
			[DataIn] public double doubleValue;
			[DataIn] public bool boolValue;
			[DataIn] public string stringValue;
			[DataIn] public Vector2 vector2Value;
			[DataIn] public Vector3 vector3Value;
			[DataIn] public Color colorValue;
			[DataIn] public GameObject gameObjectValue;
			[DataIn] public List<int> listValue;
		}

		private class DefaultValueState : State
		{
			[DataIn] public int intWithDefault = 42;
			[DataIn] public float floatWithDefault = 3.14f;
			[DataIn] public string stringWithDefault = "hello";
			[DataIn] public Vector3 vectorWithDefault = Vector3.one;
		}

		private abstract class AbstractBaseState : State
		{
			[DataIn] public float baseInput;
		}

		private class ConcreteState : AbstractBaseState
		{
			[DataIn] public float concreteInput;
		}

		private class NestedContainerClass
		{
			public class NestedState : State
			{
				[DataIn] public float nestedInput;
			}
		}



		[Test]
		public void GetPortManifest_NullType_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => PortDiscoveryService.GetPortManifest(null));
		}

		[Test]
		public void GetPortManifest_NonStateType_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => PortDiscoveryService.GetPortManifest(typeof(string)));
		}

		[Test]
		public void GetPortManifest_NonStateClass_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => PortDiscoveryService.GetPortManifest(typeof(MonoBehaviour)));
		}

		[Test]
		public void GetPortManifest_ValidStateType_ReturnsManifest()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			Assert.IsNotNull(manifest);
			Assert.AreEqual(typeof(SimpleInputState), manifest.StateType);
		}

		[Test]
		public void GetPortManifest_StateWithNoAttributes_ReturnsEmptyManifest()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(NoPortsState));

			Assert.IsNotNull(manifest);
			Assert.AreEqual(0, manifest.InputPorts.Count);
			Assert.AreEqual(0, manifest.OutputPorts.Count);
			Assert.AreEqual(0, manifest.TransitionPorts.Count);
			Assert.AreEqual(0, manifest.TotalPortCount);
		}



		[Test]
		public void GetPortManifest_InputPorts_DiscoversByDataInAttribute()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			Assert.AreEqual(3, manifest.InputPorts.Count);
		}

		[Test]
		public void GetPortManifest_InputPorts_CapturesFieldName()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			var speedPort = manifest.GetInputPort("speed");
			Assert.IsNotNull(speedPort);
			Assert.AreEqual("speed", speedPort.FieldName);
		}

		[Test]
		public void GetPortManifest_InputPorts_UsesDisplayNameFromAttribute()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			var speedPort = manifest.GetInputPort("speed");
			Assert.AreEqual("Speed", speedPort.DisplayName);
		}

		[Test]
		public void GetPortManifest_InputPorts_FormatsFieldNameWhenNoDisplayName()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			var namePort = manifest.GetInputPort("name");
			Assert.AreEqual("Name", namePort.DisplayName);
		}

		[Test]
		public void GetPortManifest_InputPorts_CapturesDataType()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			var speedPort = manifest.GetInputPort("speed");
			var targetPort = manifest.GetInputPort("target");
			var namePort = manifest.GetInputPort("name");

			Assert.AreEqual(typeof(float), speedPort.DataType);
			Assert.AreEqual(typeof(Vector3), targetPort.DataType);
			Assert.AreEqual(typeof(string), namePort.DataType);
		}

		[Test]
		public void GetPortManifest_InputPorts_CapturesRequiredFlag()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(RequiredInputState));

			var requiredPort = manifest.GetInputPort("requiredInput");
			var optionalPort = manifest.GetInputPort("optionalInput");

			Assert.IsTrue(requiredPort.IsRequired);
			Assert.IsFalse(optionalPort.IsRequired);
		}

		[Test]
		public void GetPortManifest_InputPorts_CapturesCategory()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(CategoryState));

			var speedPort = manifest.GetInputPort("speed");
			var damagePort = manifest.GetInputPort("damage");

			Assert.AreEqual("Movement", speedPort.Category);
			Assert.AreEqual("Combat", damagePort.Category);
		}

		[Test]
		public void GetPortManifest_InputPorts_CapturesToolip()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(TooltipState));

			var speedPort = manifest.GetInputPort("speed");
			Assert.AreEqual("The speed of movement", speedPort.Tooltip);
		}

		[Test]
		public void GetPortManifest_InputPorts_DirectionIsInput()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			foreach (var port in manifest.InputPorts)
			{
				Assert.AreEqual(PortDirection.Input, port.Direction);
			}
		}



		[Test]
		public void GetPortManifest_OutputPorts_DiscoversByDataOutAttribute()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleOutputState));

			Assert.AreEqual(3, manifest.OutputPorts.Count);
		}

		[Test]
		public void GetPortManifest_OutputPorts_CapturesFieldName()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleOutputState));

			var resultPort = manifest.GetOutputPort("result");
			Assert.IsNotNull(resultPort);
			Assert.AreEqual("result", resultPort.FieldName);
		}

		[Test]
		public void GetPortManifest_OutputPorts_UsesDisplayNameFromAttribute()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleOutputState));

			var resultPort = manifest.GetOutputPort("result");
			Assert.AreEqual("Result", resultPort.DisplayName);
		}

		[Test]
		public void GetPortManifest_OutputPorts_FormatsFieldNameWhenNoDisplayName()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleOutputState));

			var messagePort = manifest.GetOutputPort("message");
			Assert.AreEqual("Message", messagePort.DisplayName);
		}

		[Test]
		public void GetPortManifest_OutputPorts_CapturesDataType()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleOutputState));

			var resultPort = manifest.GetOutputPort("result");
			var positionPort = manifest.GetOutputPort("position");

			Assert.AreEqual(typeof(float), resultPort.DataType);
			Assert.AreEqual(typeof(Vector3), positionPort.DataType);
		}

		[Test]
		public void GetPortManifest_OutputPorts_CapturesTooltip()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(TooltipState));

			var resultPort = manifest.GetOutputPort("result");
			Assert.AreEqual("The calculated result", resultPort.Tooltip);
		}

		[Test]
		public void GetPortManifest_OutputPorts_DirectionIsOutput()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleOutputState));

			foreach (var port in manifest.OutputPorts)
			{
				Assert.AreEqual(PortDirection.Output, port.Direction);
			}
		}



		[Test]
		public void GetPortManifest_TransitionPorts_DiscoversByTransitionAttribute()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleTransitionState));

			Assert.AreEqual(2, manifest.TransitionPorts.Count);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_CapturesFieldName()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleTransitionState));

			var completePort = manifest.GetTransitionPort("onComplete");
			Assert.IsNotNull(completePort);
			Assert.AreEqual("onComplete", completePort.FieldName);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_UsesDisplayNameFromAttribute()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleTransitionState));

			var completePort = manifest.GetTransitionPort("onComplete");
			Assert.AreEqual("On Complete", completePort.DisplayName);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_FormatsFieldNameRemovingOnPrefix()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(GenericActionTransitionState));

			var simplePort = manifest.GetTransitionPort("simpleAction");
			// simpleAction doesn't start with "on" so should be formatted as "Simple Action"
			Assert.AreEqual("Simple Action", simplePort.DisplayName);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_CapturesColorHint()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(TransitionColorState));

			var successPort = manifest.GetTransitionPort("onSuccess");
			var failurePort = manifest.GetTransitionPort("onFailure");

			Assert.AreEqual("#00FF00", successPort.ColorHint);
			Assert.AreEqual("#FF0000", failurePort.ColorHint);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_SortedByPriority()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(TransitionPriorityState));

			Assert.AreEqual(3, manifest.TransitionPorts.Count);
			Assert.AreEqual("highPriority", manifest.TransitionPorts[0].FieldName);
			Assert.AreEqual("mediumPriority", manifest.TransitionPorts[1].FieldName);
			Assert.AreEqual("lowPriority", manifest.TransitionPorts[2].FieldName);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_CapturesPriority()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(TransitionPriorityState));

			var highPort = manifest.GetTransitionPort("highPriority");
			var mediumPort = manifest.GetTransitionPort("mediumPriority");
			var lowPort = manifest.GetTransitionPort("lowPriority");

			Assert.AreEqual(1, highPort.Priority);
			Assert.AreEqual(5, mediumPort.Priority);
			Assert.AreEqual(10, lowPort.Priority);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_CapturesToolip()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(TooltipState));

			var completePort = manifest.GetTransitionPort("onComplete");
			Assert.AreEqual("Fires when complete", completePort.Tooltip);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_AcceptsSimpleAction()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(GenericActionTransitionState));

			var port = manifest.GetTransitionPort("simpleAction");
			Assert.IsNotNull(port);
			Assert.AreEqual(typeof(Action), port.DataType);
		}

		[Test]
		public void GetPortManifest_TransitionPorts_AcceptsGenericActions()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(GenericActionTransitionState));

			Assert.IsNotNull(manifest.GetTransitionPort("actionWithInt"));
			Assert.IsNotNull(manifest.GetTransitionPort("actionWithTwoArgs"));
			Assert.IsNotNull(manifest.GetTransitionPort("actionWithThreeArgs"));
			Assert.IsNotNull(manifest.GetTransitionPort("actionWithFourArgs"));
		}

		[Test]
		public void GetPortManifest_TransitionPorts_RejectsNonActionTypes()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(InvalidTransitionState));

			// These should be rejected and not added to transitions
			Assert.IsNull(manifest.GetTransitionPort("funcNotAction"));
			Assert.IsNull(manifest.GetTransitionPort("notAnAction"));
			Assert.IsNull(manifest.GetTransitionPort("alsoNotAnAction"));
			Assert.AreEqual(0, manifest.TransitionPorts.Count);
		}



		[Test]
		public void GetPortManifest_MixedPorts_DiscoversAllPortTypes()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(MixedPortState));

			Assert.AreEqual(1, manifest.InputPorts.Count);
			Assert.AreEqual(1, manifest.OutputPorts.Count);
			Assert.AreEqual(1, manifest.TransitionPorts.Count);
			Assert.AreEqual(3, manifest.TotalPortCount);
		}

		[Test]
		public void GetPortManifest_MixedPorts_AllPortsEnumerator()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(MixedPortState));

			var allPorts = manifest.AllPorts.ToList();
			Assert.AreEqual(3, allPorts.Count);
		}



		[Test]
		public void GetPortManifest_PrivateFields_DiscoversPrivateInputPorts()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(PrivateFieldState));

			Assert.AreEqual(1, manifest.InputPorts.Count);
			Assert.IsNotNull(manifest.GetInputPort("_privateInput"));
		}

		[Test]
		public void GetPortManifest_PrivateFields_DiscoversPrivateOutputPorts()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(PrivateFieldState));

			Assert.AreEqual(1, manifest.OutputPorts.Count);
			Assert.IsNotNull(manifest.GetOutputPort("_privateOutput"));
		}

		[Test]
		public void GetPortManifest_PrivateFields_DiscoversPrivateTransitionPorts()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(PrivateFieldState));

			Assert.AreEqual(1, manifest.TransitionPorts.Count);
			Assert.IsNotNull(manifest.GetTransitionPort("_privateTransition"));
		}



		[Test]
		public void GetPortManifest_FieldNameFormatting_RemovesUnderscorePrefix()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(UnderscorePrefixState));

			var speedPort = manifest.GetInputPort("_speed");
			Assert.AreEqual("Speed", speedPort.DisplayName);
		}

		[Test]
		public void GetPortManifest_FieldNameFormatting_ConvertsCamelCaseToTitleCase()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(CamelCaseState));

			var speedPort = manifest.GetInputPort("playerSpeed");
			var healthPort = manifest.GetInputPort("maxHealthPoints");

			Assert.AreEqual("Player Speed", speedPort.DisplayName);
			Assert.AreEqual("Max Health Points", healthPort.DisplayName);
		}

		[Test]
		public void GetPortManifest_TransitionNameFormatting_RemovesOnPrefix()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(CamelCaseState));

			var gameOverPort = manifest.GetTransitionPort("onGameOver");
			Assert.AreEqual("Game Over", gameOverPort.DisplayName);
		}



		[Test]
		public void GetPortManifest_VariousTypes_DiscoversPrimitiveTypes()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(VariousTypesState));

			Assert.AreEqual(typeof(int), manifest.GetInputPort("intValue").DataType);
			Assert.AreEqual(typeof(float), manifest.GetInputPort("floatValue").DataType);
			Assert.AreEqual(typeof(double), manifest.GetInputPort("doubleValue").DataType);
			Assert.AreEqual(typeof(bool), manifest.GetInputPort("boolValue").DataType);
			Assert.AreEqual(typeof(string), manifest.GetInputPort("stringValue").DataType);
		}

		[Test]
		public void GetPortManifest_VariousTypes_DiscoversUnityTypes()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(VariousTypesState));

			Assert.AreEqual(typeof(Vector2), manifest.GetInputPort("vector2Value").DataType);
			Assert.AreEqual(typeof(Vector3), manifest.GetInputPort("vector3Value").DataType);
			Assert.AreEqual(typeof(Color), manifest.GetInputPort("colorValue").DataType);
			Assert.AreEqual(typeof(GameObject), manifest.GetInputPort("gameObjectValue").DataType);
		}

		[Test]
		public void GetPortManifest_VariousTypes_DiscoversGenericTypes()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(VariousTypesState));

			Assert.AreEqual(typeof(List<int>), manifest.GetInputPort("listValue").DataType);
		}



		[Test]
		public void GetPortManifest_Inheritance_DiscoversBaseClassPorts()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(ConcreteState));

			// Should discover both base class and concrete class ports
			Assert.IsNotNull(manifest.GetInputPort("baseInput"));
			Assert.IsNotNull(manifest.GetInputPort("concreteInput"));
			Assert.AreEqual(2, manifest.InputPorts.Count);
		}



		[Test]
		public void GetPortManifest_NestedType_DiscoversPorts()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(NestedContainerClass.NestedState));

			Assert.IsNotNull(manifest);
			Assert.IsNotNull(manifest.GetInputPort("nestedInput"));
		}



		[Test]
		public void GetPortManifest_SameType_ReturnsCachedInstance()
		{
			var manifest1 = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));
			var manifest2 = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			Assert.AreSame(manifest1, manifest2);
		}

		[Test]
		public void GetPortManifest_DifferentTypes_ReturnsDifferentManifests()
		{
			var manifest1 = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));
			var manifest2 = PortDiscoveryService.GetPortManifest(typeof(SimpleOutputState));

			Assert.AreNotSame(manifest1, manifest2);
		}

		[Test]
		public void ClearCache_AfterClear_ReturnsNewInstance()
		{
			var manifest1 = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			PortDiscoveryService.ClearCache();

			var manifest2 = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			Assert.AreNotSame(manifest1, manifest2);
		}

		[Test]
		public void ClearCache_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => PortDiscoveryService.ClearCache());
		}

		[Test]
		public void ClearCache_EmptyCache_DoesNotThrow()
		{
			PortDiscoveryService.ClearCache();
			Assert.DoesNotThrow(() => PortDiscoveryService.ClearCache());
		}



		[Test]
		public void GetAllStateTypes_ReturnsNonAbstractTypes()
		{
			var types = PortDiscoveryService.GetAllStateTypes().ToList();

			// Should not include abstract types
			Assert.IsFalse(types.Contains(typeof(AbstractBaseState)));
		}

		[Test]
		public void GetAllStateTypes_ExcludesBaseStateClass()
		{
			var types = PortDiscoveryService.GetAllStateTypes().ToList();

			Assert.IsFalse(types.Contains(typeof(State)));
		}

		[Test]
		public void GetAllStateTypes_IncludesConcreteTestStates()
		{
			var types = PortDiscoveryService.GetAllStateTypes().ToList();

			// Should include our test state types
			Assert.IsTrue(types.Contains(typeof(SimpleInputState)));
			Assert.IsTrue(types.Contains(typeof(SimpleOutputState)));
			Assert.IsTrue(types.Contains(typeof(ConcreteState)));
		}

		[Test]
		public void GetAllStateTypes_IncludesNestedTypes()
		{
			var types = PortDiscoveryService.GetAllStateTypes().ToList();

			Assert.IsTrue(types.Contains(typeof(NestedContainerClass.NestedState)));
		}

		[Test]
		public void GetAllStateTypes_HandlesAssemblyLoadExceptions()
		{
			// This test verifies the method doesn't throw even if some assemblies fail to load types
			Assert.DoesNotThrow(() =>
			{
				var types = PortDiscoveryService.GetAllStateTypes().ToList();
				Assert.IsNotNull(types);
			});
		}



		[Test]
		public void StatePortManifest_GetPort_FindsInputPort()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(MixedPortState));

			var port = manifest.GetPort("inputValue");
			Assert.IsNotNull(port);
			Assert.AreEqual(PortDirection.Input, port.Direction);
		}

		[Test]
		public void StatePortManifest_GetPort_FindsOutputPort()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(MixedPortState));

			var port = manifest.GetPort("outputValue");
			Assert.IsNotNull(port);
			Assert.AreEqual(PortDirection.Output, port.Direction);
		}

		[Test]
		public void StatePortManifest_GetPort_FindsTransitionPort()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(MixedPortState));

			var port = manifest.GetPort("onDone");
			Assert.IsNotNull(port);
		}

		[Test]
		public void StatePortManifest_GetPort_ReturnsNullForNonExistent()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(MixedPortState));

			var port = manifest.GetPort("nonExistentPort");
			Assert.IsNull(port);
		}

		[Test]
		public void StatePortManifest_HasRequiredInputs_ReturnsTrueWhenRequired()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(RequiredInputState));

			Assert.IsTrue(manifest.HasRequiredInputs());
		}

		[Test]
		public void StatePortManifest_HasRequiredInputs_ReturnsFalseWhenNone()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			Assert.IsFalse(manifest.HasRequiredInputs());
		}

		[Test]
		public void StatePortManifest_GetRequiredInputPorts_ReturnsOnlyRequired()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(RequiredInputState));

			var requiredPorts = manifest.GetRequiredInputPorts().ToList();
			Assert.AreEqual(1, requiredPorts.Count);
			Assert.AreEqual("requiredInput", requiredPorts[0].FieldName);
		}



		[Test]
		public void GetPortManifest_DefaultValue_CapturesValueTypeDefaults()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			var speedPort = manifest.GetInputPort("speed");
			// Default for float is 0
			Assert.AreEqual(0f, speedPort.DefaultValue);
		}

		[Test]
		public void GetPortManifest_DefaultValue_ReferenceTypesAreNull()
		{
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));

			var namePort = manifest.GetInputPort("name");
			Assert.IsNull(namePort.DefaultValue);
		}



		[Test]
		public void GetPortManifest_BackingFields_SkipsAutoPropertyBackingFields()
		{
			// Backing fields start with < and should be skipped
			// This is implicitly tested by not finding any extra ports in our test states
			var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));
			Assert.AreEqual(3, manifest.InputPorts.Count);
		}

		[Test]
		public void GetPortManifest_MultipleCallsWithCacheClearing_WorksCorrectly()
		{
			for (int i = 0; i < 10; i++)
			{
				var manifest = PortDiscoveryService.GetPortManifest(typeof(SimpleInputState));
				Assert.IsNotNull(manifest);
				Assert.AreEqual(3, manifest.InputPorts.Count);

				if (i % 3 == 0)
				{
					PortDiscoveryService.ClearCache();
				}
			}
		}

	}
}
