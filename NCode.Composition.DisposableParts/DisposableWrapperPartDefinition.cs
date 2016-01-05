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
using System.ComponentModel.Composition.Primitives;

namespace NCode.Composition.DisposableParts
{
	public class DisposableWrapperPartDefinition : ComposablePartDefinition, ICompositionElement
	{
		private readonly bool _isNonSharedDisposable;
		private readonly ICompositionElement _compositionOrigin;
		private string _displayName;

		public DisposableWrapperPartDefinition(ComposablePartDefinition innerPartDefinition, bool isNonSharedDisposable)
		{
			if (innerPartDefinition == null)
				throw new ArgumentNullException(nameof(innerPartDefinition));

			InnerPartDefinition = innerPartDefinition;

			_isNonSharedDisposable = isNonSharedDisposable;
			_compositionOrigin = innerPartDefinition as ICompositionElement;
		}

		public virtual ComposablePartDefinition InnerPartDefinition { get; }

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

		public override ComposablePart CreatePart()
		{
			var innerPart = InnerPartDefinition.CreatePart();
			return _isNonSharedDisposable
				? new DisposableWrapperPart(innerPart)
				: innerPart;
		}

		public override IEnumerable<ExportDefinition> ExportDefinitions => InnerPartDefinition.ExportDefinitions;

		public override IEnumerable<ImportDefinition> ImportDefinitions => InnerPartDefinition.ImportDefinitions;

		public override IDictionary<string, object> Metadata => InnerPartDefinition.Metadata;

	}
}