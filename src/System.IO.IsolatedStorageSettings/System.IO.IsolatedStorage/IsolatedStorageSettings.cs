using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml.Serialization;

#if !NET20
using System.Linq;
using System.Runtime.Serialization;
#endif

namespace System.IO.IsolatedStorage
{

    public sealed class IsolatedStorageSettings
      : IDictionary<string, object>, IDictionary,
      ICollection<KeyValuePair<string, object>>, ICollection,
      IEnumerable<KeyValuePair<string, object>>, IEnumerable
    {

        public static bool CompatibleToSilverlight { get; set; } = false;

        static private IsolatedStorageSettings application_settings;
        static private IsolatedStorageSettings site_settings;

        private IsolatedStorageFile container;
        private Dictionary<string, object> settings;

        // SL2 use a "well known" name and it's readable (and delete-able) directly by isolated storage
        private const string LocalSettings = "__LocalSettings";

        internal IsolatedStorageSettings(IsolatedStorageFile isf)
        {
            container = isf;

#if NET20
            if (!IsolatedStorageFileHelper.FileExists(isf, LocalSettings))
#else
            if (!isf.FileExists(LocalSettings))
#endif
            {
                settings = new Dictionary<string, object>();
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
                        var xmlSerializer = new XmlSerializer(settings.GetType());
                        this.settings = (Dictionary<string, object>)xmlSerializer.Deserialize(fs);
#else
                        var reader = new DataContractSerializer(typeof(Dictionary<string, object>));
                        this.settings = (Dictionary<string, object>)reader.ReadObject(fs);
#endif
                    }
                    else
                    {
                        var formatter = new BinaryFormatter();
                        this.settings = (Dictionary<string, object>)formatter.Deserialize(fs);
                    }
                }
                catch (Exception ex)
                {
                    settings = new Dictionary<string, object>();
                }
            }
        }

        ~IsolatedStorageSettings()
        {
            // settings are automatically saved if the application close normally
            Save();
        }

        // static properties

        // per application, per-computer, per-user
        public static IsolatedStorageSettings ApplicationSettings
        {
            get
            {
                if (application_settings == null)
                {
                    application_settings = new IsolatedStorageSettings(
                      HasActivationContext() ?
                        IsolatedStorageFile.GetUserStoreForApplication() : //for WPF, apps deployed via ClickOnce will have a non-null ActivationContext
                        IsolatedStorageFile.GetUserStoreForAssembly());
                }
                return application_settings;
            }
        }

        // per domain, per-computer, per-user
        public static IsolatedStorageSettings SiteSettings
        {
            get
            {
                if (site_settings == null)
                {
                    site_settings = new IsolatedStorageSettings(
                      HasActivationContext() ?
                        IsolatedStorageFile.GetUserStoreForApplication() : //for WPF, apps deployed via ClickOnce will have a non-null ActivationContext
                        IsolatedStorageFile.GetUserStoreForAssembly());
                    //IsolatedStorageFile.GetUserStoreForSite() works only for Silverlight applications
                }
                return site_settings;
            }
        }

        // properties

        public int Count
        {
            get { return settings.Count; }
        }

        public ICollection Keys
        {
            get { return settings.Keys; }
        }

        public ICollection Values
        {
            get { return settings.Values; }
        }

        public object this[string key]
        {
            get
            {
                return settings[key];
            }
            set
            {
                settings[key] = value;
            }
        }

        // methods

        public void Add(string key, object value)
        {
            settings.Add(key, value);
        }

        // This method is emitted as virtual due to: https://bugzilla.novell.com/show_bug.cgi?id=446507
        public void Clear()
        {
            settings.Clear();
        }

        public bool Contains(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return settings.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return settings.Remove(key);
        }

        public void Save()
        {
#if NET20
            using (var fs = IsolatedStorageFileHelper.CreateFile(container, LocalSettings))
#else
            using (var fs = container.CreateFile(LocalSettings))
#endif
            {
                if (CompatibleToSilverlight)
                {
#if NET20
                    var xmlSerializer = new XmlSerializer(settings.GetType());
                    xmlSerializer.Serialize(fs, settings);
#else
                    var xmlSerializer = new DataContractSerializer(settings.GetType());
                    xmlSerializer.WriteObject(fs, settings);
#endif
                }
                else
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, this.settings);
                }
                fs.Flush();
            }
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            value = default(T);
            try
            {
                if (settings.TryGetValue(key, out var v))
                {
                    value = (T)v;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetValue(string key, Type type, out object value)
        {
            value = null;
#if NET20
            var mis = typeof(IsolatedStorageSettings).GetMethods();
            foreach (var item in mis)
            {
                if (item.Name == nameof(TryGetValue) && item.IsGenericMethod)
                {
                    var mi = item.MakeGenericMethod(type);
                    var @params = new object[] { key, null };
                    if (mi.Invoke(this, @params) is bool hasKey && hasKey)
                    {
                        value = @params[1];
                        return true;
                    }
                    return false;
                }
            }
            return false;
#else
            var mi = typeof(IsolatedStorageSettings).GetMethods()
                .Where(x => x.Name == nameof(TryGetValue) && x.IsGenericMethod)
                .FirstOrDefault();
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

        // explicit interface implementations

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return settings.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            settings.Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            settings.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return settings.ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            (settings as ICollection<KeyValuePair<string, object>>).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return settings.Remove(item.Key);
        }


        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return settings.Keys; }
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return settings.Values; }
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return settings.ContainsKey(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return settings.TryGetValue(key, out value);
        }


        private string ExtractKey(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return (key as string);
        }

        void IDictionary.Add(object key, object value)
        {
            string s = ExtractKey(key);
            if (s == null)
                throw new ArgumentException("key");

            settings.Add(s, value);
        }

        void IDictionary.Clear()
        {
            settings.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            string skey = ExtractKey(key);
            if (skey == null)
                return false;
            return settings.ContainsKey(skey);
        }

        object IDictionary.this[object key]
        {
            get
            {
                string s = ExtractKey(key);
                return (s == null) ? null : settings[s];
            }
            set
            {
                string s = ExtractKey(key);
                if (s == null)
                    throw new ArgumentException("key");
                settings[s] = value;
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
            string s = ExtractKey(key);
            if (s != null)
                settings.Remove(s);
        }


        void ICollection.CopyTo(Array array, int index)
        {
            (settings as ICollection).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized
        {
            get { return (settings as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (settings as ICollection).SyncRoot; }
        }


        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return settings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return settings.GetEnumerator();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return settings.GetEnumerator();
        }

        private static bool HasActivationContext()
        {
            // Thread.GetDomain().ActivationContext
            var domain = Thread.GetDomain();
            var pi = domain.GetType().GetProperty("ActivationContext");
            var value = SystemUtils.Try(() => pi?.GetValue(domain, null), null);
            return value != null;
        }

    }

}
