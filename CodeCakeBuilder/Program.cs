using System;
using System.Linq;

namespace CodeCake
{
    class Program
    {
        /// <summary>
        /// CodeCakeBuilder entry point. This is a default, simple, implementation that can 
        /// be extended as needed.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>An error code (typically -1), 0 on success.</returns>
        static int Main( string[] args )
        {
            var app = new CodeCakeApplication();
            int result = app.Run( args );
            bool interactive = !args.Contains( '-' + InteractiveAliases.NoInteractionArgument, StringComparer.OrdinalIgnoreCase );
            if( interactive )
            {
                Console.WriteLine();
                Console.WriteLine( "Hit any key to exit. (Use -{0} parameter to exit immediately)", InteractiveAliases.NoInteractionArgument );
                Console.ReadKey();
            }
            return result;

        }
    }
}
