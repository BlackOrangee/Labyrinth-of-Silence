using Assets.Scripts.Localization;

namespace Assets.Scripts.Localization
{
    /// <summary>
    /// Interface for all localizable components
    /// Components implementing this interface will automatically receive language change notifications
    /// </summary>
    public interface ILocalizable
    {
        /// <summary>
        /// Called when the game language changes
        /// </summary>
        /// <param name="newLanguage">The new language to switch to</param>
        void UpdateLocalization(Language newLanguage);
    }
}
