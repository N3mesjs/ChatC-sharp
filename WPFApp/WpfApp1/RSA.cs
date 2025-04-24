using System;
using System.Numerics;

namespace WpfApp1
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

            do
            {
                prime = GenerateRandomBigInteger(bitLength, random);
            } while (!IsProbablyPrime(prime));

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
                BigInteger x = BigInteger.ModPow(a, d, n);

                if (x == 1 || x == n - 1)
                {
                    continue;
                }

                bool isComposite = true;
                for (int r = 0; r < s - 1; r++)
                {
                    x = BigInteger.ModPow(x, 2, n);
                    if (x == n - 1)
                    {
                        isComposite = false;
                        break;
                    }
                }

                if (isComposite)
                {
                    return false;
                }
            }

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

            do
            {
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

            return min + randomValue;
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

        public static byte[] EncryptWithRSA(byte[] data, BigInteger publicKey, BigInteger modulus)
        {
            // Check data size
            int maxBytes = (int)(modulus.ToByteArray().Length - 11); // Allow space for PKCS#1 padding
            if (data.Length > maxBytes)
            {
                throw new ArgumentException($"Data too large for RSA encryption with this key. Max {maxBytes} bytes.");
            }

            // Add simple padding to ensure message integrity
            byte[] paddedData = new byte[data.Length + 1];
            Array.Copy(data, 0, paddedData, 0, data.Length);
            paddedData[data.Length] = 0; // Add zero byte to ensure positive BigInteger

            BigInteger dataBigInt = new BigInteger(paddedData);
            BigInteger encryptedBigInt = Encrypt(dataBigInt, publicKey, modulus);

            return encryptedBigInt.ToByteArray();
        }

        public static byte[] DecryptWithRSA(byte[] data, BigInteger privateKey, BigInteger modulus)
        {
            BigInteger dataBigInt = new BigInteger(data);
            BigInteger decryptedBigInt = Decrypt(dataBigInt, privateKey, modulus);

            byte[] result = decryptedBigInt.ToByteArray();

            // Remove padding (last byte should be zero)
            int paddingIndex = result.Length - 1;
            if (paddingIndex >= 0 && result[paddingIndex] == 0)
            {
                Array.Resize(ref result, paddingIndex);
            }

            return result;
        }
    }
}
