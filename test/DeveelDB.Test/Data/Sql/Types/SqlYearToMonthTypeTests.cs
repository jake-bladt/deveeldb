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
using System.IO;

using Deveel.Data.Serialization;

using Xunit;

namespace Deveel.Data.Sql.Types {
	public static class SqlYearToMonthTypeTests {
		[Theory]
		[InlineData("1.13", "2", "2.3")]
		[InlineData("22", "1", "1.11")]
		public static void AddYearToMonth(string value1, string value2, string expected) {
			var a = SqlYearToMonth.Parse(value1);
			var b = SqlYearToMonth.Parse(value2);
			Binary(type => type.Add, a, b, expected);
		}

		[Theory]
		[InlineData("2.12", 2, "3.2")]
		[InlineData("3.1", -1, "3.0")]
		[InlineData("1.2", -15, "-0.1")]
		public static void AddNumber(string value1, object value2, string expected) {
			var a = SqlYearToMonth.Parse(value1);
			var b = SqlValueUtil.FromObject(value2);

			Binary(type => type.Add, a, b, expected);
		}

		[Theory]
		[InlineData("2.13", "2.1", "12")]
		[InlineData("5.14", "5.0", "1.2")]
		[InlineData("3.11", "-0.10", "4.9")]
		public static void SubtractYearToMonth(string value1, string value2, string expected) {
			var a = SqlYearToMonth.Parse(value1);
			var b = SqlYearToMonth.Parse(value2);
			Binary(type => type.Subtract, a, b, expected);
		}

		[Theory]
		[InlineData("1.2", 3, "0.11")]
		[InlineData("2.3", -2, "2.5")]
		public static void SubtractNumber(string value1, object value2, string expected) {
			var a = SqlYearToMonth.Parse(value1);
			var b = SqlValueUtil.FromObject(value2);

			Binary(type => type.Subtract, a, b, expected);
		}

		[Theory]
		[InlineData("22", "1.10")]
		[InlineData("1.21", "2.9")]
		[InlineData("-6", "-0.6")]
		public static void GetString(string input, string expected) {
			var type = PrimitiveTypes.YearToMonth();
			var ytm = SqlYearToMonth.Parse(input);
			var s = type.ToSqlString(ytm);
			Assert.Equal(expected, s);
		}


		private static void Binary(Func<SqlType, Func<ISqlValue, ISqlValue, ISqlValue>> selector,
			object value1,
			object value2,
			string expected) {
			var type = new SqlYearToMonthType();

			var a = value1 is SqlYearToMonth ? (ISqlValue) (SqlYearToMonth) value1 : (SqlNumber)value1;
			var b = value2 is SqlYearToMonth ? (ISqlValue)(SqlYearToMonth)value2 : (SqlNumber)value2;

			var op = selector(type);
			var result = op(a, b);

			var exp = SqlYearToMonth.Parse(expected);

			Assert.Equal(exp, result);
		}

		[Fact]
		public static void Serialize() {
			var type = PrimitiveTypes.YearToMonth();

			var result = BinarySerializeUtil.Serialize(type);

			Assert.Equal(type, result);
		}
	}
}