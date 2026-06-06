using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace HermesPet.Services
{
    /// <summary>
    /// API Key 安全存储服务
    /// 使用 Windows 凭据管理器（Credential Manager）安全存储 API Key
    /// 
    /// TDR-S01: API Key 必须存储到 Windows 凭据管理器，不能明文存储到配置文件
    /// </summary>
    public class SecureStorageService
    {
        private const string CredentialTargetPrefix = "HermesPet_";

        /// <summary>
        /// 保存 API Key 到 Windows 凭据管理器
        /// </summary>
        /// <param name="providerId">提供商 ID（如 "deepseek", "zhipu"）</param>
        /// <param name="apiKey">API Key</param>
        /// <returns>是否保存成功</returns>
        public bool SaveApiKey(string providerId, string apiKey)
        {
            if (string.IsNullOrEmpty(providerId))
                throw new ArgumentNullException(nameof(providerId));
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            var targetName = GetTargetName(providerId);
            var credential = new NativeMethods.CREDENTIAL
            {
                Type = NativeMethods.CRED_TYPE_GENERIC,
                TargetName = targetName,
                CredentialBlob = Marshal.StringToCoTaskMemUni(apiKey),
                CredentialBlobSize = (uint)Encoding.Unicode.GetByteCount(apiKey),
                Persist = NativeMethods.CRED_PERSIST_LOCAL_MACHINE,
                UserName = "APIKey"
            };

            try
            {
                var result = NativeMethods.CredWrite(ref credential, 0);
                if (!result)
                {
                    var error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, $"Failed to save API Key for {providerId}");
                }
                return true;
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(credential.CredentialBlob);
            }
        }

        /// <summary>
        /// 从 Windows 凭据管理器读取 API Key
        /// </summary>
        /// <param name="providerId">提供商 ID</param>
        /// <returns>API Key，如果不存在返回 null</returns>
        public string? LoadApiKey(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
                throw new ArgumentNullException(nameof(providerId));

            var targetName = GetTargetName(providerId);
            IntPtr credentialPtr = IntPtr.Zero;

            try
            {
                var result = NativeMethods.CredRead(
                    targetName,
                    NativeMethods.CRED_TYPE_GENERIC,
                    0,
                    out credentialPtr);

                if (!result)
                {
                    var error = Marshal.GetLastWin32Error();
                    // ERROR_NOT_FOUND = 1168
                    if (error == 1168)
                        return null;

                    throw new Win32Exception(error, $"Failed to load API Key for {providerId}");
                }

                var credential = Marshal.PtrToStructure<NativeMethods.CREDENTIAL>(credentialPtr);
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                    return null;

                // 从 CredentialBlob 读取字符串
                var apiKey = Marshal.PtrToStringUni(
                    credential.CredentialBlob,
                    (int)credential.CredentialBlobSize / 2);

                return apiKey;
            }
            finally
            {
                if (credentialPtr != IntPtr.Zero)
                    NativeMethods.CredFree(credentialPtr);
            }
        }

        /// <summary>
        /// 删除 Windows 凭据管理器中的 API Key
        /// </summary>
        /// <param name="providerId">提供商 ID</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteApiKey(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
                throw new ArgumentNullException(nameof(providerId));

            var targetName = GetTargetName(providerId);
            var result = NativeMethods.CredDelete(targetName, NativeMethods.CRED_TYPE_GENERIC, 0);

            if (!result)
            {
                var error = Marshal.GetLastWin32Error();
                // ERROR_NOT_FOUND = 1168（凭据不存在）
                if (error == 1168)
                    return false;

                throw new Win32Exception(error, $"Failed to delete API Key for {providerId}");
            }

            return true;
        }

        /// <summary>
        /// 检查 API Key 是否存在
        /// </summary>
        /// <param name="providerId">提供商 ID</param>
        /// <returns>是否存在</returns>
        public bool ApiKeyExists(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
                return false;

            var targetName = GetTargetName(providerId);
            var result = NativeMethods.CredRead(
                targetName,
                NativeMethods.CRED_TYPE_GENERIC,
                0,
                out var credentialPtr);

            if (credentialPtr != IntPtr.Zero)
                NativeMethods.CredFree(credentialPtr);

            return result;
        }

        /// <summary>
        /// 生成凭据目标名称
        /// </summary>
        private string GetTargetName(string providerId)
        {
            return $"{CredentialTargetPrefix}{providerId}";
        }

        /// <summary>
        /// Windows 凭据管理器原生 API
        /// </summary>
        private static class NativeMethods
        {
            public const int CRED_TYPE_GENERIC = 1;
            public const int CRED_PERSIST_LOCAL_MACHINE = 2;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct CREDENTIAL
            {
                public uint Flags;
                public int Type;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string TargetName;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string Comment;
                public long LastWritten;
                public uint CredentialBlobSize;
                public IntPtr CredentialBlob;
                public int Persist;
                public int AttributeCount;
                public IntPtr Attributes;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string TargetAlias;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string UserName;
            }

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CredWrite(
                ref CREDENTIAL credential,
                uint flags);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CredRead(
                string targetName,
                int type,
                uint flags,
                out IntPtr credential);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CredDelete(
                string targetName,
                int type,
                uint flags);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern void CredFree(IntPtr credential);
        }
    }
}
