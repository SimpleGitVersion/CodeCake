using Cake.Arguments;
using Cake.Core;
using Cake.Core.Configuration;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.IO.NuGet;
using Cake.Core.Tooling;
using Cake.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Crappy implementation... but it works.
    /// </summary>
    public class CodeCakeApplication
    {
        readonly IDictionary<string, CodeCakeBuildTypeDescriptor> _builds;
        private readonly string _solutionDirectory;

        /// <summary>
        /// Initializes a new CodeCakeApplication (DNX context).
        /// </summary>
        /// <param name="solutionDirectory">Solution directory: will become the <see cref="ICakeEnvironment.WorkingDirectory"/>.</param>
        /// <param name="codeContainers">Assemblies that may contain concrete <see cref="CodeCakeHost"/> objects.</param>
        public CodeCakeApplication( string solutionDirectory, params Assembly[] codeContainers )
            : this( (IEnumerable<Assembly>)codeContainers, solutionDirectory )
        {
        }

        /// <summary>
        /// Initializes a new CodeCakeApplication.
        /// </summary>
        /// <param name="codeContainers">
        /// Assemblies that may contain concrete <see cref="CodeCakeHost"/> objects.
        /// The <see cref="Assembly.GetEntryAssembly()"/> is always considered, this is why it can be let to null or be empty.
        /// </param>
        /// <param name="solutionDirectory">
        /// Solution directory: will become the <see cref="ICakeEnvironment.WorkingDirectory"/>.
        /// When null, we consider the <see cref="AppContext.BaseDirectory"/> to be running in "Solution/Builder/bin/[Configuration}/[targetFramework]" folder:
        /// we compute the solution directory by looking for the /bin/ folder and escalating 2 levels.
        /// </param>
        public CodeCakeApplication( IEnumerable<Assembly> codeContainers = null, string solutionDirectory = null )
        {
            var executingAssembly = Assembly.GetEntryAssembly();
            if( codeContainers == null ) codeContainers = Enumerable.Empty<Assembly>();
            _builds = codeContainers.Concat( new[] { executingAssembly } )
                            .Where( a => a != null )
                            .Distinct()
                            .SelectMany( a => a.GetTypes() )
                            .Where( t => !t.IsAbstract && typeof( CodeCakeHost ).IsAssignableFrom( t ) )
                            .ToDictionary( t => t.Name, t => new CodeCakeBuildTypeDescriptor( t ) );
            if( solutionDirectory == null && executingAssembly != null )
            {
                solutionDirectory = new Uri( Assembly.GetEntryAssembly().CodeBase ).LocalPath;
                while( System.IO.Path.GetFileName( solutionDirectory ) != "bin" )
                {
                    solutionDirectory = System.IO.Path.GetDirectoryName( solutionDirectory );
                    if( string.IsNullOrEmpty( solutionDirectory ) )
                    {
                        throw new ArgumentException( $"Unable to find /bin/ folder in AppContext.BaseDirectory = {AppContext.BaseDirectory}. Please provide a non null solution directory.", nameof(solutionDirectory) );
                    }
                }
                solutionDirectory = System.IO.Path.GetDirectoryName( solutionDirectory );
                solutionDirectory = System.IO.Path.GetDirectoryName( solutionDirectory );
            }
            _solutionDirectory = solutionDirectory;
        }

        /// <summary>
        /// Temporary fix waiting for PR https://github.com/cake-build/cake/pull/485
        /// </summary>
        class SafeCakeLog : IVerbosityAwareLog
        {
            Cake.Diagnostics.CakeBuildLog _logger;

            public SafeCakeLog( CakeConsole c )
            {
                _logger = new Cake.Diagnostics.CakeBuildLog( c );
            }

            public Verbosity Verbosity
            {
                get { return _logger.Verbosity; }
                set { _logger.Verbosity = value; }
            }

            public void SetVerbosity( Verbosity verbosity )
            {
                _logger.SetVerbosity( verbosity );
            }

            public void Write( Verbosity verbosity, LogLevel level, string format, params object[] args )
            {
                if( args.Length == 0 ) format = format.Replace( "{", "{{" );
                _logger.Write( verbosity, level, format, args );
            }
        }

        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="appRoot">Application root folder</param>
        /// <returns>The result of the run.</returns>
        public RunResult Run( string[] args, string appRoot = null)
        {
            var console = new CakeConsole();
            var logger = new SafeCakeLog( console );
            var engine = new CakeEngine( logger );

            ICakePlatform platform = new CakePlatform();
            ICakeRuntime runtime = new CakeRuntime();
            IFileSystem fileSystem = new FileSystem();
            MutableCakeEnvironment environment = new MutableCakeEnvironment( platform, runtime, appRoot );
            IGlobber globber = new Globber( fileSystem, environment );
            environment.Initialize( globber );
            IProcessRunner processRunner = new ProcessRunner( environment, logger );
            IRegistry windowsRegistry = new WindowsRegistry();
            // Parse options.
            var argumentParser = new ArgumentParser( logger, fileSystem );
            CakeOptions options = argumentParser.Parse( args );
            Debug.Assert( options != null );
            CakeConfigurationProvider configProvider = new CakeConfigurationProvider( fileSystem, environment );
            ICakeConfiguration configuration = configProvider.CreateConfiguration( environment.ApplicationRoot, options.Arguments );
            IToolRepository toolRepo = new ToolRepository( environment );
            IToolResolutionStrategy toolStrategy = new ToolResolutionStrategy( fileSystem, environment, globber, configuration );
            IToolLocator locator = new ToolLocator( environment, toolRepo, toolStrategy );
            IToolLocator toolLocator = new ToolLocator( environment, toolRepo, toolStrategy  );
            logger.SetVerbosity( options.Verbosity );
            ICakeArguments arguments = new CakeArguments(options.Arguments);
            var context = new CakeContext( fileSystem, environment, globber, logger, arguments, processRunner, windowsRegistry, locator );

            CodeCakeBuildTypeDescriptor choosenBuild;
            if( !AvailableBuilds.TryGetValue( options.Script, out choosenBuild ) )
            {
                logger.Error( "Build script '{0}' not found.", options.Script );
                return new RunResult( -1, context.InteractiveMode() );
            }

            // Set the working directory: the solution directory.
            environment.WorkingDirectory = new DirectoryPath( _solutionDirectory );

            // Adds additional paths from chosen build.
            foreach( var p in choosenBuild.AdditionnalPatternPaths )
            {
                environment.AddPath( p );
            }
            logger.Information( "Path(s) added: " + string.Join( ", ", environment.EnvironmentAddedPaths ) );
            logger.Information( "Dynamic pattern path(s) added: " + string.Join( ", ", environment.EnvironmentDynamicPaths ) );

            try
            {
                // Instanciates the script object.
                CodeCakeHost._injectedActualHost = new BuildScriptHost( engine, context );
                CodeCakeHost c = (CodeCakeHost)Activator.CreateInstance( choosenBuild.Type );

                var target = context.Arguments.GetArgument( "target" ) ?? "Default";
                var exclusiveTarget = context.Arguments.HasArgument( "exclusive" );
                var strategy = new CodeCakeExecutionStrategy( logger, exclusiveTarget ? target : null );
                var report = engine.RunTargetAsync( context, strategy, target ).GetAwaiter().GetResult();
                if( report != null && !report.IsEmpty )
                {
                    var printerReport = new CakeReportPrinter( console );
                    printerReport.Write( report );
                }
            }
            catch( CakeTerminateException ex )
            {
                switch( ex.Option )
                {
                    case CakeTerminationOption.Error:
                        logger.Error( "Termination with Error: '{0}'.", ex.Message );
                        return new RunResult( -2, context.InteractiveMode() );
                    case CakeTerminationOption.Warning:
                        logger.Warning( "Termination with Warning: '{0}'.", ex.Message );
                        break;
                    default:
                        Debug.Assert( ex.Option == CakeTerminationOption.Success );
                        logger.Information( "Termination with Success: '{0}'.", ex.Message );
                        break;
                }
            }
            catch( TargetInvocationException ex )
            {
                logger.Error( "Error occurred: '{0}'.", ex.InnerException?.Message ?? ex.Message );
                return new RunResult( -3, context.InteractiveMode() );
            }
            catch( AggregateException ex )
            {
                logger.Error( "Error occurred: '{0}'.", ex.Message );
                foreach( var e in ex.InnerExceptions )
                {
                    logger.Error( "  -> '{0}'.", e.Message );
                }
                return new RunResult( -4, context.InteractiveMode() );
            }
            catch( Exception ex )
            {
                logger.Error( "Error occurred: '{0}'.", ex.Message );
                return new RunResult( -5, context.InteractiveMode() );
            }
            return new RunResult( 0, context.InteractiveMode() );
        }

        /// <summary>
        /// Gets a mutable dictionary of build objects.
        /// </summary>
        public IDictionary<string, CodeCakeBuildTypeDescriptor> AvailableBuilds => _builds; 

    }
}
