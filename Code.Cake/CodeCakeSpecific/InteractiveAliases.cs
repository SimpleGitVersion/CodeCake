using Cake.Common;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using System;
using System.Linq;

namespace CodeCake
{
    /// <summary>
    /// Contains functionalities related to the interactive mode.
    /// </summary>
    [CakeAliasCategory( "Environment" )]
    public static class InteractiveAliases
    {
        /// <summary>
        /// The "nointeraction" string with no dash before.
        /// </summary>
        public static readonly string NoInteractionArgument = "nointeraction";

        /// <summary>
        /// The "autointeraction" string with no dash before.
        /// </summary>
        public static readonly string AutoInteractionArgument = "autointeraction";

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
        /// Gets whether the context supports interaction with the user (this can be true only if <see cref="IsInteractiveMode(ICakeContext)"/>
        /// is true) in automatic mode.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>True if interactive mode is available, false otherwise.</returns>
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static bool IsAutoInteractiveMode( this ICakeContext context )
        {
            return IsInteractiveMode( context ) && context.HasArgument( AutoInteractionArgument );
        }

        /// <summary>
        /// Prompts the user for one of the <paramref name="options"/> characters that MUST be uppercase.
        /// <see cref="IsInteractiveMode(ICakeContext)"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="message">
        /// Message that will be displayed in front of the input. 
        /// When null, no message is displayed, when not null, the options are automatically displayed: (Y/N/C).
        /// </param>
        /// <param name="options">Allowed characters that must be uppercase.</param>
        /// <returns>The entered char (always in uppercase). Necessarily one of the <paramref name="options"/>.</returns>
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static char ReadInteractiveOption( this ICakeContext context, string message, params char[] options )
        {
            return DoReadInteractiveOption( context, null, message, options );
        }

        /// <summary>
        /// Prompts the user for one of the <paramref name="options"/> characters that MUST be uppercase after
        /// having looked for a program argument that answers the prompt.
        /// <see cref="IsInteractiveMode(ICakeContext)"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="argumentName">Name of the command line argument.</param>
        /// <param name="message">
        /// Message that will be displayed in front of the input. 
        /// When null, no message is displayed, when not null, the options are automatically displayed: (Y/N/C).
        /// </param>
        /// <param name="options">Allowed characters that must be uppercase.</param>
        /// <returns>The entered char (always in uppercase). Necessarily one of the <paramref name="options"/>.</returns>
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static char ReadInteractiveOption( this ICakeContext context, string argumentName, string message, params char[] options )
        {
            if( String.IsNullOrWhiteSpace( argumentName ) ) throw new ArgumentException( "Must be a non empty string.", nameof( argumentName ) );
            return DoReadInteractiveOption( context, argumentName, message, options );
        }

        static char DoReadInteractiveOption( ICakeContext context, string argumentName, string message, char[] options )
        {
            if( options == null || options.Length == 0 ) throw new ArgumentException( "At least one (uppercase) character for options must be provided." );
            if( !IsInteractiveMode( context ) ) throw new InvalidOperationException( "Interactions are not allowed." );
            if( options.Any( c => char.IsLower( c ) ) ) throw new ArgumentException( "Options must be uppercase letter." );

            string choices = String.Join( "/", options );
            if( string.IsNullOrWhiteSpace( message ) )
                Console.Write( "{0}: ", choices );
            else Console.Write( "{0} ({1}): ", message, choices );

            if( argumentName != null && context.Arguments.HasArgument( argumentName ) )
            {
                Console.WriteLine();
                string arg = context.Arguments.GetArgument( argumentName );
                if( arg.Length != 1
                    || !options.Contains( char.ToUpperInvariant( arg[0] ) ) )
                {
                    context.Log.Error( $"Provided command line argument -{argumentName}={arg} is invalid. It must be a unique character in: {choices}" );
                }
                else
                {
                    context.Log.Information( $"Answered by command line argument -{argumentName}={arg}." );
                    return char.ToUpperInvariant( arg[0] );
                }
            }
            if( IsAutoInteractiveMode( context ) )
            {
                Console.WriteLine();
                if( argumentName != null )
                {
                    context.Log.Information( $"Mode -autointeraction (and no command line -{argumentName}=\"value\" argument provided): automatically answer with the first choice: {options[0]}." );
                }
                else
                {
                    context.Log.Information( $"Mode -autointeraction: automatically answer with the first choice: {options[0]}." );
                }
                return options[0];
            }
            for(; ; )
            {
                char c = char.ToUpperInvariant( Console.ReadKey().KeyChar );
                Console.WriteLine();
                if( options.Contains( c ) ) return c;
                Console.Write( $"Invalid choice '{c}'. Must be one of {choices}: " );
            }
        }

        /// <summary>
        /// Retrieves the value of the environment variable or null if the environment variable do not exist
        /// and can not be given by the user.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="variable">The environment variable.</param>
        /// <param name="setCache">By default, if the value is interactively read, it is stored in the process environment variables.</param>
        /// <returns>Retrieves the value of the environment variable or null if the environment variable do not exist.</returns>
        [CakeAliasCategory( "Environment Variables" )]
        [CakeMethodAlias]
        public static string InteractiveEnvironmentVariable( this ICakeContext context, string variable, bool setCache = true )
        {
            string v = context.EnvironmentVariable( variable );
            if( v == null && IsInteractiveMode( context ) )
            {
                Console.Write( $"Environment Variable '{variable}' not found. Enter its value: " );
                if( IsAutoInteractiveMode( context ) )
                {
                    Console.WriteLine();
                    string fromArgName = "ENV:" + variable;
                    string fromArg = context.Arguments.HasArgument( fromArgName ) ? context.Arguments.GetArgument( fromArgName ) : null;
                    if( fromArg != null )
                    {
                        context.Log.Information( $"Mode -autointeraction: automatically answer with -{fromArgName}={fromArg}." );
                        v = fromArg;
                    }
                    else
                    {
                        context.Log.Information( $"Mode -autointeraction (and no command line -{fromArgName}=XXX argument provided): automatically answer with an empty string." );
                        v = String.Empty;
                    }
                }
                else v = Console.ReadLine();
                if( setCache ) Environment.SetEnvironmentVariable( variable, v );
            }
            return v;
        }
    }
}
