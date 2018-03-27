using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCake
{
    /// <summary>
    /// Defines the 3 interactive mode handled by <see cref="InteractiveAliases"/>.
    /// </summary>
    public enum InteractiveMode
    {
        /// <summary>
        /// No user interaction are supported (command line argument -nointeraction).
        /// </summary>
        NoInteraction,

        /// <summary>
        /// Auto interaction (command line argument -autointeraction) is an hybrid mode where answers
        /// are read from the command line or, for <see cref="InteractiveAliases.ReadInteractiveOption(Cake.Core.ICakeContext, string, string, char[])"/>,
        /// the first choice is choosen.
        /// </summary>
        AutoInteraction,

        /// <summary>
        /// User has to answer the questions.
        /// </summary>
        Interactive
    }
}
