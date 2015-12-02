using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Exception uses by Terminate methods to halt script execution.
    /// </summary>
    public class CakeTerminateException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CakeTerminateException"/>.
        /// </summary>
        /// <param name="message">Termination message.</param>
        /// <param name="option">Whether it is a success, a warning or an error.</param>
        public CakeTerminateException( string message, CakeTerminationOption option )
            : base( message )
        {
            Option = option;
        }

        /// <summary>
        /// Specify the kind of termination.
        /// </summary>
        public CakeTerminationOption Option { get; }
    }
}
