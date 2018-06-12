// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Cake.Core;
using Cake.Core.Diagnostics;

namespace CodeCake
{
    /// <summary>
    /// Our execution strategy could have specialized the Cake DefaultExecutionStrategy but since it is sealed
    /// we compose.
    /// </summary>
    public sealed class CodeCakeExecutionStrategy : IExecutionStrategy
    {
        private readonly ICakeLog _log;
        private readonly IExecutionStrategy _default;
        private readonly string _exclusiveTaskName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultExecutionStrategy"/> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="exclusiveTaskName">The optional exclusive task name to execute.</param>
        public CodeCakeExecutionStrategy( ICakeLog log, string exclusiveTaskName = null )
        {
            _exclusiveTaskName = exclusiveTaskName;
            _log = log;
            _default = new DefaultExecutionStrategy( log );
        }

        /// <summary>
        /// Performs the setup.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="context">The setup context.</param>
        public void PerformSetup( Action<ISetupContext> action, ISetupContext context )
        {
            _default.PerformSetup( action, context );
        }

        /// <summary>
        /// Performs the teardown.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="teardownContext">The context.</param>
        public void PerformTeardown( Action<ITeardownContext> action, ITeardownContext teardownContext )
        {
            _default.PerformTeardown( action, teardownContext );
        }

        /// <summary>
        /// Executes the specified task.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="context">The context.</param>
        /// <returns>Returned Task</returns>
        public Task ExecuteAsync( CakeTask task, ICakeContext context )
        {
            if( task == null ) return Task.CompletedTask;

            if( _exclusiveTaskName != null && _exclusiveTaskName != task.Name )
            {
                _default.Skip( task, new CakeTaskCriteria( ctx => true, null ) );
                return Task.CompletedTask;
            }
            return _default.ExecuteAsync( task, context );
        }

        /// <summary>
        /// Skips the specified task.
        /// </summary>
        /// <param name="task">The task to skip.</param>
        /// <param name="criteria">The criteria that caused the task to be skipped.</param>
        public void Skip( CakeTask task, CakeTaskCriteria criteria )
        {
            _default.Skip( task, criteria );
        }

        /// <summary>
        /// Executes the error reporter.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="exception">The exception.</param>
        public void ReportErrors( Action<Exception> action, Exception exception )
        {
            _default.ReportErrors( action, exception );
        }

        /// <summary>
        /// Executes the error handler.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="exception">The exception.</param>
        public void HandleErrors( Action<Exception> action, Exception exception )
        {
            _default.HandleErrors( action, exception );
        }

        /// <summary>
        /// Invokes the finally handler.
        /// </summary>
        /// <param name="action">The action.</param>
        public void InvokeFinally( Action action )
        {
            _default.InvokeFinally( action );
        }

        /// <summary>
        /// Performs the specified setup action before each task is invoked.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="taskSetupContext">The context.</param>
        public void PerformTaskSetup( Action<ITaskSetupContext> action, ITaskSetupContext taskSetupContext )
        {
            _default.PerformTaskSetup( action, taskSetupContext );
        }

        /// <summary>
        /// Performs the specified teardown action after each task is invoked.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="taskTeardownContext">The context.</param>
        public void PerformTaskTeardown( Action<ITaskTeardownContext> action, ITaskTeardownContext taskTeardownContext )
        {
            _default.PerformTaskTeardown( action, taskTeardownContext );
        }
    }
}
