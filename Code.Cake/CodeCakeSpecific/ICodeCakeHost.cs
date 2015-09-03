using Cake.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake
{
    /// <summary>
    /// Defines the host for build ojects.
    /// </summary>
    public interface ICodeCakeHost
    {
        /// <summary>
        /// Gets the Cake context.
        /// </summary>
        ICakeContext Cake { get; }

        /// <summary>
        /// Registers a named task.
        /// </summary>
        /// <param name="name">Name of the task.</param>
        /// <returns>A task builder object.</returns>
        CakeTaskBuilder<ActionTask> Task( string name );

        /// <summary>
        /// Registers the Setup operation.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        void Setup( Action action );

        /// <summary>
        /// Registers the Teardown action.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        void Teardown( Action action );

    }
}
