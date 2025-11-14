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
                return true;
            }

            return owner == newOwner;
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
    }
}