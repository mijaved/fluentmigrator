#region License
// Copyright (c) 2019, FluentMigrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using FluentMigrator.Runner.Conventions;

using JetBrains.Annotations;

namespace FluentMigrator.Runner.Initialization
{
    /// <summary>
    /// Accessor for an <see cref="IConventionSet"/>
    /// </summary>
    public interface IConventionSetAccessor
    {
        /// <summary>
        /// Get the convention set to use.
        /// </summary>
        /// <returns>The convention set.</returns>
        [CanBeNull]
        IConventionSet GetConventionSet();
    }
}
