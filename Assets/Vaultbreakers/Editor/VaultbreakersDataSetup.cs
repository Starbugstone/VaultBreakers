using UnityEditor;
using UnityEngine;
using Vaultbreakers.Core;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Creates the balance data the POC tunes through. Unlike the rest of the generated assets this
    /// one is deliberately not overwritten on a rebuild: the values in it are playtest results, and
    /// regenerating the foundation must never silently throw them away.
    /// </summary>
    internal static class VaultbreakersDataSetup
    {
        public static PrototypeBalance EnsureBalanceAsset()
        {
            var balance = AssetDatabase.LoadAssetAtPath<PrototypeBalance>(VaultbreakersSetupPaths.BalancePath);
            if (balance == null)
            {
                balance = ScriptableObject.CreateInstance<PrototypeBalance>();
                balance.name = "PrototypeBalance";
                AssetDatabase.CreateAsset(balance, VaultbreakersSetupPaths.BalancePath);
            }

            // Re-serialise even when the asset already existed. Values already on disk are preserved
            // exactly, but a field added by a later phase is absent from the file until something
            // writes it, and an absent field is one nobody can find or tune in the Inspector. This
            // is the one thing the setup tool does to this asset; it never changes a value.
            EditorUtility.SetDirty(balance);
            return balance;
        }
    }
}
