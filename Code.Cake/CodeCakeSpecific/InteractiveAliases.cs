using Cake.Common;
using Cake.Core;
using Cake.Core.Annotations;
using System;
using System.Linq;

namespace CodeCake
{
    /// <summary>
    /// Contains functionality related to the interactive mode.
    /// </summary>
    [CakeAliasCategory( "Environment" )]
    public static class InteractiveAliases
    {
        /// <summary>
        /// The "nointeraction" string with no dash before.
        /// </summary>
        public static readonly string NoInteractionArgument = "nointeraction";

        /// <summary>
        /// Gets whether the context supports interaction with the user (depends on -nointeraction argument).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>True if interactive mode is available, false otherwise.</returns>
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static bool IsInteractiveMode( this ICakeContext context )
        {
            return !context.HasArgument( NoInteractionArgument );
        }

        /// <summary>
        /// Prompts the user for one of the <paramref name="options"/> characters (case insensitive).
        /// <see cref="IsInteractiveMode(ICakeContext)"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="message">
        /// Message that will be displayed in front of the input. 
        /// When null, no message is displayed, when not null, the options are automatically displayed: (Y/N/C).
        /// </param>
        /// <param name="options">Allowed characters. This is case insensitive.</param>
        /// <returns>The entered char, always in uppercase. Necessarily on of the <paramref name="options"/>.</returns>
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static char ReadInteractiveOption( this ICakeContext context, string message, params char[] options )
        {
            if( options == null || options.Length == 0 ) throw new ArgumentException( "At least one character for options must be provided." );
            if( !IsInteractiveMode( context ) ) throw new InvalidOperationException( "Interactions are not allowed." );
            var oU = options.Select( c => char.ToUpperInvariant( c ) );
            string choices = String.Join( "/", oU );
            if( message != null )
            {
                if( string.IsNullOrWhiteSpace( message ) )
                    Console.Write( "{0}: ", choices );
                else Console.Write( "{0} ({1}): ", message, choices );
            }
            for(;;)
            {
                char c = char.ToUpperInvariant( Console.ReadKey().KeyChar );
                if( oU.Contains( c ) ) return c;
                Console.WriteLine();
                Console.Write( "Invalid choice '{0}'. Must be one of {1}: ", c, choices );
            }
        }

        /// <summary>
        /// Retrieves the value of the environment variable or null if the environment variable do not exist
        /// and can not be given by the user.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="variable">The environment variable.</param>
        /// <returns>Retrieves the value of the environment variable or null if the environment variable do not exist.</returns>
        [CakeAliasCategory( "Environment Variables" )]
        [CakeMethodAlias]
        public static string InteractiveEnvironmentVariable( this ICakeContext context, string variable )
        {
            string v = context.EnvironmentVariable( variable );
            if( v == null && IsInteractiveMode( context ) )
            {
                Console.Write( "Environment Variable '{0}' not found. Enter its value: ", variable );
                v = Console.ReadLine();
            }
            return v;
        }
    }
}