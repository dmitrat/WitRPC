namespace OutWit.Examples.Services.ClientBlazor.Utils
{
    public static class Extensions
    {
        public static string Base64UrlToBase64(this string me)
        {
            string padded = me.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return padded;
        }
    }
}
