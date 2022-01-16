using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using TddsServer.Objects;

namespace TddsServer.General {
    public class AccountManager {

        public const string AdminName = "admin";
        public static string AccountInfosFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        public static ConcurrentDictionary<string, AccountInfo> ServerAccount = new ConcurrentDictionary<string, AccountInfo>();


        public static async Task Init() {
            try {
                if (!File.Exists(AccountInfosFilePath)) {
                    await Logger.Log(LogType.Info, "No any user on server. User login info file will be created.");
                    File.Create(AccountInfosFilePath).Close();
                    ServerAccount = new ConcurrentDictionary<string, AccountInfo>();
                } else {
                    string usersJson = File.ReadAllText(AccountInfosFilePath);
                    ConcurrentDictionary<string, AccountInfo>? infos = (ConcurrentDictionary<string, AccountInfo>?)JsonConvert.DeserializeObject(usersJson, typeof(ConcurrentDictionary<string, AccountInfo>));
                    ServerAccount = infos ?? new ConcurrentDictionary<string, AccountInfo>();
                }
            } catch (Exception exp) {
                await Logger.Log(LogType.Error, exp.Message);
            }
        }

        public static AccountInfo? GetLoginInfoOf(string username) {
            if (string.IsNullOrEmpty(username)) return null;
            if(ServerAccount==null || ServerAccount.IsEmpty) return null;
            if(ServerAccount.TryGetValue(username,out AccountInfo? info)) return info;
            return null;
        }

        public static TddsSvcMsg HasAdminLoginInfo() {
            bool has = ServerAccount.ContainsKey(AdminName);
            return new TddsSvcMsg(MessageType.Success, $"TDDS Server has {(has ? "a" : "no")} admin account.", has); 
        }

        public static TddsSvcMsg SetAdminLoginInfo(AccountInfo adminLoginInfo) {
            if (ServerAccount.ContainsKey(AdminName)) {
                return new TddsSvcMsg(MessageType.Error,
                    "Admin account is initialized. Please contact server administrator if you want to reset the admin account.");
            }
            if (CheckAccountInfo(adminLoginInfo) && adminLoginInfo.UserName.Equals(AdminName)) {
                Utils.SaveFile(AccountInfosFilePath, JsonConvert.SerializeObject(ServerAccount));
                return new TddsSvcMsg(MessageType.Success, "Admin account is created.");
            } else {
                return new TddsSvcMsg(MessageType.Error, 
                    "Create administrator account failed. The length of user name and password must be more than or equals 4 and less than  or equals 20. And the name of administrator account must be [admin].");
            }
        }

        public static TddsSvcMsg GetAllAccount() {
            List<string> infos = ServerAccount.Values.Where(i => !i.UserName.Equals(AdminName)).Select(i => i.UserName).ToList();
            TddsSvcMsg msg = new TddsSvcMsg(MessageType.Success, $"Got {infos.Count} users account.", infos);
            return msg;
        }

        public static TddsSvcMsg RigisterAccount(AccountInfo account) {
            if (CheckAccountInfo(account)) {
                // Encode password
                account.Password = EncodeText(account.Password);
                
                if (!ServerAccount.TryAdd(account.UserName, account))
                    return new TddsSvcMsg(MessageType.Error, $"Failed to created account with name [{account.UserName}], this name already exist.", false);
                if (!Utils.SaveFile(AccountInfosFilePath, JsonConvert.SerializeObject(ServerAccount))) {
                    ServerAccount.Remove(account.UserName, out _);
                    return new TddsSvcMsg(MessageType.Error, "Failed to create account.");
                }
                return new TddsSvcMsg(MessageType.Success, $"Successfully created a account with name [{account.UserName}].", true);
            } else {
                return new TddsSvcMsg(MessageType.Error, "Create account failed. The length of user name and password must be more than or equals 4 and less than  or equals 20.", false);
            }
        }

        public static TddsSvcMsg DestoryAccount(string userName) {
            if(!ServerAccount.TryRemove(userName,out AccountInfo? account)) {
                return new TddsSvcMsg(MessageType.Error, $"Faile to destory account. No account named {userName}.");
            }
            if (!Utils.SaveFile(AccountInfosFilePath, JsonConvert.SerializeObject(ServerAccount))) {
                ServerAccount[account.UserName] = account;
                return new TddsSvcMsg(MessageType.Error, $"Failed to destory account named {userName}.", false);
            }
            return new TddsSvcMsg(MessageType.Success, $"Successfully destoried the acouunt named {userName}.", true);
        }

        public static TddsSvcMsg ResetAccount(AccountResetInfo info) {
            if (!ServerAccount.ContainsKey(AdminName))
                return new TddsSvcMsg(MessageType.Error, "Admin account is not initialized.");
            string adminPwd = ServerAccount[AdminName].Password;
            if (!EncodeText(info.AdminPassword).Equals(adminPwd))
                return new TddsSvcMsg(MessageType.Error, "The password of admin account is not correct.", false);
            if (!ServerAccount.ContainsKey(info.UserName))
                return new TddsSvcMsg(MessageType.Error, $"The account named {info.UserName} dose not exist.");
            string oldPwd = ServerAccount[info.UserName].Password;

            // Encode and reset password
            ServerAccount[info.UserName].Password = EncodeText(info.Password);

            try {
                Utils.SaveFile(AccountInfosFilePath, JsonConvert.SerializeObject(ServerAccount));
                return new TddsSvcMsg(MessageType.Success, $"Successfully reset the account named {info.UserName}.", true);
            } catch (Exception ex) {
                // Restore password
                ServerAccount[info.UserName].Password = oldPwd;
                return new TddsSvcMsg(MessageType.Error, $"Error occur when reset account named {info.UserName}. " + ex.Message);
            }
        }
        public static TddsSvcMsg ModifyAccount(AccountInfo info) {
            if (!ServerAccount.ContainsKey(info.UserName))
                return new TddsSvcMsg(MessageType.Error, $"Account named {info.UserName} dose not exist.");
            string oldPwd = ServerAccount[info.UserName].Password;
            if (!EncodeText(info.OldPassword).Equals(oldPwd))
                return new TddsSvcMsg(MessageType.Error, $"The old password of account {info.UserName} is not correct.");
            if(!CheckAccountInfo(info)) return new TddsSvcMsg(MessageType.Error,
                     "Create administrator account failed. The length of user name and password must be more than or equals 4 and less than  or equals 20. And the name of administrator account must be [admin].");

            // Encode and modify password
            ServerAccount[info.UserName].Password = EncodeText(info.Password);
            try {
                Utils.SaveFile(AccountInfosFilePath, JsonConvert.SerializeObject(ServerAccount));
                return new TddsSvcMsg(MessageType.Success, $"Successfully modified the password of account {info.UserName}.");
            }catch(Exception exp) {
                ServerAccount[info.UserName].Password = oldPwd;
                return new TddsSvcMsg(MessageType.Error, $"Error occur when reset account named {info.UserName}. " + exp.Message);
            }
        }

        public static TddsSvcMsg Login(AccountInfo account) {
            if (ServerAccount.ContainsKey(account.UserName)) {
                bool success = ServerAccount[account.UserName].Password.Equals(EncodeText(account.Password));
                if (success) {
                    return new TddsSvcMsg(MessageType.Success, $"Successfully login server as [{account.UserName}].", true);
                } else {
                    return new TddsSvcMsg(MessageType.Error, $"The password of account [{account.UserName}] is not correct.", false);
                }
            } else {
                return new TddsSvcMsg(MessageType.Error, $"The account named [{account.UserName}] dose not exist.", false);
            }
        }


        private static string EncodeText(string text) {
            var buffer = Encoding.UTF8.GetBytes(text);
            byte[] bytes = System.Security.Cryptography.SHA256.Create().ComputeHash(buffer);
            return Convert.ToBase64String(bytes);
        }




        private static bool CheckAccountInfo(AccountInfo loginInfo) {
            if (loginInfo == null) return false;
            if (string.IsNullOrEmpty(loginInfo.UserName)
                || loginInfo.UserName.Length < 4
                || loginInfo.UserName.Length > 20) return false;
            if (string.IsNullOrEmpty(loginInfo.Password)
                || loginInfo.Password.Length < 4
                || loginInfo.Password.Length > 20) return false;
            return true;
        }
    }
}
