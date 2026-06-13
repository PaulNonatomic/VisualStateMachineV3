using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Ports;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StatePortManifestTests
	{

		[Test]
		public void StateTypeName_SetAndGet_ReturnsValue()
		{
			var manifest = new StatePortManifest();

			manifest.StateTypeName = "MyState";

			Assert.AreEqual("MyState", manifest.StateTypeName);
		}

		[Test]
		public void StateType_SetAndGet_ReturnsValue()
		{
			var manifest = new StatePortManifest();

			manifest.StateType = typeof(int);

			Assert.AreEqual(typeof(int), manifest.StateType);
		}

		[Test]
		public void StateType_Set_AlsoSetsTypeName()
		{
			var manifest = new StatePortManifest();

			manifest.StateType = typeof(string);

			Assert.IsNotNull(manifest.StateTypeName);
			Assert.IsTrue(manifest.StateTypeName.Contains("String"));
		}

		[Test]
		public void StateType_SetNull_ClearsTypeName()
		{
			var manifest = new StatePortManifest();
			manifest.StateType = typeof(int);

			manifest.StateType = null;

			Assert.IsNull(manifest.StateTypeName);
		}

		[Test]
		public void InputPorts_DefaultIsEmpty()
		{
			var manifest = new StatePortManifest();

			Assert.IsNotNull(manifest.InputPorts);
			Assert.AreEqual(0, manifest.InputPorts.Count);
		}

		[Test]
		public void OutputPorts_DefaultIsEmpty()
		{
			var manifest = new StatePortManifest();

			Assert.IsNotNull(manifest.OutputPorts);
			Assert.AreEqual(0, manifest.OutputPorts.Count);
		}

		[Test]
		public void TransitionPorts_DefaultIsEmpty()
		{
			var manifest = new StatePortManifest();

			Assert.IsNotNull(manifest.TransitionPorts);
			Assert.AreEqual(0, manifest.TransitionPorts.Count);
		}



		[Test]
		public void TotalPortCount_Empty_ReturnsZero()
		{
			var manifest = new StatePortManifest();

			Assert.AreEqual(0, manifest.TotalPortCount);
		}

		[Test]
		public void TotalPortCount_WithPorts_ReturnsTotalAcrossAllTypes()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_input1", typeof(int)));
			manifest.InputPorts.Add(new InputPortInfo("_input2", typeof(float)));
			manifest.OutputPorts.Add(new OutputPortInfo("_output", typeof(string)));
			manifest.TransitionPorts.Add(new TransitionPortInfo("OnComplete"));

			Assert.AreEqual(4, manifest.TotalPortCount);
		}



		[Test]
		public void AllPorts_Empty_ReturnsEmpty()
		{
			var manifest = new StatePortManifest();

			Assert.AreEqual(0, manifest.AllPorts.Count());
		}

		[Test]
		public void AllPorts_ContainsAllPortTypes()
		{
			var manifest = new StatePortManifest();
			var input = new InputPortInfo("_in", typeof(int));
			var output = new OutputPortInfo("_out", typeof(string));
			var transition = new TransitionPortInfo("OnDone");

			manifest.InputPorts.Add(input);
			manifest.OutputPorts.Add(output);
			manifest.TransitionPorts.Add(transition);

			var allPorts = manifest.AllPorts.ToList();

			Assert.AreEqual(3, allPorts.Count);
			Assert.Contains(input, allPorts);
			Assert.Contains(output, allPorts);
			Assert.Contains(transition, allPorts);
		}

		[Test]
		public void AllPorts_Order_InputsFirst_ThenOutputs_ThenTransitions()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_in", typeof(int)));
			manifest.OutputPorts.Add(new OutputPortInfo("_out", typeof(string)));
			manifest.TransitionPorts.Add(new TransitionPortInfo("OnDone"));

			var allPorts = manifest.AllPorts.ToList();

			Assert.IsInstanceOf<InputPortInfo>(allPorts[0]);
			Assert.IsInstanceOf<OutputPortInfo>(allPorts[1]);
			Assert.IsInstanceOf<TransitionPortInfo>(allPorts[2]);
		}



		[Test]
		public void GetInputPort_ExistingField_ReturnsPort()
		{
			var manifest = new StatePortManifest();
			var port = new InputPortInfo("_health", typeof(int));
			manifest.InputPorts.Add(port);

			var result = manifest.GetInputPort("_health");

			Assert.IsNotNull(result);
			Assert.AreEqual("_health", result.FieldName);
		}

		[Test]
		public void GetInputPort_NonExistentField_ReturnsNull()
		{
			var manifest = new StatePortManifest();

			var result = manifest.GetInputPort("nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void GetOutputPort_ExistingField_ReturnsPort()
		{
			var manifest = new StatePortManifest();
			var port = new OutputPortInfo("_result", typeof(bool));
			manifest.OutputPorts.Add(port);

			var result = manifest.GetOutputPort("_result");

			Assert.IsNotNull(result);
			Assert.AreEqual("_result", result.FieldName);
		}

		[Test]
		public void GetOutputPort_NonExistentField_ReturnsNull()
		{
			var manifest = new StatePortManifest();

			var result = manifest.GetOutputPort("nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void GetTransitionPort_ExistingField_ReturnsPort()
		{
			var manifest = new StatePortManifest();
			var port = new TransitionPortInfo("OnComplete");
			manifest.TransitionPorts.Add(port);

			var result = manifest.GetTransitionPort("OnComplete");

			Assert.IsNotNull(result);
			Assert.AreEqual("OnComplete", result.FieldName);
		}

		[Test]
		public void GetTransitionPort_NonExistentField_ReturnsNull()
		{
			var manifest = new StatePortManifest();

			var result = manifest.GetTransitionPort("nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void GetPort_FindsInputPort()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_data", typeof(int)));

			var result = manifest.GetPort("_data");

			Assert.IsNotNull(result);
			Assert.AreEqual(PortDirection.Input, result.Direction);
		}

		[Test]
		public void GetPort_FindsOutputPort()
		{
			var manifest = new StatePortManifest();
			manifest.OutputPorts.Add(new OutputPortInfo("_result", typeof(string)));

			var result = manifest.GetPort("_result");

			Assert.IsNotNull(result);
			Assert.AreEqual(PortDirection.Output, result.Direction);
		}

		[Test]
		public void GetPort_FindsTransitionPort()
		{
			var manifest = new StatePortManifest();
			manifest.TransitionPorts.Add(new TransitionPortInfo("OnDone"));

			var result = manifest.GetPort("OnDone");

			Assert.IsNotNull(result);
			Assert.AreEqual(PortDirection.Transition, result.Direction);
		}

		[Test]
		public void GetPort_NonExistentField_ReturnsNull()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_data", typeof(int)));

			var result = manifest.GetPort("nonexistent");

			Assert.IsNull(result);
		}



		[Test]
		public void HasRequiredInputs_NoInputs_ReturnsFalse()
		{
			var manifest = new StatePortManifest();

			Assert.IsFalse(manifest.HasRequiredInputs());
		}

		[Test]
		public void HasRequiredInputs_NoRequiredInputs_ReturnsFalse()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_optional", typeof(int)));

			Assert.IsFalse(manifest.HasRequiredInputs());
		}

		[Test]
		public void HasRequiredInputs_WithRequiredInput_ReturnsTrue()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_required", typeof(int), "Required", true));

			Assert.IsTrue(manifest.HasRequiredInputs());
		}

		[Test]
		public void GetRequiredInputPorts_ReturnsOnlyRequired()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_optional", typeof(int), "Optional", false));
			manifest.InputPorts.Add(new InputPortInfo("_required1", typeof(float), "Required1", true));
			manifest.InputPorts.Add(new InputPortInfo("_required2", typeof(string), "Required2", true));

			var required = manifest.GetRequiredInputPorts().ToList();

			Assert.AreEqual(2, required.Count);
			Assert.IsTrue(required.All(p => p.IsRequired));
		}

		[Test]
		public void GetRequiredInputPorts_NoRequired_ReturnsEmpty()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_optional", typeof(int)));

			var required = manifest.GetRequiredInputPorts().ToList();

			Assert.AreEqual(0, required.Count);
		}



		[Test]
		public void CacheTypeNames_StoresStateTypeName()
		{
			var manifest = new StatePortManifest();
			manifest.StateType = typeof(int);

			manifest.CacheTypeNames();

			Assert.IsNotNull(manifest.StateTypeName);
			Assert.IsTrue(manifest.StateTypeName.Contains("Int32"));
		}

		[Test]
		public void CacheTypeNames_CachesPortTypes()
		{
			var manifest = new StatePortManifest();
			var port = new InputPortInfo("_val", typeof(float));
			manifest.InputPorts.Add(port);

			manifest.CacheTypeNames();

			Assert.IsNotNull(port.DataTypeAssemblyQualifiedName);
			Assert.IsTrue(port.DataTypeAssemblyQualifiedName.Contains("Single"));
		}

		[Test]
		public void RestoreTypes_RestoresStateType()
		{
			var manifest = new StatePortManifest();
			manifest.StateType = typeof(string);
			manifest.CacheTypeNames();
			var typeName = manifest.StateTypeName;

			// Simulate deserialization
			var restored = new StatePortManifest();
			restored.StateTypeName = typeName;
			restored.RestoreTypes();

			Assert.AreEqual(typeof(string), restored.StateType);
		}

		[Test]
		public void RestoreTypes_RestoresPortTypes()
		{
			var manifest = new StatePortManifest();
			var inputPort = new InputPortInfo("_val", typeof(int));
			manifest.InputPorts.Add(inputPort);
			manifest.CacheTypeNames();
			var portTypeName = inputPort.DataTypeAssemblyQualifiedName;

			// Simulate deserialization
			var restoredManifest = new StatePortManifest();
			var restoredPort = new InputPortInfo();
			restoredPort.FieldName = "_val";
			restoredPort.DataTypeAssemblyQualifiedName = portTypeName;
			restoredManifest.InputPorts.Add(restoredPort);
			restoredManifest.RestoreTypes();

			Assert.AreEqual(typeof(int), restoredPort.DataType);
		}

		[Test]
		public void RestoreTypes_NullStateTypeName_DoesNotThrow()
		{
			var manifest = new StatePortManifest();

			Assert.DoesNotThrow(() => manifest.RestoreTypes());
		}

		[Test]
		public void RestoreTypes_EmptyStateTypeName_DoesNotThrow()
		{
			var manifest = new StatePortManifest();
			manifest.StateTypeName = "";

			Assert.DoesNotThrow(() => manifest.RestoreTypes());
		}



		[Test]
		public void GetPort_SameFieldNameAcrossTypes_ReturnsFirstMatch()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_shared", typeof(int)));
			manifest.OutputPorts.Add(new OutputPortInfo("_shared", typeof(string)));

			var result = manifest.GetPort("_shared");

			// AllPorts yields inputs first, so the input should be returned
			Assert.AreEqual(PortDirection.Input, result.Direction);
		}

		[Test]
		public void MultiplePorts_AddAndRetrieve_Independent()
		{
			var manifest = new StatePortManifest();
			manifest.InputPorts.Add(new InputPortInfo("_a", typeof(int)));
			manifest.InputPorts.Add(new InputPortInfo("_b", typeof(float)));
			manifest.OutputPorts.Add(new OutputPortInfo("_c", typeof(string)));
			manifest.TransitionPorts.Add(new TransitionPortInfo("OnX"));
			manifest.TransitionPorts.Add(new TransitionPortInfo("OnY"));

			Assert.AreEqual(2, manifest.InputPorts.Count);
			Assert.AreEqual(1, manifest.OutputPorts.Count);
			Assert.AreEqual(2, manifest.TransitionPorts.Count);
			Assert.AreEqual(5, manifest.TotalPortCount);
		}

		[Test]
		public void CacheTypeNames_NullStateType_DoesNotThrow()
		{
			var manifest = new StatePortManifest();
			manifest.StateType = null;

			Assert.DoesNotThrow(() => manifest.CacheTypeNames());
		}

	}
}
