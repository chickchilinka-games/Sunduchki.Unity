/*
    Chickchilinka CONFIDENTIAL
    __________________

    [2016] - [2022] Chickchilinka LLC
    All Rights Reserved.

    NOTICE:  All information contained herein is, and remains
    the property of Chickchilinka LLC and its suppliers,
    if any.  The intellectual and technical concepts contained
    herein are proprietary to Chickchilinka LLC
    and its suppliers and may be covered by U.S. and Foreign Patents,
    patents in process, and are protected by trade secret or copyright law.
    Dissemination of this information or reproduction of this material
    is strictly forbidden unless prior written permission is obtained
    from Chickchilinka LLC.
*/


using System;
using System.Text.RegularExpressions;

#pragma warning disable CS0162
// ReSharper disable once CheckNamespace
namespace Chickchilinka.Tools
{
    public static class AppVersion
    {
        public static readonly Version Default = new Version(1, 0, 1);

        private static Version _current;
        
        public static Version Current 
        {
            get
            {
#if CICD_CONFIG
                return _current ??= ParseFromString(AppBuildConfig.Instance.Version);
#else
                return Default;
#endif
            }
        }
        
        public static string FormatToString()
        {
            return FormatToString(Current);
        }
        
        public static string FormatToString(this Version version)
        {
            return $"{version.Major}.{version.Minor}b{version.Build}";
        }

        public static Version ParseFromString(string versionText, bool strict = true)
        {
            var regex = strict
                ? new Regex("^(\\d+)\\.(\\d+)[b|\\.](\\d+)$")   // string can contain only version
                : new Regex("(\\d+)\\.(\\d+)[b|\\.](\\d+)"); // anywhere in the string
            var matches = regex.Matches(versionText);
            if ((strict && matches.Count == 1) || (!strict && matches.Count > 0) && matches[0].Groups.Count == 4)
            {
                var groups = matches[0].Groups;
                int.TryParse(groups[1].Value, out var major);
                int.TryParse(groups[2].Value, out var minor);
                int.TryParse(groups[3].Value, out var build);
                return new Version(major, minor, build);
            }
            return Default;
        }

        #if UNITY_EDITOR
        public static void ResetVersion(Version version)
        {
            _current = version;
        }
        #endif
    }
}