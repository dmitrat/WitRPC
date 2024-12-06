using System;
namespace OutWit.Communication.Utils
{
    public static class ValueUtils
    {
        public static bool IsEqual(this object? me, object? value)
        {
            if(me == null && value == null)
                return true;

            if (me == null && value != null)
                return false;

            if (me != null && value == null)
                return false;

            return me!.Equals(value);
        }
    }
}
