﻿using System;
using System.Collections.Generic;
using System.Security.Principal;
using TrustExec.Interop;

namespace TrustExec.Library
{
    class Modules
    {
        public static bool AddVirtualAccount(
            string domain,
            string username,
            int domainRid)
        {
            if (domain == string.Empty || domain == null)
            {
                Console.WriteLine("[!] Domain name is not specified.\n");

                return false;
            }

            IntPtr hCurrentToken = WindowsIdentity.GetCurrent().Token;
            var privilegeNames = new List<string> {
                Win32Const.SE_DEBUG_NAME,
                Win32Const.SE_IMPERSONATE_NAME
            };

            Console.WriteLine();

            var results = Utilities.EnableMultiplePrivileges(
                hCurrentToken,
                privilegeNames);

            foreach (var result in results)
            {
                if (!result.Value)
                {
                    Console.WriteLine("\n[-] {0} is not available.\n", result.Key);

                    return false;
                }
            }

            if (!Utilities.ImpersonateAsSmss())
                return false;

            bool status = Utilities.AddVirtualAccount(domain, username, domainRid);
            Win32Api.RevertToSelf();

            return status;
        }


        public static bool RunTrustedInstallerProcess(
            string domain,
            string username,
            int domainRid,
            string command,
            bool fullPrivilege)
        {
            string execute;

            Console.WriteLine();

            if (domain == string.Empty || domain == null)
            {
                Console.WriteLine("[!] Domain name is not specified.\n");

                return false;
            }

            if (username == string.Empty || username == null)
            {
                Console.WriteLine("[!] Username is not specified.\n");

                return false;
            }

            if (command == string.Empty || command == null)
            {
                execute = "C:\\Windows\\System32\\cmd.exe";
            }
            else
            {
                execute = string.Format(
                    "C:\\Windows\\System32\\cmd.exe /c \"{0}\"",
                    command);
            }

            IntPtr hCurrentToken = WindowsIdentity.GetCurrent().Token;
            var privilegeNames = new List<string> {
                Win32Const.SE_DEBUG_NAME,
                Win32Const.SE_IMPERSONATE_NAME
            };

            var results = Utilities.EnableMultiplePrivileges(
                hCurrentToken,
                privilegeNames);

            foreach (var result in results)
            {
                if (!result.Value)
                {
                    Console.WriteLine(
                        "\n[-] {0} is not available.\n",
                        result.Key);

                    return false;
                }
            }

            if (!Utilities.ImpersonateAsSmss())
                return false;

            IntPtr hToken = Utilities.CreateTrustedInstallerTokenWithVirtualLogon(
                domain,
                username,
                domainRid);

            if (hToken == IntPtr.Zero)
                return false;

            if (fullPrivilege)
                Utilities.EnableAllPrivileges(hToken);

            bool status = Utilities.CreateTokenAssignedProcess(hToken, execute);
            Win32Api.CloseHandle(hToken);
            Win32Api.RevertToSelf();

            return status;
        }


        public static bool RemoveVirtualAccount(string domain, string username)
        {
            IntPtr hCurrentToken = WindowsIdentity.GetCurrent().Token;
            var privilegeNames = new List<string> {
                Win32Const.SE_DEBUG_NAME,
                Win32Const.SE_IMPERSONATE_NAME
            };

            Console.WriteLine();

            var results = Utilities.EnableMultiplePrivileges(
                hCurrentToken,
                privilegeNames);

            foreach (var result in results)
            {
                if (!result.Value)
                {
                    Console.WriteLine(
                        "\n[-] {0} is not available.\n",
                        result.Key);

                    return false;
                }
            }

            if (!Utilities.ImpersonateAsSmss())
                return false;

            bool status = Utilities.RemoveVirtualAccount(domain, username);
            Win32Api.RevertToSelf();

            return status;
        }


        public static bool SidLookup(string domain, string username, string sid)
        {
            string result;
            string accountName;

            if (sid != null)
            {
                result = Helpers.ConvertSidStringToAccountName(sid);
                
                if (result != null)
                {
                    Console.WriteLine(
                        "\n[*] Result : {0} (SID : {1})\n",
                        result,
                        sid.ToUpper());
                    
                    return true;
                }
                else
                {
                    Console.WriteLine("\n[*] No result.\n");

                    return false;
                }
            }
            else if (domain != null || username != null)
            {
                if (domain != null && username != null)
                    accountName = string.Format("{0}\\{1}", domain, username);
                else if (domain != null)
                    accountName = domain;
                else if (username != null)
                    accountName = username;
                else
                    return false;

                result = Helpers.ConvertAccountNameToSidString(ref accountName);

                if (result != null)
                {
                    Console.WriteLine(
                        "\n[*] Result : {0} (SID : {1})\n",
                        accountName.ToLower(),
                        result);

                    return true;
                }
                else
                {
                    Console.WriteLine("\n[*] No result.\n");

                    return false;
                }
            }
            else
            {
                Console.WriteLine("\n[!] SID, domain name or username to lookup is required.\n");

                return false;
            }
        }
    }
}
