using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Library.Editor
{
    public abstract class LibraryEditorInterface<TLibrary, TValue> : UnityEditor.Editor where TLibrary : Library<TValue>
    {
        private int selectedIndex;
        private SerializedObject serializedReference;
        private TLibrary targetLibrary;

        private void OnEnable()
        {
            serializedReference = serializedObject;
            targetLibrary = (TLibrary)serializedReference.targetObject;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Type selectedEnum = ShowEnumSelection();
            ShowUiFor(selectedEnum);
            serializedObject.ApplyModifiedProperties();
        }

        private void ShowUiFor(Type selectedEnum)
        {
            foreach (Enum enumValue in Enum.GetValues(selectedEnum))
            {
                TValue storedValue = targetLibrary.Get(enumValue);
                //TODO: Try to use SerializedProperty instead ?
                TValue newValue = DisplayGuiAndSelect(enumValue.ToString(), storedValue);
                if (AreDifferent(newValue, storedValue))
                {
                    EditorUtility.SetDirty(targetLibrary);
                    targetLibrary.Set(enumValue, newValue);
                }
            }
        }

        private Type ShowEnumSelection()
        {
            List<Type> authorizedEnums = targetLibrary.AuthorizedEnums().ToList();
            selectedIndex = EditorGUILayout.Popup(
                "Enum:",
                selectedIndex,
                authorizedEnums.Select(type => type.Name).ToArray()
            );
            return authorizedEnums[selectedIndex];
        }

        //Used to determine if two stored types are different, since the type can be anything, it is delegated to the concrete class.
        protected abstract bool AreDifferent(TValue a, TValue b);

        //We need to delegate the display/picking because it varies depending on the type (Color, Sprite etc don't call the same editor function)
        protected abstract TValue DisplayGuiAndSelect(string label, TValue currentValue);
    }
}
