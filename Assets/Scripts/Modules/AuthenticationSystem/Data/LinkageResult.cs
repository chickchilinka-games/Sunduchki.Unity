using System;

namespace Modules.AuthenticationSystem.Data
{
    [Serializable]
    public sealed class LinkageResult: OperationResult
    {
        private LinkageResult(bool succeeded, LinkageInfo linkageInfo, string error): base(succeeded, error)
        {
            LinkageInfo = linkageInfo;
        }
        

        public LinkageInfo LinkageInfo { get; }

        internal static LinkageResult Succeeded(LinkageInfo info) => new(true, info, null);

        internal static LinkageResult Failed(LinkageInfo info, string error) => new(false, info, error);
    }
}
