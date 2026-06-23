using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ClothingShop.Application.Services
{
    public class VnPayLibrary
    {
        private readonly SortedDictionary<string, string> _requestData = new SortedDictionary<string, string>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, string> _responseData = new SortedDictionary<string, string>(StringComparer.Ordinal);

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData[key] = value;
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData[key] = value;
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var val) ? val : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var headers = new StringBuilder();
            foreach (var kv in _requestData)
            {
                headers.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }

            string rawData = headers.ToString();
            if (rawData.EndsWith("&"))
            {
                rawData = rawData.Remove(rawData.Length - 1);
            }

            string vnpSecureHash = HmacSha512(vnpHashSecret, rawData);
            string query = rawData + "&vnp_SecureHash=" + vnpSecureHash;
            return baseUrl + "?" + query;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rawData = new StringBuilder();
            foreach (var kv in _responseData)
            {
                if (kv.Key.StartsWith("vnp_") && kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                {
                    rawData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            string raw = rawData.ToString();
            if (raw.EndsWith("&"))
            {
                raw = raw.Remove(raw.Length - 1);
            }

            string myChecksum = HmacSha512(secretKey, raw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}
