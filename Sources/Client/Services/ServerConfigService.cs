using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Common.Http;
using QuickPrice.Models;
using QuickPrice.Logging;

namespace QuickPrice.Services
{
    public class ServerConfigService
    {
        private static ServerConfigService _instance;
        public static ServerConfigService Instance => _instance ??= new ServerConfigService();

        private ServerClientConfig _config;
        private Task<bool> _updateTask;

        private ServerConfigService() { }

        public async Task<bool> UpdateServerConfigAsync()
        {
            if (_updateTask != null && !_updateTask.IsCompleted)
            {
                await _updateTask;
                return _config != null;
            }

            _updateTask = Task.Run(() =>
            {
                try
                {
                    string json = RequestHandler.GetJson("/showMeTheMoney/getClientConfig");

                    if (string.IsNullOrEmpty(json))
                    {
                        ClientLog.Warning("⚠️ 服务端配置返回空数据");
                        return false;
                    }

                    var config = JsonConvert.DeserializeObject<ServerClientConfig>(json);
                    if (config == null)
                    {
                        ClientLog.Warning("⚠️ 无法解析服务端配置");
                        return false;
                    }

                    _config = config;
                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"❌ 获取服务端配置失败: {ex.Message}");
                    return false;
                }
            });

            return await _updateTask;
        }

        public ServerClientConfig GetConfig()
        {
            return _config;
        }
    }
}
