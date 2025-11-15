using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebuggerPlugins.Logger.Data;
using ObservableCollections;
using R3;
using UnityEngine;

namespace DebuggerPlugins.Logger.Storages
{
    internal class LoggerMessagesStorage : IEnumerable<LoggerMessage>
    {
        public Observable<CollectionAddEvent<LoggerMessage>> ItemAdded { get; private set; }
        public int Count => _reactiveCollection.Count;

        private readonly ObservableList<LoggerMessage> _reactiveCollection;

        public LoggerMessagesStorage()
        {
            _reactiveCollection = new ObservableList<LoggerMessage>();
            ItemAdded = _reactiveCollection.ObserveAdd();
        }

        public void Add(LoggerMessage data)
        {
            int index = 0;
            for (int i = _reactiveCollection.Count - 1; i >= 0; i--)
            {
                index = i + 1;
                if (_reactiveCollection[i].DateTime <= data.DateTime)
                    break;
            }

            _reactiveCollection.Insert(index, data);
        }

        public void RemoveAt(int index) => _reactiveCollection.RemoveAt(index);
        public void Clear() => _reactiveCollection.Clear();
        public LoggerMessage this[int index] => _reactiveCollection[index];

        public IEnumerator<LoggerMessage> GetEnumerator()
        {
            return _reactiveCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var message in this)
            {
                string messageString = message.LogType switch
                {
                    LogType.Log or LogType.Warning => message.ToString(),
                    _ => message.ToStringExtended()
                };

                stringBuilder.AppendLine(messageString);
            }

            return stringBuilder.ToString();
        }

        public string ToFilteredString(string[] filter)
        {
            var stringBuilder = new StringBuilder();
            foreach (var message in this)
            {
                if (!filter.Contains(message.Category))
                {
                    continue;
                }
                
                string messageString = message.LogType switch
                {
                    LogType.Log or LogType.Warning => message.ToString(),
                    _ => message.ToStringExtended()
                };

                stringBuilder.AppendLine(messageString);
            }

            return stringBuilder.ToString();
        }
    }
}