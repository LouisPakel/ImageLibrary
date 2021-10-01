using UnityEditor;
using UnityEngine;


/// <summary>
/// Description : Image Library Editor
/// @author : Louis PAKEL
/// </summary>
namespace ArchNet.Library.Image
{
    [CustomEditor(typeof(ImageLibrary))]
    public class ImageLibraryEditor : UnityEditor.Editor
    {
        GUIStyle _warningInfos = null;
        ImageLibrary manager;

        int _lastDefaultValue;
        string[] _enumValues;

        #region Serialized Properties

        SerializedProperty _keyValueList = null;
        SerializedProperty _imageLibrary = null;
        SerializedProperty _enumPath = null;
        SerializedProperty _showImages = null;
        SerializedProperty _imageSize = null;
        SerializedProperty _keyType = null;
        SerializedProperty _applyFilter = null;
        SerializedProperty _filter = null;
        SerializedProperty _containOrReject = null;
        SerializedProperty _expandedSettings = null;
        SerializedProperty _forceDefaultValue = null;
        SerializedProperty _defaultValue = null;

        #endregion

        private void OnEnable()
        {
            if (manager is null)
            {
                manager = target as ImageLibrary;
            }

            this.SetWarningGUIStyle();

            if (false == manager.IsLibraryUpToDate())
            {
                manager.UpdateLibrary();
            }
        }

        public override void OnInspectorGUI()
        {
            if (false == manager.IsLibraryUpToDate())
            {
                manager.UpdateLibrary();
            }

            this.UpdateSerializedProperties();

            this.HandleAtlasField();

            EditorGUILayout.Space(10);

            this.HandleKeyTypeField();

            EditorGUILayout.Space(10);

            this.HandleEnumPathField();

            EditorGUILayout.Space(10);

            this.HandleSettingsFields();

            EditorGUILayout.Space(10);

            this.HandleImageLibraryDisplay();

            serializedObject.ApplyModifiedProperties();
        }

        #region Private Methods

        private void HandleImageLibraryDisplay()
        {
            EditorGUILayout.LabelField("Image Library", EditorStyles.boldLabel);

            if (true == IsConditionsOK())
            {
                this.HandleDatasDisplay();
                this.SaveSpriteListIfNecessary();
            }
            else
            {
                EditorGUILayout.LabelField("Missing parameters!", _warningInfos);
                if (_keyValueList.arraySize > 0)
                {
                    _keyValueList.ClearArray();
                }
            }
        }

        private void SaveSpriteListIfNecessary()
        {
            if (_keyValueList.serializedObject.hasModifiedProperties)
            {
                _keyValueList.serializedObject.ApplyModifiedProperties();
                manager.SaveDictionnary();
            }
        }

        private void HandleDatasDisplay()
        {
            Texture lSpriteTexture = null;
            SerializedProperty lSpriteData;
            SerializedProperty lSprite;
            SerializedProperty lKey;

            for (int i = 0; i < _keyValueList.arraySize; i++)
            {
                lSpriteData = _keyValueList.GetArrayElementAtIndex(i);

                lSprite = lSpriteData.FindPropertyRelative("spriteName");
                lKey = lSpriteData.FindPropertyRelative("key");

                if (true == _applyFilter.boolValue)
                {
                    if (_containOrReject.boolValue == lSprite.stringValue.ToLower().Contains(_filter.stringValue.ToLower()))
                    {
                        continue;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if (!EditorApplication.isPlaying && _showImages != null && _showImages.boolValue)
                {
                    lSpriteTexture = manager.GetSpriteTextureByIndexInEditMode(i);

                    if (lSpriteTexture != null)
                    {
                        GUILayout.Box(lSpriteTexture, GUILayout.Height(_imageSize.intValue), GUILayout.Width(_imageSize.intValue));
                    }
                }
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField(lSprite.stringValue);
                EditorGUI.EndDisabledGroup();
                switch (_keyType.intValue)
                {
                    // NONE
                    case 0:
                        break;

                    // ENUM
                    case 1:
                        lKey.intValue = EditorGUILayout.Popup(lKey.intValue, _enumValues, GUILayout.MaxWidth(200));
                        break;

                    // INT
                    case 2:
                        EditorGUILayout.LabelField("=>", GUILayout.MaxWidth(20));
                        EditorGUILayout.PropertyField(lKey, new GUIContent(""), GUILayout.MaxWidth(200));
                        break;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
        }

        private bool IsConditionsOK()
        {
            if (_imageLibrary.objectReferenceValue == null)
            {
                return false;
            }

            if (_keyType.intValue == 0)
            {
                return false;
            }
            else if (_keyType.intValue == 1)
            {
                if (true == string.IsNullOrEmpty(_enumPath.stringValue))
                {
                    return false;
                }

                _enumValues = manager.GetEnumValues(_enumPath.stringValue);
                if (_enumValues == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleSettingsFields()
        {
            _expandedSettings.isExpanded = EditorGUILayout.Foldout(_expandedSettings.isExpanded, "Settings");
            if (_expandedSettings.isExpanded)
            {
                this.HandleShowImagesSettings();
                this.HandleFilterContentSettings();
                this.HandleForceDefaultValueSettings();
            }
        }

        private void HandleForceDefaultValueSettings()
        {
            _forceDefaultValue.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Force Default Value"), _forceDefaultValue.boolValue);

            _lastDefaultValue = _defaultValue.intValue;
            if (_forceDefaultValue.boolValue)
            {
                // Enum
                if (_keyType.intValue == 1)
                {
                    if (_enumValues != null && _enumValues.Length > 0)
                    {
                        _defaultValue.intValue = EditorGUILayout.Popup(_defaultValue.intValue, _enumValues);
                        if (_defaultValue.serializedObject.hasModifiedProperties)
                        {
                            _defaultValue.serializedObject.ApplyModifiedProperties();
                            this.SetSpriteListDefaultValue();
                            manager.SaveDictionnary();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No enum values found", _warningInfos);
                    }
                }
                // Int
                else if (_keyType.intValue == 2)
                {
                    EditorGUILayout.PropertyField(_defaultValue, new GUIContent(""));
                    if (_defaultValue.serializedObject.hasModifiedProperties)
                    {
                        _defaultValue.serializedObject.ApplyModifiedProperties();
                        this.SetSpriteListDefaultValue();
                        manager.SaveDictionnary();
                    }
                }
            }
            else if (_forceDefaultValue.serializedObject.hasModifiedProperties)
            {
                _defaultValue.intValue = 0;
                _defaultValue.serializedObject.ApplyModifiedProperties();
                if (_keyValueList.arraySize > 0)
                {
                    this.SetSpriteListDefaultValue();
                    manager.SaveDictionnary();
                }
            }
        }

        private void SetSpriteListDefaultValue()
        {
            SerializedProperty spriteData;
            SerializedProperty actionKey;

            for (int i = 0; i < _keyValueList.arraySize; i++)
            {
                spriteData = _keyValueList.GetArrayElementAtIndex(i);
                actionKey = spriteData.FindPropertyRelative("key");

                if (actionKey.intValue == _lastDefaultValue)
                {
                    actionKey.intValue = _defaultValue.intValue;
                    actionKey.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void HandleFilterContentSettings()
        {
            _applyFilter.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Filter Content"), _applyFilter.boolValue);
            _applyFilter.serializedObject.ApplyModifiedProperties();
            if (_applyFilter.boolValue)
            {
                if (GUILayout.Button(_containOrReject.boolValue ? "Reject" : "Contain", GUILayout.MaxWidth(200)))
                {
                    _containOrReject.boolValue = !_containOrReject.boolValue;
                    _containOrReject.serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.PropertyField(_filter, new GUIContent(""));
                if (_filter.serializedObject.hasModifiedProperties)
                {
                    _filter.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void HandleShowImagesSettings()
        {
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.ToggleLeft(new GUIContent("Show Images"), false);
                EditorGUILayout.LabelField("In PlayMode, sprites display is disabled due to Atlas Sprites", _warningInfos);
            }
            else
            {
                _showImages.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("Show Images"), _showImages.boolValue);
                _showImages.serializedObject.ApplyModifiedProperties();
            }

            if (_showImages.boolValue)
            {
                _imageSize.intValue = EditorGUILayout.IntSlider(_imageSize.intValue, 30, 200);
                _imageSize.serializedObject.ApplyModifiedProperties();
            }
            else if (_showImages.serializedObject.hasModifiedProperties)
            {
                _imageSize.intValue = 50;
                _imageSize.serializedObject.ApplyModifiedProperties();
            }
        }

        private void HandleEnumPathField()
        {
            if (_keyType.intValue == 1)
            {
                EditorGUILayout.LabelField("Full Namespace Enum Path", EditorStyles.boldLabel);

                if (_enumPath.serializedObject.hasModifiedProperties)
                {
                    _enumPath.serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.PropertyField(_enumPath, new GUIContent(""));

                EditorGUILayout.Space(10);
            }
        }

        private void HandleKeyTypeField()
        {
            EditorGUILayout.LabelField("Key Type", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_keyType, new GUIContent(""));

            if (true == this.DoKeyTypeChanged())
            {
                if (EditorUtility.DisplayDialog("Warning", "This will clear set keys.\nDo you want to continue?", "Yes", "No"))
                {
                    ResetListKeysToDefaultValue();
                }
                else
                {
                    // Rollback serialized property to actual key Type
                    _keyType.intValue = manager.GetActualKeyType();
                    _keyType.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void ResetListKeysToDefaultValue()
        {
            SerializedProperty lKeyValueData;
            SerializedProperty lKeyData;
            for (int i = 0; i < _keyValueList.arraySize; i++)
            {
                lKeyValueData = _keyValueList.GetArrayElementAtIndex(i);
                lKeyData = lKeyValueData.FindPropertyRelative("key");
                lKeyData.intValue = _defaultValue.intValue;
                lKeyData.serializedObject.ApplyModifiedProperties();
            }
        }

        private void HandleAtlasField()
        {
            EditorGUILayout.LabelField("Atlas Sprite", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_imageLibrary, new GUIContent(""));

            if (true == this.DoAtlasSpriteChanged())
            {
                if (EditorUtility.DisplayDialog("Warning", "That will clean the actual list.\nDo you want to continue?", "Yes", "No"))
                {
                    _keyValueList.ClearArray();
                    _keyValueList.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    // Rollback serialized property to actual SpriteAtlas
                    _imageLibrary.objectReferenceValue = manager.GetActualAtlasSprite();
                    _imageLibrary.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void SetWarningGUIStyle()
        {
            _warningInfos = new GUIStyle();
            _warningInfos.normal.textColor = Color.red;
            _warningInfos.fontStyle = FontStyle.Bold;
        }

        private void UpdateSerializedProperties()
        {
            serializedObject.Update();

            _keyValueList = serializedObject.FindProperty("_keyValueList");
            _imageLibrary = serializedObject.FindProperty("_imageLibrary");
            _enumPath = serializedObject.FindProperty("_enumPath");
            _showImages = serializedObject.FindProperty("_showImages");
            _imageSize = serializedObject.FindProperty("_imageSize");
            _keyType = serializedObject.FindProperty("_keyType");
            _applyFilter = serializedObject.FindProperty("_applyFilter");
            _filter = serializedObject.FindProperty("_filter");
            _expandedSettings = serializedObject.FindProperty("_expandedSettings");
            _forceDefaultValue = serializedObject.FindProperty("_forceDefaultValue");
            _defaultValue = serializedObject.FindProperty("_defaultValue");
            _containOrReject = serializedObject.FindProperty("_containOrReject");
        }

        private bool DoAtlasSpriteChanged()
        {
            if (manager.GetActualAtlasSprite() != null && false == manager.IsAtlasUpToDate(_imageLibrary.objectReferenceValue))
            {
                return true;
            }

            return false;
        }

        private bool DoKeyTypeChanged()
        {
            if (manager.GetActualKeyType() > 0 && false == manager.isKeyTypeUpToDate(_keyType.intValue))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
