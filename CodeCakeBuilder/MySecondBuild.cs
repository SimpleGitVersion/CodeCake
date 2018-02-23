using Cake.Common.Diagnostics;
using Cake.Core.Diagnostics;

namespace CodeCake
{
    public class MySecondBuild : CodeCakeHost
    {
        public MySecondBuild()
        {
            Cake.Information( "I'm here!" );
        }
    }
}
