using System;
using System.Globalization;
using System.Reflection;
using Cake.Core.IO;
using System.Collections.Generic;
using Cake.Core;

namespace Code.Cake
{
    /// <summary>
    /// Represents the environment Cake operates in. This mutable implementation allows the PATH environment variable
    /// to be dynamically modified. Except this new <see cref="EnvironmentPaths"/> this is the same as the <see cref="CakeEnvironment"/>
    /// provided by Cake.
    /// </summary>
    public class MutableCakeEnvironment : ICakeEnvironment
    {
        readonly HashSet<string> _path;

        /// <summary>
        /// Gets or sets the working directory.
        /// </summary>
        /// <value>The working directory.</value>
        public DirectoryPath WorkingDirectory
        {
            get { return Environment.CurrentDirectory; }
            set { SetWorkingDirectory( value ); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MutableCakeEnvironment"/> class.
        /// </summary>
        public MutableCakeEnvironment()
        {
            WorkingDirectory = new DirectoryPath( Environment.CurrentDirectory );
            var pathEnv = Environment.GetEnvironmentVariable( "PATH" );
            if( !string.IsNullOrEmpty( pathEnv ) )
            {
                _path = new HashSet<string>( pathEnv.Split( Machine.IsUnix() ? ':' : ';' ) );
            }
            else
            {
                _path = new HashSet<string>();
            }
        }

        /// <summary>
        /// Gets whether or not the current operative system is 64 bit.
        /// </summary>
        /// <returns>
        /// Whether or not the current operative system is 64 bit.
        /// </returns>
        public bool Is64BitOperativeSystem()
        {
            return Machine.Is64BitOperativeSystem();
        }

        /// <summary>
        /// Determines whether the current machine is running Unix.
        /// </summary>
        /// <returns>
        /// Whether or not the current machine is running Unix.
        /// </returns>
        public bool IsUnix()
        {
            return Machine.IsUnix();
        }

        /// <summary>
        /// Gets a special path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// A <see cref="DirectoryPath" /> to the special path.
        /// </returns>
        public DirectoryPath GetSpecialPath( SpecialPath path )
        {
            switch( path )
            {
                case SpecialPath.ApplicationData:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) );
                case SpecialPath.CommonApplicationData:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ) );
                case SpecialPath.LocalApplicationData:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ) );
                case SpecialPath.ProgramFiles:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles ) );
                case SpecialPath.ProgramFilesX86:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 ) );
                case SpecialPath.Windows:
                    return new DirectoryPath( Environment.GetFolderPath( Environment.SpecialFolder.Windows ) );
                case SpecialPath.LocalTemp:
                    return new DirectoryPath( System.IO.Path.GetTempPath() );
            }
            const string format = "The special path '{0}' is not supported.";
            throw new NotSupportedException( string.Format( CultureInfo.InvariantCulture, format, path ) );
        }

        /// <summary>
        /// Gets the application root path.
        /// </summary>
        /// <returns>
        /// The application root path.
        /// </returns>
        public DirectoryPath GetApplicationRoot()
        {
            var path = System.IO.Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            return new DirectoryPath( path );
        }

        /// <summary>
        /// Gets an environment variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>
        /// The value of the environment variable.
        /// </returns>
        public string GetEnvironmentVariable( string variable )
        {
            if( StringComparer.OrdinalIgnoreCase.Equals( variable, "PATH" ) ) return String.Join( Machine.IsUnix() ? ":" : ";", EnvironmentPaths );
            return Environment.GetEnvironmentVariable( variable );
        }

        /// <summary>
        /// Gets a mutable set of paths. This is initialized with the PATH environment variable but can be changed at any time.
        /// When getting the PATH variable with <see cref="GetEnvironmentVariable"/>, this set is returned as a joined string.
        /// </summary>
        public ISet<string> EnvironmentPaths
        {
            get { return _path; }
        }

        private static void SetWorkingDirectory( DirectoryPath path )
        {
            if( path.IsRelative )
            {
                throw new CakeException( "Working directory can not be set to a relative path." );
            }
            Environment.CurrentDirectory = path.FullPath;
        }
    }
}
