﻿// 
//  Copyright 2010-2015 Deveel
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
using System.Collections.Generic;

namespace Deveel.Data.Sql.Compile {
	/// <summary>
	/// A clause node that defines the filters applied to
	/// a selection to restrict the results.
	/// </summary>
	[Serializable]
	class WhereClauseNode : SqlNode {
		/// <summary>
		/// Gets a read-only list of expressions used to filter
		/// </summary>
		public IEnumerable<IExpressionNode> Expressions { get; private set; }

		/// <inheritdoc/>
		protected override ISqlNode OnChildNode(ISqlNode node) {
			if (node.NodeName == "sql_expression_list")
				GetExpressions(node);

			return base.OnChildNode(node);
		}

		private void GetExpressions(ISqlNode node) {
			var exps = new List<IExpressionNode>();

			foreach (var childNode in node.ChildNodes) {
				if (childNode is IExpressionNode)
					exps.Add((IExpressionNode)childNode);
			}

			Expressions = exps.AsReadOnly();
		}
	}
}