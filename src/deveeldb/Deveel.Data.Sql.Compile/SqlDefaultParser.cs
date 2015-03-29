﻿using System;
using System.Collections.Generic;
using System.Linq;

using Irony;
using Irony.Ast;
using Irony.Parsing;

namespace Deveel.Data.Sql.Compile {
	class SqlDefaultParser : ISqlParser {
		public SqlDefaultParser(string dialect, bool ignoreCase) {
			Dialect = dialect;
			IgnoreCase = ignoreCase;
		}

		public void Dispose() {
		}

		public string Dialect { get; private set; }

		public bool IgnoreCase { get; private set; }

		public SqlParseResult Parse(string input) {
			var grammar = new SqlGrammar(IgnoreCase);
			var result = new SqlParseResult(Dialect);

			var startedOn = DateTimeOffset.UtcNow;

			try {
				var node = ParseNode(grammar, input, result.Errors);
				result.RootNode = node;
			} catch (Exception ex) {
				// TODO: form a better exception
				result.Errors.Add(new SqlParseError(ex.Message, 0, 0));
			} finally {
				result.ParseTime = DateTimeOffset.UtcNow.Subtract(startedOn);
			}

			return result;
		}

		private ISqlNode ParseNode(SqlGrammar grammar, string sqlSource, ICollection<SqlParseError> errors) {
			var languageData = new LanguageData(grammar);

			if (!languageData.CanParse())
				throw new InvalidOperationException();

			var parser = new Parser(languageData);
			var tree = parser.Parse(sqlSource);
			if (tree.HasErrors()) {
				BuildErrors(errors, tree.ParserMessages);
				return null;
			}

			var astContext = new AstContext(languageData) {
				DefaultNodeType = typeof (SqlNode),
				DefaultIdentifierNodeType = typeof (IdentifierNode)
			};

			var astCompiler = new AstBuilder(astContext);
			astCompiler.BuildAst(tree);

			if (tree.HasErrors())
				BuildErrors(errors, tree.ParserMessages);

			var node = (ISqlNode) tree.Root.AstNode;
			if (node.NodeName == "root")
				node = node.ChildNodes.FirstOrDefault();

			return node;
		}

		private static void BuildErrors(ICollection<SqlParseError> errors, LogMessageList logMessages) {
			foreach (var logMessage in logMessages) {
				if (logMessage.Level == ErrorLevel.Error) {
					var line = logMessage.Location.Line;
					var column = logMessage.Location.Column;
					// TODO: build the message traversing the source ...

					errors.Add(new SqlParseError(logMessage.Message, line, column));
				}
			}
		}
	}
}