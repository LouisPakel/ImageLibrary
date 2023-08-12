using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Description : Image Library is a link between sprite and enum or integer
/// Avoid mass refacto on a change  on a sprite
/// Works only with Atlas Sprite pack
/// @author : Louis PAKEL
/// </summary>
namespace ArchNet.Library.Image
{
    // Dictionnary can't be serialized
    // We use this class as Dictionnary copy
    [System.Serializable]
    public class ImageData
    {
        public ImageData(int pKey, string pSpriteName)
        {
            key = pKey;
            spriteName = pSpriteName;
        }
        public int key;
        public string spriteName;
    }

    public class SpriteNameComparer : IComparer<Sprite>
    {
        public int Compare(Sprite x, Sprite y)
        {
            return string.Compare(x.name, y.name);
        }
    }

    [CreateAssetMenu(fileName = "NewImageLibrary", menuName = "ArchNet/ImageLibrary")]
    public class ImageLibrary : ScriptableObject
    {
        public enum KeyType
        {
            NONE,
            ENUM,
            INT
        }

        #region SerializeField

        // Our image library we'd like to link with our enum
        [SerializeField]
        private SpriteAtlas _imageLibrary;

        // The Full namespace enum path
        [SerializeField]
        private string _enumPath;

        // Our buffer list to fill the Dictionnary
        [SerializeField]
        private List<ImageData> _keyValueList = new List<ImageData>();

        [SerializeField]
        bool _expandedSettings = true;

        [SerializeField]
        bool _forceDefaultValue = false;

        [SerializeField]
        private int _defaultValue = 0;

        [SerializeField]
        bool _showImages = false;

        [SerializeField]
        bool _applyFilter = false;

        [SerializeField]
        private string _filter;

        [SerializeField]
        private bool _containOrReject;

        [SerializeField]
        int _imageSize = 50;

        [SerializeField]
        KeyType _keyType = KeyType.NONE;

        #endregion

        #region Private Properties

        // Our final Dictionnary
        private Dictionary<int, string> _spritesDict;
        private string[] _enumValues;
        private Type _enumType;

        #endregion

        #region Public Methods

        public Sprite GetSprite(int enumValue)
        {
            if (CheckExistingSprite(enumValue))
            {
                return _imageLibrary.GetSprite(_spritesDict[enumValue]);
            }
            return null;
        }

        public Sprite GetSprite(Enum enumValue)
        {
            Type actualEnumType = GetEnumType(_enumPath);

            if (this._keyType != KeyType.ENUM)
            {
                Debug.LogWarning("[" + this.name + "] It seems that you're trying to get a sprite with an enum within a library not set for enum key");
            }
            else if (enumValue.GetType() != actualEnumType)
            {
                Debug.LogWarning("[" + this.name + "] It seems that you're trying to get a sprite with a different enum that the one defined to be the key in the library");
            }

            int value = Convert.ToInt32(enumValue);
            if (CheckExistingSprite(value))
            {
                return _imageLibrary.GetSprite(_spritesDict[value]);
            }

            return null;
        }

        public bool IsLibraryUpToDate()
        {
            string str = "CHECK FOR LIBRARY UPDATE\n";

            ImageData lSpriteNameFromLibrary = null;
            Sprite lSpriteFromAtlas = null;

            if (false == this.IsPrimaryParametersError())
            {
                List<string> lLibrarySpritesList = RetrieveLibrarySprites();
                List<Sprite> lAtlasSpritesList = RetrieveAtlasSprites();

                for (int i = 0; i < lAtlasSpritesList.Count; i++)
                {
                    lSpriteFromAtlas = lAtlasSpritesList[i];
                    lSpriteFromAtlas.name = lSpriteFromAtlas.name.Replace("(Clone)", "");
                    lSpriteNameFromLibrary = this.TryToGetSpriteFromLibrary(lSpriteFromAtlas.name, _keyValueList);

                    // Sprite is not in the library
                    if (lSpriteNameFromLibrary == default(ImageData))
                    {
                        str += "\nLibrary need update => Sprite is not in the library => " + lSpriteFromAtlas.name;
                        //Debug.Log(str);
                        return false;
                    }
                }
            }
            else
            {
                str += "\nABORT => Parameters aren't all sets";
            }

            str += "\nNo update needed";
            //Debug.Log(str);
            return true;
        }

        public void UpdateLibrary()
        {
            string str = "UPDATING LIBRARY\n";

            ImageData lSpriteNameFromLibrary = null;
            Sprite lSpriteFromAtlas = null;
            List<ImageData> lNewDataList = new List<ImageData>();

            if (false == this.IsPrimaryParametersError())
            {
                List<Sprite> lAtlasSpritesList = RetrieveAtlasSprites();

                str += "\nLibrary Contain " + _keyValueList.Count + "elements";
                str += "\nAtlas Contain " + lAtlasSpritesList.Count + "elements";
                str += "\n-------------------------------------------------------------";

                for (int i = 0; i < lAtlasSpritesList.Count; i++)
                {
                    lSpriteFromAtlas = lAtlasSpritesList[i];
                    lSpriteFromAtlas.name = lSpriteFromAtlas.name.Replace("(Clone)", "");
                    lSpriteNameFromLibrary = this.TryToGetSpriteFromLibrary(lSpriteFromAtlas.name, _keyValueList);

                    if (lSpriteNameFromLibrary != default(ImageData))
                    {
                        lNewDataList.Add(lSpriteNameFromLibrary);
                    }
                    else
                    {
                        string nameToUse = lSpriteFromAtlas.name.Replace("(Clone)", "");
                        lNewDataList.Add(new ImageData(this._defaultValue, nameToUse));
                    }
                    _keyValueList = lNewDataList;
                }
            }
            else
            {
                Debug.LogError("[" + this.name + "] It seems that you're trying to update a Library which parameters aren't properly sets");
                str += ("Abort => parameters aren't all sets");
            }

            //Debug.Log(str);
        }

        public bool IsEnumEqual(Type pEnumType)
        {
            if (pEnumType == this._enumType)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Private Methods

        private List<string> RetrieveLibrarySprites()
        {
            List<string> lLibrarySpritesList = new List<string>();
            for (int i = 0; i < _keyValueList.Count; i++)
            {
                ImageData lEntry = _keyValueList[i];
                lLibrarySpritesList.Add(lEntry.spriteName);
            }

            return lLibrarySpritesList;
        }

        private List<Sprite> RetrieveAtlasSprites()
        {
            Sprite[] lTmpAtlasSpritesArray;
            List<Sprite> lTmpAtlasSpritesList = new List<Sprite>();

            if (_imageLibrary != null)
            {
                lTmpAtlasSpritesArray = new Sprite[_imageLibrary.spriteCount];
                _imageLibrary.GetSprites(lTmpAtlasSpritesArray);
                lTmpAtlasSpritesList = new List<Sprite>(lTmpAtlasSpritesArray);
                this.SortSpritetAlphabetically(ref lTmpAtlasSpritesList);
            }

            return lTmpAtlasSpritesList;
        }

        private ImageData TryToGetSpriteFromLibrary(string pSpriteName, List<ImageData> pLibrary)
        {
            for (int i = 0; i < pLibrary.Count; i++)
            {
                if (pSpriteName == pLibrary[i].spriteName)
                {
                    return pLibrary[i];
                }
            }
            return default;
        }

        private bool IsPrimaryParametersError()
        {
            if (_imageLibrary is null)
            {
                return true;
            }
            if (_keyType == KeyType.NONE)
            {
                return true;
            }
            else if (_keyType == KeyType.ENUM && _enumType is null)
            {
                return true;
            }

            return false;
        }

        private bool CheckExistingSprite(int enumValue)
        {
            if (_spritesDict == null)
            {
                if (_keyValueList != null)
                {
                    this.SaveDictionnary();
                }

                if (_keyValueList.Count == 0)
                {
                    Debug.LogWarning("Image Library \'" + this.name + "\' doesn't contain anything");
                    return false;
                }
            }

            if (false == _spritesDict.ContainsKey(enumValue))
            {
                Debug.LogWarning("Library do not contain a sprite for value : " + enumValue);
                return false;
            }

            if (_spritesDict.ContainsKey(enumValue))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SortSpritetAlphabetically(ref List<Sprite> pSpriteList)
        {
            SpriteNameComparer snc = new SpriteNameComparer();
            pSpriteList.Sort(snc);
        }

        private Type GetEnumType(string enumName)
        {
            if (false == string.IsNullOrEmpty(enumName))
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(enumName);
                    if (type == null)
                        continue;
                    if (type.IsEnum)
                        return type;
                }
            }
            return null;
        }

        #endregion

        #region Editor Methods

        /// <summary>
        /// FOR CUSTOM EDITOR PURPOSE ONLY! DO NOT USE
        /// </summary>
        public Texture GetSpriteTextureByIndexInEditMode(int index)
        {
            if (_imageLibrary != null)
            {
                return _imageLibrary.GetSprite(_keyValueList[index].spriteName).texture;
            }
            return null;
        }

        /// <summary>
        /// FOR CUSTOM EDITOR PURPOSE ONLY! DO NOT USE
        /// </summary>
        public string[] GetEnumValues(string enumName)
        {
            Type type = GetEnumType(enumName);
            if (type != null)
            {
                this._enumType = type;
                this._enumPath = enumName;
                _enumValues = Enum.GetNames(type);
                return _enumValues;
            }

            return null;
        }

        /// <summary>
        /// FOR CUSTOM EDITOR PURPOSE ONLY! DO NOT USE
        /// </summary>
        public int GetActualKeyType()
        {
            return (int)this._keyType;
        }

        /// <summary>
        /// FOR CUSTOM EDITOR PURPOSE ONLY! DO NOT USE
        /// </summary>
        public bool isKeyTypeUpToDate(int pKeyValue)
        {
            if ((int)this._keyType == pKeyValue)
            { return true; }
            else
            { return false; }
        }

        /// <summary>
        /// FOR CUSTOM EDITOR PURPOSE ONLY! DO NOT USE
        /// </summary>
        public bool IsAtlasUpToDate(object atlas)
        {
            if (atlas == null && this._imageLibrary == null)
            {
                return true;
            }
            if (atlas is SpriteAtlas)
            {
                return (atlas as SpriteAtlas) == this._imageLibrary;
            }
            return false;
        }

        /// <summary>
        /// FOR CUSTOM EDITOR PURPOSE ONLY! DO NOT USE
        /// </summary>
        public UnityEngine.Object GetActualAtlasSprite()
        {
            return this._imageLibrary;
        }

        /// <summary>
        /// FOR CUSTOM EDITOR PURPOSE ONLY! DO NOT USE
        /// </summary>
        public void SaveDictionnary()
        {
            string str;
            if (_keyValueList == null)
            { return; }

            str = "SAVING DICTIONNARY";

            _spritesDict = new Dictionary<int, string>();
            for (int i = 0; i < _keyValueList.Count; i++)
            {
                if (_keyValueList[i].key != this._defaultValue)
                {
                    // Check that there's not two sprite for the same enum
                    if (_spritesDict.ContainsKey(_keyValueList[i].key))
                    {
                        if (_spritesDict[_keyValueList[i].key] != _keyValueList[i].spriteName)
                        {
                            if (this._keyType == KeyType.ENUM)
                            {
                                str += ("\nSprite \'" + _spritesDict[_keyValueList[i].key] + "\' is already defined for \'" + this._enumValues[_keyValueList[i].key] + "\'\nCannot define \'" + _keyValueList[i].spriteName + "\' as well.");
                            }
                            else
                            {
                                str += ("\nSprite \'" + _spritesDict[_keyValueList[i].key] + "\' is already defined for \'" + _keyValueList[i].key + "\'\nCannot define \'" + _keyValueList[i].spriteName + "\' as well.");
                            }
                        }
                    }
                    else
                    {
                        str += ("\nNew sprite added: " + _keyValueList[i].spriteName + " with key: " + _keyValueList[i].key);
                        _spritesDict.Add(_keyValueList[i].key, _keyValueList[i].spriteName);
                    }
                }
            }
            //Debug.Log(str);
        }

        #endregion
    }
}

