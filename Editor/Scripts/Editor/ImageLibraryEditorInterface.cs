using UnityEditor;
using UnityEngine;

namespace Library.Editor
{
    [CustomEditor(typeof(ImageLibrary))]
    public class ImageLibraryEditorInterface : LibraryEditorInterface<ImageLibrary, Sprite>
    {
        protected override bool AreDifferent(Sprite a, Sprite b) => a != b;

        protected override Sprite DisplayGuiAndSelect(string label, Sprite currentValue)
        {
            return EditorGUILayout.ObjectField(
                label,
                currentValue,
                typeof(Sprite),
                false
            ) as Sprite;
        }
    }
}
