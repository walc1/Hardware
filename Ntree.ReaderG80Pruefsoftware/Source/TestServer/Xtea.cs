using System;

namespace TestServer
{
    public class Xtea
    {
        private byte[] _key;

        public Xtea(byte[] key)
        {
            _key = key;
        }

        public byte[] Decrypt(byte[] encryptedData)
        {
            var dataSize = encryptedData.Length;
            var plainData = new byte[dataSize];
            if (dataSize % 8 == 0 && dataSize != 0)
            {
                dataSize /= 8;
                var v = new uint[2];
                var lkey = new uint[4];
                var eoff = 0;
                var poff = 0;

                Buffer.BlockCopy(_key, 0, lkey, 0, 16);
                const uint delta = 0x9E3779B9;

                while (dataSize-- != 0)
                {
                    Buffer.BlockCopy(encryptedData, eoff, v, 0, 8);
                    uint sum = 0xC6EF3720;
                    uint y = v[0];
                    uint z = v[1];

                    uint i;
                    for (i = 0; i < 32; i++)
                    {
                        z -= (y << 4 ^ y >> 5) + y ^ sum + lkey[sum >> 11 & 3];
                        sum -= delta;
                        y -= (z << 4 ^ z >> 5) + z ^ sum + lkey[sum & 3];
                    }
                    v[0] = y;
                    v[1] = z;
                    Buffer.BlockCopy(v, 0, plainData, poff, 8);
                    poff += 8;
                    eoff += 8;
                }
                return plainData;
            }
            return null;
        }

        public byte[] Encrypt(byte[] data)
        {
            var dataSize = data.Length;
            var plainData = new byte[dataSize];
            if (dataSize % 8 == 0 && dataSize != 0)
            {
                dataSize /= 8;
                var v = new uint[2];
                var lkey = new uint[4];
                var eoff = 0;
                var poff = 0;

                Buffer.BlockCopy(_key, 0, lkey, 0, 16);
                const uint delta = 0x9E3779B9;

                while (dataSize-- != 0)
                {
                    Buffer.BlockCopy(data, eoff, v, 0, 8);
                    uint y = v[0];
                    uint z = v[1];
                    uint sum = 0;
                    uint i;
                    for (i = 0; i < 32; i++)
                    {
                        y += (z << 4 ^ z >> 5) + z ^ sum + lkey[sum & 3];
                        sum += delta;
                        z += (y << 4 ^ y >> 5) + y ^ sum + lkey[sum >> 11 & 3];

                    }
                    v[0] = y;
                    v[1] = z;
                    Buffer.BlockCopy(v, 0, plainData, poff, 8);
                    poff += 8;
                    eoff += 8;
                }
                return plainData;
            }
            return null;
        }
    }
}
