// InteractionLocker.cs
// Простий глобальний лоч для взаємодій — дозволяє тримати "власника" UI-попапа.

public static class InteractionLocker
{
    private static object owner = null;

    public static object CurrentOwner => owner;
    public static bool IsLocked => owner != null;

    // Повертає true якщо вдалось захопити (або якщо newOwner вже є власником)
    public static bool Claim(object newOwner)
    {
        if (newOwner == null) return false;
        if (owner == null)
        {
            owner = newOwner;
            return true;
        }
        return owner == newOwner;
    }

    // Звільнити лок — робить це тільки якщо releasingOwner є поточним власником
    public static void Release(object releasingOwner)
    {
        if (releasingOwner == null) return;
        if (owner == releasingOwner)
            owner = null;
    }

    public static bool IsOwner(object candidate)
    {
        return owner == candidate;
    }
}
