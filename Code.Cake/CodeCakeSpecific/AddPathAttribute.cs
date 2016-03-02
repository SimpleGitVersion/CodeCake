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
        /// Initializes a new <see cref="AddPathAttribute"/> with a path that can be a pattern with * and ?. Examples: 
        /// <code>[AddPath( "%LOCALAPPDATA%/NuGet" )]</code> or <code>[AddPath( "packages/**/tools/**" )]</code>.
        /// </summary>
        /// <param name="path">
        /// The path that will be expansed in PATH environement variable.
        /// It it relative to the Solution directory.
        /// </param>
        /// <param name="isDynamicPath">False to take only pre-existing folders on the file system.</param>
        public AddPathAttribute( string path, bool isDynamicPath = true )
        {
            Path = path;
            IsDynamicPath = isDynamicPath;
        }

        /// <summary>
        /// Gets whether the path is a dynamic path: it will be recomputed each time it is needed instead of 
        /// being expansed/globbed at the start of the build script (folders and files must pre-exist on the file system).
        /// </summary>
        public bool IsDynamicPath { get; }

        /// <summary>
        /// Gets the pattern that will be expansed in PATH environement variable.
        /// It it relative to the Solution directory.
        /// </summary>
        public string Path { get; private set; }
    }
}
