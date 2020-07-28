using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System;

namespace Gcsb.NSwag.Configuration
{
    public class ProcessorSwaggerControllers : IOperationProcessor
    {
        private readonly Predicate<Type> predicate;

        public ProcessorSwaggerControllers(Predicate<Type> predicate)
        {
            this.predicate = predicate;
        }

        public bool Process(OperationProcessorContext context)
        {
            return predicate(context.ControllerType);
        }
    }
}
