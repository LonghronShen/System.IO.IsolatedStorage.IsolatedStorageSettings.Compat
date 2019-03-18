using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml.Serialization;

#if !NET20
using System.Linq;
using System.Runtime.Serialization;
using InternalDictionary=System.Collections.Generic.Dictionary<string, object>;
#else
using InternalDictionary = System.Collections.Generic.SerializableDictionary<string, object>;
#endif

namespace System.IO.IsolatedStorage
{

    /// <summary>
    /// IsolatedStorageSettings
    /// </summary>
    public sealed class IsolatedStorageSettings
      : IDictionary<string, object>, IDictionary,
      ICollection<KeyValuePair<string, object>>, ICollection,
      IEnumerable<KeyValuePair<string, object>>, IEnumerable
    {

        /// <summary>
        /// Global settings which indicates weather to be compatible to Silverlight platform.
        /// </summary>
        public static bool CompatibleToSilverlight { get; set; } = false;

        private static IsolatedStorageSettings _applicationSettings;
        private static IsolatedStorageSettings _siteSettings;

        private readonly IsolatedStorageFile _container;
        private readonly InternalDictionary _settings;

        /// <summary>
        /// SL2 use a "well known" name and it's readable (and deletable) directly by isolated storage
        /// </summary>
        private const string LocalSettings = "__LocalSettings";

        #region Static Properties
        /// <summary>
        /// Per application, per-computer, per-user
        /// </summary>
        public static IsolatedStorageSettings ApplicationSettings
        {
            get
            {
                return _applicationSettings ??
                    (_applicationSettings = new IsolatedStorageSettings(
                           HasActivationContext()
                               ? IsolatedStorageFile.GetUserStoreForApplication()
                               : //for WPF, apps deployed via ClickOnce will have a non-null ActivationContext
                               IsolatedStorageFile.GetUserStoreForAssembly()));
            }
        }

        /// <summary>
        /// Per domain, per-computer, per-user
        /// </summary>
        public static IsolatedStorageSettings SiteSettings
        {
            get
            {
                return _siteSettings ??
                    (_siteSettings = new IsolatedStorageSettings(
                           HasActivationContext()
                               ? IsolatedStorageFile.GetUserStoreForApplication()
                               : //for WPF, apps deployed via ClickOnce will have a non-null ActivationContext
                               IsolatedStorageFile.GetUserStoreForAssembly()));
            }
        }
        #endregion

        #region Properties

        internal IsolatedStorageFile Container
        {
            get
            {
                return this._container;
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get { return _settings.Count; }
        }

        /// <inheritdoc />
        public ICollection Keys
        {
            get { return _settings.Keys; }
        }

        /// <inheritdoc />
        public ICollection Values
        {
            get { return _settings.Values; }
        }

        /// <inheritdoc />
        public object this[string key]
        {
            get
            {
                return _settings[key];
            }
            set
            {
                _settings[key] = value;
            }
        }
        #endregion

        #region Constructors
        internal IsolatedStorageSettings(IsolatedStorageFile isf)
        {
            _container = isf;

#if NET20
            if (!IsolatedStorageFileHelper.FileExists(isf, LocalSettings))
#else
            if (!isf.FileExists(LocalSettings))
#endif
            {
                _settings = new InternalDictionary();
                return;
            }

#if NET20
            using (var fs = IsolatedStorageFileHelper.OpenFile(isf, LocalSettings, FileMode.Open))
#else
            using (var fs = isf.OpenFile(LocalSettings, FileMode.Open))
#endif
            {
                try
                {
                    if (CompatibleToSilverlight)
                    {
#if NET20
                        var xmlSerializer = new XmlSerializer(_settings.GetType());
                        this._settings = (InternalDictionary)xmlSerializer.Deserialize(fs);
#else
                        var reader = new DataContractSerializer(typeof(Dictionary<string, object>));
                        this._settings = (Dictionary<string, object>)reader.ReadObject(fs);
#endif
                    }
                    else
                    {
                        var formatter = new BinaryFormatter();
                        this._settings = (InternalDictionary)formatter.Deserialize(fs);
                    }
                }
                catch (Exception)
                {
                    _settings = new InternalDictionary();
                }
            }
        }

        /// <summary>
        /// Finalizer for the IsolatedStorageSettings object.
        /// </summary>
        ~IsolatedStorageSettings()
        {
            // settings are automatically saved if the application close normally
            Save();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds the specified key and value to the IsolateStorageSettings object.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            _settings.Add(key, value);
        }

        /// <summary>
        /// Clear all the items in this IsolatedStorageSettings object.
        /// </summary>
        /// <remarks>
        /// This method is emitted as virtual due to: https://bugzilla.novell.com/show_bug.cgi?id=446507
        /// </remarks>
        public void Clear()
        {
            _settings.Clear();
        }

        /// <summary>
        /// Check if the given key exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _settings.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return _settings.Remove(key);
        }

        /// <summary>
        /// Save all the settings.
        /// </summary>
        public void Save()
        {
#if NET20
            using (var fs = IsolatedStorageFileHelper.CreateFile(_container, LocalSettings))
#else
            using (var fs = _container.CreateFile(LocalSettings))
#endif
            {
                if (CompatibleToSilverlight)
                {
#if NET20
                    var xmlSerializer = new XmlSerializer(_settings.GetType());
                    xmlSerializer.Serialize(fs, _settings);
#else
                    var xmlSerializer = new DataContractSerializer(_settings.GetType());
                    xmlSerializer.WriteObject(fs, _settings);
#endif
                }
                else
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, this._settings);
                }
                fs.Flush();
            }
        }

        /// <summary>
        /// Try to get item value by key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            value = default(T);
            try
            {
                if (!_settings.TryGetValue(key, out var v)) return false;
                value = (T)v;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Try to get item value by key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, Type type, out object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            value = null;
#if NET20
            var mis = typeof(IsolatedStorageSettings).GetMethods();
            foreach (var item in mis)
            {
                if (item.Name == nameof(TryGetValue) && item.IsGenericMethod)
                {
                    var mi = item.MakeGenericMethod(type);
                    var @params = new object[] { key, null };
                    if (!(mi.Invoke(this, @params) is bool hasKey) || !hasKey) return false;
                    value = @params[1];
                    return true;
                }
            }
            return false;
#else
            var mi = typeof(IsolatedStorageSettings)
                .GetMethods()
                .FirstOrDefault(x => x.Name == nameof(TryGetValue) && x.IsGenericMethod);
            if (mi == null) return false;
            mi = mi.MakeGenericMethod(type);
            var @params = new object[] { key, null };
            if (mi.Invoke(this, @params) is bool hasKey && hasKey)
            {
                value = @params[1];
                return true;
            }
            return false;
#endif
        }
        #endregion

        #region Explicit interface implementations
        #region ICollection<KeyValuePair<string, object>>
        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return _settings.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            _settings.Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            _settings.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return _settings.ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            (_settings as ICollection<KeyValuePair<string, object>>).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return _settings.Remove(item.Key);
        }
        #endregion

        #region IDictionary<string, object>
        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return _settings.Keys; }
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return _settings.Values; }
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return _settings.ContainsKey(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return _settings.TryGetValue(key, out value);
        }
        #endregion

        #region IDictionary
        void IDictionary.Add(object key, object value)
        {
            var s = ExtractKey(key, false);
            if (s == null) throw new ArgumentException(nameof(key));
            this.Add(s, value);
        }

        void IDictionary.Clear()
        {
            this.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            var s = ExtractKey(key, false) ?? string.Empty;
            return this.Contains(s);
        }

        object IDictionary.this[object key]
        {
            get
            {
                var s = ExtractKey(key, false);
                return s == null ? null : _settings[s];
            }
            set
            {
                var s = ExtractKey(key, false);
                if (s == null) throw new ArgumentException(nameof(key));
                _settings[s] = value;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        void IDictionary.Remove(object key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var s = ExtractKey(key, false);
            if (s == null) return;
            _settings.Remove(s);
        }
        #endregion

        #region ICollection
        void ICollection.CopyTo(Array array, int index)
        {
            (_settings as ICollection).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized
        {
            get { return (_settings as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (_settings as ICollection).SyncRoot; }
        }
        #endregion

        #region IEnumerable<KeyValuePair<string, object>>
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _settings.GetEnumerator();
        }
        #endregion

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _settings.GetEnumerator();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return _settings.GetEnumerator();
        }
        #endregion
        #endregion

        #region Utils

        /// <summary>
        /// Try to convert the given object key to its string form.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="throwIfNotString"></param>
        /// <returns></returns>
        private string ExtractKey(object key, bool throwIfNotString = true)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var strKey = key as string;
            if (throwIfNotString && strKey == null)
            {
                throw new ArgumentException("Wrong type given for the argument.", nameof(key));
            }
            return strKey;
        }

        private static bool HasActivationContext()
        {
#if NETSTANDARD2_0
            var domain = Thread.GetDomain();
            var pi = domain.GetType().GetProperty("ActivationContext");
            var value = SystemUtils.Try(() => pi?.GetValue(domain, null), null);
            return value != null;
#else
            return Thread.GetDomain().ActivationContext != null;
#endif
        }
        #endregion

    }

}
