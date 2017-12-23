using System;
using System.Security.Cryptography;

namespace RsaKeyGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048);

            string pathPublic;
            string pathPublicPrivate;
            if (args.Length > 1)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains("private"))
                    {
                        pathPublicPrivate = args[i + 1];
                    }
                    else if (args[i].Contains("public"))
                    {
                        pathPublic = args[i + 1];
                    }
                }
            }
            else
            {
                Console.WriteLine("Input path (w/filename) to save the public key to (or leave empty to print) >>>");
                PrintOrWrite(Console.ReadLine(), RSA.ToJsonString(false));

                Console.WriteLine("Input path (w/filename) to save the public & private keys to (or leave empty to print) >>>");
                PrintOrWrite(Console.ReadLine(), RSA.ToJsonString(true));
            }

            
        }

        private static void PrintOrWrite(string path, string key)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine($"\n{key}\n");
            }
            else
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.IO.File.Create(path)))
                {
                    file.WriteLine(key);
                }
                Console.WriteLine($"Done, wrote key to {path}.");
            }
        }
    }
}
