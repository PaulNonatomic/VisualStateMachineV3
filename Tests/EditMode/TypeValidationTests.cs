using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Utils;
using UnityEngine;

// Test types in custom user namespaces to verify denylist allows them
namespace MyGame.AI
{
	public class PatrolState { }
}

namespace Company.Project.States
{
	public class CustomState { }
}

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class TypeValidationTests
	{
		[Test]
		public void IsAllowedType_VsmType_ReturnsTrue()
		{
			var type = typeof(State);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_UnityEngineType_ReturnsTrue()
		{
			var type = typeof(Vector3);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_UnityEngineMonoBehaviour_ReturnsTrue()
		{
			var type = typeof(MonoBehaviour);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemPrimitive_ReturnsTrue()
		{
			Assert.IsTrue(TypeValidation.IsAllowedType(typeof(int)));
			Assert.IsTrue(TypeValidation.IsAllowedType(typeof(float)));
			Assert.IsTrue(TypeValidation.IsAllowedType(typeof(bool)));
			Assert.IsTrue(TypeValidation.IsAllowedType(typeof(double)));
		}

		[Test]
		public void IsAllowedType_SystemString_ReturnsTrue()
		{
			var type = typeof(string);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemCollections_ReturnsTrue()
		{
			Assert.IsTrue(TypeValidation.IsAllowedType(typeof(List<int>)));
			Assert.IsTrue(TypeValidation.IsAllowedType(typeof(Dictionary<string, int>)));
		}

		[Test]
		public void IsAllowedType_SystemDiagnosticsProcess_ReturnsFalse()
		{
			var type = typeof(Process);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemIOFileInfo_ReturnsFalse()
		{
			var type = typeof(System.IO.FileInfo);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemNetWebClient_ReturnsFalse()
		{
			var type = typeof(System.Net.WebClient);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemReflectionAssembly_ReturnsFalse()
		{
			var type = typeof(System.Reflection.Assembly);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_NullType_ReturnsFalse()
		{
			var result = TypeValidation.IsAllowedType(null);

			Assert.IsFalse(result);
		}

		[Test]
		public void SafeGetType_ValidVsmType_ReturnsType()
		{
			var typeName = typeof(State).AssemblyQualifiedName;

			var result = TypeValidation.SafeGetType(typeName);

			Assert.IsNotNull(result);
			Assert.AreEqual(typeof(State), result);
		}

		[Test]
		public void SafeGetType_ValidUnityType_ReturnsType()
		{
			var typeName = typeof(Vector3).AssemblyQualifiedName;

			var result = TypeValidation.SafeGetType(typeName);

			Assert.IsNotNull(result);
			Assert.AreEqual(typeof(Vector3), result);
		}

		[Test]
		public void SafeGetType_DangerousType_ReturnsNull()
		{
			var typeName = typeof(Process).AssemblyQualifiedName;

			var result = TypeValidation.SafeGetType(typeName, logWarnings: false);

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_NullString_ReturnsNull()
		{
			var result = TypeValidation.SafeGetType(null);

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_EmptyString_ReturnsNull()
		{
			var result = TypeValidation.SafeGetType("");

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_InvalidTypeName_ReturnsNull()
		{
			var result = TypeValidation.SafeGetType("NonExistent.Type, NonExistent.Assembly");

			Assert.IsNull(result);
		}

		[Test]
		public void IsAllowedType_SystemDateTime_ReturnsTrue()
		{
			var type = typeof(DateTime);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemGuid_ReturnsTrue()
		{
			var type = typeof(Guid);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_UserDefinedEnum_ReturnsTrue()
		{
			var type = typeof(DayOfWeek);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_UserType_CustomNamespace_ReturnsTrue()
		{
			var type = typeof(MyGame.AI.PatrolState);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_UserType_DeeplyNestedNamespace_ReturnsTrue()
		{
			var type = typeof(Company.Project.States.CustomState);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void SafeGetType_MalformedAssemblyName_MissingVersion_DoesNotThrow()
		{
			var malformed = "System.String, mscorlib";

			Assert.DoesNotThrow(() => TypeValidation.SafeGetType(malformed, logWarnings: false));
		}

		[Test]
		public void SafeGetType_MalformedAssemblyName_GarbageString_ReturnsNull()
		{
			var malformed = "Not.A.Real.Type, !!!GARBAGE!!!, Version=0.0.0.0";

			var result = TypeValidation.SafeGetType(malformed, logWarnings: false);

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_MalformedAssemblyName_EmptyTypeName_ReturnsNull()
		{
			var malformed = ", mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

			var result = TypeValidation.SafeGetType(malformed, logWarnings: false);

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_MalformedAssemblyName_SpecialCharacters_ReturnsNull()
		{
			var malformed = "<script>alert('xss')</script>, FakeAssembly";

			var result = TypeValidation.SafeGetType(malformed, logWarnings: false);

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_MalformedAssemblyName_SqlInjectionAttempt_ReturnsNull()
		{
			var malformed = "'; DROP TABLE Users; --, FakeAssembly";

			var result = TypeValidation.SafeGetType(malformed, logWarnings: false);

			Assert.IsNull(result);
		}

		[Test]
		public void IsAllowedType_VsmSubNamespace_ReturnsTrue()
		{
			var type = typeof(StateNode);
			Assert.IsTrue(TypeValidation.IsAllowedType(type));
		}

		[Test]
		public void IsAllowedType_SystemObject_ReturnsTrue()
		{
			var type = typeof(object);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemType_ReturnsTrue()
		{
			var type = typeof(Type);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemAction_ReturnsTrue()
		{
			var type = typeof(Action);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_GenericList_WithSafeInnerType_ReturnsTrue()
		{
			var type = typeof(List<string>);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_GenericList_WithDangerousInnerType_StillReturnsTrue()
		{
			// Current implementation only checks the outer type's namespace.
			// List<Process> returns TRUE because List is in System.Collections.Generic.
			var type = typeof(List<Process>);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_NestedGeneric_ReturnsTrue()
		{
			var type = typeof(List<List<int>>);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemTimeSpan_ReturnsTrue()
		{
			var type = typeof(TimeSpan);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemDecimal_ReturnsTrue()
		{
			var type = typeof(decimal);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_ArrayOfSafeType_ReturnsTrue()
		{
			var type = typeof(int[]);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_ArrayOfDangerousType_StillReturnsTrue()
		{
			// Arrays have null namespace, allowed by current implementation.
			var type = typeof(Process[]);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemRuntimeInteropServices_ReturnsFalse()
		{
			var type = typeof(System.Runtime.InteropServices.Marshal);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemReflectionEmit_ReturnsFalse()
		{
			var type = typeof(System.Reflection.Emit.DynamicMethod);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_NUnitFrameworkType_ReturnsTrue()
		{
			// Third-party types in non-denied namespaces should be allowed
			var type = typeof(NUnit.Framework.TestAttribute);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsTrue(result);
		}

		[Test]
		public void IsAllowedType_SystemThreadingThread_ReturnsFalse()
		{
			var type = typeof(System.Threading.Thread);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemThreadingMutex_ReturnsFalse()
		{
			var type = typeof(System.Threading.Mutex);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemRuntimeSerialization_ReturnsFalse()
		{
			var type = typeof(System.Runtime.Serialization.IFormatter);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemAppDomain_ReturnsFalse()
		{
			var type = typeof(AppDomain);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void IsAllowedType_SystemEnvironment_ReturnsFalse()
		{
			var type = typeof(Environment);

			var result = TypeValidation.IsAllowedType(type);

			Assert.IsFalse(result);
		}

		[Test]
		public void SafeGetType_SystemThreadingThread_ReturnsNull()
		{
			var typeName = typeof(System.Threading.Thread).AssemblyQualifiedName;

			var result = TypeValidation.SafeGetType(typeName, logWarnings: false);

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_SystemAppDomain_ReturnsNull()
		{
			var typeName = typeof(AppDomain).AssemblyQualifiedName;

			var result = TypeValidation.SafeGetType(typeName, logWarnings: false);

			Assert.IsNull(result);
		}

		[Test]
		public void SafeGetType_SystemEnvironment_ReturnsNull()
		{
			var typeName = typeof(Environment).AssemblyQualifiedName;

			var result = TypeValidation.SafeGetType(typeName, logWarnings: false);

			Assert.IsNull(result);
		}
	}
}
