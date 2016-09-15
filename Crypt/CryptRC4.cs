using System.Linq;

namespace Crypt
{
    public class CryptRC4
    {
        private byte[] S = new byte[256];
        private int x = 0;
        private int y = 0;

        public CryptRC4(byte[] key)
        {
            int keyLength = key.Length;

            for (int i = 0; i < 256; i++)
            {
                S[i] = (byte)i;
            }

            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + S[i] + key[i % keyLength]) % 256;
            }
        }

        private byte keyItem()
        {
            x = (x + 1) % 256;
            y = (y + S[x]) % 256;

            byte temp = S[x];
            S[x] = S[y];
            S[y] = temp;

            int index = (S[x] + S[y]) % 256;
            return S[index];
        }

        public byte[] Encode(byte[] dataB, int size)
        {
            byte[] data = dataB.Take(size).ToArray();
            byte[] cipher = new byte[data.Length];

            for(int m = 0; m < data.Length; m++)
            {
                cipher[m] = (byte)(data[m] ^ keyItem());
            }

            return cipher;
        }

        public byte[] Decode(byte[] dataB, int size)
        {
            return Encode(dataB, size);
        }
    }
}
