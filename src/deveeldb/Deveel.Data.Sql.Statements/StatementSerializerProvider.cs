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

using Deveel.Data.Serialization;

namespace Deveel.Data.Sql.Statements {
	class StatementSerializerProvider : ObjectSerializerProvider {
		protected override void Init() {
			Register<AlterTableStatement.Prepared, AlterTableStatement.PreparedSerializer>();
			Register<AlterUserStatement.Prepared, AlterUserStatement.PreparedSerializer>();
			Register<LoopControlStatement.Prepared, LoopControlStatement.Serializer>();
			Register<CloseStatement, CloseStatement.Serializer>();
			Register<CreateTableStatement.Prepared, CreateTableStatement.PreparedSerializer>();
			Register<CreateUserStatement.Prepared, CreateUserStatement.PreparedSerializer>();
			Register<CreateViewStatement.Prepared, CreateViewStatement.PreparedSerializer>();
			Register<DeclareCursorStatement, DeclareCursorStatement.Serializer>();
			Register<DropTableStatement.Prepared, DropTableStatement.PreparedSerializer>();
			Register<DropViewStatement.Prepared, DropViewStatement.PreparedSerializer>();
			Register<InsertSelectStatement.Prepared, InsertSelectStatement.PreparedSerializer>();
			Register<InsertStatement.Prepared, InsertStatement.PreparedSerializer>();
			Register<OpenStatement.Prepared, OpenStatement.PreparedSerializer>();
			Register<SelectIntoStatement.Prepared, SelectIntoStatement.PreparedSerializer>();
		}
	}
}