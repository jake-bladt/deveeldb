﻿using System;

using NUnit.Framework;

namespace Deveel.Data.Sql.Compile {
	[TestFixture]
	public class SqlCompileTestBase {
		protected SqlCompileResult Compile(string sql) {
			var compiler=new SqlDefaultCompiler();
			return compiler.Compile(new SqlCompileContext(sql));
		}
	}
}