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
using System.Threading;

namespace NCode.Composition.DisposableParts.Synchronization
{
	public struct ReadLock : IDisposable
	{
		private int _isDisposed;
		private readonly ReaderWriterLockSlim _lock;

		public ReadLock(ReaderWriterLockSlim @lock, int millisecondsTimeout)
		{
			_lock = @lock;
			_isDisposed = 0;

			if (!_lock.TryEnterReadLock(millisecondsTimeout))
				throw new TimeoutException();
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
				_lock.ExitReadLock();
		}

		#endregion

	}
}