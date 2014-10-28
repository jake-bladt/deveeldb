﻿// 
//  Copyright 2010-2014 Deveel
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

using System;
using System.Collections.Generic;

using Deveel.Data.Sql.Compile;
using Deveel.Data.Sql.Objects;

namespace Deveel.Data.Types {
	/// <summary>
	/// Defines the properties of a specific SQL Type and handles the
	/// <see cref="ISqlObject">values compatible</see>.
	/// </summary>
	public abstract class DataType : IComparer<ISqlObject>, IEquatable<DataType> {
		/// <summary>
		/// Constructs the <see cref="DataType"/> for the given specific
		/// <see cref="SqlTypeCode">SQL TYPE</see>.
		/// </summary>
		/// <remarks>
		/// This constructor will set the <see cref="Name"/> value to the equivalent
		/// of the SQL Type specified.
		/// </remarks>
		/// <param name="sqlType">The code of the SQL Type this object will represent.</param>
		protected DataType(SqlTypeCode sqlType) 
			: this(sqlType.ToString().ToUpperInvariant(), sqlType) {
		}

		/// <summary>
		/// Constructs the <see cref="DataType"/> for the given specific
		/// <see cref="SqlTypeCode">SQL TYPE</see> and a given name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="sqlType"></param>
		protected DataType(string name, SqlTypeCode sqlType) {
			SqlType = sqlType;
			Name = name;
		}

		/// <summary>
		/// Gets the name of the data-type that is used to resolve it within the context.
		/// </summary>
		/// <remarks>
		/// This value is always unique within a database system and can be simple
		/// (eg. for <see cref="IsPrimitive">primitive</see> types like <c>NUMERIC</c>),
		/// or composed by multiple parts (eg. for user-defined types).
		/// <para>
		/// Some primitive types (for example <c>NUMERIC</c>) can handle multiple SQL types,
		/// so this property works as an identificator for the type handler.
		/// </para>
		/// </remarks>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the kind of SQL type this data-type handles.
		/// </summary>
		/// <remarks>
		/// The same instance of a <see cref="DataType"/> can handle multiple
		/// kind of <see cref="SqlTypeCode">SQL types</see>, making such instances,
		/// of the same kind, to be different in attributes.
		/// <para>
		/// In fact, for example a <c>NUMERIC</c> data-type materialized as <c>INTEGER</c>
		/// is not equal to <c>NUMERIC</c> data-type materialized as <c>BIGINT</c>: the
		/// two instances will be comparable, but they won't be considered coincident.
		/// </para>
		/// </remarks>
		/// <see cref="IsComparable"/>
		public SqlTypeCode SqlType { get; private set; }

		/// <summary>
		/// Indicates if the values handled by the type can be part of an index.
		/// </summary>
		/// <remarks>
		/// By default, this returns <c>true</c>, since most of primitive types
		/// are indexable (except for Long Objects).
		/// </remarks>
		public virtual bool IsIndexable {
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating if this data-type is primitive.
		/// </summary>
		/// <remarks>
		/// This returns <c>false</c> only incase that <see cref="SqlType"/>
		/// is equal to <see cref="SqlTypeCode.Object"/> or <see cref="SqlTypeCode.Unknown"/>.
		/// </remarks>
		public bool IsPrimitive {
			get {
				return SqlType != SqlTypeCode.Object &&
				       SqlType != SqlTypeCode.Unknown;
			}
		}

		/// <summary>
		/// Verifies if a given <see cref="DataType"/> is comparable to
		/// this data-type.
		/// </summary>
		/// <param name="type">The other data-type to verify the compatibility.</param>
		/// <remarks>
		/// It is not required two <see cref="DataType"/> to be identical to be compared:
		/// when overridden by a derived class, this methods verifies the properties of the
		/// argument type, to see if values handled by the types can be compared.
		/// <para>
		/// By default, this method compares the values returned by <see cref="SqlType"/>
		/// to see if they are identical.
		/// </para>
		/// </remarks>
		/// <returns>
		/// Returns <c>true</c> if the values handled by this data-type can be compared to ones handled 
		/// by the given <paramref name="type"/>, or <c>false</c> otherwise.
		/// </returns>
		public virtual bool IsComparable(DataType type) {
			return SqlType == type.SqlType;
		}

		/// <summary>
		/// Verifies if this type can cast any value to the given <see cref="DataType"/>.
		/// </summary>
		/// <param name="type">The other type, destination of the cast, to verify.</param>
		/// <remarks>
		/// By default, this method returns <c>false</c>, because cast process must be
		/// specified per type: when overriding the method <see cref="CastTo"/>, pay attention
		/// to also override this method accordingly.
		/// </remarks>
		/// <returns>
		/// </returns>
		/// <see cref="CastTo"/>
		public virtual bool CanCastTo(DataType type) {
			return false;
		}

		/// <summary>
		/// Converts the given <see cref="DataObject">object value</see> to a
		/// <see cref="DataType"/> specified.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="destType">The destination type of the conversion.</param>
		/// <remarks>
		/// If the given <paramref name="destType">destination type</paramref> is equivalent
		/// to this type, it will return the <paramref name="value"/> provided, otherwise
		/// it will throw an exception by default.
		/// <para>
		/// Casting values to specific types is specific to each data-type: override this
		/// method to support type-specific conversions.
		/// </para>
		/// <para>
		/// When overriding this method, <see cref="CanCastTo"/> should be overridden accordingly
		/// to indicate the type supports casting.
		/// </para>
		/// </remarks>
		/// <returns>
		/// Returns an instance of <see cref="DataObject"/> that is the result
		/// of the conversion from this data-type to the other type given.
		/// </returns>
		public virtual DataObject CastTo(DataObject value, DataType destType) {
			if (Equals(destType))
				return value;

			// TODO: Should we return a null value instead? NULL OF TYPE anyway is still a cast ...
			throw new NotSupportedException();
		}

		public virtual ISqlObject Add(ISqlObject a, ISqlObject b) {
			return SqlNull.Value;
		}

		public virtual ISqlObject Subtract(ISqlObject a, ISqlObject b) {
			return SqlNull.Value;
		}

		public virtual ISqlObject Multiply(ISqlObject a, ISqlObject b) {
			return SqlNull.Value;
		}

		public virtual ISqlObject Divide(ISqlObject a, ISqlObject b) {
			return SqlNull.Value;
		}

		public virtual ISqlObject Modulus(ISqlObject a, ISqlObject b) {
			return SqlNull.Value;
		}

		public virtual ISqlObject Negate(ISqlObject value) {
			return SqlNull.Value;
		}

		public virtual SqlBoolean IsEqualTo(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual SqlBoolean IsNotEqualTo(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual SqlBoolean IsGreatherThan(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual SqlBoolean IsSmallerThan(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual SqlBoolean IsGreaterOrEqualThan(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual SqlBoolean IsSmallerOrEqualThan(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual SqlBoolean And(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual ISqlObject And(ISqlObject value) {
			return SqlNull.Value;
		}

		public virtual SqlBoolean Or(ISqlObject a, ISqlObject b) {
			return SqlBoolean.Null;
		}

		public virtual ISqlObject XOr(ISqlObject x, ISqlObject y) {
			return SqlNull.Value;
		}

		public virtual ISqlObject Or(ISqlObject value) {
			return SqlNull.Value;
		}

		public virtual ISqlObject Reverse(ISqlObject value) {
			return SqlNull.Value;
		}

		/// <summary>
		/// Gets the one data-type between this and the other one given
		/// that handles the wider range of values.
		/// </summary>
		/// <param name="otherType">The other type to verify.</param>
		/// <remarks>
		/// This is very important for operations and functions, when
		/// operating on <see cref="DataObject">objects</see> with comparable
		/// but different data-types, to ensure the result of the operation
		/// will be capable to handle the final value.
		/// <para>
		/// By default, this method returns this instance, as it is not able
		/// to dynamically define the wider type.
		/// </para>
		/// </remarks>
		/// <returns>
		/// Returns this type if it handles a wider range of values or <paramref name="otherType">other 
		/// type</paramref> given otherwise.
		/// </returns>
		public virtual DataType Wider(DataType otherType) {
			return this;
		}

		/// <summary>
		/// Parses a SQL formatted string that defines a data-type into
		/// a constructed <see cref="DataType"/> object equivalent.
		/// </summary>
		/// <param name="s">The SQL formatted data-type string, defining the properties of the type.</param>
		/// <remarks>
		/// This method only supports primitive types.
		/// </remarks>
		/// <returns>
		/// </returns>
		/// <seealso cref="PrimitiveTypes.IsPrimitive(Deveel.Data.Types.SqlTypeCode)"/>
		/// <seealso cref="ToString()"/>
		public static DataType Parse(string s) {
			var sqlCompiler = new SqlCompiler();

			try {
				var node = sqlCompiler.CompileDataType(s);
				if (!node.IsPrimitive)
					throw new NotSupportedException("Cannot resolve the given string to a primitive type.");

				SqlTypeCode sqlTypeCode;
				if (String.Equals(node.TypeName, "LONG VARCHAR")) {
					sqlTypeCode = SqlTypeCode.LongVarChar;
				} else if (String.Equals(node.TypeName, "LONG VARBINARY")) {
					sqlTypeCode = SqlTypeCode.LongVarBinary;
				} else {
					sqlTypeCode = (SqlTypeCode) Enum.Parse(typeof (SqlTypeCode), node.TypeName, true);
				}

				if (sqlTypeCode == SqlTypeCode.Bit ||
					sqlTypeCode == SqlTypeCode.Boolean ||
					sqlTypeCode == SqlTypeCode.BigInt ||
				    sqlTypeCode == SqlTypeCode.Integer ||
				    sqlTypeCode == SqlTypeCode.SmallInt ||
				    sqlTypeCode == SqlTypeCode.TinyInt)
					return PrimitiveTypes.Type(sqlTypeCode);

				if (sqlTypeCode == SqlTypeCode.Float ||
				    sqlTypeCode == SqlTypeCode.Real ||
				    sqlTypeCode == SqlTypeCode.Double ||
				    sqlTypeCode == SqlTypeCode.Decimal) {
					if (node.HasScale && node.HasPrecision)
						return PrimitiveTypes.Type(sqlTypeCode, node.Scale, node.Precision);
					if (node.HasScale && !node.HasPrecision)
						return PrimitiveTypes.Type(sqlTypeCode, node.Scale);

					return PrimitiveTypes.Type(sqlTypeCode);
				}

				if (sqlTypeCode == SqlTypeCode.Char ||
				    sqlTypeCode == SqlTypeCode.VarChar ||
				    sqlTypeCode == SqlTypeCode.LongVarChar) {
					if (node.HasSize && node.HasLocale)
						return PrimitiveTypes.Type(sqlTypeCode, node.Size, node.Locale);
					if (node.HasSize && !node.HasLocale)
						return PrimitiveTypes.Type(sqlTypeCode, node.Size);
					if (node.HasLocale && !node.HasSize)
						return PrimitiveTypes.Type(sqlTypeCode, node.Locale);

					return PrimitiveTypes.Type(sqlTypeCode);
				}

				if (sqlTypeCode == SqlTypeCode.Date ||
				    sqlTypeCode == SqlTypeCode.Time ||
				    sqlTypeCode == SqlTypeCode.TimeStamp)
					return PrimitiveTypes.Type(sqlTypeCode);


				// TODO: Support %ROWTYPE and %TYPE

				throw new NotSupportedException(String.Format("The SQL type {0} is not supported here.", sqlTypeCode));
			} catch (SqlParseException) {
				throw new FormatException("Unable to parse the given string to a valid data type.");
			}
		}

		/// <inheritdoc/>
		public virtual int Compare(ISqlObject x, ISqlObject y) {
			if (!x.IsComparableTo(y))
				throw new NotSupportedException();

			if (x.IsNull && y.IsNull)
				return 0;
			if (x.IsNull && !y.IsNull)
				return 1;
			if (!x.IsNull && y.IsNull)
				return -1;

			return ((IComparable) x).CompareTo(y);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var dataType = obj as DataType;
			if (dataType == null)
				return false;

			return Equals(dataType);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return SqlType.GetHashCode();
		}

		/// <inheritdoc/>
		public virtual bool Equals(DataType other) {
			if (other == null)
				return false;

			return SqlType == other.SqlType;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return Name;
		}
	}
}