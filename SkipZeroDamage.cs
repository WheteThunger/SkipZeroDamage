using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("Skip Zero Damage", "WhiteThunder", "1.0.0")]
    [Description("Prevents processing damage that will amount to 0.")]
    internal class SkipZeroDamage : CovalencePlugin
    {
        #region Fields

        private const string PermissionReport = "skipzerodamage.report";

        private int ZeroDamageEventsBlocked = 0;
        private int LowDamageEventsBlocked = 0;
        private float LowDamageCumulativeBlocked = 0;

        private Configuration pluginConfig;

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionReport, this);
        }

        object OnEntityTakeDamage(BuildingBlock entity, HitInfo info)
        {
            var totalDamage = info.damageTypes.Total();
            if (totalDamage == 0)
            {
                ZeroDamageEventsBlocked++;
                return true;
            }

            if (totalDamage < pluginConfig.LowDamageThreshold)
            {
                LowDamageEventsBlocked++;
                LowDamageCumulativeBlocked += totalDamage;
                return true;
            }

            return null;
        }

        #endregion

        #region Commands

        [Command("skipzerodamage.report")]
        private void DamageBlockReportCommand(IPlayer player)
        {
            if (!player.IsServer && !player.IsAdmin)
            {
                if (!player.HasPermission(PermissionReport))
                {
                    player.Reply("You don't have permission to use this command.");
                    return;
                }
            }

            var sb = new StringBuilder();
            sb.Append($"Zero-damage events blocked: {ZeroDamageEventsBlocked}");

            if (pluginConfig.LowDamageThreshold > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Low-damage events blocked (below {pluginConfig.LowDamageThreshold}): {LowDamageEventsBlocked}");
                sb.Append($"Low-damage cumulatively blocked: {LowDamageCumulativeBlocked}");
            }

            player.Reply(sb.ToString());
        }

        #endregion

        #region Configuration

        internal class Configuration : SerializableConfiguration
        {
            [JsonProperty("LowDamageThreshold")]
            public float LowDamageThreshold = 0f;
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        internal static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                pluginConfig = Config.ReadObject<Configuration>();
                if (pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(pluginConfig, true);
        }

        #endregion
    }
}
