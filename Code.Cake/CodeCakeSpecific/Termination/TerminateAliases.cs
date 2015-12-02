using Cake.Common;
using Cake.Core;
using Cake.Core.Annotations;
using System;
using System.Linq;

namespace CodeCake
{
    /// <summary>
    /// Contains functionalities related to script termination.
    /// </summary>
    public static class TerminateAliases
    {
        /// <summary>
        /// Terminates the current script in a success, warning or error state.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="option">Whether it is a successful termination or not.</param>
        [CakeMethodAlias]
        public static void Terminate( this ICakeContext context, string message, CakeTerminationOption option )
        {
            throw new CakeTerminateException( message, option );
        }

        /// <summary>
        /// Terminates the current script in a success state.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="message">The message to display.</param>
        [CakeMethodAlias]
        public static void TerminateWithSuccess( this ICakeContext context, string message )
        {
            Terminate( context, message, CakeTerminationOption.Success );
        }

        /// <summary>
        /// Terminates the current script in a warning state.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="message">The message to display.</param>
        [CakeMethodAlias]
        public static void TerminateWithWarning( this ICakeContext context, string message )
        {
            Terminate( context, message, CakeTerminationOption.Warning );
        }

        /// <summary>
        /// Terminates the current script in a warning state.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="message">The message to display.</param>
        [CakeMethodAlias]
        public static void TerminateWithError( this ICakeContext context, string message )
        {
            Terminate( context, message, CakeTerminationOption.Error );
        }
    }
}