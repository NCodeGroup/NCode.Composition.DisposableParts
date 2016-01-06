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
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using NUnit.Framework;

namespace NCode.Composition.DisposableParts.Tests
{
	[TestFixture]
	public class CompositionTests
	{
		[Test]
		public void OriginalWithMemoryLeak()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyDisposablePolicyNonShared)))
			using (var container = new CompositionContainer(typeCatalog, false))
			{
				var weak = DisposeAndGetWeak(container);

				// not sure if all this is necessary, but it seams to force GC
				GC.Collect();
				Thread.Sleep(500);
				GC.WaitForFullGCApproach();
				GC.WaitForFullGCComplete();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				DummyDisposablePolicyNonShared target;
				var isAlive = weak.TryGetTarget(out target);
				Assert.IsTrue(isAlive, "Checking IsAlive");
			}
		}

		[Test]
		public void WrapperWithoutMemoryLeak()
		{
			using (var typeCatalog = new TypeCatalog(typeof(DummyDisposablePolicyNonShared)))
			using (var wrapperCatalog = new DisposableWrapperCatalog(typeCatalog, false))
			using (var container = new CompositionContainer(wrapperCatalog, false))
			{
				var weak = DisposeAndGetWeak(container);

				// not sure if all this is necessary, but it seams to force GC
				GC.Collect();
				Thread.Sleep(500);
				GC.WaitForFullGCApproach();
				GC.WaitForFullGCComplete();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				DummyDisposablePolicyNonShared target;
				var isAlive = weak.TryGetTarget(out target);
				Assert.IsFalse(isAlive, "Checking IsAlive");
			}
		}

		// FYI: in order for the GC to reclaim the reference, we must use a separate non-inline function
		private static WeakReference<DummyDisposablePolicyNonShared> DisposeAndGetWeak(CompositionContainer container)
		{
			var item = container.GetExportedValue<DummyDisposablePolicyNonShared>();
			item.Dispose();
			return new WeakReference<DummyDisposablePolicyNonShared>(item);
		}

	}
}