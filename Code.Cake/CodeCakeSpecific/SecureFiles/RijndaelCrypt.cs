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
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes( secret, Encoding.UTF8.GetBytes( Salt ), 10000 );

            Rijndael alg = Rijndael.Create();
            alg.Key = pbkdf2.GetBytes( 32 );
            alg.IV = pbkdf2.GetBytes( 16 );

            return alg;
        }
    }
}
