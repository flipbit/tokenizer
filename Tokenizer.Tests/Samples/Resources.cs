using System.IO;

namespace Tokens.Samples
{
    internal static class Resources
    {
        public static string Data_bbc_co_uk => ReadResource("Tokens.Samples.Data.bbc_co_uk.txt");
        public static string Data_com => ReadResource("Tokens.Samples.Data.com.txt");
        public static string Data_abogado => ReadResource("Tokens.Samples.Data.abogado.txt");
        public static string Data_facebook_com_redirect => ReadResource("Tokens.Samples.Data.facebook_com_redirect.txt");

        public static string Pattern_iana => ReadResource("Tokens.Samples.Patterns.Iana.txt");
        public static string Pattern_nominet => ReadResource("Tokens.Samples.Patterns.nominet.txt");
        public static string Pattern_verisign_grs => ReadResource("Tokens.Samples.Patterns.verisign-grs.com.txt");

        private static string ReadResource(string path)
        {
            var assembly = typeof(Resources).Assembly;

            using (var stream = assembly.GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
