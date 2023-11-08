using System;
using System.Collections.Generic;
using UnityEngine;

namespace Library
{
    /// <summary>
    /// ImageLibrary is a library that stores common Sprites 
    /// </summary>
    [CreateAssetMenu(menuName = "Catalyst/Libraries/ImageLibrary", fileName = nameof(ImageLibrary) + ".asset")]
    public class ImageLibrary : Library<Sprite>
    {
        protected override Sprite DefaultValue() => null;
        public override ISet<Type> AuthorizedEnums()
        {
            return new HashSet<Type>
            {
                typeof(Illustration)
            };
        }
    }
}
