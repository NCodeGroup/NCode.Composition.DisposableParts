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
		public void NeedsIsFalseUsingWrapper()
		{
			using (var wrapperCatalog = new DisposableWrapperCatalog(new TypeCatalog(typeof(DummyDisposable)), false))
			{
				var partDefinition = wrapperCatalog.Parts.First();
				var partDefinitionWrapper = new DisposableWrapperPartDefinition(partDefinition);
				var needsWrapper = wrapperCatalog.NeedsWrapper(partDefinitionWrapper);
				Assert.IsFalse(needsWrapper);
			}
		}

		[Test]
		public void NeedsIsFalseUsingNonDisposable()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyNonDisposable)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(typeCatalog, false))
			{
				var innerPartDefinition = typeCatalog.Parts.Single();
				var needsWrapper = wrapperCatalog.NeedsWrapper(innerPartDefinition);
				Assert.IsFalse(needsWrapper);
			}
		}

		[Test]
		public void NeedsIsTrueWithDisposableShared()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyDisposableShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(typeCatalog, false))
			{
				var innerPartDefinition = typeCatalog.Parts.Single();
				var needsWrapper = wrapperCatalog.NeedsWrapper(innerPartDefinition);
				Assert.IsFalse(needsWrapper);
			}
		}

		[Test]
		public void NeedsIsTrueWithDisposableNonShared()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyDisposableNonShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(typeCatalog, false))
			{
				var innerPartDefinition = typeCatalog.Parts.Single();
				var needsWrapper = wrapperCatalog.NeedsWrapper(innerPartDefinition);
				Assert.IsTrue(needsWrapper);
			}
		}

		[Test]
		public void LookupDoesNothingForNonDisposable()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyNonDisposable)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(typeCatalog, false))
			{
				var innerPartDefinition = typeCatalog.Parts.Single();
				var lookup = wrapperCatalog.LookupOrCreate(innerPartDefinition);
				Assert.AreSame(innerPartDefinition, lookup);
			}
		}

		[Test]
		public void LookupDoesNothingForDummyDisposableShared()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyDisposableShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(typeCatalog, false))
			{
				var innerPartDefinition = typeCatalog.Parts.Single();
				var lookup = wrapperCatalog.LookupOrCreate(innerPartDefinition);
				Assert.AreSame(innerPartDefinition, lookup);
			}
		}

		[Test]
		public void LookupWillCreateOnce()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyDisposableNonShared)))
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

	}
}