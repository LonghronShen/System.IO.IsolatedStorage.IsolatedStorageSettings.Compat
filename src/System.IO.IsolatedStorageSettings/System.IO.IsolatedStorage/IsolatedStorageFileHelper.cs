using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace System.IO.IsolatedStorage
{

    internal static class IsolatedStorageFileHelper
    {

#if NET20 || NET35
#if NET20
        public static bool FileExists(IsolatedStorageFile isf, string fileName)
#elif NET35
        public static bool FileExists(this IsolatedStorageFile isf, string fileName)
#endif
        {
            return isf.GetFileNames(fileName).Length > 0;
        }
#endif

#if NET20 || NET35
#if NET20
        public static Stream OpenFile(IsolatedStorageFile isf, string fileName, FileMode mode)
#elif NET35
        public static Stream OpenFile(this IsolatedStorageFile isf, string fileName, FileMode mode)
#endif
        {
            return new IsolatedStorageFileStream(fileName, mode, isf);
        }
#endif

#if NET20 || NET35
#if NET20
        public static Stream CreateFile(IsolatedStorageFile isf, string fileName)
#elif NET35
        public static Stream CreateFile(this IsolatedStorageFile isf, string fileName)
#endif
        {
            return new IsolatedStorageFileStream(fileName, FileMode.CreateNew, isf);
        }
#endif

    }

}
