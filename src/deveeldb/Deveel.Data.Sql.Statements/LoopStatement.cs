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

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public class LoopStatement : CodeBlockStatement, IPlSqlStatement {
		private bool Continue { get; set; }

		private bool Exit { get; set; }

		internal void Control(LoopControlType controlType) {
			if (controlType == LoopControlType.Continue) {
				Continue = true;
			} else if (controlType == LoopControlType.Exit) {
				Exit = true;
			}
		}

		// TODO: Review the logic to control the loop...
		protected virtual bool Loop(ExecutionContext context) {
			return !Exit;
		}

		protected virtual bool CanExecute(ExecutionContext context) {
			return !Continue;
		}

		protected virtual void BeforeLoop(ExecutionContext context) {
		}

		protected virtual void AfterLoop(ExecutionContext context) {
		}

		protected override void ExecuteStatement(ExecutionContext context) {
			BeforeLoop(context);

			while (Loop(context)) {
				if (CanExecute(context))
					base.ExecuteStatement(context);
			}

			AfterLoop(context);
		}


		protected override void AppendTo(SqlStringBuilder builder) {
			if (!String.IsNullOrEmpty(Label)) {
				builder.Append("<<{0}>>", Label);
				builder.AppendLine();
			}

			builder.AppendLine("LOOP");
			builder.Indent();

			foreach (var statement in Statements) {
				statement.Append(builder);
				builder.AppendLine();
			}

			builder.DeIndent();
			builder.AppendLine("END LOOP");
		}
	}
}