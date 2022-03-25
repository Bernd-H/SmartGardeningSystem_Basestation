using System;
using System.Security.Cryptography.X509Certificates;

namespace RemoveSelfSignedCertificates {
    /// <summary>
    /// Program to remove self generated certificates from the X509Store.
    /// </summary>
    class Program {

        private static StoreName DefaultStoreName = StoreName.My;

        private static StoreLocation DefaultStoreLocation = StoreLocation.CurrentUser;

        static void Main(string[] args) {
            if (args.Length == 1) {
                if (args[0] == "-s") {
                    // show all certificates form the X509Store
                    X509Store store = new X509Store(DefaultStoreName, DefaultStoreLocation);

                    try {
                        store.Open(OpenFlags.ReadOnly);

                        foreach (var cert in store.Certificates) {
                            Console.WriteLine($"{cert.Thumbprint} | {cert.IssuerName.Name} | {cert.SubjectName.Name} | {cert.Subject} | {cert.NotAfter}");
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine("ERROR: " + ex.Message);
                    }
                    finally {
                        store.Close();
                    }
                }
                else if (args[0] == "-r") {
                    // remove all certificates from the X509Store
                    X509Store store = new X509Store(DefaultStoreName, DefaultStoreLocation);

                    try {
                        store.Open(OpenFlags.ReadWrite);

                        foreach (var cert in store.Certificates) {
                            store.Remove(cert);
                            Console.WriteLine($"Removed cert with thumbprint {cert.Thumbprint}.");
                        }

                        Console.WriteLine("Finished.");
                    }
                    catch (Exception ex) {
                        Console.WriteLine("ERROR: " + ex.Message);
                    }
                    finally {
                        store.Close();
                    }
                }
                else {
                    Console.WriteLine($"\"{args[0]}\" is not a valid command.");
                }
            }
            else {
                Console.WriteLine("Args:\n-> -s\t... shows self signed certificates\n" +
                    "-> -r\t... removes all self signed certificates");
            }
        }
    }
}
