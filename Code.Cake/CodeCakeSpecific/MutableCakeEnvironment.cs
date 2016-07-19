using System;
using System.Globalization;
using System.Reflection;
using Cake.Core.IO;
using System.Collections.Generic;
using Cake.Core;
using System.Linq;
using System.Runtime.Versioning;

namespace CodeCake
{
    /// <summary>
    /// Represents the environment Cake operates in. This mutable implementation allows the PATH environment variable
    /// to be dynamically modified. Except this new <see cref="EnvironmentPaths"/> this is the same as the <see cref="CakeEnvironment"/>
    /// provided by Cake.
    /// </summary>
    public class MutableCakeEnvironment : ICakeEnvironment
    {
        readonly ICakePlatform _platform;
        readonly ICakeRuntime _runtime;
        readonly DirectoryPath _applicationRoot;
        readonly List<string> _paths;
        readonly List<string> _addedPaths;
        readonly List<string> _dynamicPaths;
        IGlobber _globber;

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
        /// Initializes a new instance of the <see cref="MutableCakeEnvironment" /> class.
        /// </summary>
        /// <param name="platform">The platform.</param>
        /// <param name="runtime">The runtime.</param>
        public MutableCakeEnvironment( ICakePlatform platform, ICakeRuntime runtime )
        {
            _platform = platform;
            _runtime = runtime;
            _applicationRoot = System.IO.Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );

            WorkingDirectory = new DirectoryPath( Environment.CurrentDirectory );
            var pathEnv = Environment.GetEnvironmentVariable( "PATH" );
            if( !string.IsNullOrEmpty( pathEnv ) )
            {
                _paths = pathEnv.Split( new char[] { _platform.IsUnix() ? ':' : ';' }, StringSplitOptions.RemoveEmptyEntries )
                                .Select( s => s.Trim() )
                                .Where( s => s.Length > 0 )
                                .ToList();
            }
            else
            {
                _paths = new List<string>();
            }
            _addedPaths = new List<string>();
            _dynamicPaths = new List<string>();
        }

        internal void Initialize( IGlobber globber )
        {
            _globber = globber;
        }

        /// <summary>
        /// Gets whether or not the current operative system is 64 bit.
        /// </summary>
        /// <returns>
        /// Whether or not the current operative system is 64 bit.
        /// </returns>
        [Obsolete( "Please use CakeEnvironment.Platform.Is64Bit instead." )]
        public bool Is64BitOperativeSystem() => _platform.Is64Bit;

        public bool IsUnix() => _platform.IsUnix();

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
        /// <value>The application root path.</value>
        public DirectoryPath ApplicationRoot => _applicationRoot;

        [Obsolete( "Please use CakeEnvironment.ApplicationRoot instead." )]
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
            if( StringComparer.OrdinalIgnoreCase.Equals( variable, "PATH" ) ) return string.Join( _platform.IsUnix() ? ":" : ";", FinalEnvironmentPaths );
            return Environment.GetEnvironmentVariable( variable );
        }

        /// <summary>
        /// Gets a list of paths in PATH environement variable. 
        /// When getting the PATH variable with <see cref="GetEnvironmentVariable"/>, the <see cref="FinalEnvironmentPaths"/> is returned as a joined string.
        /// </summary>
        public IReadOnlyList<string> EnvironmentPaths
        {
            get { return _paths; }
        }

        /// <summary>
        /// Gets a list of paths added via <see cref="AddPath(EnvironmentAddedPath)"/>. 
        /// When getting the PATH variable with <see cref="GetEnvironmentVariable"/>, the <see cref="FinalEnvironmentPaths"/> is returned as a joined string.
        /// </summary>
        public IList<string> EnvironmentAddedPaths
        {
            get { return _addedPaths; }
        }

        /// <summary>
        /// Gets a list of dynamic paths added via <see cref="AddPath"/>.
        /// When getting the PATH variable with <see cref="GetEnvironmentVariable"/>, the <see cref="FinalEnvironmentPaths"/> is returned as a joined string.
        /// </summary>
        public IReadOnlyList<string> EnvironmentDynamicPaths
        {
            get { return _dynamicPaths; }
        }

        /// <summary>
        /// Get the final environment paths: it is the <see cref="EnvironmentPaths"/>, the <see cref="EnvironmentAddedPaths"/> 
        /// and the <see cref="ExistingPathsFromDynamicPaths"/>.
        /// </summary>
        public IEnumerable<string> FinalEnvironmentPaths => _paths.Concat(_addedPaths ).Concat( ExistingPathsFromDynamicPaths );

        /// <summary>
        /// Adds a path to <see cref="EnvironmentAddedPaths"/> or <see cref="EnvironmentDynamicPaths"/>.
        /// </summary>
        /// <param name="p">The path to add.</param>
        public void AddPath( EnvironmentAddedPath p )
        {
            if( p.IsDynamicPattern )
            {
                if( !_dynamicPaths.Contains( p.Path ) ) _dynamicPaths.Add( p.Path );
            }
            else
            {
                string expansed = Environment.ExpandEnvironmentVariables( p.Path );
                foreach( var final in _globber.GetDirectories( expansed ).Select( d => d.FullPath ) )
                {
                    if( !_addedPaths.Contains( final ) ) _addedPaths.Add( final );
                }
            }
        }

        /// <summary>
        /// Gets the existing paths defined by <see cref="EnvironmentDynamicPaths"/>.
        /// </summary>
        public IEnumerable<string> ExistingPathsFromDynamicPaths => _dynamicPaths.SelectMany( p => _globber.GetDirectories( Environment.ExpandEnvironmentVariables( p ) ).Select( d => d.FullPath ) );

        /// <summary>
        /// Gets the platform Cake is running on.
        /// </summary>
        /// <value>The platform Cake is running on.</value>
        public ICakePlatform Platform => _platform;

        /// <summary>
        /// Gets the runtime Cake is running in.
        /// </summary>
        /// <value>The runtime Cake is running in.</value>
        public ICakeRuntime Runtime => _runtime;

        private static void SetWorkingDirectory( DirectoryPath path )
        {
            if( path.IsRelative )
            {
                throw new CakeException( "Working directory can not be set to a relative path." );
            }
            Environment.CurrentDirectory = path.FullPath;
        }

        /// <summary>
        /// Gets all environment variables.
        /// </summary>
        /// <returns>The environment variables as IDictionary&lt;string, string&gt; </returns>
        public IDictionary<string, string> GetEnvironmentVariables()
        {
            return Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .Select( e => StringComparer.OrdinalIgnoreCase.Equals( e.Key, "PATH" ) 
                                ? new System.Collections.DictionaryEntry( e.Key, GetEnvironmentVariable( "PATH" ) )
                                : e )
                .ToDictionary(
                key => (string)key.Key,
                value => value.Value as string,
                StringComparer.OrdinalIgnoreCase );
        }

        [Obsolete( "Please use CakeEnvironment.Runtime.TargetFramework instead." )]
        public FrameworkName GetTargetFramework() => _runtime.TargetFramework;

    }
}
