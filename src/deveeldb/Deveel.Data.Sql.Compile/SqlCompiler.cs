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
using System.Linq;

using Irony;
using Irony.Ast;
using Irony.Parsing;

namespace Deveel.Data.Sql.Compile {
	public sealed class SqlCompiler {
		public SqlCompiler() 
			: this(true) {
		}

		public SqlCompiler(bool ignoreCase) {
			IgnoreCase = ignoreCase;
		}

		public bool IgnoreCase { get; set; }

		public TNode Compile<TNode>(string sqlSource) where TNode : ISqlNode {
			var grammar = new SqlGrammar(IgnoreCase);
			return (TNode) ParseNode(grammar, sqlSource);
		}

		private ISqlNode ParseNode(SqlGrammar grammar, string sqlSource) {
			var languageData = new LanguageData(grammar);

			if (!languageData.CanParse())
				throw new InvalidOperationException();

			var parser = new Parser(languageData);
			var tree = parser.Parse(sqlSource);
			if (tree.HasErrors())
				throw BuildSqlException(tree.ParserMessages);

			var astContext = new AstContext(languageData);
			astContext.DefaultNodeType = typeof(SqlNode);
			astContext.DefaultIdentifierNodeType = typeof(IdentifierNode);
			var astCompiler = new AstBuilder(astContext);
			astCompiler.BuildAst(tree);

			if (tree.HasErrors())
				throw BuildSqlException(tree.ParserMessages);

			var node = (ISqlNode) tree.Root.AstNode;
			if (node.NodeName == "root")
				node = node.ChildNodes.FirstOrDefault();

			return node;
		}

		private SqlParseException BuildSqlException(LogMessageList parserMessages) {
			foreach (var message in parserMessages) {
				// TODO:
			}

			throw new SqlParseException();
		}

		internal StatementSequenceNode CompileStatements(string sqlSource) {
			return Compile<StatementSequenceNode>(sqlSource);
		}

		internal DataTypeNode CompileDataType(string s) {
			var grammar = new SqlGrammar(IgnoreCase);
			grammar.SetRootToDataType();
			return (DataTypeNode) ParseNode(grammar, s);
		}

		internal IExpressionNode CompileExpression(string s) {
			var grammar = new SqlGrammar(IgnoreCase);
			grammar.SetRootToExpression();
			return (IExpressionNode) ParseNode(grammar, s);
		}
	}
}