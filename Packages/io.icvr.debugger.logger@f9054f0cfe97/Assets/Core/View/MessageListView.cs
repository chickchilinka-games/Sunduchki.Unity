using System;
using System.Collections.Generic;
using System.Linq;
using DebuggerPlugins.Logger.Data;
using DebuggerPlugins.Logger.Extensions;
using DebuggerPlugins.Logger.Factories;
using DebuggerPlugins.Logger.Storages;
using ObservableCollections;
using R3;
using UnityEngine;
using Zenject;

namespace DebuggerPlugins.Logger.View
{
    internal class MessageListView : MonoBehaviour
    {
        [SerializeField] private MessageScrollRect _messageScrollRect;
        [SerializeField] private MessageFilter _messageFilter;

        public Observable<MessageView> MessageClicked => _messageClicked;
        private readonly Subject<MessageView> _messageClicked = new();

        public Observable<MessageView> MessageDisposed => _messageDisposed;
        private readonly Subject<MessageView> _messageDisposed = new();

        private MessageViewFactory _messageViewFactory;
        private LoggerMessagesStorage _messagesStorage;

        private List<MessageView> _messageViewsList;
        private int _firstItemIndex;
        private int CurrentCount => _messageViewsList.Count;
        private int NextItemIndex => _firstItemIndex + CurrentCount;

        private readonly List<LoggerMessage> _cachedFilteredMessages = new();
        private readonly List<(LoggerMessage key, int count)> _cachedFilteredGroups = new();

        private const int MaxItemsCount = 50;
        private const int MaxCountToLoad = MaxItemsCount / 5;

        private enum LoadDirection
        {
            Up,
            Down
        }

        [Inject]
        public void Construct(MessageViewFactory factory,
            LoggerMessagesStorage storage,
            LoggerCategoriesStorage categoriesStorage)
        {
            _messageViewFactory = factory;
            _messagesStorage = storage;
            _messagesStorage.ItemAdded.Subscribe(OnItemAdded).AddTo(this);
            _messageViewsList = new List<MessageView>();
            
            _messageScrollRect.Init();
            _messageScrollRect.LoadUp.Subscribe(_ => LoadUp()).AddTo(this);
            _messageScrollRect.LoadDown.Subscribe(_ => LoadDown()).AddTo(this);

            _messageFilter.Init(categoriesStorage);
            _messageFilter.Redraw.Subscribe(_ => Redraw()).AddTo(this);
        }

        private void OnDestroy()
        {
            _messageScrollRect.Dispose();
            _messageFilter.Dispose();
        }

        private void OnItemAdded(CollectionAddEvent<LoggerMessage> eventInfo)
        {
            RecalculateCachedMessages();
            
            var isFilterApproved = _messageFilter.Check(eventInfo.Value);

            if (!isFilterApproved
                || !TryGetNewMessageIndex(eventInfo, out var messageIndex))
            {
                return;
            }

            var listItemIndex = messageIndex - _firstItemIndex;

            if (listItemIndex < 0 
                || listItemIndex > CurrentCount
                || _messageScrollRect.IsOutOfFollowRange)
            {
                return;
            }

            if (CurrentCount + 1 > MaxItemsCount)
            {
                RemoveItemAtIndex(0);
                _firstItemIndex++;
                listItemIndex--;
            }

            InsertItemAtIndex(listItemIndex, eventInfo.Value);

            _messageScrollRect.AutoScroll(1.0f / CurrentCount);
        }

        private bool TryGetNewMessageIndex(CollectionAddEvent<LoggerMessage> eventInfo, out int messageIndex)
        {
            if (!_messageFilter.IsEnabled && !_messageFilter.IsCollapsed.CurrentValue)
            {
                messageIndex = eventInfo.Index;
            }
            else
            {
                messageIndex = GetMessageIndex(eventInfo.Value);
                if (messageIndex == -1 && _messageFilter.IsCollapsed.CurrentValue)
                {
                    var groupKey = GetCollapsedItemGroup(eventInfo.Value).key;
                    var listItemIndex = GetMessageIndex(groupKey) - _firstItemIndex;
                    
                    if (listItemIndex >= 0 && listItemIndex < CurrentCount)
                    {
                        _messageViewsList[listItemIndex].CollapsedCounter.Value++;
                    }

                    return false;
                }
            }

            return true;
        }

        private void InsertItemAtIndex(int listItemIndex, LoggerMessage data)
        {
            var newMessage = _messageViewFactory.Create(data);

            if (_messageFilter.IsCollapsed.CurrentValue)
            {
                newMessage.DisplayCollapsedCounter(true);
                newMessage.CollapsedCounter.Value = GetCollapsedItemGroup(data).count;
            }

            newMessage.transform.SetParent(_messageScrollRect.MessagesContainer, false);
            newMessage.transform.localScale = Vector3.one;
            newMessage.Clicked.AddListener(() => _messageClicked.OnNext(newMessage));

            if (_messageViewsList.Count > 0)
            {
                var startIndex = _messageViewsList[0].transform.GetSiblingIndex();
                newMessage.transform.SetSiblingIndex(startIndex + listItemIndex);
            }

            _messageViewsList.Insert(listItemIndex, newMessage);
        }

        private void RemoveItemAtIndex(int listItemIndex)
        {
            var target = _messageViewsList[listItemIndex];
            
            _messageDisposed.OnNext(target);
            
            target.Dispose();
            
            _messageViewsList.RemoveAt(listItemIndex);
        }

        private void LoadUp()
        {
            var maxCountToLoad = Mathf.Max(MaxCountToLoad, MaxItemsCount - CurrentCount);
            var count = Mathf.Min(_firstItemIndex, maxCountToLoad);
            
            LoadMessages(count, LoadDirection.Up);
        }

        private void LoadDown()
        {
            var maxCountToLoad = Mathf.Max(MaxCountToLoad, MaxItemsCount - CurrentCount);
            var validItemsCount = GetValidItems().Count();
            var remainingItemsCount = validItemsCount - NextItemIndex;
            var count = Mathf.Min(remainingItemsCount, maxCountToLoad);
            
            LoadMessages(count, LoadDirection.Down);
        }

        private void LoadMessages(int addCount, LoadDirection direction)
        {
            if (addCount == 0)
            {
                return;
            }

            var oldNormalizedPos = _messageScrollRect.NormalizedPosition;
            var isLoadingUp = direction == LoadDirection.Up;
            var skipCount = isLoadingUp 
                ? _firstItemIndex - addCount 
                : _firstItemIndex + CurrentCount;
            
            var loadedItems = GetValidItems()
                .Skip(skipCount)
                .Take(addCount)
                .ToArray();

            var excessCount = Mathf.Max(0, addCount + CurrentCount - MaxItemsCount);
            var removeAtIndex = isLoadingUp ? CurrentCount - excessCount : 0;
            
            for (int i = 0; i < excessCount; i++)
            {
                RemoveItemAtIndex(removeAtIndex);
            }

            var insertStartIndex = isLoadingUp ? 0 : CurrentCount;
            
            for (int i = 0; i < addCount; i++)
            {
                InsertItemAtIndex(insertStartIndex + i, loadedItems[i]);
            }

            _firstItemIndex += isLoadingUp ? -addCount : excessCount;

            var offsetDirection = isLoadingUp ? -1 : 1;
            var absoluteOffset = excessCount * offsetDirection;
            var normalizedOffset = absoluteOffset / CurrentCount;
            
            _messageScrollRect.NormalizedPosition = oldNormalizedPos + normalizedOffset;
        }

        private void Redraw()
        {
            Clear();
            RecalculateCachedMessages();
            
            var loadCount = GetValidItems().CountLessOrEqual(MaxItemsCount);
            
            LoadMessages(loadCount, LoadDirection.Down);
        }

        private void Clear()
        {
            foreach (var view in _messageViewsList)
            {
                view.Dispose();
            }

            _messageViewsList.Clear();

            _firstItemIndex = 0;
            _messageScrollRect.NormalizedPosition = 1;
        }

        private IEnumerable<LoggerMessage> GetValidItems()
        {
            IEnumerable<LoggerMessage> result;

            if (!_messageFilter.IsCollapsed.CurrentValue)
            {
                result = _cachedFilteredMessages;
            }
            else
            {
                result = _cachedFilteredGroups.Select(group => group.key);
            }

            return result;
        }

        private (LoggerMessage key, int count) GetCollapsedItemGroup(LoggerMessage targetItem)
        {
            return _cachedFilteredGroups.FirstOrDefault(grouping => grouping.key == targetItem);
        }

        private int GetMessageIndex(LoggerMessage targetMessage)
        {
            var messageTuple = GetValidItems()
                .Select((message, i) => (message, index: i))
                .FirstOrDefault(tuple => tuple.message.Equals(targetMessage));

            if (messageTuple == default)
            {
                return -1;
            }

            return messageTuple.index;
        }

        private void RecalculateCachedMessages()
        {
            _cachedFilteredMessages.Clear();
            
            var targetList = _messagesStorage.AsEnumerable();

            if (_messageFilter.IsEnabled)
            {
                targetList = targetList.Where(_messageFilter.Check);
            }

            _cachedFilteredMessages.AddRange(targetList);

            if (_messageFilter.IsCollapsed.CurrentValue)
            {
                _cachedFilteredGroups.Clear();
                
                var collapsedItems = _cachedFilteredMessages
                    .GroupBy(message => message, new LoggerMessage.Comparer())
                    .Select(grouping => (grouping.Key, grouping.Count()));
                
                _cachedFilteredGroups.AddRange(collapsedItems);
            }
        }
    }
}