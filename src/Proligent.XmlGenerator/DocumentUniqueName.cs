using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Proligent.XmlGenerator
{
    /// <summary>
    /// Provides functionality for generating deterministic, interoperable UUID strings that uniquely identify files
    /// based on their filename and content hash.
    /// </summary>
    public static class DocumentUniqueName
    {
        /// <summary>
        /// Creates a deterministic, interoperable UUID string from:
        ///   - the filename (case-insensitive)
        ///   - the SHA-256 checksum of the file contents
        ///
        /// Same filename + same contents => same UUID.
        /// </summary>
        public static string FromFilePath(string filePath)
        {
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File path must contain a filename.", nameof(filePath));

            string canonicalFileName = CanonicalizeFileName(fileName);
            byte[] fileHash = ComputeFileSha256(filePath);
            string fileHashHex = ToLowerHex(fileHash);

            // Deterministic input (no domain separation or versioning)
            string input = $"{canonicalFileName}|{fileHashHex}";
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            byte[] digest = SHA256.HashData(inputBytes);
            byte[] uuidBytes = digest.Take(16).ToArray();

            // UUID-style version and variant bits (cosmetic)
            uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x40);
            uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);

            return ToUuidStringNetworkOrder(uuidBytes);
        }

        private static string CanonicalizeFileName(string fileName)
        {
            string s = fileName.Trim();

            if (s.Length == 0)
                throw new ArgumentException("Filename must not be empty.", nameof(fileName));

            // Unicode normalization for cross-platform consistency
            s = s.Normalize(NormalizationForm.FormC);

            // Case-insensitive policy
            s = s.ToLowerInvariant();

            // Guardrail: filename only, no path separators
            if (s.Contains('/') || s.Contains('\\'))
                throw new ArgumentException("Filename must not contain path separators.", nameof(fileName));

            return s;
        }

        private static byte[] ComputeFileSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(stream);
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        /// <summary>
        /// Formats 16 bytes (big-endian / network order) as a canonical UUID string.
        /// </summary>
        private static string ToUuidStringNetworkOrder(byte[] b)
        {
            if (b is null) throw new ArgumentNullException(nameof(b));
            if (b.Length != 16)
                throw new ArgumentException("UUID byte array must be exactly 16 bytes.", nameof(b));

            return string.Create(
                CultureInfo.InvariantCulture,
                $"{b[0]:x2}{b[1]:x2}{b[2]:x2}{b[3]:x2}" +
                $"-{b[4]:x2}{b[5]:x2}" +
                $"-{b[6]:x2}{b[7]:x2}" +
                $"-{b[8]:x2}{b[9]:x2}" +
                $"-{b[10]:x2}{b[11]:x2}{b[12]:x2}{b[13]:x2}{b[14]:x2}{b[15]:x2}"
            );
        }
    }
}
