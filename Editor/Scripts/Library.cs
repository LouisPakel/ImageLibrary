using System;
using System.Collections.Generic;
using UnityEngine;

namespace Library
{
    [Serializable]
    public abstract class Library<TValue> : ScriptableObject
    {
        [Serializable]
        public class StoredItem
        {
            public int key; //ex: (int)Illustration.About 
            public TValue value; //ex: ref to UnitSplashLogoSprite.png
            public string enumName; //ex: Common.Scripts.Illustration

            public StoredItem(string enumName, int key, TValue value)
            {
                this.key = key;
                this.value = value;
                this.enumName = enumName;
            }

            public bool Matches(string enumFullName, int keyIntValue) => enumName == enumFullName && key == keyIntValue;
        }

        [SerializeField] private List<StoredItem> database = new List<StoredItem>();
        protected abstract TValue DefaultValue();

        public abstract ISet<Type> AuthorizedEnums();

        public TValue Get<TEnum>(TEnum key) where TEnum : Enum
        {
            return Search(key, out StoredItem found)
                ? found.value
                : DefaultValue();
        }

        ///<summary> Saves a value for a key. Somewhat equivalent to anyDictionary[key] = value;</summary>
        /// <remarks>
        /// Will raise ArgumentOutOfRangeException if typeof(key) is not in the list of AuthorizedEnums()
        /// </remarks>
        public void Set<TEnum>(TEnum key, TValue value) where TEnum : Enum
        {
            Type enumType = key.GetType(); // ex: -> typeof(Theme)
            int intVal = Convert.ToInt32(key);
            if (!AuthorizedEnums().Contains(enumType))
                throw new ArgumentOutOfRangeException(
                    $"[{nameof(Library<TValue>)}] : Cannot store key of type {enumType} because it's not in the AuthorizedEnums()"
                );
            
            if (Search(key, out StoredItem item))
                database.Remove(item);
            
            StoredItem storedItem = new StoredItem(enumType.FullName, intVal, value);
            database.Add(storedItem);
        }

        private bool Search<TEnum>(TEnum key, out StoredItem found) where TEnum : Enum
        {
            found = database.Find(s => s.Matches(key.GetType().FullName, Convert.ToInt32(key)));
            return found != null;
        }
    }
}
