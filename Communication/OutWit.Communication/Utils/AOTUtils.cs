using System;
using System.Diagnostics.CodeAnalysis;
using OutWit.Communication.Responses;

namespace OutWit.Communication.Utils
{
    public static class AOTUtils
    {
        public static void EnsureTypesRegistered()
        {
            if(IsInitialized)
                return;

            IsInitialized = true;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private static Type ResponseInitialization = typeof(WitComResponseInitialization);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private static Type ResponseAuthorization = typeof(WitComResponseAuthorization);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private static Type Response = typeof(WitComResponse);

        private static bool IsInitialized { get; set; }
    }
}
