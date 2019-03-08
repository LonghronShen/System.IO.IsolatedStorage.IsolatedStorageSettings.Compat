using System;
using System.Collections.Generic;
using System.Text;

namespace System
{

    internal static class SystemUtils
    {

        public static T Try<T>(Func<T> action, T @defaultValue)
        {
            try
            {
                return action();
            }
            catch
            {
                return @defaultValue;
            }
        }

    }

}