// See https://aka.ms/new-console-template for more information
using OpenMeteo.Services;

namespace OpenMeteo
{
    public class Program
    {

        static void Main(string[] args)
        {

            using var service = new OpenMeteoService();

            try
            {
                service.Start();
                Console.WriteLine("Pritisnite Enter za zaustavljanje servera...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Došlo je do greške: {ex.Message}");
            }
        }
    }
}