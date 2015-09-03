using Cake.Core;
using Cake.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cake.Common.Solution
{
    /// <summary>
    /// Supports extension methods for Solution related objects.
    /// </summary>
    public static class CodeCakeSolutionExtensions
    {
        /// <summary>
        /// Creates a <see cref="ITemporarySolutionFile"/> for a solution (.sln) file.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="solutionPath">The solution path.</param>
        /// <returns>A temporary solution file that must be disposed.</returns>
        public static ITemporarySolutionFile CreateTemporarySolutionFile( this ICakeContext context, FilePath solutionPath )
        {
            if( solutionPath == null ) throw new ArgumentNullException( "solution" );
            return new TemporarySolutionFile( context, solutionPath );
        }
    }
}
