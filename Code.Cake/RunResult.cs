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
        /// Gets the context's interactive mode.
        /// </summary>
        public readonly InteractiveMode InteractiveMode;

        /// <summary>
        /// Gets whether the run succeeded (<see cref="ReturnCode"/> is 0).
        /// </summary>
        public bool Success => ReturnCode == 0;

        internal RunResult( int returnCode, InteractiveMode mode )
        {
            ReturnCode = returnCode;
            InteractiveMode = mode;
        }

        /// <summary>
        /// Implicitly converts a <see cref="RunResult"/> into its <see cref="RunResult.ReturnCode"/>.
        /// </summary>
        /// <param name="r">The run result.</param>
        public static implicit operator int( RunResult r ) => r.ReturnCode;
    }
}
