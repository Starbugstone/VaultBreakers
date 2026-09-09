using UnityEngine;
namespace Vaultbreakers.Dungeon
{
    public static class DungeonLayout
    {
        public const float ArtScale = 1.2f;
        public const float RoomSpacing = 28 * ArtScale;
        public const float AdvanceOffset = 20 * ArtScale;
        public static Vector3 CorePosition => new Vector3(0, 0, 63 * ArtScale);
    }
}
