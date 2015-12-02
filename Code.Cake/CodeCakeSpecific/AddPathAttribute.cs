using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Decorates <see cref="CodeCakeHost"/> classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AddPathAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new <see cref="AddPathAttribute"/> with a pattern. Examples: 
        /// <code>[AddPath( "%LOCALAPPDATA%/NuGet" )]</code> or <code>[AddPath( "packages/**/tools/**" )]</code>.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that will be expansed in PATH environement variable.
        /// It it relative to the Solution directory.
        /// </param>
        public AddPathAttribute( string pattern )
        {
            Pattern = pattern;
        }

        /// <summary>
        /// Gets the pattern that will be expansed in PATH environement variable.
        /// It it relative to the Solution directory.
        /// </summary>
        public string Pattern { get; private set; }
    }
}
