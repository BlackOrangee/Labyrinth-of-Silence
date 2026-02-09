using UnityEngine;

namespace Assets.Scripts
{
    public static class InteractionLocker
    {
        private static object owner = null;

        public static object CurrentOwner => owner;
        public static bool IsLocked => owner != null;

        public static bool Claim(object newOwner)
        {
            if (newOwner == null)
            {
                return false;
            }

            if (owner == null)
            {
                owner = newOwner;
                Debug.Log($"[InteractionLocker] Claimed by: {newOwner.GetType().Name}");
                return true;
            }

            if (owner == newOwner)
            {
                return true;
            }

            Debug.LogWarning($"[InteractionLocker] Claim failed - already owned by {owner.GetType().Name}, requester: {newOwner.GetType().Name}");
            return false;
        }

        public static void Release(object releasingOwner)
        {
            if (releasingOwner == null)
            {
                return;
            }

            if (owner == releasingOwner)
            {
                owner = null;
            }
        }

        public static bool IsOwner(object candidate)
        {
            return owner == candidate;
        }

        /// <summary>
        /// Force release the lock - use with caution! Only for cleanup/reset scenarios
        /// </summary>
        public static void ForceRelease()
        {
            if (owner != null)
            {
                Debug.LogWarning($"[InteractionLocker] Force released - was owned by: {owner.GetType().Name}");
                owner = null;
            }
        }

        /// <summary>
        /// Get debug info about current lock state
        /// </summary>
        public static string GetDebugInfo()
        {
            if (owner == null)
            {
                return "[InteractionLocker] Not locked";
            }
            return $"[InteractionLocker] Locked by: {owner.GetType().Name}";
        }
    }
}