using System;
using System.Collections.Generic;
using Modules.AppData.Interfaces;

namespace Modules.BonusSystem.Config
{
    public sealed class BonusCardInfoConfigProvider : IBonusCardInfoProvider, IAppDataConsumer
    {
        private readonly Dictionary<string, BonusCardInfo> _info =
            new(StringComparer.OrdinalIgnoreCase);

        public string ModuleName => "bonus_cards";

        public bool TryGetInfo(string bonusType, out BonusCardInfo info)
        {
            if (string.IsNullOrWhiteSpace(bonusType))
            {
                info = BonusCardInfo.Empty;
                return false;
            }

            return _info.TryGetValue(bonusType.Trim(), out info);
        }

        public void SetData(string serializedData, ISerializer serializer)
        {
            if (serializer == null || string.IsNullOrWhiteSpace(serializedData))
            {
                return;
            }

            if (!serializer.TryDeserialize<BonusCardInfoConfig>(serializedData, out var config) ||
                config?.Cards == null)
            {
                return;
            }

            _info.Clear();
            foreach (var entry in config.Cards)
            {
                if (entry == null)
                {
                    continue;
                }

                var typeKey = NormalizeType(entry.Type);
                if (string.IsNullOrWhiteSpace(typeKey))
                {
                    continue;
                }

                var info = new BonusCardInfo(entry.Title, entry.Description);
                _info[typeKey] = info;
            }
        }

        private static string NormalizeType(string type)
        {
            return string.IsNullOrWhiteSpace(type) ? string.Empty : type.Trim();
        }
    }

    public readonly struct BonusCardInfo
    {
        public static BonusCardInfo Empty => new(string.Empty, string.Empty);

        public string Title { get; }
        public string Description { get; }

        public BonusCardInfo(string title, string description)
        {
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }

    internal sealed class BonusCardInfoConfig
    {
        public List<BonusCardInfoEntry> Cards { get; set; } = new();
    }

    internal sealed class BonusCardInfoEntry
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
