using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils.Logger;

namespace SPTarkov.Server.Core.Utils;

[Injectable(InjectionType.Singleton)]
public class OnLoadUtil
{
    public void Initialize(IEnumerable<IOnLoad> onLoadComponents)
    {
        OnLoadDictionaryInt = new OnLoadDictionaryClass(onLoadComponents);
        OnLoadDictionary = OnLoadDictionaryInt;
    }

    internal OnLoadDictionaryClass OnLoadDictionaryInt { get; private set; } = null!;
    public IReadOnlyDictionary<int, OnLoadList> OnLoadDictionary { get; private set; } = null!;

    internal class OnLoadDictionaryClass(IEnumerable<IOnLoad> onLoadComponents) : IReadOnlyDictionary<int, OnLoadList>
    {
        internal class Enumerable(OnLoadDictionaryClass dictionary) : IEnumerable<object>
        {
            public IEnumerator<object> GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            internal class Enumerator(OnLoadDictionaryClass dictionary) : IEnumerator<object>
            {
                private object? _current;
                private int _currentIndex = -1;
                private int _listSize;
                private List<object>? _list;
                private int _currentListIndex;
                private int _dictionarySize;
                private bool _finished;

                public bool MoveNext()
                {
                    if (_finished)
                    {
                        return false;
                    }

                    if (_current is null || _list is null)
                    {
                        if (dictionary._dictionary.Count == 0)
                        {
                            _finished = true;
                            return false;
                        }

                        _list = dictionary._dictionary.GetValueAtIndex(0)._list;
                    }
                    else if (_list.Count != _listSize)
                    {
                        _listSize = _list.Count;
                        while (_list[_currentIndex] != _current)
                        {
                            _currentIndex++;
                        }
                    }

                    _currentIndex++;
                    if (_currentIndex >= _listSize)
                    {
                        _currentIndex = 0;
                        if (_dictionarySize != dictionary._dictionary.Count)
                        {
                            _dictionarySize = dictionary._dictionary.Count;
                            while (dictionary._dictionary.GetValueAtIndex(_currentListIndex)._list != _list)
                            {
                                _currentListIndex++;
                            }
                        }

                        _currentListIndex++;

                        while (_currentListIndex < _dictionarySize)
                        {
                            _list = dictionary._dictionary.GetValueAtIndex(_currentListIndex)._list;
                            _listSize = _list.Count;
                            if (_listSize > 0)
                            {
                                break;
                            }

                            _currentListIndex++;
                        }

                        if (_currentListIndex >= _dictionarySize)
                        {
                            _current = null;
                            _list = null;
                            _finished = true;
                            return false;
                        }
                    }

                    _current = _list[_currentIndex];
                    return true;
                }

                public void Reset()
                {
                    _current = null;
                    _currentIndex = -1;
                    _listSize = 0;
                    _list = null;
                    _currentListIndex = 0;
                    _dictionarySize = 0;
                    _finished = false;
                }

                private object Current
                {
                    get { return _current ?? throw new InvalidOperationException(); }
                }

                object IEnumerator<object>.Current
                {
                    get { return Current; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                }
            }
        }

        internal IEnumerable<object> OnLoadEnumerable()
        {
            return new Enumerable(this);
        }

        internal readonly SortedList<int, OnLoadList> _dictionary = new(onLoadComponents
            .GroupBy(onload =>
                ((Injectable) Attribute.GetCustomAttribute(onload.GetType(), typeof(Injectable))!).TypePriority)
            .ToDictionary(group => group.Key, group => new OnLoadList(group)));

        public IEnumerator<KeyValuePair<int, OnLoadList>> GetEnumerator()
        {
            return _dictionary.Where(pair => pair.Value._list.Count > 0).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return _dictionary.Count(pair => pair.Value._list.Count > 0); }
        }

        public bool ContainsKey(int key)
        {
            return _dictionary.TryGetValue(key, out var value) && value._list.Count > 0;
        }

        public bool TryGetValue(int key, [MaybeNullWhen(false)] out OnLoadList value)
        {
            return _dictionary.TryGetValue(key, out value) && value._list.Count > 0;
        }

        public OnLoadList this[int key]
        {
            get
            {
                if (_dictionary.TryGetValue(key, out var value))
                {
                    return value;
                }

                var list = new OnLoadList([]);
                _dictionary[key] = list;
                return list;
            }
        }

        public IEnumerable<int> Keys
        {
            get { return _dictionary.Where(pair => pair.Value._list.Count > 0).Select(pair => pair.Key); }
        }

        public IEnumerable<OnLoadList> Values
        {
            get { return _dictionary.Where(pair => pair.Value._list.Count > 0).Select(pair => pair.Value); }
        }
    }

    public class OnLoadList : IReadOnlyList<IOnLoad>
    {
        internal OnLoadList(IEnumerable<IOnLoad> onLoadComponents)
        {
            _list = [..onLoadComponents];
            _onLoadIndexes = Enumerable.Range(0, _list.Count).ToArray();
        }

        internal readonly List<object> _list;
        internal readonly int[] _onLoadIndexes;

        public IEnumerator<IOnLoad> GetEnumerator()
        {
            return _onLoadIndexes.Select(i => (IOnLoad) _list[i]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Delegate item)
        {
            if (item.Method.ReturnType != typeof(Task))
            {
                throw new ArgumentException("Delegate must return a Task");
            }

            _list.Add(item);
        }

        public int Count
        {
            get { return _onLoadIndexes.Length; }
        }

        public void Insert(int index, Delegate item)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _onLoadIndexes.Length);
            if (item.Method.ReturnType != typeof(Task))
            {
                throw new ArgumentException("Delegate must return a Task");
            }

            if (index == _onLoadIndexes.Length)
            {
                _list.Add(item);
                return;
            }

            _list.Insert(_onLoadIndexes[index], item);
            for (var i = index; i < _onLoadIndexes.Length; i++)
            {
                _onLoadIndexes[i]++;
            }
        }

        public IOnLoad this[int index]
        {
            get { return (IOnLoad) _list[_onLoadIndexes[index]]; }
        }
    }
}
