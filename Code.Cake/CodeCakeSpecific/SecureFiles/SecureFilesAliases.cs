using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using CodeCake;

namespace Cake.Common
{
    /// <summary>
    /// Provides Rijndael encrypt/uncrypt helpers.
    /// </summary>
    public static class SecureFileAliases
    {

        /// <summary>
        /// Uncrypts a file into a <see cref="TemporaryFile"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">File to decrypt.</param>
        /// <param name="secret">Secret to use.</param>
        /// <returns>A temporary uncrypted file.</returns>
        [CakeAliasCategory( "SecureFile" )]
        [CakeMethodAlias]
        public static TemporaryFile SecureFileUncrypt( this ICakeContext context, FilePath file, string secret )
        {
            string extension = file.GetExtension();
            if( extension == "enc" ) extension = file.GetFilenameWithoutExtension().GetExtension();
            var f = new TemporaryFile( extension );
            RijndaelCrypt.Decrypt( file.FullPath, f.Path, secret );
            return f;
        }

        /// <summary>
        /// Encrypts an existing file into a new file.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="file">File to encrypt.</param>
        /// <param name="encryptedFile">Path of the file. The file must not exist.</param>
        /// <param name="secret">The secret to use.</param>
        [CakeAliasCategory( "SecureFile" )]
        [CakeMethodAlias]
        public static void SecureFileCrypt( this ICakeContext context, FilePath file, FilePath encryptedFile, string secret )
        {
            RijndaelCrypt.Encrypt( file.FullPath, encryptedFile.FullPath, secret );
        }
    }
}
