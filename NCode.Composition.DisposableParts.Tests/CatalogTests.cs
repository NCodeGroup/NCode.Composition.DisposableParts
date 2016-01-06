#region Copyright Preamble
// 
//    Copyright © 2015 NCode Group
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// 
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace NCode.Composition.DisposableParts.Tests
{
	[TestFixture]
	public class CatalogTests
	{
		[Test]
		public void ChangedEvent()
		{
			using (var aggregate = new AggregateCatalog())
			using (var wrapperCatalog = new DisposableWrapperCatalog(aggregate, false))
			{
				var received = false;
				wrapperCatalog.Changed += (sender, args) => received = true;

				aggregate.Catalogs.Add(new TypeCatalog());
				Assert.IsTrue(received);
			}
		}

		[Test]
		public void ChangingEvent()
		{
			using (var aggregate = new AggregateCatalog())
			using (var wrapperCatalog = new DisposableWrapperCatalog(aggregate, false))
			{
				var received = false;
				wrapperCatalog.Changing += (sender, args) => received = true;

				aggregate.Catalogs.Add(new TypeCatalog());
				Assert.IsTrue(received);
			}
		}

		[Test]
		public void IsNonSharedDisposable_DummyNonDisposablePolicyUnknown_IsFalse()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyNonDisposablePolicyUnknown)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var innerPartDefinition = innerCatalog.Parts.First();
				var needsWrapper = wrapperCatalog.IsNonSharedDisposable(innerPartDefinition);
				Assert.IsFalse(needsWrapper);
			}
		}

		[Test]
		public void IsNonSharedDisposable_DummyDisposablePolicyUnknown_IsFalse()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyDisposablePolicyUnknown)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var innerPartDefinition = innerCatalog.Parts.First();
				var needsWrapper = wrapperCatalog.IsNonSharedDisposable(innerPartDefinition);
				Assert.IsFalse(needsWrapper);
			}
		}

		[Test]
		public void IsNonSharedDisposable_DummyDisposablePolicyShared_IsFalse()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyDisposablePolicyShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var innerPartDefinition = innerCatalog.Parts.First();
				var needsWrapper = wrapperCatalog.IsNonSharedDisposable(innerPartDefinition);
				Assert.IsFalse(needsWrapper);
			}
		}

		[Test]
		public void IsNonSharedDisposable_DummyDisposablePolicyNonShared_IsTrue()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyDisposablePolicyNonShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var innerPartDefinition = innerCatalog.Parts.First();
				var needsWrapper = wrapperCatalog.IsNonSharedDisposable(innerPartDefinition);
				Assert.IsTrue(needsWrapper);
			}
		}

		[Test]
		public void LookupOrCreate_OnceForSameDefinition()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyDisposablePolicyNonShared)))
			{
				var innerPartDefinition = typeCatalog.Parts.Single();
				var moq = new Mock<DisposableWrapperCatalog>(MockBehavior.Loose, typeCatalog, false) { CallBase = true };
				using (var wrapperCatalog = moq.Object)
				{
					var partDefinition1 = wrapperCatalog.LookupOrCreate(innerPartDefinition);
					Assert.IsNotNull(partDefinition1);
					Assert.AreNotSame(innerPartDefinition, partDefinition1);

					var partDefinition2 = wrapperCatalog.LookupOrCreate(innerPartDefinition);
					Assert.AreSame(partDefinition1, partDefinition2);

					moq.Verify(_ => _.CreateWrapper(It.IsAny<ComposablePartDefinition>()), Times.Once);
				}
			}
		}

		[Test]
		public void CreateWrapper_UsesPreExistingWrapper()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyDisposablePolicyNonShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var partDefinition1 = wrapperCatalog.Parts.Single();
				var partDefinition2 = wrapperCatalog.CreateWrapper(partDefinition1);
				Assert.AreSame(partDefinition1, partDefinition2);
			}
		}

		[Test]
		public void CreatePart_DummyNonDisposablePolicyUnknown_IsNotDisposable()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyNonDisposablePolicyUnknown)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var partDefinition = wrapperCatalog.Parts.Single();
				var part = partDefinition.CreatePart();
				Assert.IsNotInstanceOf<IDisposable>(part);
			}
		}

		[Test]
		public void CreatePart_DummyDisposablePolicyUnknown_IsDisposable()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyDisposablePolicyUnknown)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var partDefinition = wrapperCatalog.Parts.Single();
				var part = partDefinition.CreatePart();
				Assert.IsInstanceOf<IDisposable>(part);
			}
		}

		[Test]
		public void CreatePart_DummyDisposablePolicyShared_IsDisposable()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyDisposablePolicyShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var partDefinition = wrapperCatalog.Parts.Single();
				var part = partDefinition.CreatePart();
				Assert.IsInstanceOf<IDisposable>(part);
			}
		}

		[Test]
		public void CreatePart_DummyDisposablePolicyNonShared_IsNotDisposable()
		{
			using (var innerCatalog = new TypeCatalog(typeof(DummyDisposablePolicyNonShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				var partDefinition = wrapperCatalog.Parts.Single();
				var part = partDefinition.CreatePart();
				Assert.IsNotInstanceOf<IDisposable>(part);
			}
		}

		public void ApplicationCatalog(Type contractType, bool expectedIsDisposableNormal, bool expectedIsDisposableWrapped)
		{
			// ApplicationCatalog is a good comprehensive test because it uses the following hierarchy:
			// + AggregateCatalog
			//   + DirectoryCatalog
			//     + AssemblyCatalog
			//       + TypeCatalog

			using (var innerCatalog = new ApplicationCatalog())
			using (var wrapperCatalog = new DisposableWrapperCatalog(innerCatalog, false))
			{
				const ImportCardinality cardinality = ImportCardinality.ExactlyOne;
				var metadata = new Dictionary<string, object>();
				var typeIdentity = AttributedModelServices.GetTypeIdentity(contractType);
				var contractName = AttributedModelServices.GetContractName(contractType);
				var definition = new ContractBasedImportDefinition(contractName, typeIdentity, null, cardinality, false, true, CreationPolicy.Any, metadata);

				var partNormal = innerCatalog.GetExports(definition).Single().Item1.CreatePart();
				var partWrapped = wrapperCatalog.GetExports(definition).Single().Item1.CreatePart();

				var isDisposableNormal = partNormal is IDisposable;
				var isDisposableWrapped = partWrapped is IDisposable;

				Assert.AreEqual(expectedIsDisposableNormal, isDisposableNormal, "Checking Normal Part");
				Assert.AreEqual(expectedIsDisposableWrapped, isDisposableWrapped, "Checking Wrapped Part");
			}
		}

		[Test]
		public void ApplicationCatalog_DummyNonDisposablePolicyUnknown()
		{
			ApplicationCatalog(typeof(DummyNonDisposablePolicyUnknown), false, false);
		}

		[Test]
		public void ApplicationCatalog_DummyDisposablePolicyUnknown()
		{
			ApplicationCatalog(typeof(DummyDisposablePolicyUnknown), true, true);
		}

		[Test]
		public void ApplicationCatalog_DummyDisposablePolicyShared()
		{
			ApplicationCatalog(typeof(DummyDisposablePolicyShared), true, true);
		}

		[Test]
		public void ApplicationCatalog_DummyDisposablePolicyNonShared()
		{
			ApplicationCatalog(typeof(DummyDisposablePolicyNonShared), true, false);
		}

	}
}