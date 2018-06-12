using Cake.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeCake
{
    internal sealed class CodeCakeDataService : ICakeDataService
    {
        readonly Dictionary<Type, object> _data;

        public CodeCakeDataService()
        {
            _data = new Dictionary<Type, object>();
        }

        public TData Get<TData>()
            where TData : class
        {
            if( _data.TryGetValue( typeof( TData ), out var data ) )
            {
                if( data is TData typedData )
                {
                    return typedData;
                }
                var message = $"Context data exists but is of the wrong type ({data.GetType().FullName}).";
                throw new InvalidOperationException( message );
            }
            throw new InvalidOperationException( "The context data has not been setup." );
        }

        public void Add<TData>( TData value )
            where TData : class
        {
            if( _data.ContainsKey( typeof( TData ) ) )
            {
                var message = $"Context data of type '{typeof( TData ).FullName}' has already been registered.";
                throw new InvalidOperationException( message );
            }
            _data.Add( typeof( TData ), value );
        }
    }
}
