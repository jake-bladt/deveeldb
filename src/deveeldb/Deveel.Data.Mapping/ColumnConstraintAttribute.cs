﻿using System;

namespace Deveel.Data.Mapping {
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public class ColumnConstraintAttribute : Attribute {
		public ColumnConstraintAttribute(ColumnConstraintType constraintType) {
			ConstraintType = constraintType;
		}

		public ColumnConstraintType ConstraintType { get; private set; }
	}
}
