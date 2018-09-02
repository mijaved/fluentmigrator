
using System;
using System.Collections.Generic;
using System.Linq;
using FluentMigrator.Exceptions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Base;

namespace FluentMigrator.Runner.Generators.Oracle
{
    internal class Oracle12cColumn : OracleColumn
    {
        protected new const int OracleObjectNameMaxLength = 128;

        public Oracle12cColumn(IQuoter quoter)
            : base(quoter)
        {
        }

        /// <inheritdoc />
        protected override string FormatIdentity(ColumnDefinition column)
        {
            return column.IsIdentity ? GetIdentityString(column) : string.Empty;
        }

        private static string GetIdentityString(ColumnDefinition column)
        {
            return string.Format("GENERATED ALWAYS AS IDENTITY");
        }

    }
}
