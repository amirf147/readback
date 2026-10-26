// Copyright 2026 Amir Farhadi
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace ReadBack.Core.TTS;

/// <summary>
/// Implements Microsoft Edge TTS Sec-MS-GEC v2 authentication token generation.
/// Ported from the Edge DRM v2 algorithm (FIPS 180-4 SHA256 customization + CS64 digest).
/// </summary>
public static class EdgeTTSDrm
{
    public const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    public const string SecMsGecVersion = "2-143.0.3650.75";

    private const long WinEpoch = 11644473600L;

    private static readonly uint[] Sha256Initial =
    [
        0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A,
        0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19
    ];

    private static readonly uint[] Sha256State =
    [
        0xFD9FBEA2, 0x91243CE7, 0xBE499094, 0xCD2E1E0A,
        0x6C006F49, 0x04BD35AB, 0xFEB0A7E4, 0x03EFFBD2
    ];

    private static readonly uint[] Sha256Schedule =
    [
        0xEB24BF9E, 0x1558691F, 0xA0C09EE2, 0x0CFA32BA,
        0xA622EE8C, 0xD1DD9491, 0xC0DD25A0, 0xF9E70AB6,
        0xF0C31573, 0x7058CE4E, 0x7188E606, 0x00E5AB33,
        0xD258B060, 0x72766564
    ];

    private static readonly uint[] Sha256RoundConstants =
    [
        0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
        0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
        0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
        0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
        0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
        0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
        0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
        0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2
    ];

    private const uint Sha256Adjustment = 0xC9DC06A7;
    private const uint Sha256Sigma0Adjustment = 0x059C0B80;
    private const uint Sha256Sigma1Adjustment = 0x000B92C0;

    private static readonly (uint Coeff, uint Offset)[] Cs64Coefficients =
    [
        (0xE82E91B1, 0xB32700D0),
        (0x44DE24C6, 0x88DFF51D),
        (0x3BEB65BC, 0x29514038),
        (0xC89C5933, 0xB328CBDF),
        (0x8D1EC211, 0xD405B4D5),
        (0x314EF5D5, 0x26E55F3C),
        (0xE92491B1, 0x607801C6),
        (0xF0A5B9D5, 0x82B2C1FB),
        (0x06064CF3, 0xFA51A3DA),
        (0xDA79FA60, 0x207CC464),
        (0xBED20560, 0x3C3C74F3),
        (0x89F9C9F8, 0xD2D14B61),
        (0x35FA38C6, 0xBB04A2C1),
        (0xCB49E3C6, 0xC5626167)
    ];

    private const uint Cs64Factor = 0x82893933;
    private const uint Cs64Adjustment0 = 0xF2F90B1A;
    private const uint Cs64Adjustment1 = 0x0B1A0000;

    private static uint Rol16(uint value) => (value << 16) | (value >> 16);

    private static uint Sigma0(uint value) =>
        (value >> 7) ^ (value << 25) ^ (value >> 18) ^ (value << 14) ^ (value >> 3);

    private static uint Sigma1(uint value) =>
        (value >> 17) ^ (value << 15) ^ (value >> 19) ^ (value << 13) ^ (value >> 10);

    private static List<uint> WordsLe4Even(byte[] data)
    {
        int rem = (-data.Length) % 4;
        if (rem < 0) rem += 4;
        byte[] padded = new byte[data.Length + rem];
        Buffer.BlockCopy(data, 0, padded, 0, data.Length);

        if (padded.Length == 0) return [];

        var list = new List<uint>(padded.Length / 4 + 1);
        for (int i = 0; i < padded.Length; i += 4)
        {
            list.Add(BinaryPrimitives.ReadUInt32LittleEndian(padded.AsSpan(i, 4)));
        }
        if (list.Count % 2 != 0)
        {
            list.Add(0);
        }
        return list;
    }

    private static void ExpandSha256Schedule(uint[] words, int firstWord = 16)
    {
        for (int i = firstWord; i < 64; i++)
        {
            uint x = words[i - 2];
            uint y = words[i - 15];
            uint s1 = (x >> 17) ^ (x << 15) ^ (x >> 19) ^ (x << 13) ^ (x >> 10);
            uint s0 = (y >> 7) ^ (y << 25) ^ (y >> 18) ^ (y << 14) ^ (y >> 3);
            words[i] = s1 + words[i - 7] + s0 + words[i - 16];
        }
    }

    private static void Sha256Rounds(uint[] state, uint[] words, int firstRound = 0)
    {
        uint a = state[0], b = state[1], c = state[2], d = state[3];
        uint e = state[4], f = state[5], g = state[6], h = state[7];

        for (int i = firstRound; i < 64; i++)
        {
            uint k = (firstRound == 14 && i == 14) ? Sha256Adjustment : Sha256RoundConstants[i];
            uint s1 = BitOperations.RotateRight(e, 6) ^ BitOperations.RotateRight(e, 11) ^ BitOperations.RotateRight(e, 25);
            uint ch = g ^ (e & (f ^ g));
            uint t1 = h + s1 + ch + k + words[i];
            uint s0 = BitOperations.RotateRight(a, 2) ^ BitOperations.RotateRight(a, 13) ^ BitOperations.RotateRight(a, 22);
            uint maj = (a & b) | (c & (a | b));
            uint t2 = s0 + maj;

            h = g;
            g = f;
            f = e;
            e = d + t1;
            d = c;
            c = b;
            b = a;
            a = t1 + t2;
        }

        state[0] = a; state[1] = b; state[2] = c; state[3] = d;
        state[4] = e; state[5] = f; state[6] = g; state[7] = h;
    }

    private static uint[] Sha256Init(byte[] data)
    {
        uint[] words = new uint[64];
        // words 0..13 are 0
        uint w14 = 0;
        for (int i = 0; i < 3 && i < data.Length; i++)
        {
            w14 = (w14 << 8) | data[i];
        }
        words[14] = w14;

        uint w15 = 0;
        for (int i = 3; i < 7 && i < data.Length; i++)
        {
            w15 = (w15 << 8) | data[i];
        }
        words[15] = w15;

        for (int index = 16; index < 30; index++)
        {
            uint value = Sha256Schedule[index - 16];
            if (index - 2 >= 14)
            {
                value += (index == 16)
                    ? (Sigma1(words[index - 2]) ^ Sha256Sigma1Adjustment)
                    : Sigma1(words[index - 2]);
            }
            if (index - 7 >= 14)
            {
                value += words[index - 7];
            }
            if (index - 15 >= 14)
            {
                value += (index == 29)
                    ? (Sigma0(words[index - 15]) ^ Sha256Sigma0Adjustment)
                    : Sigma0(words[index - 15]);
            }
            words[index] = value;
        }

        words[30] = Sigma1(words[28])
            + words[23]
            + Sigma0(words[15])
            + words[14]
            + Sha256Adjustment
            - Sha256RoundConstants[14];

        ExpandSha256Schedule(words, 31);

        uint[] working = (uint[])Sha256State.Clone();
        Sha256Rounds(working, words, 14);

        uint[] result = new uint[8];
        for (int i = 0; i < 8; i++)
        {
            result[i] = Sha256Initial[i] + working[i];
        }
        return result;
    }

    private static byte[] Sha256Digest(byte[] data)
    {
        if (data.Length < 7)
            throw new ArgumentException("Token salt must contain at least 7 bytes");

        int length = 57 + data.Length;
        int padZeros = (55 - length) % 64;
        if (padZeros < 0) padZeros += 64;

        byte[] padded = new byte[(data.Length - 7) + 1 + padZeros + 8];
        Buffer.BlockCopy(data, 7, padded, 0, data.Length - 7);
        padded[data.Length - 7] = 0x80;
        // remaining zeros are default
        BinaryPrimitives.WriteUInt64BigEndian(padded.AsSpan(padded.Length - 8, 8), (ulong)length * 8);

        uint[] state = Sha256Init(data);
        uint[] words = new uint[64];

        for (int offset = 0; offset < padded.Length; offset += 64)
        {
            for (int i = 0; i < 16; i++)
            {
                words[i] = BinaryPrimitives.ReadUInt32BigEndian(padded.AsSpan(offset + i * 4, 4));
            }
            ExpandSha256Schedule(words, 16);

            uint[] working = (uint[])state.Clone();
            Sha256Rounds(working, words, 0);

            for (int i = 0; i < 8; i++)
            {
                state[i] += working[i];
            }
        }

        byte[] digest = new byte[32];
        for (int i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(digest.AsSpan(i * 4, 4), state[i]);
        }
        return digest;
    }

    private static (uint, uint) Cs64Digest(byte[] data, byte[] digest)
    {
        uint keyA = BinaryPrimitives.ReadUInt32LittleEndian(digest.AsSpan(0, 4)) | 1;
        uint keyB = BinaryPrimitives.ReadUInt32LittleEndian(digest.AsSpan(4, 4)) | 1;

        uint factor0 = keyA * Cs64Factor;
        uint factor1 = keyB * Cs64Factor;

        var terms = new List<uint>(Cs64Coefficients.Length + data.Length);
        var offsets = new List<uint>(Cs64Coefficients.Length + data.Length);

        for (int i = 0; i < Cs64Coefficients.Length; i++)
        {
            terms.Add(Cs64Coefficients[i].Coeff * (i % 2 == 0 ? factor0 : factor1));
            offsets.Add(Cs64Coefficients[i].Offset);
        }

        byte[] prefixed = new byte[data.Length + 1];
        Buffer.BlockCopy(data, 0, prefixed, 1, data.Length);
        var words = WordsLe4Even(prefixed);

        for (int i = 0; i < words.Count; i++)
        {
            uint word = words[i];
            uint term = word * (i % 2 == 0 ? keyA : keyB);
            uint offset = Rol16(word) * (i % 2 == 0 ? 0xEF0569FBu : 0xC31713DBu);
            if (i == 0)
            {
                term += Cs64Adjustment0 * factor0;
                offset += Cs64Adjustment1;
            }
            terms.Add(term);
            offsets.Add(offset);
        }

        uint swapAcc = 0, swapLast = 0, state = 0, acc = 0;
        for (int i = 0; i < terms.Count; i += 2)
        {
            uint x = terms[i], y = terms[i + 1];
            uint t = Rol16(offsets[i] + x);
            uint q = t * 0x76F50F34;
            swapAcc += Rol16(q) * 0xC3EFEA97 + q;

            t = Rol16(offsets[i + 1] + y);
            q = t * 0x1881CE3C;
            swapLast = Rol16(q) * 0x35BD1EC9 + q;
            swapAcc += swapLast;

            state = state * keyA + x;
            state = Rol16(state) * 0xCF98B111;
            state = Rol16(state) * 0x87085B9F;
            state = Rol16(state) * 0x12CEB96D;
            state = Rol16(state) * 0x257E1D83;
            acc += state;

            t = state * keyB + y;
            t = Rol16(t) * 0xA27416F5;
            t = Rol16(t) * 0xD38396FF;
            state = Rol16(Rol16(t) * 0x7C932B89) * 0xBFA49F69;
            acc += state;
        }

        return (swapLast ^ state, swapAcc ^ acc);
    }

    public static string GenerateSecMsGec(double? unixTimestamp = null)
    {
        double unixSec = unixTimestamp ?? (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
        double ticks = unixSec + WinEpoch;
        ticks -= ticks % 300.0;
        ticks *= 10000000.0; // 100-nanosecond intervals

        string strToHash = $"{ticks:0}{TrustedClientToken}";
        byte[] bytesToHash = Encoding.ASCII.GetBytes(strToHash);

        byte[] digest = Sha256Digest(bytesToHash);
        var (cs0, cs1) = Cs64Digest(bytesToHash, digest);

        byte[] csBytes = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(csBytes.AsSpan(0, 4), cs0);
        BinaryPrimitives.WriteUInt32LittleEndian(csBytes.AsSpan(4, 4), cs1);

        string cs64Hex = Convert.ToHexString(csBytes);
        string shaHex = Convert.ToHexString(digest);

        return $"{cs64Hex}-{shaHex}";
    }
}
