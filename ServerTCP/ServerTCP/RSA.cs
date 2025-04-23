using System;
using System.Numerics;

namespace ServerTCP
{
    public class RSA
    {
        public struct RsaKeyPair
        {
            public BigInteger PublicKey;
            public BigInteger PrivateKey;
            public BigInteger Modulus;
        }

        private static BigInteger GeneratePrime(int bitLength)
        {
            Random random = new Random();
            BigInteger prime;
            int attempts = 0;

            Console.WriteLine($"Inizio generazione numero primo di {bitLength} bit...");

            do
            {
                attempts++;
                Console.WriteLine($"Tentativo {attempts} per trovare un numero primo di {bitLength} bit.");
                prime = GenerateRandomBigInteger(bitLength, random);
                Console.WriteLine($"  Candidato primo generato: {prime}");
            } while (!IsProbablyPrime(prime));

            Console.WriteLine($"Numero primo di {bitLength} bit trovato dopo {attempts} tentativi: {prime}");
            return prime;
        }

        private static BigInteger GenerateRandomBigInteger(int bitLength, Random random)
        {
            byte[] bytes = new byte[(bitLength + 7) / 8];
            random.NextBytes(bytes);

            bytes[bytes.Length - 1] &= 0x7F;
            if (bytes.Length > 0)
            {
                bytes[0] |= 0x80;
            }

            return new BigInteger(bytes);
        }

        private static bool IsProbablyPrime(BigInteger n, int k = 10)
        {
            Console.WriteLine($"  Esecuzione test di primalità Miller-Rabin per: {n}");

            if (n < 2) return false;
            if (n == 2 || n == 3) return true;
            if (n % 2 == 0) return false;

            BigInteger d = n - 1;
            int s = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                s++;
            }

            Random random = new Random();
            for (int i = 0; i < k; i++)
            {
                BigInteger a = GenerateRandomBigIntegerInRange(2, n - 2, random);
                Console.WriteLine($"    Miller-Rabin: Test con base a = {a}, iterazione {i + 1}/{k}");
                BigInteger x = BigInteger.ModPow(a, d, n);

                if (x == 1 || x == n - 1)
                {
                    Console.WriteLine($"    Miller-Rabin: Test con base {a} superato (x = 1 o x = n-1).");
                    continue;
                }

                bool isComposite = true;
                for (int r = 0; r < s - 1; r++)
                {
                    x = BigInteger.ModPow(x, 2, n);
                    Console.WriteLine($"      Miller-Rabin: Test intermedio con x = {x}");
                    if (x == n - 1)
                    {
                        isComposite = false;
                        Console.WriteLine($"      Miller-Rabin: Test intermedio superato (x = n-1).");
                        break;
                    }
                }

                if (isComposite)
                {
                    Console.WriteLine($"  {n} non è probabilmente primo (fallito test con base {a}).");
                    return false;
                }
            }

            Console.WriteLine($"  {n} è probabilmente primo (superati tutti i test).");
            return true;
        }

        private static BigInteger GenerateRandomBigIntegerInRange(BigInteger min, BigInteger max, Random random)
        {
            if (min > max) throw new ArgumentException("min cannot be greater than max.");
            if (min == max) return min;

            BigInteger range = max - min;
            int byteLength = (int)BigInteger.Log(range, 2) / 8 + 1;
            if (byteLength < 1) byteLength = 1;

            byte[] bytes = new byte[byteLength];
            BigInteger randomValue;
            int attempts = 0;

            do
            {
                attempts++;
                random.NextBytes(bytes);
                randomValue = new BigInteger(bytes);

                if (randomValue < 0)
                {
                    randomValue *= -1;
                }

                if (range != 0)
                {
                    randomValue %= (range + 1);
                }

            } while (min + randomValue > max);

            BigInteger result = min + randomValue;
            return result;
        }

        private static BigInteger Gcd(BigInteger a, BigInteger b)
        {
            while (b != 0)
            {
                BigInteger temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        private static BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            BigInteger m0 = m, x0 = 0, x1 = 1;

            if (m == 1) return 0;

            while (a > 1)
            {
                BigInteger q = a / m;
                (a, m) = (m, a % m);
                (x0, x1) = (x1 - q * x0, x0);
            }

            if (x1 < 0) x1 += m0;

            return x1;
        }

        public static RsaKeyPair GenerateKeys(int bitLength = 512)
        {
            Console.WriteLine("Generating RSA keys...");
            BigInteger p = GeneratePrime(bitLength / 2);
            BigInteger q;
            do
            {
                q = GeneratePrime(bitLength / 2);
            } while (q == p);


            BigInteger n = p * q;
            BigInteger phi = (p - 1) * (q - 1);

            BigInteger e = 65537;
            if (Gcd(e, phi) != 1)
            {
                throw new Exception("e non è coprimo con phi. Generazione fallita.");
            }

            BigInteger d = ModInverse(e, phi);

            Console.WriteLine("RSA keys generated successfully.");

            return new RsaKeyPair
            {
                PublicKey = e,
                PrivateKey = d,
                Modulus = n
            };
        }

        public static BigInteger Encrypt(BigInteger message, BigInteger publicKey, BigInteger modulus)
        {
            return BigInteger.ModPow(message, publicKey, modulus);
        }

        public static BigInteger Decrypt(BigInteger cipherText, BigInteger privateKey, BigInteger modulus)
        {
            return BigInteger.ModPow(cipherText, privateKey, modulus);
        }
    }
}
