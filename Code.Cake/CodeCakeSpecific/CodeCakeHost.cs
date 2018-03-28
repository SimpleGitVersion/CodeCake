using Cake.Core;
using Cake.Core.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Base class for build objects.
    /// </summary>
    public abstract class CodeCakeHost
    {
        [ThreadStatic]
        internal static IScriptHost _injectedActualHost;

        readonly IScriptHost _host;

        /// <summary>
        /// Initializes a new host.
        /// </summary>
        protected CodeCakeHost()
        {
            Debug.Assert( _injectedActualHost != null );
            _host = _injectedActualHost;
        }

        /// <summary>
        /// Gets the Cake context.
        /// </summary>
        public ICakeContext Cake
        {
            get { return _host.Context; }
        }

        /// <summary>
        /// Obsolete: Use Setup( Action&lt;ICakeContext&gt; ) instead.
        /// </summary>
        /// <param name="action"></param>
        [Obsolete( "Use Setup( Action<ICakeContext> ) instead." )]
        public void Setup( Action action )
        {
            _host.Setup( action );
        }


        /// <summary>
        /// Allows registration of an action that's executed before any tasks are run. If
        /// setup fails, no tasks will be executed but teardown will be performed.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        public void Setup( Action<ICakeContext> action )
        {
            _host.Setup( action );
        }

        /// <summary>
        /// Registers a named task.
        /// </summary>
        /// <param name="name">Name of the task.</param>
        /// <returns>A task builder object.</returns>
        public CakeTaskBuilder<ActionTask> Task( string name )
        {
            return _host.Task( name );
        }

        /// <summary>
        /// Obsolete: Use Teardown( Action&lt;ICakeContext&gt; ) instead.
        /// </summary>
        /// <param name="action"></param>
        [Obsolete( "Use Teardown( Action<ICakeContext> ) instead." )]
        public void Teardown( Action action )
        {
            _host.Teardown( action );
        }

        /// <summary>
        /// Allows registration of an action that's executed after all other tasks have been
        /// run. If a setup action or a task fails with or without recovery, the specified
        /// teardown action will still be executed.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        public void Teardown( Action<ICakeContext> action )
        {
            _host.Teardown( action );
        }

        /// <summary>
        /// Allows registration of an action that's executed before each task is run. If
        /// the task setup fails, its task will not be executed but the task teardown will
        /// be performed.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        public void TaskSetup( Action<ITaskSetupContext> action )
        {
            _host.TaskSetup( action );
        }

        /// <summary>
        /// Allows registration of an action that's executed after each task has been run.
        /// If a task setup action or a task fails with or without recovery, the specified
        /// task teardown action will still be executed.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        public void TaskTeardown( Action<ITaskTeardownContext> action )
        {
            _host.TaskTeardown( action );
        }


    }

}
