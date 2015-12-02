using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Defines termination options.
    /// </summary>
    public enum CakeTerminationOption
    {
        /// <summary>
        /// Indicates a successul termination of the script.
        /// The build script halts and returns the 0 no-error result.
        /// </summary>
        Success,
        /// <summary>
        /// Indicates a successul termination but with warning of the script.
        /// The build script halts and returns the 0 no-error result.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicates an error.
        /// The build script halts and returns an error result. (Process exit code is 1.)
        /// </summary>
        Error
    }

}
