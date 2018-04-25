#region License
// Copyright (c) 2018, FluentMigrator Project
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

using System;
using System.Collections.Generic;
using System.Linq;

using FluentMigrator.Exceptions;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors
{
    /// <summary>
    /// An <see cref="IProcessorAccessor"/> implementation that selects one generator by name
    /// </summary>
    public class SelectingProcessorAccessor : IProcessorAccessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectingProcessorAccessor"/> class.
        /// </summary>
        /// <param name="processors">The processors to select from</param>
        /// <param name="options">The options used to determine the generator to be returned</param>
        public SelectingProcessorAccessor(
            [NotNull, ItemNotNull] IEnumerable<IMigrationProcessor> processors,
            [NotNull] IOptions<SelectingProcessorAccessorOptions> options)
        {
            var procs = processors.ToList();
            if (string.IsNullOrEmpty(options.Value.ProcessorId))
            {
                // No generator selected
                if (procs.Count == 0)
                    throw new ProcessorFactoryNotFoundException("No migration processor registered.");
                if (procs.Count > 1)
                    throw new ProcessorFactoryNotFoundException("More than one processor registered, but no processor id given. Specify the processor id by configuring SelectingProcessorAccessorOptions.");
                Processor = procs.Single();
            }
            else
            {
                // One of multiple generators
                Processor = FindGenerator(procs, options.Value.ProcessorId);
            }
        }

        /// <inheritdoc />
        public IMigrationProcessor Processor { get; }

        [NotNull]
        private IMigrationProcessor FindGenerator(
            [NotNull, ItemNotNull] IReadOnlyCollection<IMigrationProcessor> processors,
            [NotNull] string processorsId)
        {
            foreach (var processor in processors)
            {
                if (string.Equals(processor.DatabaseType, processorsId, StringComparison.OrdinalIgnoreCase))
                    return processor;
            }

            foreach (var processor in processors)
            {
                foreach (var databaseTypeAlias in processor.DatabaseTypeAliases)
                {
                    if (string.Equals(databaseTypeAlias, processorsId, StringComparison.OrdinalIgnoreCase))
                        return processor;
                }
            }

            var generatorNames = string.Join(", ", processors.Select(p => p.DatabaseType).Union(processors.SelectMany(p => p.DatabaseTypeAliases)));
            throw new ProcessorFactoryNotFoundException($@"A migration generator with the ID {processorsId} couldn't be found. Available generators are: {generatorNames}");
        }
    }
}
