using Cake.Core;
using Cake.Core.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Code.Cake.CodeCakeSpecific
{
    /// <summary>
    /// Contains functionalities related to the interactive mode.
    /// </summary>
    [CakeAliasCategory( "Environment" )]
    public static class FileAndPathAliases
    {
        /// <summary>
        /// Finds a directory above the current working directory.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>Null if not found, otherwise the path of the directory.</returns>
        [CakeAliasCategory( "Directory" )]
        [CakeMethodAlias]
        public static string FindDirectoryAbove( this ICakeContext context, string directoryName )
        {
            return FindSiblingDirectoryAbove( context, Path.GetDirectoryName( context.Environment.WorkingDirectory.FullPath ), directoryName );
        }

        /// <summary>
        /// Finds a named directory above or next to the specified <paramref name="start"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="start">Starting directory.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>Null if not found, otherwise the path of the directory.</returns>
        [CakeAliasCategory( "Directory" )]
        [CakeMethodAlias]
        public static string FindSiblingDirectoryAbove( this ICakeContext context, string start, string directoryName )
        {
            if( start == null ) throw new ArgumentNullException( "start" );
            if( directoryName == null ) throw new ArgumentNullException( "directortyName" );
            string p = Path.GetDirectoryName( start );
            string pF;
            while( !Directory.Exists( pF = Path.Combine( p, directoryName ) ) )
            {
                p = Path.GetDirectoryName( p );
                if( String.IsNullOrEmpty( p ) ) return null;
            }
            return pF;
        }
    }

}