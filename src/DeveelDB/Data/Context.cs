﻿// 
//  Copyright 2010-2018 Deveel
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

using System;

using Deveel.Data.Services;

namespace Deveel.Data {
	public abstract class Context : IContext {
		private readonly string contextName;

		protected Context(IContext parent) : this(parent, null) {
		}

		protected Context(IContext parent, string contextName) {
			Parent = parent;
			this.contextName = contextName;

			if (parent == null) {
				Scope = new ServiceContainer();
			} else {
				Scope = parent.Scope.OpenScope(contextName);
			}
		}

		~Context() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {

			}
		}

		IContext IContext.ParentContext => Parent;

		protected IContext Parent { get; }

		string IContext.ContextName => contextName;

		IScope IContext.Scope => Scope;

		protected IScope Scope { get; }
	}
}