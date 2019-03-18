extern alias PortableIsolatedStorageSettings;

using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;

using IsolatedStorageSettings = PortableIsolatedStorageSettings::System.IO.IsolatedStorage.IsolatedStorageSettings;

namespace System.IO.IsolatedStorage.IsolatedStorageSettingsTests
{

    public class IsolatedStorageSettingsTest
    {

        private static class AssertEx
        {

            internal static void AreEqual<T>(T v1, T v2, string message)
            {
                Assert.True(object.Equals(v1, v2), message);
            }

            internal static void IsFalse(bool condition, string message)
            {
                Assert.False(condition, message);
            }

            internal static void IsNotNull(object @object, string message)
            {
                Assert.True(@object != null, message);
            }

            internal static void Throws(Action action, Type type, string message)
            {
                Assert.Throws(type, action ?? throw new ArgumentNullException(nameof(action)));
            }

            internal static void IsTrue(bool condition, string message)
            {
                Assert.True(condition, message);
            }

            internal static void IsNull(object @object, string message)
            {
                Assert.True(@object == null, message);
            }

        }

        static IsolatedStorageSettingsTest()
        {
            IsolatedStorageSettings.CompatibleToSilverlight = true;
        }

        private void CheckICollection(IsolatedStorageSettings settings)
        {
            var c = (settings as ICollection);
            AssertEx.AreEqual(0, c.Count, "ICollection.Count");
            AssertEx.IsFalse(c.IsSynchronized, "ICollection.IsSynchronized");
            AssertEx.IsNotNull(c.SyncRoot, "ICollection.SyncRoot");
            AssertEx.IsNotNull(c.GetEnumerator(), "ICollection.GetEnumerator");
        }

        private void CheckICollectionKeyPairValue(IsolatedStorageSettings settings)
        {
            var c = (settings as ICollection<KeyValuePair<string, object>>);
            AssertEx.AreEqual(0, c.Count, "Count");
            AssertEx.IsFalse(c.IsReadOnly, "IsReadOnly");
            AssertEx.IsNotNull(c.GetEnumerator(), "GetEnumerator");

            var kvp = new KeyValuePair<string, object>("key", "value");
            c.Add(kvp);
            AssertEx.AreEqual(1, c.Count, "Add/Count");
            AssertEx.Throws(() => c.Add(new KeyValuePair<string, object>(null, "value")),
                typeof(ArgumentNullException), "Add(KVP(null))");
            AssertEx.Throws(() => c.Add(new KeyValuePair<string, object>("key", "value")),
                typeof(ArgumentException), "Add(twice)");

            AssertEx.IsTrue(c.Contains(kvp), "Contains(kvp)");
            AssertEx.IsTrue(c.Contains(new KeyValuePair<string, object>("key", "value")), "Contains(new)");
            AssertEx.IsFalse(c.Contains(new KeyValuePair<string, object>("value", "key")), "Contains(bad)");

            c.Remove(kvp);
            AssertEx.IsFalse(c.Contains(kvp), "Remove/Contains(kvp)");
            AssertEx.AreEqual(0, c.Count, "Remove/Count");

            c.Add(kvp);
            c.Clear();
            AssertEx.AreEqual(0, c.Count, "Clear/Count");
        }

        private void CheckIDictionary(IsolatedStorageSettings settings)
        {
            var d = (settings as IDictionary);
            AssertEx.IsFalse(d.IsFixedSize, "Empty-IsFixedSize");
            AssertEx.IsFalse(d.IsReadOnly, "Empty-IsReadOnly");

            var key = new object();

            d.Add("key", "string");
            AssertEx.AreEqual(1, d.Count, "Add/Count");
            AssertEx.Throws(() => d.Add(key, "object"), typeof(ArgumentException), "Add(object)");
            AssertEx.Throws(() => d.Add(null, "null"), typeof(ArgumentNullException), "Add(null)");
            AssertEx.Throws(() => d.Add("key", "another string"), typeof(ArgumentException), "Add(twice)");

            d.Remove("value");
            AssertEx.AreEqual(1, d.Count, "Remove/Bad/Count");
            d.Remove("key");
            AssertEx.AreEqual(0, d.Count, "Remove/Count");
            d.Remove(key); // no exception
            AssertEx.Throws(() => d.Remove(null), typeof(ArgumentNullException), "Remove(null)");

            d.Add("key", null);
            AssertEx.AreEqual(1, d.Count, "Add2/Count");
            AssertEx.IsTrue(d.Contains("key"), "Contains(key)");
            AssertEx.IsFalse(d.Contains(key), "Contains(object)");
            AssertEx.Throws(() => _ = d.Contains(null), typeof(ArgumentNullException), "Contains(null)");

            AssertEx.IsNull(d["key"], "this[key]"); // since we added null :)
            AssertEx.IsNull(d[key], "this[object]");
            AssertEx.Throws(() => _ = d[null].ToString(), typeof(ArgumentNullException), "d[null] get");

            d["key"] = "value"; // replace
            AssertEx.AreEqual(1, d.Count, "Replace/Count");
            AssertEx.Throws(() => d[key] = key, typeof(ArgumentException), "d[key] set");
            AssertEx.Throws(() => d[null] = null, typeof(ArgumentNullException), "d[null] set");

            d.Clear();
            AssertEx.AreEqual(0, d.Count, "Clear/Count");
        }

        private void CheckIDictionaryStringObject(IsolatedStorageSettings settings)
        {
            var d = (settings as IDictionary<string, object>);
            AssertEx.AreEqual(0, d.Keys.Count, "Keys.Count");
            AssertEx.AreEqual(0, d.Values.Count, "Values.Count");
        }

        private void CheckSettings(IsolatedStorageSettings settings)
        {
            AssertEx.AreEqual(0, settings.Count, "Empty-Count");
            AssertEx.AreEqual(0, settings.Keys.Count, "Empty-Keys.Count");
            AssertEx.AreEqual(0, settings.Values.Count, "Empty-Values.Count");

            settings.Add("key", "value");
            AssertEx.Throws(() => settings.Add(null, "x"), typeof(ArgumentNullException), "Add(null,x)");
            AssertEx.Throws(() => settings.Add("key", "another string"), typeof(ArgumentException), "Add(twice)");

            AssertEx.AreEqual(1, settings.Count, "Count");
            AssertEx.AreEqual(1, settings.Keys.Count, "Keys.Count");
            AssertEx.AreEqual(1, settings.Values.Count, "Values.Count");
            AssertEx.AreEqual(1, (settings as ICollection).Count, "ICollection.Count");

            AssertEx.IsTrue(settings.Contains("key"), "Contains-key");
            AssertEx.IsFalse(settings.Contains("value"), "Contains-value");
            AssertEx.Throws(() => settings.Contains(null), typeof(ArgumentNullException), "Contains(null)");

            AssertEx.AreEqual("value", settings["key"], "this[key]");
            settings["key"] = null;
            AssertEx.IsNull(settings["key"], "this[key]-null");
            AssertEx.Throws(() => Console.WriteLine(settings["non-existing"]), typeof(KeyNotFoundException),
                "this[non-existing]");
            AssertEx.Throws(() => settings[null] = null, typeof(ArgumentNullException), "this[null] set");

            settings.Remove("key");
            AssertEx.AreEqual(0, settings.Count, "Remove/Count");
            AssertEx.IsFalse(settings.Remove("non-existing"), "Remove(non-existing)");
            AssertEx.Throws(() => settings.Remove(null), typeof(ArgumentNullException), "Remove(null)");

            settings.Add("key", "value");
            AssertEx.AreEqual(1, settings.Count, "Add2/Count");

            AssertEx.IsTrue(settings.TryGetValue("key", out string s), "TryGetValue(key)");
            AssertEx.AreEqual("value", s, "out value");
            AssertEx.IsTrue(settings.TryGetValue<object>("key", out var o), "TryGetValue(object)");
            AssertEx.AreEqual("value", s, "out value/object");
            AssertEx.IsFalse(settings.TryGetValue("value", out s), "TryGetValue(value)");
            AssertEx.Throws(() => settings.TryGetValue(null, out s), typeof(ArgumentNullException),
                "TryGetValue(null)");

            settings.Clear();
            AssertEx.AreEqual(0, settings.Count, "Clear/Count");
        }

        private void CheckAll(IsolatedStorageSettings settings)
        {
            settings.Clear();
            try
            {
                CheckSettings(settings);
                CheckICollection(settings);
                CheckICollectionKeyPairValue(settings);
                CheckIDictionary(settings);
                CheckIDictionaryStringObject(settings);
            }
            finally
            {
                settings.Clear();
            }
        }

        [Fact]
        public void ApplicationSettingsTest()
        {
            // Fails in Silverlight 3
            CheckAll(IsolatedStorageSettings.ApplicationSettings);
        }

        [Fact]
        public void SiteSettingsTest()
        {
            CheckAll(IsolatedStorageSettings.SiteSettings);
        }

        private void Format(IsolatedStorageSettings settings, IsolatedStorageFile isf)
        {
            settings.Clear();
            settings.Add("a", 1);
            settings.Save();

            Dictionary<string, object> dict = null;
            using (var fs = new IsolatedStorageFileStream("__LocalSettings", FileMode.Open, isf))
            {
                using (var sr = new StreamReader(fs))
                {
                    var reader = new DataContractSerializer(typeof(Dictionary<string, object>));
                    dict = (Dictionary<string, object>)reader.ReadObject(fs);
                }
            }

            AssertEx.AreEqual(1, dict.Count, "settings.Count");
            AssertEx.AreEqual(1, dict["a"], "settings.a");
            dict["b"] = 2;

            using (var fs = new IsolatedStorageFileStream("__LocalSettings", FileMode.Create, isf))
            {
                using (var sr = new StreamReader(fs))
                {
                    var writer = new DataContractSerializer(dict.GetType());
                    writer.WriteObject(fs, dict);
                }
            }

            // saved but not re-loaded
            AssertEx.AreEqual(1, settings.Count, "Count");
            settings.Clear();
        }

        [Fact]
        public void Format_Application()
        {
#if NETCOREAPP
            Format(IsolatedStorageSettings.ApplicationSettings, IsolatedStorageSettings.ApplicationSettings.Container);
#else
            Format(IsolatedStorageSettings.ApplicationSettings, IsolatedStorageFile.GetUserStoreForApplication());
#endif
        }

        [Fact]
        public void Format_Site()
        {
#if NETCOREAPP
            Format(IsolatedStorageSettings.SiteSettings, IsolatedStorageSettings.SiteSettings.Container);
#else
            Format(IsolatedStorageSettings.SiteSettings, IsolatedStorageFile.GetUserStoreForSite());
#endif
        }

    }

}