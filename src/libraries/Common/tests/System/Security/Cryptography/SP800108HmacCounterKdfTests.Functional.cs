// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class SP800108HmacCounterKdfTests
    {
        [Theory]
        [InlineData("V47WmHzPSkdC2vkLAomIjCzZlDOAetll3yJLcSvon7LJFjJpEN+KnSNp+gIpeydKMsENkflbrIZ/3s6GkEaH")]
        [InlineData("mVaFM4deXLl610CmnCteNzxgbM/VkmKznAlPauHcDBn0le06uOjAKLHx0LfoU2/Ttq9nd78Y6Nk6wArmdwJgJg==")]
        [InlineData("GaHPeqdUxriFpjRtkYQYWr5/iqneD/+hPhVJQt4rXblxSpB1UUqGqL00DMU/FJkX0iMCfqUjQXtXyfks+p++Ev4=")]
        public static void AspNetCoreTestVectors_Basic(string expectedBase64)
        {
            // These tests are from the dotnet/aspnetcore repo.
            byte[] expected = Convert.FromBase64String(expectedBase64);
            VerifyKbkdf(expected, s_kdk, HashAlgorithmName.SHA512, Label.ToCharArray(), Context.ToCharArray());
        }

        [Theory]
        [InlineData("rt2hM6kkQ8hAXmkHx0TU4o3Q+S7fie6b3S1LAq107k++P9v8uSYA2G+WX3pJf9ZkpYrTKD7WUIoLkgA1R9lk")]
        [InlineData("RKiXmHSrWq5gkiRSyNZWNJrMR0jDyYHJMt9odOayRAE5wLSX2caINpQmfzTH7voJQi3tbn5MmD//dcspghfBiw==")]
        [InlineData("KedXO0zAIZ3AfnPqY1NnXxpC3HDHIxefG4bwD3g6nWYEc5+q7pjbam71Yqj0zgHMNC9Z7BX3wS1/tajFocRWZUk=")]
        public static void AspNetCoreTestVectors_LargeKdk(string expectedBase64)
        {
            // These tests are from the dotnet/aspnetcore repo.
            // Win32 BCryptKeyDerivation doesn't perform RFC 2104, section 2 key adjustment for the KDK.
            // We do this for CNG so that there is no functional limit on the KDK size.
            byte[] kdk = new byte[50000];

            for (int i = 0; i < kdk.Length; i++)
            {
                kdk[i] = (byte)i;
            }

            byte[] expected = Convert.FromBase64String(expectedBase64);
            VerifyKbkdf(expected, kdk, HashAlgorithmName.SHA512, Label.ToCharArray(), Context.ToCharArray());
        }

        [Theory]
        [MemberData(nameof(GetRfc8009TestVectors))]
        public static void Rfc8009Tests(byte[] kdk, byte[] expected, HashAlgorithmName hashAlgorithm)
        {
            VerifyKbkdf(expected, kdk, hashAlgorithm, "prf".ToCharArray(), "test".ToCharArray());
        }

        [Theory]
        [InlineData(new byte[] { 0xcf, 0x4b, 0xfe, 0x4f, 0x85, 0xa1, 0x0b, 0xad }, nameof(HashAlgorithmName.SHA1))]
        [InlineData(new byte[] { 0x00, 0x26, 0x4b, 0xbb, 0x14, 0x97, 0x40, 0x54 }, nameof(HashAlgorithmName.SHA256))]
        [InlineData(new byte[] { 0xc7, 0x10, 0x27, 0x87, 0xd8, 0x96, 0xbc, 0x89 }, nameof(HashAlgorithmName.SHA384))]
        [InlineData(new byte[] { 0xdb, 0x3a, 0x18, 0xd9, 0x6c, 0x4a, 0xd4, 0x1e }, nameof(HashAlgorithmName.SHA512))]
        public static void SymCryptTestVectors(byte[] expected, string hashAlgorithm)
        {
            // These test vectors come from https://github.com/microsoft/SymCrypt.
            // See sp800_108_hmacsha1.c, sp800_108_hmacsha256.c, and sp800_108_hmacsha512.c.
            byte[] symCryptKey = new byte[]
            {
                0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            };

            byte[] key = symCryptKey.AsSpan(0, 8).ToArray();
            byte[] label = "Label"u8.ToArray();
            byte[] context = symCryptKey.AsSpan(16, 16).ToArray();

            HashAlgorithmName hashAlgorithmName = new HashAlgorithmName(hashAlgorithm);
            VerifyKbkdfBytes(expected, key, hashAlgorithmName, label, context);
        }

        [Theory]
        [InlineData(new byte[] { }, "label!", "context?", new byte[] { 0xb6, 0xff, 0x26, 0x61, 0xea, 0x43, 0x76, 0xd2 })]
        [InlineData(new byte[] { 0xFE }, "", "context?", new byte[] { 0xed, 0xdf, 0x50, 0x06, 0x3c, 0x26, 0x3e, 0xd9 })]
        [InlineData(new byte[] { 0xFE }, "label!", "", new byte[] { 0x98, 0x83, 0x67, 0x41, 0x3f, 0x2d, 0x90, 0x72 })]
        [InlineData(new byte[] { }, "", "", new byte[] { 0x18, 0x0f, 0xf7, 0xa2, 0xbc, 0x8d, 0x6e, 0x98 })]
        [InlineData(new byte[] { }, "", "", new byte[] { })]
        public static void EmptyTests(byte[] key, string label, string context, byte[] expected)
        {
            VerifyKbkdf(expected, key, HashAlgorithmName.SHA256, label.ToCharArray(), context.ToCharArray());
        }

        [Theory]
        [InlineData(nameof(HashAlgorithmName.SHA1), 512 / 8 - 1, new byte[] { 0xc9, 0x0f, 0x9d, 0x91, 0x85, 0xe5, 0xeb, 0x9b })]
        [InlineData(nameof(HashAlgorithmName.SHA1), 512 / 8, new byte[] { 0x7b, 0xdb, 0x38, 0x28, 0xc0, 0x9f, 0x49, 0x05 })]
        [InlineData(nameof(HashAlgorithmName.SHA1), 512 / 8 + 1, new byte[] { 0x6c, 0x3a, 0xba, 0x28, 0x38, 0xad, 0x51, 0x2c })]
        [InlineData(nameof(HashAlgorithmName.SHA256), 512 / 8 - 1, new byte[] { 0x88, 0xaa, 0xc7, 0xee, 0x05, 0x65, 0xfd, 0xda })]
        [InlineData(nameof(HashAlgorithmName.SHA256), 512 / 8, new byte[] { 0x3d, 0xdc, 0x7d, 0xec, 0x0a, 0xfd, 0x7a, 0xc0 })]
        [InlineData(nameof(HashAlgorithmName.SHA256), 512 / 8 + 1, new byte[] { 0x47, 0x95, 0x00, 0xd5, 0x55, 0x1f, 0xb3, 0x85 })]
        [InlineData(nameof(HashAlgorithmName.SHA384), 1024 / 8 - 1, new byte[] { 0x84, 0xd8, 0xfd, 0x33, 0x4f, 0x07, 0x81, 0x9b })]
        [InlineData(nameof(HashAlgorithmName.SHA384), 1024 / 8, new byte[] { 0x6c, 0xa2, 0x5d, 0x4f, 0x61, 0x2d, 0x0f, 0x20 })]
        [InlineData(nameof(HashAlgorithmName.SHA384), 1024 / 8 + 1, new byte[] { 0xe4, 0x0e, 0xbd, 0x41, 0x14, 0xe6, 0x80, 0x59 })]
        [InlineData(nameof(HashAlgorithmName.SHA512), 1024 / 8 - 1, new byte[] { 0xa4, 0xe5, 0x24, 0xe8, 0x56, 0x2b, 0x48, 0xa4 })]
        [InlineData(nameof(HashAlgorithmName.SHA512), 1024 / 8, new byte[] { 0xba, 0xf6, 0xed, 0xa7, 0x3a, 0xf7, 0x12, 0x27 })]
        [InlineData(nameof(HashAlgorithmName.SHA512), 1024 / 8 + 1, new byte[] { 0x34, 0xdf, 0x2d, 0x21, 0xfd, 0xf1, 0x0e, 0x13 })]
        public static void Kdk_HmacBlockBoundarySizes(string hashAlgorithmName, int kdkSize, byte[] expected)
        {
            // We do HMAC key adjust for the CNG implementation when the kdk exceeds the block size of the HMAC algorithm.
            // This tests one byte below, at, and above the block size for each HMAC algorithm.
            // Verified against OpenSSL 3. Example command used below. Adjust the digest and the seq upper boundary as needed.
            // Note that OpenSSL calls the label "salt" and the context "info".
            //
            // openssl kdf -keylen 8 -kdfopt mac:HMAC -kdfopt digest:SHA1 \
            //     -kdfopt hexkey:$(seq  1 63 | awk '{ printf "%02x", $1 }') -kdfopt salt:icecream \
            //     -kdfopt info:sandwiches -binary KBKDF | xxd -i

            byte[] kdk = new byte[kdkSize];

            for (int i = 0; i < kdkSize; i++)
            {
                kdk[i] = (byte)checked(i + 1);
            }

            HashAlgorithmName alg = new HashAlgorithmName(hashAlgorithmName);
            VerifyKbkdf(expected, kdk, alg, "icecream".ToCharArray(), "sandwiches".ToCharArray());
        }

        [Theory]
        [MemberData(nameof(GetOutputLengthBoundaries))]
        public static void OutputLength_AroundPrfOutputBoundaries(string hashAlgorithmName, byte[] expected)
        {
            byte[] kdk = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            HashAlgorithmName alg = new HashAlgorithmName(hashAlgorithmName);
            VerifyKbkdf(expected, kdk, alg, "mustard".ToCharArray(), "ketchup".ToCharArray());
        }

        [Fact]
        public static void MultipleDisposes_NoThrow()
        {
            SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(s_kdk, HashAlgorithmName.SHA256);
            kdf.Dispose();

            Assert.Throws<ObjectDisposedException>(() => kdf.DeriveKey(s_labelBytes, s_contextBytes, 42));
            kdf.Dispose();
        }

        public static IEnumerable<object[]> GetOutputLengthBoundaries()
        {
            // All outputs assume a kdk of 1, 2, 3, 4, 5, 6, 7, 8 with a label of "mustard" and context of "ketchup".
            // Outputs were generated from the "openssl kdf" command.

            // HMACSHA1 output size is 20 bytes
            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA1),
                new byte[39]
                {
                    0x05, 0xed, 0x32, 0x78, 0xfe, 0x72, 0x9e, 0x7a, 0x1b, 0x49, 0x2c, 0x1b, 0x40, 0x3b, 0x31, 0xd8,
                    0xef, 0x74, 0xfc, 0xe6, 0x1f, 0x75, 0x4b, 0xa5, 0x47, 0x8e, 0xb9, 0x1d, 0x9e, 0x0c, 0x4f, 0x03,
                    0xf7, 0x92, 0x68, 0x8a, 0x94, 0x0c, 0xea,
                },
            };

            yield return new object[]
            {
                nameof (HashAlgorithmName.SHA1),
                new byte[40]
                {
                    0xa5, 0x4a, 0x74, 0xe5, 0x01, 0x0c, 0x69, 0x0e, 0xc8, 0x9e, 0x24, 0x95, 0x94, 0x6a, 0x57, 0x06,
                    0xe8, 0x88, 0x63, 0x3b, 0xae, 0x1d, 0x13, 0xd8, 0x9d, 0x80, 0x4c, 0xcd, 0x60, 0x2b, 0x15, 0x3a,
                    0xb9, 0xd3, 0x77, 0xc7, 0xc4, 0xb7, 0x2a, 0xe6,
                }
            };

            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA1),
                new byte[41]
                {
                    0x44, 0xdc, 0x8a, 0x94, 0x18, 0xd1, 0x3a, 0x35, 0x39, 0xb8, 0xfb, 0x33, 0x0d, 0xf8, 0x1d, 0x3d,
                    0x95, 0xa3, 0x3a, 0xfb, 0x0c, 0x7f, 0x54, 0x20, 0x32, 0x75, 0x3c, 0x9c, 0xf0, 0xe2, 0xce, 0x4c,
                    0xe0, 0x98, 0xbe, 0x6b, 0x61, 0x19, 0xd0, 0x79, 0xea,
                }
            };

            // HMACSHA256 output size is 32 bytes
            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA256),
                new byte[63]
                {
                    0x86, 0x02, 0xc7, 0x0a, 0x80, 0xf3, 0xc9, 0x46, 0xb4, 0x10, 0xa8, 0xde, 0x36, 0x03, 0x5a, 0xb2,
                    0x29, 0x48, 0x16, 0xc6, 0x97, 0x99, 0xa4, 0x48, 0x85, 0x55, 0x34, 0xbc, 0xa6, 0x29, 0x68, 0x88,
                    0x8a, 0xd3, 0x42, 0x42, 0x22, 0x55, 0xa1, 0xf9, 0xaf, 0x1b, 0xb4, 0xfb, 0x69, 0xd3, 0x9d, 0x2e,
                    0x89, 0x4f, 0x0e, 0x19, 0x5a, 0x98, 0xce, 0x5c, 0x7e, 0xfd, 0xba, 0xc7, 0x51, 0x18, 0xdf,
                },
            };

            yield return new object[]
            {
                nameof (HashAlgorithmName.SHA256),
                new byte[64]
                {
                    0xcc, 0x06, 0x77, 0xaf, 0x45, 0xae, 0x66, 0x3f, 0x58, 0x8f, 0xd4, 0xa5, 0x6f, 0x31, 0xef, 0xd7,
                    0x29, 0x9e, 0x45, 0xb1, 0xac, 0x5b, 0x27, 0xb4, 0x10, 0xc1, 0xaf, 0x63, 0xe9, 0xbb, 0xb2, 0xe6,
                    0x76, 0x65, 0xbf, 0x2d, 0xd3, 0x14, 0xfb, 0xdf, 0xab, 0x6b, 0x95, 0xf4, 0x67, 0x53, 0xe0, 0xd7,
                    0x46, 0xd3, 0x4c, 0x72, 0xf3, 0x08, 0x95, 0x37, 0xbf, 0xa4, 0x67, 0xb0, 0xe0, 0x00, 0x46, 0x79,
                }
            };

            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA256),
                new byte[65]
                {
                    0x2b, 0x57, 0x00, 0xe5, 0xef, 0xda, 0x41, 0x7b, 0x75, 0x04, 0xe8, 0x37, 0x7a, 0x7f, 0xfd, 0x30,
                    0xd6, 0x56, 0x07, 0x69, 0x00, 0x75, 0x7b, 0xb9, 0x64, 0x15, 0x51, 0xac, 0x88, 0x55, 0x87, 0x24,
                    0xcc, 0xb9, 0x8b, 0xb2, 0x55, 0xcc, 0x02, 0xda, 0xf1, 0x4e, 0xc9, 0xa2, 0x40, 0x95, 0xfb, 0xff,
                    0xa0, 0x57, 0x73, 0x51, 0x66, 0x4d, 0x65, 0xd4, 0xc9, 0xc0, 0xe6, 0xf4, 0x40, 0xf4, 0x30, 0x17,
                    0x7b,
                }
            };

            // HMACSHA384 output size is 48 bytes
            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA384),
                new byte[95]
                {
                    0x82, 0x1d, 0x9b, 0x3c, 0x7f, 0xad, 0xd4, 0x1b, 0x91, 0xdc, 0x6e, 0x4f, 0xf5, 0xd8, 0xf7, 0xc8,
                    0x33, 0x18, 0xc8, 0xf8, 0x23, 0x3f, 0x5d, 0xf4, 0x95, 0x32, 0x81, 0x72, 0x96, 0xbd, 0xb9, 0xcc,
                    0xc1, 0x91, 0x0c, 0x5b, 0x5c, 0x86, 0x2c, 0x0d, 0x5b, 0xe4, 0xfb, 0xc6, 0x70, 0xc4, 0x20, 0xd6,
                    0x9c, 0xfd, 0x67, 0x56, 0x86, 0x16, 0xd6, 0xf8, 0x05, 0x86, 0x5c, 0xa0, 0x64, 0x5f, 0x72, 0xe0,
                    0xa5, 0x52, 0x5d, 0x72, 0xe8, 0x5e, 0x07, 0xf1, 0xf5, 0xcf, 0xf9, 0x63, 0x85, 0xc2, 0x77, 0x87,
                    0x89, 0x75, 0x9d, 0xd2, 0xc6, 0x2b, 0xf3, 0x23, 0x73, 0xd9, 0x1d, 0x01, 0x17, 0x9c, 0x01,
                },
            };

            yield return new object[]
            {
                nameof (HashAlgorithmName.SHA384),
                new byte[96]
                {
                    0xe4, 0x30, 0x8b, 0x7e, 0x5b, 0x64, 0xcd, 0xd7, 0x3d, 0x27, 0xd9, 0x3a, 0x9e, 0xee, 0xcc, 0xc6,
                    0x79, 0xa7, 0x39, 0xca, 0x91, 0xb8, 0x93, 0xcd, 0xe8, 0xb8, 0xb7, 0x8a, 0x48, 0xad, 0xb4, 0x3d,
                    0x3a, 0x02, 0xb9, 0xba, 0x81, 0x81, 0x01, 0x5f, 0xef, 0x8a, 0xc1, 0xcd, 0x6b, 0xae, 0x99, 0xb9,
                    0xfd, 0xaf, 0x28, 0x18, 0xcf, 0x48, 0xa1, 0xfa, 0x57, 0xce, 0x0a, 0x79, 0x1f, 0xbf, 0xc8, 0x7f,
                    0xd8, 0x34, 0x34, 0x16, 0x27, 0xc1, 0x12, 0x6e, 0x4c, 0x8c, 0x62, 0xc6, 0x11, 0x01, 0xb8, 0xb8,
                    0xa5, 0x06, 0xc8, 0x4a, 0x2f, 0xf2, 0x91, 0x08, 0x1a, 0x02, 0x5e, 0x72, 0x48, 0xd1, 0x11, 0x6a,

                }
            };

            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA384),
                new byte[97]
                {
                    0xa1, 0x01, 0x9f, 0x83, 0x55, 0x8f, 0x4a, 0x67, 0x2f, 0xbf, 0xf7, 0x7b, 0xbb, 0xfd, 0x22, 0x35,
                    0x35, 0x97, 0x01, 0x53, 0x5d, 0x9f, 0x7e, 0xa4, 0xbf, 0xb9, 0xfd, 0xdf, 0x7d, 0x86, 0x8f, 0x86,
                    0x98, 0x9d, 0x87, 0x21, 0x41, 0x75, 0x4e, 0x2c, 0xaf, 0x10, 0x25, 0x40, 0xe8, 0x26, 0xb8, 0x4d,
                    0x37, 0xe2, 0x43, 0x5f, 0x2b, 0x30, 0x03, 0xde, 0xb3, 0x1e, 0x31, 0x8c, 0x59, 0x2d, 0x26, 0xd5,
                    0x08, 0xe4, 0x55, 0x46, 0x0d, 0x76, 0xe7, 0x58, 0xe5, 0xa9, 0xaf, 0xe8, 0xe7, 0x7c, 0xa3, 0x05,
                    0x61, 0x7b, 0xcb, 0x98, 0x6e, 0xaa, 0xea, 0xd0, 0x7e, 0x52, 0x20, 0xf8, 0xfe, 0x49, 0x02, 0x08,
                    0xee,
                }
            };

            // HMACSHA512 output size is 64 bytes
            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA512),
                new byte[127]
                {
                    0x3d, 0x72, 0x04, 0x07, 0x9c, 0x28, 0x34, 0x33, 0x7a, 0x2f, 0x8e, 0x22, 0x00, 0x1f, 0x1f, 0x22,
                    0xd9, 0x24, 0x38, 0xd1, 0x56, 0xa4, 0x03, 0x46, 0x89, 0x09, 0xde, 0x13, 0xb3, 0x9c, 0xa9, 0x24,
                    0x0d, 0x3e, 0x4c, 0x97, 0x7f, 0xd5, 0x9f, 0x86, 0x45, 0x36, 0xcd, 0xc8, 0x0f, 0x44, 0x9a, 0xad,
                    0xe3, 0xba, 0xfc, 0x93, 0x08, 0x21, 0xa8, 0xfd, 0xde, 0xef, 0x4f, 0xe3, 0xaa, 0xb3, 0xcf, 0xc1,
                    0x81, 0x1b, 0x44, 0xf9, 0xae, 0xf6, 0x73, 0xc7, 0xf0, 0x71, 0xd6, 0x14, 0x8e, 0x18, 0x5d, 0x43,
                    0xa1, 0xfb, 0x09, 0x11, 0x24, 0x84, 0x56, 0x9c, 0x97, 0x6e, 0x2e, 0x5a, 0xcd, 0xd3, 0xa5, 0xfb,
                    0x81, 0x33, 0x6a, 0x3d, 0x95, 0xa5, 0xd9, 0xcd, 0x04, 0x36, 0x76, 0xc2, 0x4c, 0xed, 0x65, 0x81,
                    0x6f, 0x8c, 0xec, 0xfd, 0xde, 0xdd, 0x3c, 0xd9, 0x1a, 0xe1, 0xf1, 0x02, 0x7e, 0xb8, 0x3a,
                },
            };

            yield return new object[]
            {
                nameof (HashAlgorithmName.SHA512),
                new byte[128]
                {
                    0x0e, 0x6d, 0x21, 0x43, 0xca, 0xa7, 0x88, 0x13, 0x70, 0x0e, 0xc5, 0x7b, 0x5e, 0x5a, 0x41, 0x21,
                    0x03, 0x07, 0x30, 0x35, 0x10, 0xe9, 0x42, 0x12, 0x80, 0x64, 0x10, 0x71, 0x5d, 0x41, 0xb9, 0xf5,
                    0x3a, 0xb2, 0xcd, 0xf8, 0x71, 0x52, 0x01, 0xf8, 0xc5, 0x27, 0x65, 0xc0, 0x6b, 0x31, 0x35, 0xfc,
                    0x0d, 0x38, 0xbb, 0xf4, 0xc2, 0xeb, 0x9a, 0x85, 0x3f, 0x16, 0xf0, 0x25, 0x40, 0x33, 0x57, 0xc1,
                    0x08, 0x25, 0xcf, 0x31, 0x10, 0xbf, 0x78, 0x3a, 0x37, 0x64, 0x13, 0xa7, 0x2f, 0xd8, 0x32, 0x2b,
                    0x93, 0x1f, 0x0b, 0x6d, 0x6c, 0x6c, 0x45, 0x9f, 0x6a, 0xdb, 0x97, 0x8b, 0x33, 0x7d, 0x31, 0xa8,
                    0xd9, 0x92, 0xe4, 0x50, 0x38, 0x25, 0x22, 0x09, 0x98, 0x11, 0xce, 0x55, 0xf8, 0x6d, 0x27, 0x36,
                    0x5f, 0xab, 0xca, 0x4b, 0x54, 0x78, 0x11, 0xf9, 0xaf, 0x57, 0xfa, 0x02, 0x31, 0x27, 0x52, 0x69,
                }
            };

            yield return new object[]
            {
                nameof(HashAlgorithmName.SHA512),
                new byte[129]
                {
                    0x9e, 0xd0, 0xad, 0x78, 0x5d, 0xb3, 0x50, 0xce, 0x0a, 0x1b, 0xa2, 0xd1, 0x1d, 0xb8, 0x43, 0xc0,
                    0xc1, 0x0f, 0x2f, 0x13, 0xbe, 0x6b, 0x56, 0x53, 0x4e, 0x9f, 0x18, 0x76, 0xcf, 0xf2, 0xf0, 0xa1,
                    0xa6, 0xe8, 0x57, 0x2b, 0x05, 0xf2, 0x2e, 0x3e, 0xbf, 0xfe, 0x5c, 0x20, 0x93, 0x5e, 0x73, 0xca,
                    0x23, 0xda, 0x63, 0x24, 0xdf, 0x6c, 0xb7, 0x5c, 0xe7, 0xe9, 0x6e, 0x2a, 0x0c, 0x3a, 0xa9, 0xb7,
                    0x65, 0x25, 0x8a, 0x8c, 0xf0, 0xae, 0x0e, 0x53, 0x84, 0x82, 0x8b, 0xa6, 0xd9, 0xed, 0x3d, 0xfe,
                    0x6f, 0x1d, 0xbf, 0x36, 0x7c, 0xfd, 0xa2, 0xf9, 0x22, 0x79, 0x79, 0x54, 0x5f, 0xed, 0x3b, 0xb9,
                    0xab, 0x84, 0x75, 0xba, 0xb0, 0x12, 0x22, 0x6a, 0x07, 0xee, 0x35, 0xc4, 0x9a, 0xca, 0xd8, 0x28,
                    0x3c, 0x0b, 0xcd, 0x85, 0xbb, 0x6e, 0x7b, 0x0e, 0xaa, 0xcb, 0xf9, 0xb1, 0xa4, 0xcd, 0x65, 0x87,
                    0x61,
                }
            };
        }

        public static IEnumerable<object[]> GetRfc8009TestVectors()
        {
            // See RFC 8009 Appendix A "Sample pseudorandom function (PRF) invocations"
            // section. These vectors are also used by OpenSSL 3 for KBKDF.
            // Section 5 of RFC 8009 defines KDF-HMAC-SHA2 as always using "prf" as the label.
            // The appendix defines the context as "test".
            // (kdk, expected, hmac algorithm)
            yield return new object[]
            {
                new byte[]
                {
                    0x37, 0x05, 0xD9, 0x60, 0x80, 0xC1, 0x77, 0x28,
                    0xA0, 0xE8, 0x00, 0xEA, 0xB6, 0xE0, 0xD2, 0x3C,
                },
                new byte[]
                {
                    0x9D, 0x18, 0x86, 0x16, 0xF6, 0x38, 0x52, 0xFE,
                    0x86, 0x91, 0x5B, 0xB8, 0x40, 0xB4, 0xA8, 0x86,
                    0xFF, 0x3E, 0x6B, 0xB0, 0xF8, 0x19, 0xB4, 0x9B,
                    0x89, 0x33, 0x93, 0xD3, 0x93, 0x85, 0x42, 0x95,
                },
                HashAlgorithmName.SHA256,
            };

            yield return new object[]
            {
                new byte[]
                {
                    0x6D, 0x40, 0x4D, 0x37, 0xFA, 0xF7, 0x9F, 0x9D,
                    0xF0, 0xD3, 0x35, 0x68, 0xD3, 0x20, 0x66, 0x98,
                    0x00, 0xEB, 0x48, 0x36, 0x47, 0x2E, 0xA8, 0xA0,
                    0x26, 0xD1, 0x6B, 0x71, 0x82, 0x46, 0x0C, 0x52,
                },
                new byte[]
                {
                    0x98, 0x01, 0xF6, 0x9A, 0x36, 0x8C, 0x2B, 0xF6,
                    0x75, 0xE5, 0x95, 0x21, 0xE1, 0x77, 0xD9, 0xA0,
                    0x7F, 0x67, 0xEF, 0xE1, 0xCF, 0xDE, 0x8D, 0x3C,
                    0x8D, 0x6F, 0x6A, 0x02, 0x56, 0xE3, 0xB1, 0x7D,
                    0xB3, 0xC1, 0xB6, 0x2A, 0xD1, 0xB8, 0x55, 0x33,
                    0x60, 0xD1, 0x73, 0x67, 0xEB, 0x15, 0x14, 0xD2,
                },
                HashAlgorithmName.SHA384,
            };
        }
    }
}
