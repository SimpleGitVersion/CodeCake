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
        /// Gets the current <see cref="CodeCake.InteractiveMode"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The mode.</returns>
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static InteractiveMode InteractiveMode( this ICakeContext context )
        {
            if( context.HasArgument( NoInteractionArgument ) ) return CodeCake.InteractiveMode.NoInteraction;
            if( context.HasArgument( AutoInteractionArgument ) ) return CodeCake.InteractiveMode.AutoInteraction;
            return CodeCake.InteractiveMode.Interactive;
        }

        /// <summary>
        /// Gets whether the context requires interaction with the user:
        /// False if -nointeraction or -autointeraction argument has been provided, true if no specific argument have been provided.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>True if interactive mode is available, false otherwise.</returns>
        [Obsolete( "Use the less ambiguous InteractiveMode() enum value instead.", false)]
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static bool IsInteractiveMode( this ICakeContext context )
        {
            return !context.HasArgument( NoInteractionArgument );
        }

        /// <summary>
        /// Gets whether the context supports automatic interaction (argument -autointeraction has been provided).
        /// When this is true, <see cref="IsInteractiveMode"/> is false. 
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>True if interactive mode is available, false otherwise.</returns>
        [Obsolete( "Use the less ambiguous InteractiveMode() enum value instead.", false )]
        [CakeAliasCategory( "Interactive mode" )]
        [CakeMethodAlias]
        public static bool IsAutoInteractiveMode( this ICakeContext context )
        {
            return IsInteractiveMode( context ) && context.HasArgument( AutoInteractionArgument );
        }

        /// <summary>
        /// Prompts the user for one of the <paramref name="options"/> characters that MUST be uppercase.
        /// <see cref="InteractiveMode()"/> must be <see cref="CodeCake.InteractiveMode.AutoInteraction"/>
        /// or <see cref="CodeCake.InteractiveMode.Interactive"/> otherwise an <see cref="InvalidOperationException"/> is thrown.
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
        /// <see cref="InteractiveMode()"/> must be <see cref="CodeCake.InteractiveMode.AutoInteraction"/>
        /// or <see cref="CodeCake.InteractiveMode.Interactive"/> otherwise an <see cref="InvalidOperationException"/> is thrown.
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
            var mode = InteractiveMode( context );
            if( mode == CodeCake.InteractiveMode.NoInteraction ) throw new InvalidOperationException( "Interactions are not allowed." );
            if( options.Any( c => char.IsLower( c ) ) ) throw new ArgumentException( "Options must be uppercase letter." );

            string choices = String.Join( "/", options );
            if( string.IsNullOrWhiteSpace( message ) )
                Console.Write( "{0}: ", choices );
            else Console.Write( "{0} ({1}): ", message, choices );

            if( argumentName != null && context.Arguments.HasArgument( argumentName ) )
            {
                string arg = context.Arguments.GetArgument( argumentName );
                if( arg.Length != 1
                    || !options.Contains( char.ToUpperInvariant( arg[0] ) ) )
                {
                    Console.WriteLine();
                    context.Log.Error( $"Provided command line argument -{argumentName}={arg} is invalid. It must be a unique character in: {choices}" );
                    // Fallback to interactive mode below.
                }
                else
                {
                    var c = char.ToUpperInvariant( arg[0] );
                    Console.WriteLine( c );
                    context.Log.Information( $"Answered by command line argument -{argumentName}={arg}." );
                    return c;
                }
            }
            if( mode == CodeCake.InteractiveMode.AutoInteraction )
            {
                char c = options[0];
                Console.WriteLine( c );
                if( argumentName != null )
                {
                    context.Log.Information( $"Mode -autointeraction (and no command line -{argumentName}=\"value\" argument found): automatically answer with the first choice: {c}." );
                }
                else
                {
                    context.Log.Information( $"Mode -autointeraction: automatically answer with the first choice: {c}." );
                }
                return c;
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
        /// In -autointeraction mode, the value can be provided on the commannd line using -ENV:<paramref name="variable"/>=... parameter.
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
            var mode = InteractiveMode( context );
            if( v == null && mode != CodeCake.InteractiveMode.NoInteraction )
            {
                Console.Write( $"Environment Variable '{variable}' not found. Enter its value: " );
                if( mode == CodeCake.InteractiveMode.AutoInteraction )
                {
                    string fromArgName = "ENV:" + variable;
                    string fromArg = context.Arguments.HasArgument( fromArgName ) ? context.Arguments.GetArgument( fromArgName ) : null;
                    if( fromArg != null )
                    {
                        Console.WriteLine( v = fromArg );
                        context.Log.Information( $"Mode -autointeraction: automatically answer with command line -{fromArgName}={fromArg} argument." );
                    }
                    else
                    {
                        Console.WriteLine( v = String.Empty );
                        context.Log.Information( $"Mode -autointeraction (and no command line -{fromArgName}=XXX argument): automatically answer with an empty string." );
                    }
                }
                else v = Console.ReadLine();
                if( setCache ) Environment.SetEnvironmentVariable( variable, v );
            }
            return v;
        }
    }
}
