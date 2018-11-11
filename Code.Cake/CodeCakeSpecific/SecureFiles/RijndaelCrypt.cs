using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    /// <summary>
    /// This is the same algorithm and configuration as https://github.com/appveyor/secure-file.
    /// </summary>
    public static class RijndaelCrypt
    {
        private static string Salt = "{E4E66F59-CAF2-4C39-A7F8-46097B1C461B}";


        /// <summary>
        /// Decrypts a list of key value pairs previously encrypted by <see cref="EncryptValuesToString"/>.
        /// </summary>
        /// <param name="crypted">The crypted string.</param>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <returns>The list of key value pairs.</returns>
        public static IList<KeyValuePair<string, string>> DecryptValues( string crypted, string passPhrase )
        {
            var result = new List<KeyValuePair<string, string>>();
            string[] lines = crypted.Split( new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
            foreach( var l in lines )
            {
                if( l.StartsWith( "--Version:" ) ) continue;
                if( l.StartsWith( " > " ) )
                {
                    byte[] bytes = Convert.FromBase64String( l.Substring( 3 ) );
                    using( var mem = new MemoryStream( bytes ) )
                    using( var algo = GetRijndael( passPhrase ) )
                    using( var read = new CryptoStream( mem, algo.CreateDecryptor(), CryptoStreamMode.Read ) )
                    using( var r = new BinaryReader( read ) )
                    {
                        for( int i = 0; i < result.Count; ++i )
                        {
                            var s = r.ReadBoolean() ? r.ReadString() : null;
                            result[i] = new KeyValuePair<string, string>( result[i].Key, s );
                        }
                    }
                    return result;
                }
                result.Add( new KeyValuePair<string, string>( l, null ) );
            }
            throw new InvalidDataException( "Unable to find crypted values section." );
        }

        /// <summary>
        /// Encrypts values in a text based format where the keys are easily readable.
        /// </summary>
        /// <param name="keys">
        /// The key value pairs for which value must be encrypted.
        /// No key must not be null, empty or contain \\r or \\n or space characters.</param>
        /// <param name="passPhrase">Must not be null, empty or white space.</param>
        /// <returns>The encrypted string.</returns>
        public static string EncryptValuesToString( IEnumerable<KeyValuePair<string, string>> keys, string passPhrase )
        {
            if( keys == null ) throw new ArgumentNullException( nameof( keys ) );
            if( String.IsNullOrWhiteSpace( passPhrase ) ) throw new ArgumentNullException( nameof( passPhrase ) );
            using( var mem = new MemoryStream() )
            using( var algo = GetRijndael( passPhrase ) )
            using( var output = new CryptoStream( mem, algo.CreateEncryptor(), CryptoStreamMode.Write ) )
            using( var w = new BinaryWriter( output ) )
            {
                var b = new StringBuilder();
                b.AppendLine( "--Version: 1" );
                foreach( var kv in keys )
                {
                    if( String.IsNullOrEmpty( kv.Key )
                        || kv.Key.IndexOf( '\n' ) >= 0
                        || kv.Key.IndexOf( '\r' ) >= 0
                        || kv.Key.IndexOf( ' ' ) >= 0 )
                    {
                        throw new ArgumentException( $"Key must not be null, empty or contain \\r or \\n characters: {kv.Key}", nameof( keys ) );
                    }
                    b.AppendLine( kv.Key.Trim() );
                    if( kv.Value == null ) w.Write( false );
                    else
                    {
                        w.Write( true );
                        w.Write( kv.Value );
                    }
                }
                w.Flush();
                output.FlushFinalBlock();
                b.Append( " > " ).AppendLine( Convert.ToBase64String( mem.ToArray() ) );
                return b.ToString();
            }
        }

        /// <summary>
        /// Encrypts a file into another file (that must not exist) with a given secret.
        /// </summary>
        /// <param name="fileName">File to encrypt.</param>
        /// <param name="outFileName">Target file that will be encrypted. Must not exist.</param>
        /// <param name="secret">Secret to use.</param>
        public static void Encrypt( string fileName, string outFileName, string secret )
        {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( outFileName == null ) throw new ArgumentNullException( "outFileName" );
            if( secret == null ) throw new ArgumentNullException( "secret" );

            var alg = GetRijndael( secret );
            using( var inStream = File.OpenRead( fileName ) )
            {
                using( var outStream = File.Create( outFileName ) )
                {
                    using( var cryptoStream = new CryptoStream( outStream, alg.CreateEncryptor(), CryptoStreamMode.Write ) )
                    {
                        inStream.CopyTo( cryptoStream );
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts a file into another file (that must not exist) with a given secret.
        /// </summary>
        /// <param name="fileName">File to decrypt.</param>
        /// <param name="outFileName">Target file that will be decrypted. Must not exist.</param>
        /// <param name="secret">Secret to use.</param>
        public static void Decrypt( string fileName, string outFileName, string secret )
        {
            var alg = GetRijndael( secret );

            using( var inStream = File.OpenRead( fileName ) )
            {
                using( var outStream = File.Create( outFileName ) )
                {
                    using( var cryptoStream = new CryptoStream( outStream, alg.CreateDecryptor(), CryptoStreamMode.Write ) )
                    {
                        inStream.CopyTo( cryptoStream );
                    }
                }
            }
        }

        static Rijndael GetRijndael( string secret )
        {
            using( var pbkdf2 = new Rfc2898DeriveBytes( secret, Encoding.UTF8.GetBytes( Salt ), 10000 ) )
            {
                Rijndael alg = Rijndael.Create();
                alg.Key = pbkdf2.GetBytes( 32 );
                alg.IV = pbkdf2.GetBytes( 16 );
                return alg;
            }
        }
    }
}
