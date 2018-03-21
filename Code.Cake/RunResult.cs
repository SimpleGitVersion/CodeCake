using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Captures the result of the <see cref="CodeCakeApplication.Run(string[], string)"/>.
    /// </summary>
    public struct RunResult
    {
        /// <summary>
        /// Gets the run result code.
        /// </summary>
        public readonly int ReturnCode;

        /// <summary>
        /// Gets whether the context is in interactive
        /// mode (see <see cref="InteractiveAliases.IsInteractiveMode(Cake.Core.ICakeContext)"/>).
        /// </summary>
        public readonly bool IsInteractiveMode;

        /// <summary>
        /// Gets whether the context is in auto interactive
        /// mode (see <see cref="InteractiveAliases.IsAutoInteractiveMode(Cake.Core.ICakeContext)"/>).
        /// </summary>
        public readonly bool IsAutoInteractiveMode;

        /// <summary>
        /// Gets whether the run succeeded (<see cref="ReturnCode"/> is 0).
        /// </summary>
        public bool Success => ReturnCode == 0;

        internal RunResult( int returnCode, bool isInteractiveMode, bool isAutoInteractiveMode )
        {            ReturnCode = returnCode;
            IsInteractiveMode = isInteractiveMode;
            IsAutoInteractiveMode = isAutoInteractiveMode;
        }

        /// <summary>
        /// Implicitly converts a <see cref="RunResult"/> into its <see cref="RunResult.ReturnCode"/>.
        /// </summary>
        /// <param name="r">The run result.</param>
        public static implicit operator int( RunResult r ) => r.ReturnCode;
    }
}
