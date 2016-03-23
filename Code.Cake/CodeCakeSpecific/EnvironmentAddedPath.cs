using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Captures a path that must be added to the PATH environment variable.
    /// When <see cref="IsDynamicPattern"/> is true, the path is expansed and/or gobbled
    /// dynamically instead of beeing resolved at the very beginning of the build script execution.
    /// </summary>
    public struct EnvironmentAddedPath
    {
        /// <summary>
        /// The path (may contain * and ? wildcards).
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Whether the path is a dynamic one: it must be challenged each time it is needed instead of being 
        /// expansed/globbed at the start of the build script (folders and files must pre-exist on the file system).
        /// </summary>
        public readonly bool IsDynamicPattern;

        /// <summary>
        /// Initializes a new <see cref="EnvironmentAddedPath"/>.
        /// </summary>
        /// <param name="path">Path or pattern with * and ? wildcards.</param>
        /// <param name="isDynamic">True if the pattern must be expansed/gobbled dynamically.</param>
        public EnvironmentAddedPath( string path, bool isDynamic )
        {
            if( string.IsNullOrWhiteSpace( path ) ) throw new ArgumentException( nameof( path ) );
            Path = path;
            IsDynamicPattern = isDynamic; 
        }

    }
}
