using System;
using DebuggerPlugins.TestDataView.Core.Data;
using R3;

namespace InternalPlugins.DataView.Core.Data.ImplementsData
{
    internal abstract class SimpleUpdatableByTimeData : StringData, IDisposable
    {
        private readonly CompositeDisposable _compositeDisposable = new();

        internal SimpleUpdatableByTimeData(string title, int secondsUpdateInterval) : base(title, string.Empty)
        {
            UpdateData();
            Observable.Interval(TimeSpan.FromSeconds(secondsUpdateInterval)).Subscribe(_ => UpdateData()).AddTo(_compositeDisposable);
        }

        protected abstract void UpdateData();
        
        public void Dispose()
        {
            _compositeDisposable.Dispose();
        }
    }
}