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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;
using System.Threading;
using NCode.Composition.DisposableParts.Synchronization;

namespace NCode.Composition.DisposableParts
{
	public class DisposableWrapperCatalog : ComposablePartCatalog, INotifyComposablePartCatalogChanged, ICompositionElement
	{
		private static readonly object EventChanged = new object();
		private static readonly object EventChanging = new object();

		private int _isDisposed;
		private readonly Lock _lock;
		private readonly ComposablePartCatalog _innerCatalog;
		private readonly ICompositionElement _compositionOrigin;
		private readonly IDictionary<ComposablePartDefinition, ComposablePartDefinition> _cache;
		private EventHandlerList _events;
		private string _displayName;

		public DisposableWrapperCatalog(ComposablePartCatalog innerCatalog, bool isThreadSafe)
		{
			if (innerCatalog == null)
				throw new ArgumentNullException(nameof(innerCatalog));

			_lock = new Lock(isThreadSafe, LockRecursionPolicy.NoRecursion);
			_cache = new Dictionary<ComposablePartDefinition, ComposablePartDefinition>();
			_innerCatalog = innerCatalog;
			_compositionOrigin = innerCatalog as ICompositionElement;

			var notify = innerCatalog as INotifyComposablePartCatalogChanged;
			if (notify == null) return;

			notify.Changed += OnChanged;
			notify.Changing += OnChanging;
		}

		#region IDisposable Members

		protected virtual bool IsDisposed => _isDisposed == 1;

		protected virtual void ThrowIfDisposed()
		{
			if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && Thread.VolatileRead(ref _isDisposed) == 0)
			{
				var aquired = false;
				using (_lock.WriteLock())
				{
					if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
					{
						aquired = true;
						_cache.Clear();
						_events?.Dispose();

						var notify = _innerCatalog as INotifyComposablePartCatalogChanged;
						if (notify != null)
						{
							notify.Changed -= OnChanged;
							notify.Changing -= OnChanging;
						}
					}
				}
				if (aquired) _lock.Dispose();
			}
			base.Dispose(disposing);
		}

		#endregion

		#region ICompositionElement Members

		public virtual ICompositionElement Origin => _compositionOrigin;

		public virtual string DisplayName => _displayName ?? (_displayName = FormatDisplayName());

		protected virtual string FormatDisplayName()
		{
			var displayName = GetType().Name;
			var parentName = _compositionOrigin?.DisplayName;

			if (!string.IsNullOrEmpty(parentName))
				_displayName = $"{displayName} ({parentName})";

			return displayName;
		}

		#endregion

		#region INotifyComposablePartCatalogChanged Members

		protected virtual EventHandlerList Events => _events ?? (_events = new EventHandlerList());

		public event EventHandler<ComposablePartCatalogChangeEventArgs> Changed
		{
			add { Events.AddHandler(EventChanged, value); }
			remove { Events.RemoveHandler(EventChanged, value); }
		}

		protected virtual void OnChanged(ComposablePartCatalogChangeEventArgs e)
		{
			var handler = Events[EventChanged] as EventHandler<ComposablePartCatalogChangeEventArgs>;
			handler?.Invoke(this, e);
		}

		private void OnChanged(object sender, ComposablePartCatalogChangeEventArgs e)
		{
			OnChanged(e);
		}

		public event EventHandler<ComposablePartCatalogChangeEventArgs> Changing
		{
			add { Events.AddHandler(EventChanging, value); }
			remove { Events.RemoveHandler(EventChanging, value); }
		}

		protected virtual void OnChanging(ComposablePartCatalogChangeEventArgs e)
		{
			var handler = Events[EventChanging] as EventHandler<ComposablePartCatalogChangeEventArgs>;
			handler?.Invoke(this, e);
		}

		private void OnChanging(object sender, ComposablePartCatalogChangeEventArgs e)
		{
			OnChanging(e);
		}

		#endregion

		#region ComposablePartCatalog Members

		public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
		{
			return InnerCatalog.GetExports(definition).Select(tuple => Tuple.Create(LookupOrCreate(tuple.Item1), tuple.Item2));
		}

		public override IQueryable<ComposablePartDefinition> Parts => InnerCatalog.Parts.Select(LookupOrCreate).AsQueryable();

		#endregion

		public virtual ComposablePartCatalog InnerCatalog => _innerCatalog;

		protected virtual Lock Lock => _lock;

		protected virtual IDictionary<ComposablePartDefinition, ComposablePartDefinition> Cache => _cache;

		internal protected virtual bool IsNonSharedDisposable(ComposablePartDefinition innerPartDefinition)
		{
			if (!ReflectionModelServices.IsDisposalRequired(innerPartDefinition))
				return false;

			object obj;
			if (!innerPartDefinition.Metadata.TryGetValue(CompositionConstants.PartCreationPolicyMetadataName, out obj) || !(obj is CreationPolicy))
				return false;

			var creationPolicy = (CreationPolicy)obj;
			return creationPolicy == CreationPolicy.NonShared;
		}

		internal protected virtual ComposablePartDefinition LookupOrCreate(ComposablePartDefinition innerPartDefinition)
		{
			ThrowIfDisposed();

			ComposablePartDefinition wrapper;

			if (Lock.IsThreadSafe)
			{
				using (Lock.ReadLock())
				{
					if (Cache.TryGetValue(innerPartDefinition, out wrapper)) return wrapper;
				}

				ThrowIfDisposed();
			}

			using (Lock.WriteLock())
			{
				if (Cache.TryGetValue(innerPartDefinition, out wrapper)) return wrapper;

				wrapper = CreateWrapper(innerPartDefinition);
				Cache[innerPartDefinition] = wrapper;
			}

			return wrapper;
		}

		internal protected virtual ComposablePartDefinition CreateWrapper(ComposablePartDefinition innerPartDefinition)
		{
			var wrapper = innerPartDefinition as DisposableWrapperPartDefinition;
			if (wrapper == null)
			{
				var isNonSharedDisposable = IsNonSharedDisposable(innerPartDefinition);
				wrapper = new DisposableWrapperPartDefinition(innerPartDefinition, isNonSharedDisposable);
			}
			return wrapper;
		}

	}
}