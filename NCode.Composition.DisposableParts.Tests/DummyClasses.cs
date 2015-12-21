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
using System.ComponentModel.Composition;

namespace NCode.Composition.DisposableParts.Tests
{
	[Export]
	public class DummyNonDisposable
	{
		// nothing
	}

	[Export]
	public class DummyDisposable : IDisposable
	{
		public void Dispose()
		{
			// nothing
		}
	}

	[Export]
	[PartCreationPolicy(CreationPolicy.Shared)]
	public class DummyDisposableShared : IDisposable
	{
		public void Dispose()
		{
			// nothing
		}
	}

	[Export]
	[PartCreationPolicy(CreationPolicy.NonShared)]
	public class DummyDisposableNonShared : IDisposable
	{
		public void Dispose()
		{
			// nothing
		}
	}

}