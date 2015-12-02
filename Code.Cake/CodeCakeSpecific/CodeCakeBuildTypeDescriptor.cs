using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// Describes a Build class.
    /// </summary>
    public class CodeCakeBuildTypeDescriptor
    {
        readonly Type _type;
        readonly HashSet<string> _additionalPatternPaths;

        /// <summary>
        /// initializes a new <see cref="CodeCakeBuildTypeDescriptor"/>.
        /// </summary>
        /// <param name="t">The type of the build object.</param>
        internal CodeCakeBuildTypeDescriptor( Type t )
        {
            _type = t;
            _additionalPatternPaths = new HashSet<string>( _type.GetCustomAttributes( false ).OfType<AddPathAttribute>().Select( a => a.Pattern ) );
        }

        /// <summary>
        /// Gets the type of the build object.
        /// </summary>
        public Type Type { get { return _type; } }

        /// <summary>
        /// Gets a set of pattern paths that should be available when executing script.
        /// </summary>
        public ISet<string> AdditionnalPatternPaths { get { return _additionalPatternPaths; } }
    }
}
