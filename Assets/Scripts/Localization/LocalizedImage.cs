using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Localizable image component that works with both UI Image and SpriteRenderer
    /// Stores sprite variants for each language and automatically updates on language change
    /// </summary>
    public class LocalizedImage : LocalizedComponentBase
    {
        [Header("Localized Sprite Variants")]
        [Tooltip("Sprite to display when English is selected")]
        public Sprite englishSprite;

        [Tooltip("Sprite to display when Ukrainian is selected")]
        public Sprite ukrainianSprite;

        [Header("Component Settings")]
        [Tooltip("Use UI Image instead of SpriteRenderer")]
        public bool useUIImage = true;

        private Image imageComponent;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (useUIImage)
            {
                imageComponent = GetComponent<Image>();
            }
            else
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Apply the localized sprite based on the selected language
        /// </summary>
        protected override void ApplyLocalization(Language newLanguage)
        {
            Sprite localizedSprite = GetLocalizedSprite(newLanguage);

            if (localizedSprite == null)
            {
                return;
            }

            if (imageComponent != null)
            {
                imageComponent.sprite = localizedSprite;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = localizedSprite;
            }
        }

        /// <summary>
        /// Gets the appropriate sprite for the given language
        /// </summary>
        private Sprite GetLocalizedSprite(Language language)
        {
            switch (language)
            {
                case Language.Ukrainian:
                    return ukrainianSprite != null ? ukrainianSprite : englishSprite;
                case Language.English:
                default:
                    return englishSprite;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (GetComponent<Image>() != null)
            {
                useUIImage = true;
            }
            else if (GetComponent<SpriteRenderer>() != null)
            {
                useUIImage = false;
            }
        }
#endif
    }
}
