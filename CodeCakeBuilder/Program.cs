using System;

namespace CodeCake
{
    class Program
    {
        /// <summary>
        /// CodeCakeBuilder entry point. This is a default, simple, implementation that can 
        /// be extended as needed.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>An error code (typically negative), 0 on success.</returns>
        static int Main( string[] args )
        {
            var app = new CodeCakeApplication();
            RunResult result = app.Run( args );
            if( result.InteractiveMode == InteractiveMode.Interactive )
            {
                Console.WriteLine();
                Console.WriteLine( $"Hit any key to exit." );
                Console.WriteLine( $"Use -{InteractiveAliases.NoInteractionArgument} or -{InteractiveAliases.AutoInteractionArgument} parameter to exit immediately." );
                Console.ReadKey();
            }
            return result.ReturnCode;
        }
    }
}
