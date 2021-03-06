using System;
using System.Threading;
using System.Threading.Tasks;
using Cqrs.Infrastructure.Dispatcher;

namespace Cqrs.Infrastructure.Workflow
{
    /// <summary>
    /// Реестр процессов.
    /// </summary>
    /// <typeparam name="TWorkflow">Тип процесса.</typeparam>
    public abstract class WorkflowRegistry<TWorkflow> : IWorkflowRegistry<TWorkflow> 
        where TWorkflow : class, IWorkflow
    {
        /// <summary>
        /// Обработчик команд.
        /// </summary>
        protected IMessageDispatcher Dispatcher { get; }
        
        protected WorkflowRegistry(IMessageDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }
        
        /// <inheritdoc />
        public async Task<TWorkflow> FindAsync(Guid id, CancellationToken cancellation)
        {
            var workflow = await FindCoreAsync(id, cancellation)
                .ConfigureAwait(continueOnCapturedContext: false);

            return workflow;
        }

        public abstract Task<TWorkflow> FindCoreAsync(Guid id, CancellationToken cancellation);
        
        /// <inheritdoc />
        public async Task PersistAsync(WorkflowEnvelope<TWorkflow> envelope, CancellationToken cancellation)
        {
            try
            {
                await PersistCoreAsync(envelope, cancellation)
                    .ConfigureAwait(continueOnCapturedContext: false);
      
                if (envelope.OutputCommands != null)
                {
                    while (envelope.OutputCommands.TryDequeue(out var command))
                    {
                        command.WorkflowId = envelope.Workflow.WorkflowId;
                        
                        await Dispatcher.DispatchCommandAsync(command, cancellation)
                            .ConfigureAwait(continueOnCapturedContext: false);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Не удалось сохранить процесс '{typeof(TWorkflow)}'", e);
            }
        }

        protected abstract Task PersistCoreAsync(WorkflowEnvelope<TWorkflow> envelope, CancellationToken cancellation);
    }
}